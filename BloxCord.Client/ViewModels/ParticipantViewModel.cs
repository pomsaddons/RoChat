using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Diagnostics;

namespace BloxCord.Client.ViewModels;

public class ParticipantViewModel : INotifyPropertyChanged
{
    private string _avatarUrl = string.Empty;
    private bool _isTyping;
    private bool _isSelected;

    public ICommand GoToProfileCommand { get; }

    public ParticipantViewModel()
    {
        GoToProfileCommand = new RelayCommand(_ => OpenProfile());
    }

    private void OpenProfile()
    {
        if (UserId.HasValue)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"https://www.roblox.com/users/{UserId}/profile",
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignore errors
            }
        }
    }

    public string Username { get; init; } = string.Empty;

    private long? _userId;
    public long? UserId
    {
        get => _userId;
        set => SetField(ref _userId, value);
    }

    public string AvatarUrl
    {
        get => _avatarUrl;
        set => SetField(ref _avatarUrl, value);
    }

    public bool IsTyping
    {
        get => _isTyping;
        set => SetField(ref _isTyping, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
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
