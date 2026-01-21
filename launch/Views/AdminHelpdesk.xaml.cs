using launch.Views.UserControls;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace launch.Views
{
    public partial class AdminHelpdesk : Page
    {
        private string ConnectionString = "Database=launcherDB;Datasource=127.0.0.1;User=root;Password=";
        private readonly Dictionary<int, UserInfo> _usersCache = new();

        public AdminHelpdesk()
        {
            InitializeComponent();
            LoadDataAsync().ConfigureAwait(false);
        }

        private record UserInfo(string Name, string Telegram);

        private async Task LoadDataAsync()
        {
            try
            {
                await LoadUsersCacheAsync();
                await LoadErrorMessagesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private async Task LoadUsersCacheAsync()
        {
            _usersCache.Clear();

            await using var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, nickname, telegram FROM users";

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                _usersCache.Add(
                    reader.GetInt32(0),
                    new UserInfo(reader.GetString(1), reader.GetString(2))
                );
            }
        }

        private async Task LoadErrorMessagesAsync()
        {
            helpdeskScroll.Children.Clear();
            await using var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, message, image, user FROM errormessages";

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var messageControl = new UserControlHelpdeskMessage
                {
                    messageText = reader.GetString(1),
                    tagBtn = reader.GetInt32(0).ToString()
                };

                if (_usersCache.TryGetValue(reader.GetInt32(3), out var userInfo))
                {
                    messageControl.user = userInfo.Name;
                    messageControl.userTelegram = userInfo.Telegram;
                }

                await LoadMessageImageAsync(messageControl, reader);
                messageControl.Click += OnDeleteClick;
                messageControl.ClickView += OnViewClick;

                helpdeskScroll.Dispatcher.Invoke(() =>
                    helpdeskScroll.Children.Add(messageControl));
            }
        }

        private async Task LoadMessageImageAsync(UserControlHelpdeskMessage control, DbDataReader reader)
        {
            try
            {
                if (!reader.IsDBNull(2))
                {
                    await using var stream = reader.GetStream(2);
                    var image = await LoadImageFromStreamAsync(stream);
                    control.Dispatcher.Invoke(() => control.imageTB.Source = image);
                }
                else
                {
                    control.Dispatcher.Invoke(() =>
                        control.imageTB.Source = new BitmapImage(
                            new Uri(BaseUriHelper.GetBaseUri(this), "Images/noImage.png")));
                }
            }
            catch
            {
                control.Dispatcher.Invoke(() =>
                    control.imageTB.Source = new BitmapImage(
                        new Uri(BaseUriHelper.GetBaseUri(this), "Images/noImage.png")));
            }
        }

        private async Task<BitmapImage> LoadImageFromStreamAsync(Stream stream)
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = memoryStream;
            image.EndInit();
            image.Freeze();

            return image;
        }

        private async void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || string.IsNullOrEmpty(button.Tag?.ToString()))
                return;

            var result = MessageBox.Show(
                "Вы уверены, что хотите удалить это сообщение?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM errormessages WHERE id = @id";
                command.Parameters.AddWithValue("@id", button.Tag);

                await command.ExecuteNonQueryAsync();
                await LoadErrorMessagesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}");
            }
        }

        private void OnViewClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && !string.IsNullOrEmpty(button.Tag?.ToString()))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(button.Tag.ToString())
                    {
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии: {ex.Message}");
                }
            }
        }
    }
}