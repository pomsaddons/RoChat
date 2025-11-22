using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using BloxCord.Client.Models;

namespace BloxCord.Client.Services;

public sealed class ChatClient : IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private HubConnection? _hubConnection;
    private string? _jobId;
    private string? _username;
    private long? _userId;

    public ChatClient(string backendUrl)
    {
        if (string.IsNullOrWhiteSpace(backendUrl))
            throw new ArgumentException("Backend URL is required", nameof(backendUrl));

        if (!backendUrl.EndsWith('/'))
            backendUrl += "/";

        BackendUrl = backendUrl.TrimEnd('/');
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BackendUrl + "/")
        };
    }

    public string BackendUrl { get; }

    public event EventHandler<ChatMessageDto>? MessageReceived;
    public event EventHandler<List<ChannelParticipantDto>>? ParticipantsChanged;
    public event EventHandler<List<ChatMessageDto>>? HistoryReceived;
    public event EventHandler<TypingIndicatorDto>? TypingIndicatorReceived;

    public async Task ConnectAsync(string username, string jobId, long? userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);

        _username = username;
        _jobId = jobId;
        _userId = userId;

        await CreateChannelAsync(username, jobId, userId, cancellationToken);

        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{BackendUrl}/hubs/chat")
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<ChatMessageDto>("ReceiveMessage", message =>
        {
            MessageReceived?.Invoke(this, message);
        });

        _hubConnection.On<string, List<ChannelParticipantDto>>("ParticipantsChanged", (job, participants) =>
        {
            ParticipantsChanged?.Invoke(this, participants);
        });

        _hubConnection.On<ChannelSnapshotDto>("ChannelSnapshot", snapshot =>
        {
            HistoryReceived?.Invoke(this, snapshot.History ?? new List<ChatMessageDto>());
            ParticipantsChanged?.Invoke(this, snapshot.Participants ?? new List<ChannelParticipantDto>());
        });

        _hubConnection.On<TypingIndicatorDto>("TypingIndicator", payload =>
        {
            TypingIndicatorReceived?.Invoke(this, payload);
        });

        await _hubConnection.StartAsync(cancellationToken);
        await _hubConnection.InvokeAsync("JoinChannel", new ChannelJoinDto
        {
            JobId = jobId,
            Username = username,
            UserId = userId
        }, cancellationToken);
    }

    public async Task SendAsync(string content, CancellationToken cancellationToken = default)
    {
        if (_hubConnection is null || _jobId is null || _username is null)
            throw new InvalidOperationException("Client is not connected");

        await _hubConnection.InvokeAsync("SendMessage", new PostChatMessageDto
        {
            JobId = _jobId,
            Username = _username,
            UserId = _userId,
            Content = content
        }, cancellationToken);
    }

    public async Task NotifyTypingAsync(bool isTyping, CancellationToken cancellationToken = default)
    {
        if (_hubConnection is null || _jobId is null || _username is null)
            return;

        await _hubConnection.InvokeAsync("NotifyTyping", new TypingNotificationDto
        {
            JobId = _jobId,
            Username = _username,
            IsTyping = isTyping
        }, cancellationToken);
    }

    private async Task CreateChannelAsync(string username, string jobId, long? userId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("api/channels", new ChannelJoinDto
        {
            JobId = jobId,
            Username = username,
            UserId = userId
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
            await _hubConnection.DisposeAsync();

        _httpClient.Dispose();
    }
}
