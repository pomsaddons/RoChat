using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using BloxCord.Client.Models;
using BloxCord.Client.Services;
using BloxCord.Client.ViewModels;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Text.Json;

namespace BloxCord.Client;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private ChatClient? _chatClient;
    private readonly DiscordRpcService _discordRpc = new();
    private readonly Dictionary<string, ParticipantViewModel> _participantLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<long, string> _avatarUrlCache = new();
    private readonly DispatcherTimer _typingTimer;
    private bool _isTypingLocally;

    public MainWindow()
    {
        InitializeComponent();
        
        _discordRpc.Initialize();
        _discordRpc.SetStatus("Browsing Servers", "Idle");

        // Load config
        ConfigService.Load();
        _viewModel.BackendUrl = ConfigService.Current.BackendUrl;
        _viewModel.Username = ConfigService.Current.Username;
        
        // Apply theme
        if (ConfigService.Current.UseGradient)
        {
            try
            {
                var startColor = (Color)ColorConverter.ConvertFromString(ConfigService.Current.GradientStart);
                var endColor = (Color)ColorConverter.ConvertFromString(ConfigService.Current.GradientEnd);
                var gradient = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1)
                };
                gradient.GradientStops.Add(new GradientStop(startColor, 0));
                gradient.GradientStops.Add(new GradientStop(endColor, 1));
                MainGrid.Background = gradient;
            }
            catch { }
        }
        else
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(ConfigService.Current.SolidColor);
                MainGrid.Background = new SolidColorBrush(color);
            }
            catch { }
        }

        DataContext = _viewModel;
        Closed += async (_, _) => 
        {
            await DisposeClientAsync();
            _discordRpc.Dispose();
        };

        _typingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };

        _typingTimer.Tick += TypingTimer_Tick;

        _viewModel.Messages.CollectionChanged += (s, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                MessagesScrollViewer.ScrollToBottom();
            }
        };

        _ = FetchBannerAsync();
    }

    private async Task FetchBannerAsync()
    {
        try
        {
            using var client = new HttpClient();
            // Add cache buster
            var url = $"https://raw.githubusercontent.com/pompompur1nn/RoChatBanner/refs/heads/main/banners.json?t={DateTime.UtcNow.Ticks}";
            var json = await client.GetStringAsync(url);
            var banner = JsonSerializer.Deserialize<BannerDto>(json);

            if (banner != null && banner.Enabled)
            {
                _viewModel.Banner = new BannerViewModel(banner);
                _viewModel.IsBannerVisible = true;
            }
        }
        catch
        {
            // Ignore banner fetch errors
        }
    }

    public void EnableTestMode()
    {
        var random = new Random();
        var testId = random.Next(1000000, 9999999);
        _viewModel.Username = $"TestUser_{testId}";
        _viewModel.UserId = testId.ToString();
        _viewModel.SessionUserId = testId;
        _viewModel.IsTestMode = true;
        Title += " [TEST MODE]";
        _viewModel.JobId = "TEST_SERVER_JOB_ID";
    }

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.IsServerBrowserVisible = false;
        
        if (!_viewModel.IsTestMode)
        {
            // Auto-load session info first
            _viewModel.StatusMessage = "Reading Roblox logs...";
            try
            {
                var session = await RobloxLogParser.TryReadLatestAsync();
                if (session is not null)
                {
                    if (!string.IsNullOrWhiteSpace(session.Username))
                        _viewModel.Username = session.Username;

                    _viewModel.JobId = session.JobId;
                    _viewModel.UserId = session.UserId?.ToString() ?? string.Empty;
                    _viewModel.SessionUserId = session.UserId;
                    _viewModel.PlaceId = session.PlaceId;
                    _viewModel.StatusMessage = "Session info loaded from Roblox logs.";
                }
                else
                {
                    _viewModel.StatusMessage = "No active Roblox session detected.";
                    MessageBox.Show("Could not find an active Roblox session in the logs. Please ensure you are in a game.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            catch (Exception ex)
            {
                _viewModel.StatusMessage = $"Failed to read logs: {ex.Message}";
                MessageBox.Show($"Failed to read logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        else
        {
             _viewModel.StatusMessage = "Test Mode Active.";
        }

        if (string.IsNullOrWhiteSpace(_viewModel.BackendUrl) ||
            string.IsNullOrWhiteSpace(_viewModel.JobId) ||
            string.IsNullOrWhiteSpace(_viewModel.Username))
        {
            _viewModel.StatusMessage = "Backend URL, Job ID, and Username are required.";
            MessageBox.Show("Connection details are missing even after reading logs.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await DisposeClientAsync();
            _viewModel.StatusMessage = "Connecting to chat server...";

            _chatClient = new ChatClient(_viewModel.BackendUrl);
            _chatClient.MessageReceived += HandleMessageReceived;
            _chatClient.ParticipantsChanged += HandleParticipantsChanged;
            _chatClient.HistoryReceived += HandleHistoryReceived;
            _chatClient.TypingIndicatorReceived += HandleTypingIndicator;
            _chatClient.PrivateMessageReceived += HandlePrivateMessageReceived;

            _participantLookup.Clear();
            _viewModel.Participants.Clear();
            _viewModel.ResetMessages();

            var userId = ResolveUserId();
            
            if (userId == null && !string.IsNullOrEmpty(_viewModel.Username))
            {
                try 
                {
                    userId = await RobloxUsernameDirectory.TryResolveUserIdAsync(_viewModel.Username);
                    if (userId.HasValue)
                    {
                        _viewModel.SessionUserId = userId;
                    }
                }
                catch
                {
                    // Ignore resolution errors, proceed without ID (DMs might not work)
                }
            }

            await _chatClient.ConnectAsync(_viewModel.Username, _viewModel.JobId, userId, _viewModel.PlaceId);

            _viewModel.IsConnected = true;
            _viewModel.StatusMessage = "Connected!";
            
            _discordRpc.SetStatus("Chatting in Game", $"Playing as {_viewModel.Username}", startTime: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _viewModel.StatusMessage = $"Connect failed: {ex.Message}";
            MessageBox.Show($"Connect failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Disconnect_Click(object sender, RoutedEventArgs e)
    {
        await TryNotifyTypingAsync(false);
        await DisposeClientAsync();
        _participantLookup.Clear();
        _viewModel.Participants.Clear();
        _viewModel.ResetMessages();
        _viewModel.IsConnected = false;
        _viewModel.StatusMessage = "Disconnected.";
        _discordRpc.SetStatus("Browsing Servers", "Idle");
    }

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.OutgoingMessage))
            return;

        if (_chatClient is null)
        {
            _viewModel.StatusMessage = "Connect before sending messages.";
            return;
        }

        try
        {
            var message = _viewModel.OutgoingMessage.Trim();

            if (message.Length == 0)
                return;

            if (_viewModel.SelectedConversation != null)
            {
                if (_viewModel.SelectedConversation.IsDirectMessage)
                {
                    // Use the new "Game" based DM system (JobId = -TargetUserId)
                    if (long.TryParse(_viewModel.SelectedConversation.Id, out var toUserId))
                    {
                        // If the ID is already negative (from incoming), use it. If positive (from user selection), negate it.
                        // Actually, GetOrCreateDm sets ID to positive UserId usually.
                        // But we want to send to "-TargetUserId".
                        // Wait, if I send to -200, I am talking to User 200.
                        // So jobId should be "-200".
                        
                        var targetJobId = toUserId > 0 ? $"-{toUserId}" : toUserId.ToString();
                        await _chatClient.SendToChannelAsync(targetJobId, message);
                    }
                }
                else
                {
                    await _chatClient.SendAsync(message);
                }
            }
            else
            {
                await _chatClient.SendAsync(message);
            }

            _viewModel.OutgoingMessage = string.Empty;
            await TryNotifyTypingAsync(false);
            _typingTimer.Stop();
            _isTypingLocally = false;
        }
        catch (Exception ex)
        {
            _viewModel.StatusMessage = $"Send failed: {ex.Message}";
        }
    }

    private void MessageInput_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != System.Windows.Input.ModifierKeys.Shift)
        {
            e.Handled = true;
            Send_Click(sender, e);
        }
    }

    private async void HandleMessageReceived(object? sender, ChatMessageDto dto)
    {
        var resolution = await ResolveAvatarAsync(dto.UserId, dto.Username, dto.AvatarUrl);
        var imageUrl = await ExtractImageUrlAsync(dto.Content);

        await Dispatcher.InvokeAsync(() =>
        {
            // Check if this is a DM (Negative JobId)
            if (dto.JobId.StartsWith("-"))
            {
                // Parse the ID to find out who the conversation is with
                if (long.TryParse(dto.JobId.Substring(1), out var otherUserId))
                {
                    // If I sent it, the JobId is -TargetUserId.
                    // If I received it, the JobId is -SenderUserId.
                    // In both cases, the "otherUserId" derived from JobId is the person I am talking to.
                    // Wait.
                    // If I send to -200. Server echoes with JobId -200.
                    // I should see it in conversation with User 200.
                    // If User 200 sends to me (-100). Server sends to me with JobId -200.
                    // I should see it in conversation with User 200.
                    // So in ALL cases, the JobId tells me which conversation it belongs to.
                    // JobId "-200" -> Conversation with User 200.
                    
                    // We need the username. If it's a new conversation, we might not have it.
                    // If it's incoming from someone else, dto.Username is their username.
                    // If it's my own echo, dto.Username is my username.
                    
                    string conversationName = "Unknown";
                    if (dto.UserId != _viewModel.SessionUserId)
                    {
                        conversationName = dto.Username;
                    }
                    else
                    {
                        // It's me. I need to find the name of User 200.
                        // We might have it in cache or existing conversation.
                        var existing = _viewModel.Conversations.FirstOrDefault(c => c.Id == otherUserId.ToString());
                        if (existing != null) conversationName = existing.Title;
                    }

                    var conv = _viewModel.GetOrCreateDm(otherUserId, conversationName);
                    
                    conv.Messages.Add(new ClientChatMessage
                    {
                        JobId = dto.JobId,
                        Username = dto.Username,
                        Content = dto.Content,
                        Timestamp = dto.Timestamp.LocalDateTime,
                        UserId = dto.UserId,
                        AvatarUrl = resolution.AvatarUrl,
                        ImageUrl = imageUrl
                    });

                    if (_viewModel.SelectedConversation != conv)
                    {
                        conv.IsUnread = true;
                        ShowNotification($"DM from {dto.Username}", dto.Content);
                    }
                    else if (!IsActive)
                    {
                        ShowNotification($"DM from {dto.Username}", dto.Content);
                    }
                    return;
                }
            }

            // Find Server conversation
            var serverConv = _viewModel.Conversations.FirstOrDefault(c => !c.IsDirectMessage);
            if (serverConv != null)
            {
                serverConv.Messages.Add(new ClientChatMessage
                {
                    Content = dto.Content,
                    JobId = dto.JobId,
                    Username = dto.Username,
                    Timestamp = dto.Timestamp.LocalDateTime,
                    UserId = resolution.UserId,
                    AvatarUrl = resolution.AvatarUrl,
                    ImageUrl = imageUrl
                });

                if (_viewModel.SelectedConversation != serverConv)
                {
                    serverConv.IsUnread = true;
                    ShowNotification($"#{serverConv.Title}", $"{dto.Username}: {dto.Content}");
                }
                else if (!IsActive)
                {
                    ShowNotification($"#{serverConv.Title}", $"{dto.Username}: {dto.Content}");
                }
            }
        }, DispatcherPriority.Background);
    }

    private async void HandlePrivateMessageReceived(object? sender, PrivateMessageDto dto)
    {
        // Determine the other party
        // If I sent it, the other party is ToUserId. If I received it, the other party is FromUserId.
        long otherUserId;
        string otherUsername;
        
        // We need to know our own UserId. It's in _viewModel.SessionUserId
        var myUserId = _viewModel.SessionUserId;

        if (dto.FromUserId == myUserId)
        {
            otherUserId = dto.ToUserId;
            // If we are DMing ourselves, we know the username
            if (otherUserId == myUserId)
            {
                otherUsername = dto.FromUsername;
            }
            else
            {
                otherUsername = "Unknown"; // Placeholder, will be updated if conversation exists
            }
        }
        else
        {
            otherUserId = dto.FromUserId;
            otherUsername = dto.FromUsername;
        }

        var resolution = await ResolveAvatarAsync(dto.FromUserId, dto.FromUsername, null);
        var imageUrl = await ExtractImageUrlAsync(dto.Content);

        await Dispatcher.InvokeAsync(() =>
        {
            var conv = _viewModel.GetOrCreateDm(otherUserId, otherUsername);
            // If we just created it and didn't know the username (outgoing case), we might want to update title if possible
            // But for outgoing, we usually create conversation BEFORE sending.
            
            if (conv.Title == "Unknown" && !string.IsNullOrEmpty(otherUsername) && otherUsername != "Unknown")
            {
                conv.Title = otherUsername;
            }

            conv.Messages.Add(new ClientChatMessage
            {
                Content = dto.Content,
                Username = dto.FromUsername,
                Timestamp = dto.Timestamp.LocalDateTime,
                UserId = dto.FromUserId,
                AvatarUrl = resolution.AvatarUrl,
                IsSystemMessage = false,
                ImageUrl = imageUrl,
                JobId = "DM"
            });

            if (_viewModel.SelectedConversation != conv)
            {
                conv.IsUnread = true;
                ShowNotification($"DM from {dto.FromUsername}", dto.Content);
            }
            else if (!IsActive)
            {
                ShowNotification($"DM from {dto.FromUsername}", dto.Content);
            }
        });
    }

    private async Task<string?> ExtractImageUrlAsync(string content)
    {
        var match = Regex.Match(content, @"rbxassetid://(\d+)");
        if (!match.Success)
        {
            match = Regex.Match(content, @"roblox\.com/library/(\d+)");
        }

        if (match.Success && long.TryParse(match.Groups[1].Value, out var assetId))
        {
            return await RobloxAssetService.ResolveDecalAsync(assetId);
        }
        return null;
    }

    private void ShowNotification(string title, string content)
    {
        try
        {
            var builder = new ToastContentBuilder()
                .AddText(title)
                .AddText(content);

            var iconPath = System.IO.Path.GetFullPath("rochatlogo.png");
            if (System.IO.File.Exists(iconPath))
            {
                builder.AddAppLogoOverride(new Uri(iconPath), ToastGenericAppLogoCrop.Circle);
            }

            builder.Show();
        }
        catch { }
    }

    private async void HandleParticipantsChanged(object? sender, List<ChannelParticipantDto> participants)
    {
        try 
        {
            var ordered = participants
                .OrderBy(p => p.Username, StringComparer.OrdinalIgnoreCase)
                .ToList();

            await Dispatcher.InvokeAsync(() =>
            {
                var incomingSet = new HashSet<string>(ordered.Select(p => p.Username), StringComparer.OrdinalIgnoreCase);
                
                // Detect Left
                var toRemove = _participantLookup.Keys
                    .Where(existing => !incomingSet.Contains(existing))
                    .ToList();

                var serverConv = _viewModel.Conversations.FirstOrDefault(c => !c.IsDirectMessage);

                foreach (var username in toRemove)
                {
                    if (_participantLookup.TryGetValue(username, out var existingVm))
                    {
                        _participantLookup.Remove(username);
                        _viewModel.Participants.Remove(existingVm);
                        
                        // Add System Message
                        if (serverConv != null)
                        {
                            serverConv.Messages.Add(new ClientChatMessage
                            {
                                Username = "System",
                                Content = $"{username} left the chat",
                                Timestamp = DateTime.Now,
                                JobId = _viewModel.JobId,
                                IsSystemMessage = true
                            });
                        }
                    }
                }

                // Detect Joined
                foreach (var dto in ordered)
                {
                    if (!_participantLookup.TryGetValue(dto.Username, out var vm))
                    {
                        vm = new ParticipantViewModel
                        {
                            Username = dto.Username,
                            UserId = dto.UserId,
                            AvatarUrl = dto.AvatarUrl ?? string.Empty
                        };

                        _participantLookup[dto.Username] = vm;
                        InsertParticipantSorted(vm);
                        _ = EnsureAvatarForParticipantAsync(vm);

                        // Add System Message (only if we are already connected to avoid spam on initial load)
                        if (_viewModel.IsConnected && serverConv != null) 
                        {
                            serverConv.Messages.Add(new ClientChatMessage
                            {
                                Username = "System",
                                Content = $"{dto.Username} joined the chat",
                                Timestamp = DateTime.Now,
                                JobId = _viewModel.JobId,
                                IsSystemMessage = true,
                                AvatarUrl = dto.AvatarUrl ?? string.Empty
                            });
                        }
                    }
                    else
                    {
                        // Update existing participant details if needed
                        if (vm.UserId != dto.UserId || vm.AvatarUrl != dto.AvatarUrl)
                        {
                            vm.UserId = dto.UserId;
                            vm.AvatarUrl = dto.AvatarUrl ?? string.Empty;
                            _ = EnsureAvatarForParticipantAsync(vm);
                        }
                    }
                }
                
                _viewModel.ParticipantCount = _viewModel.Participants.Count;
                _viewModel.StatusMessage = $"Received {participants.Count} participants. Count: {_viewModel.ParticipantCount}";

            }, DispatcherPriority.Background);
        }
        catch (Exception ex)
        {
             Dispatcher.Invoke(() => 
             {
                _viewModel.StatusMessage = $"Error updating participants: {ex.Message}";
                MessageBox.Show($"Error updating participants: {ex.Message}");
             });
        }
    }

    private async void HandleHistoryReceived(object? sender, List<ChatMessageDto> history)
    {
        var serverConv = _viewModel.Conversations.FirstOrDefault(c => !c.IsDirectMessage);
        if (serverConv == null) return;

        var ordered = history
            .OrderBy(m => m.Timestamp)
            .ToList();

        var prepared = new List<ClientChatMessage>();

        foreach (var entry in ordered)
        {
            var resolution = await ResolveAvatarAsync(entry.UserId, entry.Username, entry.AvatarUrl);

            prepared.Add(new ClientChatMessage
            {
                Content = entry.Content,
                JobId = entry.JobId,
                Username = entry.Username,
                Timestamp = entry.Timestamp.LocalDateTime,
                UserId = resolution.UserId,
                AvatarUrl = resolution.AvatarUrl
            });
        }

        await Dispatcher.InvokeAsync(() =>
        {
            serverConv.Messages.Clear();

            foreach (var message in prepared)
                serverConv.Messages.Add(message);

            // Add Safety Warning
            serverConv.Messages.Add(new ClientChatMessage
            {
                Username = "System",
                Content = "This chat is unmoderated. Be careful sharing personal information and connecting outside of RoChat.",
                Timestamp = DateTime.Now,
                JobId = _viewModel.JobId,
                IsSystemMessage = true,
                AvatarUrl = string.Empty
            });
        }, DispatcherPriority.Background);
    }

    private void HandleTypingIndicator(object? sender, TypingIndicatorDto payload)
    {
        Dispatcher.Invoke(() =>
        {
            var active = new HashSet<string>(payload.Usernames ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in _participantLookup)
                kvp.Value.IsTyping = active.Contains(kvp.Key);

            _viewModel.UpdateTypingIndicator(active);
        }, DispatcherPriority.Background);
    }

    private async void MessageInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_chatClient is null || !_viewModel.IsConnected)
            return;

        if (!_isTypingLocally)
        {
            _isTypingLocally = true;
            await TryNotifyTypingAsync(true);
        }

        _typingTimer.Stop();
        _typingTimer.Start();
    }

    private async void TypingTimer_Tick(object? sender, EventArgs e)
    {
        _typingTimer.Stop();

        if (_isTypingLocally)
        {
            _isTypingLocally = false;
            await TryNotifyTypingAsync(false);
        }
    }

    private async Task DisposeClientAsync()
    {
        if (_chatClient is null)
            return;

        try
        {
            await _chatClient.DisposeAsync();
        }
        catch
        {
            // ignore shutdown exceptions
        }
        finally
        {
            _chatClient = null;
        }
    }

    private long? ResolveUserId()
    {
        if (_viewModel.SessionUserId.HasValue)
            return _viewModel.SessionUserId;

        if (long.TryParse(_viewModel.UserId, out var parsed))
            return parsed;

        return null;
    }

    private void InsertParticipantSorted(ParticipantViewModel participant)
    {
        int index = 0;

        while (index < _viewModel.Participants.Count &&
               string.Compare(_viewModel.Participants[index].Username, participant.Username, StringComparison.OrdinalIgnoreCase) < 0)
        {
            index++;
        }

        _viewModel.Participants.Insert(index, participant);
    }

    private async Task EnsureAvatarForParticipantAsync(ParticipantViewModel participant)
    {
        var resolution = await ResolveAvatarAsync(participant.UserId, participant.Username, participant.AvatarUrl);

        if (string.IsNullOrEmpty(resolution.AvatarUrl))
            return;

        await Dispatcher.InvokeAsync(() =>
        {
            participant.UserId ??= resolution.UserId;
            participant.AvatarUrl = resolution.AvatarUrl;
        }, DispatcherPriority.Background);
    }

    private async Task<AvatarResolution> ResolveAvatarAsync(long? userId, string username, string? existingUrl)
    {
        if (!string.IsNullOrEmpty(existingUrl))
            return new AvatarResolution(userId, existingUrl);

        if (userId.HasValue && _avatarUrlCache.TryGetValue(userId.Value, out var cached))
            return new AvatarResolution(userId, cached);

        var resolved = await RobloxAvatarDirectory.TryResolveAsync(userId, username);

        if (resolved?.UserId is long resolvedId && !string.IsNullOrEmpty(resolved.AvatarUrl))
        {
            _avatarUrlCache[resolvedId] = resolved.AvatarUrl;
            return new AvatarResolution(resolvedId, resolved.AvatarUrl);
        }

        return new AvatarResolution(resolved?.UserId ?? userId, resolved?.AvatarUrl ?? string.Empty);
    }

    private async Task TryNotifyTypingAsync(bool isTyping)
    {
        if (_chatClient is null)
            return;

        try
        {
            await _chatClient.NotifyTypingAsync(isTyping);
        }
        catch
        {
            // ignore typing notification issues
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settings = new SettingsWindow(this);
        settings.Owner = this;
        settings.ShowDialog();
    }

    private void OnGamesListReceived(object? sender, List<GameDto> games)
    {
        if (games is null) return;

        Dispatcher.Invoke(() =>
        {
            _viewModel.Games.Clear();
            foreach (var game in games)
                _viewModel.Games.Add(game);
        });
    }

    private async void Browse_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.IsServerBrowserVisible = true;
        
        try
        {
            if (_chatClient == null)
            {
                _chatClient = new ChatClient(_viewModel.BackendUrl);
            }

            _chatClient.GamesListReceived -= OnGamesListReceived;
            _chatClient.GamesListReceived += OnGamesListReceived;

            await _chatClient.GetGamesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load games: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.IsServerBrowserVisible = false;
    }

    private async void JoinServer_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string jobId)
        {
            // Find the game for this job
            var game = _viewModel.Games.FirstOrDefault(g => g.Servers.Any(s => s.JobId == jobId));
            if (game != null)
            {
                try
                {
                    // Use browser URL to ensure authentication
                    var placeId = game.PlaceId;
                    var targetJobId = game.Servers.FirstOrDefault(s => s.JobId == (string)btn.Tag)?.JobId ?? (string)btn.Tag;

                    // https://www.roblox.com/games/start?placeId=<ID>&gameId=<JOBID>
                    var url = $"https://www.roblox.com/games/start?placeId={placeId}&gameId={targetJobId}";
                    
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });

                    // Auto connect chat
                    _viewModel.JobId = jobId;
                    _viewModel.PlaceId = game.PlaceId;
                    _viewModel.IsServerBrowserVisible = false;
                    
                    // We need a username to connect
                    if (string.IsNullOrWhiteSpace(_viewModel.Username))
                    {
                        // Try to get from logs or prompt
                        var session = await RobloxLogParser.TryReadLatestAsync();
                        if (session != null && !string.IsNullOrWhiteSpace(session.Username))
                        {
                            _viewModel.Username = session.Username;
                            _viewModel.UserId = session.UserId?.ToString() ?? string.Empty;
                        }
                        else
                        {
                            MessageBox.Show("Please enter a username in settings or start Roblox first.", "Username Required");
                            return;
                        }
                    }

                    await DisposeClientAsync();
                    _chatClient = new ChatClient(_viewModel.BackendUrl);
                    _chatClient.MessageReceived += HandleMessageReceived;
                    _chatClient.ParticipantsChanged += HandleParticipantsChanged;
                    _chatClient.HistoryReceived += HandleHistoryReceived;
                    _chatClient.TypingIndicatorReceived += HandleTypingIndicator;

                    var userId = ResolveUserId();
                    await _chatClient.ConnectAsync(_viewModel.Username, _viewModel.JobId, userId, _viewModel.PlaceId);
                    _viewModel.IsConnected = true;
                    
                    _discordRpc.SetStatus($"Playing {game.Name}", $"Server: {game.Servers.FirstOrDefault(s => s.JobId == jobId)?.PlayerCount ?? 0} Players", startTime: DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to join: {ex.Message}");
                }
            }
        }
    }

    private void NewConversation_Click(object sender, RoutedEventArgs e)
    {
        if (_chatClient == null || !_viewModel.IsConnected)
        {
            MessageBox.Show("You must be connected to a server to search for users.", "Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var searchWindow = new UserSearchWindow(_chatClient)
        {
            Owner = this
        };

        if (searchWindow.ShowDialog() == true && searchWindow.SelectedUser != null)
        {
            var user = searchWindow.SelectedUser;
            // Check if we already have a conversation with this user
            var existingConv = _viewModel.Conversations.FirstOrDefault(c => c.IsDirectMessage && c.Title == user.Username);
            
            if (existingConv != null)
            {
                _viewModel.SelectedConversation = existingConv;
            }
            else
            {
                // Create new DM conversation
                if (user.UserId.HasValue)
                {
                     var conv = _viewModel.GetOrCreateDm(user.UserId.Value, user.Username);
                     _viewModel.SelectedConversation = conv;
                }
                else
                {
                     MessageBox.Show("Could not determine User ID for selected user.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

internal sealed record AvatarResolution(long? UserId, string AvatarUrl);
