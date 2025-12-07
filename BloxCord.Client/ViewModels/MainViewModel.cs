using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BloxCord.Client.Models;
using System.Linq;
using System.Windows.Input;

namespace BloxCord.Client.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private string _backendUrl = "http://localhost:5158";
    private string _username = string.Empty;
    private string _jobId = string.Empty;
    private string _statusMessage = "Disconnected";
    private string _outgoingMessage = string.Empty;
    private string _userId = string.Empty;
    private bool _isConnected;
    private string _typingIndicator = string.Empty;
    private bool _typingIndicatorVisible;
    private long? _sessionUserId;
    private int _participantCount;
    private bool _isServerBrowserVisible;
    private GameDto? _selectedGame;

    public ObservableCollection<ClientChatMessage> Messages { get; } = new();

    public ObservableCollection<ParticipantViewModel> Participants { get; } = new();

    public ObservableCollection<GameDto> Games { get; } = new();

    public ICommand OpenGamePageCommand { get; }
    public ICommand ViewServersCommand { get; }
    public ICommand ClearSelectedGameCommand { get; }

    public MainViewModel()
    {
        OpenGamePageCommand = new RelayCommand(param =>
        {
            if (param is long placeId)
            {
                try
                {
                    var url = $"https://www.roblox.com/games/{placeId}";
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        });

        ViewServersCommand = new RelayCommand(param =>
        {
            if (param is GameDto game)
            {
                SelectedGame = game;
            }
        });

        ClearSelectedGameCommand = new RelayCommand(_ => SelectedGame = null);
    }

    public GameDto? SelectedGame
    {
        get => _selectedGame;
        set
        {
            if (SetField(ref _selectedGame, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsGameSelected)));
            }
        }
    }

    public bool IsGameSelected => SelectedGame != null;

    public bool IsServerBrowserVisible
    {
        get => _isServerBrowserVisible;
        set => SetField(ref _isServerBrowserVisible, value);
    }

    public string BackendUrl
    {
        get => _backendUrl;
        set => SetField(ref _backendUrl, value);
    }

    public string Username
    {
        get => _username;
        set => SetField(ref _username, value);
    }

    public string JobId
    {
        get => _jobId;
        set => SetField(ref _jobId, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public string OutgoingMessage
    {
        get => _outgoingMessage;
        set => SetField(ref _outgoingMessage, value);
    }

    public string UserId
    {
        get => _userId;
        set => SetField(ref _userId, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => SetField(ref _isConnected, value);
    }

    public string TypingIndicator
    {
        get => _typingIndicator;
        set
        {
            if (SetField(ref _typingIndicator, value))
                IsTypingIndicatorVisible = !string.IsNullOrWhiteSpace(value);
        }
    }

    public bool IsTypingIndicatorVisible
    {
        get => _typingIndicatorVisible;
        private set => SetField(ref _typingIndicatorVisible, value);
    }

    public long? SessionUserId
    {
        get => _sessionUserId;
        set => SetField(ref _sessionUserId, value);
    }

    public long? PlaceId { get; set; }

    public int ParticipantCount
    {
        get => _participantCount;
        set => SetField(ref _participantCount, value);
    }

    public void ResetMessages()
    {
        Messages.Clear();
    }

    public void UpdateTypingIndicator(IEnumerable<string> usernames)
    {
        var list = usernames?.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();

        if (list.Count == 0)
        {
            TypingIndicator = string.Empty;
            return;
        }

        if (list.Count == 1)
        {
            TypingIndicator = $"{list[0]} is typing...";
            return;
        }

        TypingIndicator = $"{string.Join(", ", list)} are typing...";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
