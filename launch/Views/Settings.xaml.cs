using CmlLib.Core;
using launch.Views.Other;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace launch.Views
{
    public partial class Settings : Page
    {
        const string fileName = "userData.json";
        public Settings()
        {
            InitializeComponent();
           
            try
            {
                GCMemoryInfo gcInfo = GC.GetGCMemoryInfo();
                memorySlider.Minimum = 512;
                memorySlider.Maximum = Int32.Parse($"{gcInfo.TotalAvailableMemoryBytes / 1024 / 1024}");

                string jsonString = File.ReadAllText(fileName);
                wind userData = JsonSerializer.Deserialize<wind>(jsonString)!;
                directoryTB.Text = userData.directory;
                memoryTB.Text = userData.ram.ToString();
                widthTB.Text = userData.width.ToString();
                heightTB.Text = userData.height.ToString();
                memorySlider.Value = userData.ram;
                btnFullScreen.IsChecked = userData.fullScreen;

                if (!Directory.Exists(userData.directory))
                {
                    Directory.CreateDirectory(userData.directory);
                }
            }
            catch (Exception ex) 
            {
                MassageWindow.Show("Ошибка при загрузке настроек: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            this.DataContext = new ModSettingsViewModel(directoryTB.Text);
        }
        public static void UpdateDirectoryOnly(string newDirectory, bool? newFullScreen, int newWidth, int newHeight, int memory)
        {
            try
            {

                string json = File.ReadAllText(fileName);
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    var updatedData = new
                    {
                        id = root.GetProperty("id").GetInt32(),
                        login = root.GetProperty("login").GetString(),
                        password = root.GetProperty("password").GetString(),
                        subscription = root.GetProperty("subscription").GetInt32(),
                        nickname = root.GetProperty("nickname").GetString(),
                        directory = newDirectory,
                        fullScreen = newFullScreen,
                        width = newWidth,
                        height = newHeight,
                        ram = memory
                    };
                    string updatedJson = JsonSerializer.Serialize(updatedData);
                    File.WriteAllText(fileName, updatedJson);
                }
                EventService.RaiseAssemblyDownloadCompleted();
            }
            catch (Exception ex) 
            {
                MassageWindow.Show("Ошибка при сохранении настроек: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnGameSettings_Click(object sender, RoutedEventArgs e)
        {
            gameSettingsPage.Visibility = Visibility.Visible;
            modSettingsPage.Visibility = Visibility.Hidden;
        }

        private void BtnModSettings_Click(object sender, RoutedEventArgs e)
        {
            gameSettingsPage.Visibility = Visibility.Hidden;
            modSettingsPage.Visibility = Visibility.Visible;
        }

        private void memorySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            memoryTB.Text = ((int)memorySlider.Value).ToString();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MassageWindow.Show(
                    "Вы уверены, что хотите сбросить все настройки?",
                    "Подтверждение сброса",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    string minecraftPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ".LauncherLS",
                    ".minecraft");
                    if (!Directory.Exists(minecraftPath))
                    {
                        Directory.CreateDirectory(minecraftPath);
                    }
                    directoryTB.Text = minecraftPath;
                    memorySlider.Value = 4000;
                    memoryTB.Text = "4000";
                    btnFullScreen.IsChecked = false;
                    widthTB.Text = "920";
                    heightTB.Text = "530";
                    UpdateDirectoryOnly(minecraftPath, false, 920, 530, 4000);
                    MassageWindow.Show("Настройки успешно сброшены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при сбросе настроек: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (widthTB.Text.Length < 0 || heightTB.Text.Length < 0) { textErrMes.Text = "введите разрешение !"; errorMessage.Visibility = Visibility.Visible; }
            else { UpdateDirectoryOnly(directoryTB.Text, btnFullScreen.IsChecked, Int32.Parse(widthTB.Text), Int32.Parse(heightTB.Text), Int32.Parse(memoryTB.Text)); MassageWindow.Show("Настройки успешно сохранены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);}
        }

        private void btnViewFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFolderDialog
            {
                Multiselect = false,
                Title = "Выберите папку"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                directoryTB.Text = openFileDialog.FolderName;
            }
        }

        private void text_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (errorMessage != null)
            {
                errorMessage.Visibility = Visibility.Hidden;
            }
        }
    }
}
