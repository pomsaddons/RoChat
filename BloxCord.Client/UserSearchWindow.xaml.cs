using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BloxCord.Client.Models;
using BloxCord.Client.Services;
using BloxCord.Client.ViewModels;

namespace BloxCord.Client
{
    public partial class UserSearchWindow : Window
    {
        private readonly ChatClient _chatClient;
        public ParticipantViewModel? SelectedUser { get; private set; }

        public UserSearchWindow(ChatClient chatClient)
        {
            InitializeComponent();
            _chatClient = chatClient;
            _chatClient.SearchResultsReceived += OnSearchResultsReceived;
        }

        private void OnSearchResultsReceived(object? sender, List<ChannelParticipantDto> results)
        {
            Dispatcher.Invoke(() =>
            {
                var viewModels = new List<ParticipantViewModel>();
                foreach (var dto in results)
                {
                    viewModels.Add(new ParticipantViewModel
                    {
                        Username = dto.Username,
                        UserId = dto.UserId,
                        AvatarUrl = dto.AvatarUrl ?? string.Empty,
                        IsTyping = dto.IsTyping
                    });
                }
                ResultsList.ItemsSource = viewModels;
                NoResultsText.Visibility = (viewModels.Count == 0 && !string.IsNullOrWhiteSpace(SearchBox.Text)) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            });
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = SearchBox.Text;
            if (string.IsNullOrWhiteSpace(query))
            {
                ResultsList.ItemsSource = null;
                NoResultsText.Visibility = Visibility.Collapsed;
                return;
            }

            await _chatClient.SearchUsers(query);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ResultsList.SelectedItem is ParticipantViewModel user)
            {
                SelectedUser = user;
                DialogResult = true;
                Close();
            }
        }

        private void StartChat_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsList.SelectedItem is ParticipantViewModel user)
            {
                SelectedUser = user;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a user to start a chat with.", "No User Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _chatClient.SearchResultsReceived -= OnSearchResultsReceived;
        }
    }
}