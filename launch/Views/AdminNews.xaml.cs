using launch.Views.Other;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace launch.Views
{
    public partial class AdminNews : Page
    {
        private readonly List<NewsItem> _newsItems = new();
        private int _currentIndex;
        private readonly string _connectionString;

        public AdminNews()
        {
            InitializeComponent();
            _connectionString = $"Database=launcherDB;Datasource=127.0.0.1;User=root;Password=";
            LoadDataAsync().ConfigureAwait(false);
        }

        private record NewsItem(
            int Id,
            string Name,
            string Description,
            string Date,
            BitmapImage Image1,
            BitmapImage Image2
        );

        private async Task LoadDataAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                _newsItems.Clear();
                await using (var command = new MySqlCommand("SELECT * FROM news", connection))
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var image1 = await LoadImageAsync(reader.GetStream(4));
                        var image2 = await LoadImageAsync(reader.GetStream(5));

                        _newsItems.Add(new NewsItem(
                            reader.GetInt32(0),
                            reader.GetString(1),
                            reader.GetString(2),
                            reader.GetDateTime(3).ToString("dd.MM.yyyy"),
                            image1,
                            image2
                        ));
                    }
                }

                if (_newsItems.Count > 0)
                    DisplayCurrentNews();
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка загрузки: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static async Task<BitmapImage> LoadImageAsync(Stream stream)
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
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка загрузки: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return new BitmapImage(new Uri("pack://application:,,,/Views/Images/noImage.png"));
            }
        }

        private void DisplayCurrentNews()
        {
            if (_currentIndex < 0 || _currentIndex >= _newsItems.Count)
                return;

            var current = _newsItems[_currentIndex];
            nameTB.Text = current.Name;
            descTB.Text = current.Description;
            dateTB.Text = current.Date;
            imageBitmap1.Source = current.Image1;
            imageBitmap2.Source = current.Image2;
        }

        private async void BtnLeft_Click(object sender, RoutedEventArgs e)
        {
            _currentIndex = _currentIndex > 0 ? _currentIndex - 1 : _newsItems.Count - 1;
            DisplayCurrentNews();
        }

        private async void BtnRight_Click(object sender, RoutedEventArgs e)
        {
            _currentIndex = _currentIndex < _newsItems.Count - 1 ? _currentIndex + 1 : 0;
            DisplayCurrentNews();
        }

        private async Task UpdateImageAsync(Image imageControl, bool isNewImage)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "PNG files (*.png)|*.png",
                Title = "Выберите изображение"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.UriSource = new Uri(openFileDialog.FileName);
                    image.EndInit();
                    image.Freeze();

                    imageControl.Source = image;

                    if (!isNewImage && _currentIndex >= 0)
                    {
                        var current = _newsItems[_currentIndex];
                        _newsItems[_currentIndex] = current with
                        {
                            Image1 = imageControl == imageBitmap1 ? image : current.Image1,
                            Image2 = imageControl == imageBitmap2 ? image : current.Image2
                        };
                    }
                }
                catch (Exception ex)
                {
                    MassageWindow.Show("Ошибка загрузки изображения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(newNameTB.Text) ||
                string.IsNullOrWhiteSpace(newDescTB.Text))
            {
                MassageWindow.Show("Заполните обязательные поля", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!(newImageBitmap1.Source is BitmapImage image1) || image1.UriSource == null || 
                    !(newImageBitmap2.Source is BitmapImage image2) || image2.UriSource == null)
                {
                    MassageWindow.Show("Выберите изображения для новости", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                byte[] imageBytes1 = await GetImageBytesAsync(newImageBitmap1.Source);
                byte[] imageBytes2 = await GetImageBytesAsync(newImageBitmap2.Source);

                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"INSERT INTO news 
                            (name, bescription, date, image1, image2) 
                            VALUES (@name, @description, @date, @image1, @image2)";

                await using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@name", newNameTB.Text);
                command.Parameters.AddWithValue("@description", newDescTB.Text);
                command.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd"));
                command.Parameters.Add("@image1", MySqlDbType.MediumBlob).Value = imageBytes1;
                command.Parameters.Add("@image2", MySqlDbType.MediumBlob).Value = imageBytes2;

                await command.ExecuteNonQueryAsync();
                await LoadDataAsync();
                ClearAddForm();
                EventService.RaiseNewsChanged();
                MassageWindow.Show("Новость успешно добавлена", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при добавлении новости: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<byte[]> GetImageBytesAsync(object imageSource)
        {
            try
            {
                if (imageSource is not BitmapImage bitmap || bitmap.UriSource == null)
                    return Array.Empty<byte>();

                using var stream = File.OpenRead(bitmap.UriSource.LocalPath);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при обработке изображения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return Array.Empty<byte>();
            }
        }

        private void ClearAddForm()
        {
            newNameTB.Text = string.Empty;
            newDescTB.Text = string.Empty;
            newDateTB.Text = string.Empty;
            newImageBitmap1.Source = new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Images/noImage.png"));
            newImageBitmap2.Source = new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Images/noImage.png"));
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < 0 || _currentIndex >= _newsItems.Count)
                return;

            try
            {
                byte[] imageBytes1 = await GetImageBytesAsync(imageBitmap1.Source);
                byte[] imageBytes2 = await GetImageBytesAsync(imageBitmap2.Source);

                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"UPDATE news SET 
                            name = @name, 
                            bescription = @description,
                            date = @date,
                            image1 = @image1,
                            image2 = @image2
                            WHERE id = @id";

                await using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@name", nameTB.Text);
                command.Parameters.AddWithValue("@description", descTB.Text);
                command.Parameters.AddWithValue("@date", dateTB.Text);
                command.Parameters.Add("@image1", MySqlDbType.MediumBlob).Value = imageBytes1;
                command.Parameters.Add("@image2", MySqlDbType.MediumBlob).Value = imageBytes2;
                command.Parameters.AddWithValue("@id", _newsItems[_currentIndex].Id);

                await command.ExecuteNonQueryAsync();
                await LoadDataAsync();
                MassageWindow.Show("Новость успешно обновлена", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка обновления новости: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < 0 || _currentIndex >= _newsItems.Count)
                return;

            var result = MassageWindow.Show(
                "Вы уверены, что хотите удалить эту новость?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var query1 = "DELETE FROM userratings WHERE news = @newsId";
                await using var command1 = new MySqlCommand(query1, connection);
                command1.Parameters.AddWithValue("@newsId", _newsItems[_currentIndex].Id);
                await command1.ExecuteNonQueryAsync();

                var query = "DELETE FROM news WHERE id = @id";
                await using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", _newsItems[_currentIndex].Id);
                await command.ExecuteNonQueryAsync();

                await LoadDataAsync();
                _currentIndex = Math.Clamp(_currentIndex, 0, _newsItems.Count - 1);

                if (_newsItems.Count > 0)
                    DisplayCurrentNews();

                EventService.RaiseNewsChanged();
                MassageWindow.Show("Новость успешно удалена", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при удалении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnViewFile1_Click(object sender, RoutedEventArgs e)
            => UpdateImageAsync(imageBitmap1, false).ConfigureAwait(false);

        private void btnViewFile2_Click(object sender, RoutedEventArgs e)
            => UpdateImageAsync(imageBitmap2, false).ConfigureAwait(false);

        private void btnViewNewFile1_Click(object sender, RoutedEventArgs e)
            => UpdateImageAsync(newImageBitmap1, true).ConfigureAwait(false);

        private void btnViewNewFile2_Click(object sender, RoutedEventArgs e)
            => UpdateImageAsync(newImageBitmap2, true).ConfigureAwait(false);

        private void btnAddDownload_Click(object sender, RoutedEventArgs e)
        {
            viewDownloadsPage.Visibility = Visibility.Hidden;
            addDownloadPage.Visibility = Visibility.Visible;
        }

        private void btnViewDownloads_Click(object sender, RoutedEventArgs e)
        {
            viewDownloadsPage.Visibility = Visibility.Visible;
            addDownloadPage.Visibility = Visibility.Hidden;
        }
    }
}