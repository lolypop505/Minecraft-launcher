using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using MySql.Data.MySqlClient;

namespace launch.Views
{
    public partial class News : Page
    {
        private const string ConnectionString = "Database=launcherDB;Datasource=127.0.0.1;User=root;Password=";

        private int _currentUserId;
        private int _currentNewsIndex;
        private readonly List<NewsItem> _newsItems = new();
        private readonly Dictionary<int, UserRating> _userRatings = new();

        public News()
        {
            InitializeComponent();
            LoadUserData();
            LoadNewsDataAsync().ConfigureAwait(false);
        }

        private record NewsItem(
            int Id,
            string Title,
            string Description,
            string Date,
            BitmapImage Image1,
            BitmapImage Image2,
            int LikesCount,
            int DislikesCount
        );

        private record UserRating(bool IsLiked, bool IsDisliked);

        private void LoadUserData()
        {
            try
            {
                var jsonString = File.ReadAllText("userData.json");
                wind userData = JsonSerializer.Deserialize<wind>(jsonString)!;
                _currentUserId = userData.id;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных пользователя: {ex.Message}");
            }
        }

        private async Task LoadNewsDataAsync()
        {
            try
            {
                await using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                await LoadUserRatingsAsync(connection);
                await LoadNewsItemsAsync(connection);

                if (_newsItems.Count > 0)
                    DisplayNewsItem(_currentNewsIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки новостей: {ex.Message}");
            }
        }

        private async Task LoadUserRatingsAsync(MySqlConnection connection)
        {
            _userRatings.Clear();

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT news, liked FROM userratings WHERE user = @userId";
            command.Parameters.AddWithValue("@userId", _currentUserId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var newsId = reader.GetInt32(0);
                var liked = reader.GetBoolean(1);
                _userRatings[newsId] = new UserRating(liked, !liked);
            }
        }

        private async Task LoadNewsItemsAsync(MySqlConnection connection)
        {
            _newsItems.Clear();

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT n.id, n.name, n.bescription, n.date, n.image1, n.image2,
                       SUM(CASE WHEN ur.liked = 1 THEN 1 ELSE 0 END) as likes,
                       SUM(CASE WHEN ur.liked = 0 THEN 1 ELSE 0 END) as dislikes
                FROM news n
                LEFT JOIN userratings ur ON n.id = ur.news
                GROUP BY n.id";

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var newsItem = new NewsItem(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetDateTime(3).ToString("dd.MM.yyyy"),
                    await LoadImageAsync(reader.GetStream(4)),
                    await LoadImageAsync(reader.GetStream(5)),
                    reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                    reader.IsDBNull(7) ? 0 : reader.GetInt32(7)
                );
                _newsItems.Add(newsItem);
            }
        }

        private async Task<BitmapImage> LoadImageAsync(Stream stream)
        {
            try
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
            catch
            {
                return new BitmapImage(new Uri("pack://application:,,,/Images/noImage.png"));
            }
        }

        private void DisplayNewsItem(int index)
        {
            if (index < 0 || index >= _newsItems.Count)
                return;

            _currentNewsIndex = index;
            var newsItem = _newsItems[index];

            nameNews.Text = newsItem.Title;
            bescriptionNews.Text = newsItem.Description;
            dateNews.Text = newsItem.Date;
            imageNews1.Source = newsItem.Image1;
            imageNews2.Source = newsItem.Image2;
            countLike.Text = newsItem.LikesCount.ToString();
            countDislike.Text = newsItem.DislikesCount.ToString();

            UpdateRatingButtons(newsItem.Id);
        }

        private void UpdateRatingButtons(int newsId)
        {
            if (_userRatings.TryGetValue(newsId, out var rating))
            {
                btnLike.IsChecked = rating.IsLiked;
                btnDisLike.IsChecked = rating.IsDisliked;
            }
            else
            {
                btnLike.IsChecked = false;
                btnDisLike.IsChecked = false;
            }
        }

        private async void BtnLeft_Click(object sender, RoutedEventArgs e)
        {
            var newIndex = _currentNewsIndex > 0
                ? _currentNewsIndex - 1
                : _newsItems.Count - 1;
            DisplayNewsItem(newIndex);
        }

        private async void BtnRight_Click(object sender, RoutedEventArgs e)
        {
            var newIndex = _currentNewsIndex < _newsItems.Count - 1
                ? _currentNewsIndex + 1
                : 0;
            DisplayNewsItem(newIndex);
        }

        private async void btnLike_Click(object sender, RoutedEventArgs e)
        {
            var newsId = _newsItems[_currentNewsIndex].Id;
            await ToggleRatingAsync(newsId, true);
        }

        private async void btnDisLike_Click(object sender, RoutedEventArgs e)
        {
            var newsId = _newsItems[_currentNewsIndex].Id;
            await ToggleRatingAsync(newsId, false);
        }

        private async Task ToggleRatingAsync(int newsId, bool isLike)
        {
            try
            {
                await using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                if (_userRatings.TryGetValue(newsId, out var currentRating))
                {
                    // Если повторно нажали на ту же кнопку - удаляем оценку
                    if ((isLike && currentRating.IsLiked) || (!isLike && currentRating.IsDisliked))
                    {
                        await RemoveRatingAsync(connection, newsId);
                    }
                    else // Если нажали другую кнопку - обновляем оценку
                    {
                        await UpdateRatingAsync(connection, newsId, isLike);
                    }
                }
                else // Если оценки не было - добавляем новую
                {
                    await AddRatingAsync(connection, newsId, isLike);
                }

                // Обновляем данные
                await LoadNewsDataAsync();
                DisplayNewsItem(_currentNewsIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении оценки: {ex.Message}");
                UpdateRatingButtons(newsId); // Восстанавливаем предыдущее состояние
            }
        }

        private async Task AddRatingAsync(MySqlConnection connection, int newsId, bool isLike)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO userratings (news, user, liked) VALUES (@newsId, @userId, @isLike)";
            command.Parameters.AddWithValue("@newsId", newsId);
            command.Parameters.AddWithValue("@userId", _currentUserId);
            command.Parameters.AddWithValue("@isLike", isLike);
            await command.ExecuteNonQueryAsync();
        }

        private async Task UpdateRatingAsync(MySqlConnection connection, int newsId, bool isLike)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE userratings SET liked = @isLike WHERE news = @newsId AND user = @userId";
            command.Parameters.AddWithValue("@newsId", newsId);
            command.Parameters.AddWithValue("@userId", _currentUserId);
            command.Parameters.AddWithValue("@isLike", isLike);
            await command.ExecuteNonQueryAsync();
        }

        private async Task RemoveRatingAsync(MySqlConnection connection, int newsId)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM userratings WHERE news = @newsId AND user = @userId";
            command.Parameters.AddWithValue("@newsId", newsId);
            command.Parameters.AddWithValue("@userId", _currentUserId);
            await command.ExecuteNonQueryAsync();
        }
    }
}