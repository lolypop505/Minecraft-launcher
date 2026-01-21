using CmlLib.Core.Version;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;
using launch.Views.Other;

namespace launch.Views
{
    public partial class AdminDownloads : Page
    {
        private record AssemblyData(
           int Id,
           string Name,
           string Description,
           float Weight,
           string Version,
           BitmapImage Image,
           int RequiredSubscription,
           string Url
        );

        private readonly List<AssemblyData> _assemblies = new();
        private int _currentIndex;
        private string _connectionString;

        public AdminDownloads()
        {
            InitializeComponent();
            _connectionString = $"Database=launcherDB;Datasource=127.0.0.1;User=root;Password=";
            Loaded += AdminDownloads_Loaded;
            EventService.SubscriptionListChanged += OnSubscriptionListChanged;
        }

        private void AdminDownloads_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadData();
        }

        private void OnSubscriptionListChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => ReloadData());
        }

        private void ReloadData()
        {
            _assemblies.Clear();
            LoadDataAsync().ConfigureAwait(false);
        }

        public async Task LoadDataAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Загрузка сборок
            _assemblies.Clear();
            await using (var command = new MySqlCommand("SELECT * FROM assembly", connection))
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var image = await LoadImageAsync(reader.GetStream(5));
                    _assemblies.Add(new AssemblyData(
                        reader.GetInt32(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetFloat(3),
                        reader.GetString(4),
                        image,
                        reader.GetInt32(6),
                        reader.GetString(7)
                    ));
                }
            }

            // Загрузка подписок
            await LoadSubscriptionsAsync(connection);

            if (_assemblies.Count > 0)
                DisplayCurrentAssembly();
        }
        private static async Task<BitmapImage> LoadImageAsync(Stream stream)
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

        private void DisplayCurrentAssembly()
        {
            if (_currentIndex < 0)
                _currentIndex = _assemblies.Count-1;
            if (_currentIndex >= _assemblies.Count)
                _currentIndex = 0;

            var current = _assemblies[_currentIndex];

            // Используем привязку данных вместо ручного обновления
            nameTB.Text = current.Name;
            descTB.Text = current.Description;
            weightTB.Text = current.Weight.ToString("0.0");
            versTB.Text = current.Version;
            urlTB.Text = current.Url;
            imageBitmap.Source = current.Image;

            // Оптимизированный поиск в ComboBox
            versionsBox.SelectedValue = current.RequiredSubscription;
        }

        private async Task LoadSubscriptionsAsync(MySqlConnection connection)
        {
            newVersionsBox.Items.Clear();
            versionsBox.Items.Clear();

            await using var command = new MySqlCommand("SELECT id, name FROM subscriptions", connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32(0);
                var name = reader.GetString(1);

                // Используем привязку данных
                newVersionsBox.Items.Add(new { Id = id, Name = name });
                versionsBox.Items.Add(new { Id = id, Name = name });
            }

            versionsBox.DisplayMemberPath = "Name";
            versionsBox.SelectedValuePath = "Id";
            newVersionsBox.DisplayMemberPath = "Name";
            newVersionsBox.SelectedValuePath = "Id";
        }

        private async Task SaveCurrentAssemblyAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"UPDATE assembly SET 
                    name = @name, 
                    bescription = @description,
                    weight = @weight,
                    version = @version,
                    requiredSubscription = @subscription,
                    assemblyURL = @url
                    WHERE id = @id";

                await using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@name", nameTB.Text);
                command.Parameters.AddWithValue("@description", descTB.Text);
                command.Parameters.AddWithValue("@weight", float.Parse(weightTB.Text));
                command.Parameters.AddWithValue("@version", versTB.Text);
                command.Parameters.AddWithValue("@subscription", versionsBox.SelectedValue);
                command.Parameters.AddWithValue("@url", urlTB.Text);
                command.Parameters.AddWithValue("@id", _assemblies[_currentIndex].Id);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }
        private async Task UpdateImageAsync(bool isNewImage)
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
                    // Асинхронная загрузка с оптимизацией памяти
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.UriSource = new Uri(openFileDialog.FileName);
                    image.EndInit();
                    image.Freeze();

                    if (isNewImage)
                    {
                        newImageBitmap.Source = image;
                    }
                    else
                    {
                        imageBitmap.Source = image;
                        // Обновляем кэш изображений
                        _assemblies[_currentIndex] = _assemblies[_currentIndex] with { Image = image };
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}");
                }
            }
        }
        private void btnViewDownloads_Click(object sender, RoutedEventArgs e)
        {
            viewDownloadsPage.Visibility = Visibility.Visible;
            addDownloadPage.Visibility = Visibility.Hidden;
        }

        private void btnAddDownload_Click(object sender, RoutedEventArgs e)
        {
            viewDownloadsPage.Visibility = Visibility.Hidden;
            addDownloadPage.Visibility = Visibility.Visible;
        }

        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Валидация данных
            if (string.IsNullOrWhiteSpace(newNameTB.Text) ||
                string.IsNullOrWhiteSpace(newUrlTB.Text) ||
                newVersionsBox.SelectedValue == null)
            {
                MessageBox.Show("Заполните все обязательные поля");
                return;
            }

            try
            {
                // Проверяем изображение
                if (!(newImageBitmap.Source is BitmapImage image) || image.UriSource == null)
                {
                    MessageBox.Show("Выберите изображение для сборки");
                    return;
                }

                // Читаем изображение асинхронно
                byte[] imageBytes;
                using (var stream = File.OpenRead(image.UriSource.LocalPath))
                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    imageBytes = memoryStream.ToArray();
                }

                // Добавляем запись в БД
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"INSERT INTO assembly 
                        (name, bescription, weight, version, image, requiredSubscription, assemblyURL) 
                        VALUES (@name, @description, @weight, @version, @image, @subscription, @url)";

                    await using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", newNameTB.Text);
                        command.Parameters.AddWithValue("@description", newDescTB.Text);
                        command.Parameters.AddWithValue("@weight", float.TryParse(newWeightTB.Text, out var weight) ? weight : 0);
                        command.Parameters.AddWithValue("@version", newVersTB.Text);
                        command.Parameters.Add("@image", MySqlDbType.MediumBlob).Value = imageBytes;
                        command.Parameters.AddWithValue("@subscription", newVersionsBox.SelectedValue);
                        command.Parameters.AddWithValue("@url", newUrlTB.Text);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Обновляем данные и интерфейс
                await LoadDataAsync();
                ClearAddForm();
                EventService.RaiseNewsChanged();
                MessageBox.Show("Сборка успешно добавлена");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}");
            }
        }
        private void ClearAddForm()
        {
            newNameTB.Text = string.Empty;
            newDescTB.Text = string.Empty;
            newWeightTB.Text = string.Empty;
            newVersTB.Text = string.Empty;
            newUrlTB.Text = string.Empty;
            newImageBitmap.Source = new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Images/noImage.png"));
            newVersionsBox.SelectedIndex = -1;
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < 0 || _currentIndex >= _assemblies.Count)
                return;

            // Подтверждение удаления
            var result = MessageBox.Show(
                "Вы уверены, что хотите удалить эту сборку?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var assemblyId = _assemblies[_currentIndex].Id;

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = "DELETE FROM assembly WHERE id = @id";
                    await using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", assemblyId);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Обновляем данные
                await LoadDataAsync();

                // Корректируем текущий индекс
                if (_currentIndex >= _assemblies.Count)
                    _currentIndex = Math.Max(0, _assemblies.Count - 1);

                if (_assemblies.Count > 0)
                    DisplayCurrentAssembly();

                EventService.RaiseNewsChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}");
            }
        }

        private void BtnLeft_Click(object sender, RoutedEventArgs e)
        {
            _currentIndex = _currentIndex > 0 ? _currentIndex - 1 : _assemblies.Count - 1;
            DisplayCurrentAssembly();
        }

        private void BtnRight_Click(object sender, RoutedEventArgs e)
        {
            _currentIndex = _currentIndex < _assemblies.Count - 1 ? _currentIndex + 1 : 0;
            DisplayCurrentAssembly();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentAssemblyAsync().ConfigureAwait(false);
        }


        private void btnViewFile_Click(object sender, RoutedEventArgs e)
        {
            UpdateImageAsync(false).ConfigureAwait(false);
        }

        private void btnViewNewFile_Click(object sender, RoutedEventArgs e)
        {
            UpdateImageAsync(true).ConfigureAwait(false);
        }

        ~AdminDownloads()
        {
            EventService.SubscriptionListChanged -= OnSubscriptionListChanged;
            Loaded -= AdminDownloads_Loaded;
        }
    }
}
