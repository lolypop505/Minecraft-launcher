using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MySql.Data.MySqlClient;
using launch.Views.Other;

namespace launch.Views
{
    public partial class Downloads : Page
    {
        private const string ConnectionString = "Database=launcherDB;Datasource=127.0.0.1;User=root;Password=";
        private const string TempZipPath = @"C:\Temp\downloaded_file.zip";

        private int _currentUserId;
        private int _currentSubscriptionLevel;
        private string _extractPath = @"C:\Users\Nastyshka\AppData\Roaming\.LauncherLS\.minecraft";
        private string _yandexDiskUrl = string.Empty;
        private bool _isCanDownload;

        private readonly List<AssemblyItem> _assemblyItems = new();
        private readonly Dictionary<int, BitmapImage> _assemblyImages = new();

        private int _currentAssemblyIndex;
        private CancellationTokenSource _cancellationTokenSource;
        private HttpClient _httpClient;
        private bool _isDownloadPaused;
        private bool _isDownloadInProgress;

        public Downloads()
        {
            InitializeComponent();
            Loaded += Downloads_Loaded;
            EventService.SubscriptionChanged += OnSubscriptionChanged;
        }

        private void Downloads_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadData();
        }

        private void ReloadData()
        {
            _assemblyItems.Clear();
            _assemblyImages.Clear();
            LoadUserData();
            LoadAssemblyDataAsync().ConfigureAwait(false);
        }

        private void OnSubscriptionChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => ReloadData());
        }

        private record AssemblyItem(
            int Id,
            string Name,
            string Description,
            float Weight,
            string MinecraftVersion,
            bool IsAvailable,
            string YandexDiskUrl,
            int RequiredSubscription,
            string SubscriptionName
        );

        private void LoadUserData()
        {
            try
            {
                var jsonString = File.ReadAllText("userData.json");
                wind userData = JsonSerializer.Deserialize<wind>(jsonString)!;

                _currentUserId = userData?.id ?? 0;
                _currentSubscriptionLevel = userData?.subscription ?? 0;
                _extractPath = userData?.directory ?? _extractPath;
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при загрузке данных пользователя: " + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task LoadAssemblyDataAsync()
        {
            try
            {
                await using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT a.id, a.name, a.bescription, a.weight, a.version, a.image, 
                           a.requiredSubscription, a.assemblyURL, s.name as subscriptionName
                    FROM assembly a
                    LEFT JOIN subscriptions s ON a.requiredSubscription = s.id";

                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var requiredSubscription = reader.GetInt32(6);
                    var isAvailable = requiredSubscription <= _currentSubscriptionLevel;

                    var assemblyItem = new AssemblyItem(
                        reader.GetInt32(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetFloat(3),
                        reader.GetString(4),
                        isAvailable,
                        reader.GetString(7),
                        requiredSubscription,
                        reader.IsDBNull(8) ? "Неизвестно" : reader.GetString(8)
                    );

                    _assemblyItems.Add(assemblyItem);

                    // Загрузка изображения
                    try
                    {
                        if (!reader.IsDBNull(5))
                        {
                            var image = await LoadImageAsync(reader.GetStream(5));
                            _assemblyImages[assemblyItem.Id] = image;
                        }
                    }
                    catch (Exception ex)
                    {
                        MassageWindow.Show("Ошибка при загрузке изображения сборки: " + ex.Message,
                            "Предупреждение",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }

                if (_assemblyItems.Count > 0)
                    DisplayAssemblyItem(0);
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при загрузке данных сборок: " + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task<BitmapImage> LoadImageAsync(Stream stream)
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

        private void DisplayAssemblyItem(int index)
        {
            try
            {
                if (index < 0 || index >= _assemblyItems.Count)
                    return;

                _currentAssemblyIndex = index;
                var assembly = _assemblyItems[index];

                nameAssembly.Text = assembly.Name;
                bescriptionAssembly.Text = assembly.Description;
                minecraftVersion.Text = assembly.MinecraftVersion;
                weightAssembly.Text = $"{assembly.Weight} ГБ";
                _yandexDiskUrl = assembly.YandexDiskUrl;

                if (_assemblyImages.TryGetValue(assembly.Id, out var image))
                    imageAssembly.Source = image;

                BtnBlocked.Visibility = assembly.IsAvailable ? Visibility.Hidden : Visibility.Visible;
                if (!_assemblyItems[_currentAssemblyIndex].IsAvailable)
                {
                    btnDownload.Opacity = 0.8;
                    btnDownload.IsEnabled = false;
                }
                else
                {
                    btnDownload.Opacity = 1;
                    btnDownload.IsEnabled = true;
                }
                tooltip.Text = $"Для загрузки данной сборки требуется подписка уровня \"{assembly.SubscriptionName}\"";
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при отображении сборки: " + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnLeft_Click(object sender, RoutedEventArgs e)
        {
            if (_isDownloadInProgress)
            {
                MassageWindow.Show("Дождитесь завершения текущей загрузки",
                    "Внимание",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var newIndex = _currentAssemblyIndex > 0
                ? _currentAssemblyIndex - 1
                : _assemblyItems.Count - 1;
            DisplayAssemblyItem(newIndex);
        }

        private void BtnRight_Click(object sender, RoutedEventArgs e)
        {
            if (_isDownloadInProgress)
            {
                MassageWindow.Show("Дождитесь завершения текущей загрузки",
                    "Внимание",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var newIndex = _currentAssemblyIndex < _assemblyItems.Count - 1
                ? _currentAssemblyIndex + 1
                : 0;
            DisplayAssemblyItem(newIndex);
        }

        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (_isDownloadInProgress)
            {
                MassageWindow.Show("Уже выполняется загрузка",
                    "Внимание",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!_assemblyItems[_currentAssemblyIndex].IsAvailable)
            {
                MassageWindow.Show(
                    $"Для загрузки данной сборки требуется подписка уровня \"{_assemblyItems[_currentAssemblyIndex].SubscriptionName}\"",
                    "Недоступно",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!CheckInternetConnection())
            {
                MassageWindow.Show("Проверьте подключение к интернету",
                    "Ошибка соединения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            try
            {
                SetDownloadControlsState(false);
                DownloadPanel.Visibility = Visibility.Visible;
                _isDownloadInProgress = true;

                _cancellationTokenSource = new CancellationTokenSource();
                _httpClient = new HttpClient();

                await DownloadAndExtractAsync();
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при запуске загрузки: " + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                ResetDownloadState();
            }
        }

        private async void btnPause_Click(object sender, RoutedEventArgs e)
        {
            if (_isDownloadPaused)
            {
                _isDownloadPaused = false;
                statusText.Text = "--- загрузка ---";
            }
            else
            {
                _isDownloadPaused = true;
                statusText.Text = "--- на паузе ---";
            }
        }

        private async Task DownloadAndExtractAsync()
        {
            bool operationCanceled = false;
            bool errorOccurred = false;
            string errorMessage = string.Empty;

            try
            {
                string downloadUrl = GetDirectDownloadLink(_yandexDiskUrl);
                if (string.IsNullOrEmpty(downloadUrl))
                {
                    throw new Exception("Не удалось получить ссылку для скачивания");
                }

                await DownloadFileWithProgressAsync(downloadUrl, TempZipPath, _cancellationTokenSource.Token);

                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    await ExtractZipAsync(TempZipPath, _extractPath, _cancellationTokenSource.Token);
                    statusText.Text = "--- готово ---";
                    MassageWindow.Show("Сборка успешно загружена и установлена!",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    EventService.RaiseAssemblyDownloadCompleted();
                }
            }
            catch (OperationCanceledException)
            {
                operationCanceled = true;
                statusText.Text = "--- загрузка ---";
            }
            catch (Exception ex)
            {
                errorOccurred = true;
                errorMessage = ex.Message;
                statusText.Text = "--- ошибка ---";
            }
            finally
            {
                _httpClient?.Dispose();
                _cancellationTokenSource?.Dispose();

                try
                {
                    if (File.Exists(TempZipPath))
                        File.Delete(TempZipPath);
                }
                catch { /* Игнорируем ошибки удаления временного файла */ }

                if (errorOccurred && !operationCanceled)
                {
                    MassageWindow.Show($"Ошибка: {errorMessage}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                ResetDownloadState();
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (!_isDownloadInProgress) return;

            var result = MassageWindow.Show(
                        "Вы уверены, что хотите прервать загрузку?",
                        "Подтверждение",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _cancellationTokenSource?.Cancel();
                ResetDownloadState();
            }
        }

        private async Task DownloadFileWithProgressAsync(string url, string savePath, CancellationToken token)
        {
            try
            {
                using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength;
                    var totalBytesRead = 0L;
                    var buffer = new byte[8192];

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                    {
                        int bytesRead;
                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                        {
                            while (_isDownloadPaused && !token.IsCancellationRequested)
                            {
                                await Task.Delay(500, token);
                            }

                            token.ThrowIfCancellationRequested();

                            await fileStream.WriteAsync(buffer, 0, bytesRead, token);
                            totalBytesRead += bytesRead;

                            if (totalBytes.HasValue)
                            {
                                var progressPercentage = (double)totalBytesRead / totalBytes.Value * 100;
                                Dispatcher.Invoke(() => progressBar.Value = progressPercentage);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки файла: {ex.Message}");
            }
        }


        private string GetDirectDownloadLink(string shareUrl)
        {
            try
            {
                return $"https://getfile.dokpub.com/yandex/get/{shareUrl}";
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при получении ссылки для скачивания: " + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return null;
            }
        }
        private async Task ExtractZipAsync(string zipPath, string extractPath, CancellationToken token)
        {
            try
            {
                await Task.Run(() =>
                {
                    using (var archive = ZipFile.OpenRead(zipPath))
                    {
                        var totalEntries = archive.Entries.Count;
                        var processedEntries = 0;

                        foreach (var entry in archive.Entries)
                        {
                            token.ThrowIfCancellationRequested();

                            while (_isDownloadPaused && !token.IsCancellationRequested)
                            {
                                Thread.Sleep(500);
                            }

                            var fullPath = Path.Combine(extractPath, entry.FullName);

                            Dispatcher.Invoke(() =>
                            {
                                progressBar.Value = (double)processedEntries / totalEntries * 100;
                                statusText.Text = "--- распаковка ---";
                            });

                            if (entry.FullName.EndsWith("/"))
                            {
                                Directory.CreateDirectory(fullPath);
                            }
                            else
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                                entry.ExtractToFile(fullPath, overwrite: true);
                            }

                            processedEntries++;
                        }
                    }
                }, token);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка распаковки архива: {ex.Message}");
            }
        }

        private bool CheckInternetConnection()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var response = client.GetAsync("http://www.google.com").GetAwaiter().GetResult();
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        private void SetDownloadControlsState(bool isEnabled)
        {
            if(!isEnabled)
            {
                btnDownload.Opacity = 0.8;
                BtnLeft.Opacity = 0.8;
                BtnRight.Opacity = 0.8;
            }
            else
            {
                btnDownload.Opacity = 1;
                BtnLeft.Opacity = 1;
                BtnRight.Opacity = 1;
            }
            btnDownload.IsEnabled = isEnabled;
            BtnLeft.IsEnabled = isEnabled;
            BtnRight.IsEnabled = isEnabled;
        }

        private void ResetDownloadState()
        {
            _isDownloadInProgress = false;
            _isDownloadPaused = false;
            SetDownloadControlsState(true);
            DownloadPanel.Visibility = Visibility.Collapsed;
            progressBar.Value = 0;
        }
        ~Downloads()
        {
            EventService.SubscriptionChanged -= OnSubscriptionChanged;
            Loaded -= Downloads_Loaded;
        }
    }
}