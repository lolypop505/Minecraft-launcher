using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.IO;
using Path = System.IO.Path;
using CmlLib.Core.VersionMetadata;
using System.Threading;
using CmlLib.Core.Version;
using CmlLib.Core.Installers;
using System.Text.Json;
using System.Runtime.Intrinsics.Arm;
using System.Windows.Media.Media3D;
using launch.Views.Other;

namespace launch.Views
{
    public partial class Home : Page
    {
        private enum LaunchStatus
        {
            Preparing,
            CheckingFiles,
            DownloadingRequirements,
            StartingGame,
            Complete,
            Error
        }

        private LaunchStatus _currentStatus;

        private LaunchStatus CurrentStatus
        {
            get => _currentStatus;
            set
            {
                _currentStatus = value;
                UpdateStatusText();
            }
        }

        private void UpdateStatusText()
        {
            Dispatcher.Invoke(() =>
            {
                switch (CurrentStatus)
                {
                    case LaunchStatus.Preparing:
                        statusText.Text = "--- подготовка ---";
                        progressBar.Value = 10;
                        break;
                    case LaunchStatus.CheckingFiles:
                        statusText.Text = "--- проверка файлов ---";
                        progressBar.Value = 30;
                        break;
                    case LaunchStatus.DownloadingRequirements:
                        statusText.Text = "--- загрузка компонентов ---";
                        progressBar.Value = 60;
                        break;
                    case LaunchStatus.StartingGame:
                        statusText.Text = "--- запуск игры ---";
                        progressBar.Value = 90;
                        break;
                    case LaunchStatus.Complete:
                        statusText.Text = "--- готово ---";
                        progressBar.Value = 100;
                        break;
                    case LaunchStatus.Error:
                        statusText.Text = "--- ошибка ---";
                        progressBar.Value = 0;
                        break;
                }
            });
        }

        const string fileName = "userData.json";
        int ram;
        int width;
        int height;
        bool fullScreen;
        string nickname;
        private string selectedVersion = "1.20.1-forge-47.3.0";
        private string minecraftPath = @"C:\Users\Nastyshka\AppData\Roaming\.LauncherLS\.minecraft";
        public Home()
        {
            InitializeComponent();
            Loaded += Home_Loaded;
            EventService.AssemblyDownloadCompleted += OnAssemblyDownloadCompleted;
        }
        private void Home_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadData();
        }

        private void OnAssemblyDownloadCompleted(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => ReloadData());
        }

        private void ReloadData()
        {
            LoadPage();
        }

        public void LoadPage()
        {
            try
            {
                string jsonString = File.ReadAllText(fileName);
                wind userData = JsonSerializer.Deserialize<wind>(jsonString)!;
                minecraftPath = userData.directory;
                ram = userData.ram;
                width = userData.width;
                height = userData.height;
                fullScreen = userData.fullScreen;
                nickname = userData.nickname;

                var folderNames = Directory.GetDirectories(minecraftPath)
                                      .Select(Path.GetFileName)
                                      .ToList();
                versionsBox.Items.Clear();
                foreach (var folderName in folderNames)
                {
                    versionsBox.Items.Add(folderName);
                }
                if (folderNames.Count != 0) { versionsBox.Text = folderNames[0]; }
                else { versionsBox.Items.Add("(у вас пока нет сборок)"); versionsBox.Text = "(у вас пока нет сборок)"; }
            }
            catch (Exception ex)
            { //MessageBox.Show("произошла ошибка !" + ex.Message);
            }
        }

        private async void BtnGoGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (versionsBox.Text == "(у вас пока нет сборок)")
                {
                    MessageBox.Show("В этой директории нет установленных сборок!");
                    return;
                }

                ShowProgressBar();
                CurrentStatus = LaunchStatus.Preparing;

                string directory = Path.Combine(minecraftPath, versionsBox.Text);
                var path = new MinecraftPath(directory);
                var launcher = new MinecraftLauncher(path);

                CurrentStatus = LaunchStatus.CheckingFiles;

                var launchOption = new MLaunchOption
                {
                    MaximumRamMb = ram,
                    Session = MSession.CreateOfflineSession(nickname),
                    FullScreen = fullScreen,
                    ScreenWidth = width,
                    ScreenHeight = height
                };

                CurrentStatus = LaunchStatus.DownloadingRequirements;

                var process = await launcher.InstallAndBuildProcessAsync(selectedVersion, launchOption);

                CurrentStatus = LaunchStatus.StartingGame;

                var processUtil = new ProcessWrapper(process);
                processUtil.OutputReceived += (s, e) => Console.WriteLine(e);
                processUtil.StartWithEvents();

                await processUtil.WaitForExitTaskAsync();

                CurrentStatus = LaunchStatus.Complete;

                await Task.Delay(3000);
                HideProgressBar();
            }
            catch (Exception ex)
            {
                CurrentStatus = LaunchStatus.Error;
                MessageBox.Show($"Произошла ошибка при запуске: {ex.Message}");

                await Task.Delay(3000);
                HideProgressBar();
            }
        }

        private void ShowProgressBar()
        {
            Dispatcher.Invoke(() =>
            {
                progressBorder.Visibility = Visibility.Visible;
                progressBar.Value = 0;
                statusText.Text = "--- подготовка ---";
            });
        }

        private void HideProgressBar()
        {
            Dispatcher.Invoke(() =>
            {
                progressBorder.Visibility = Visibility.Collapsed;
            });
        }

        private void viewVK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://vk.com/talesteam") { UseShellExecute = true });
            }
            catch { MessageBox.Show("проверьте подключение к интернету !"); }
        }

        private void viewDC_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://discord.gg/mATT7BhHA7") { UseShellExecute = true });
            }
            catch { MessageBox.Show("проверьте подключение к интернету !"); }
        }

        private void viewTG_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://t.me/Lostsoulsmodpack") { UseShellExecute = true });
            }
            catch { MessageBox.Show("проверьте подключение к интернету !"); }
        }
    }
}
