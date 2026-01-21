using CmlLib.Core.CommandParser;
using launch.Views.UserControls;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
using System.Windows.Threading;
using launch.Views.Other;
using System.Reflection.PortableExecutable;

namespace launch.Views
{
    public partial class Profile : Page
    {
        int id;
        int subscriptionUser;
        string nameImg;
        Dictionary<int, string[]> dataSub = new Dictionary<int, string[]>();
        string hostDB = "127.0.0.1";
        string databaseDB = "launcherDB";
        string userDB = "root";
        string passwordDB = "";
        MySqlConnection mysql_connection;

        string dateT;
        string dateF;
        public Profile()
        {
            InitializeComponent();
            try
            {
                string fileName = "userData.json";
                string jsonString = File.ReadAllText(fileName);
                wind userData = JsonSerializer.Deserialize<wind>(jsonString)!;

                id = userData.id;

                string Connect = "Database=" + databaseDB + ";Datasource=" + hostDB + ";User=" + userDB + ";Password=" + passwordDB;
                mysql_connection = new MySqlConnection(Connect);

                mysql_connection.Open();

                MySqlCommand mysql_query = mysql_connection.CreateCommand();
                mysql_query.CommandText = "SELECT personalInformation, telegram, lookingFriengs, nickName, skinFileName, login, subscription, dateFromSub, dateToSub, skinImage FROM users WHERE id = " + id + ";";
                MySqlDataReader mysql_result = mysql_query.ExecuteReader();
                mysql_result.Read();

                personalInformationTB.Text = mysql_result.GetString(0);
                telegramTB.Text = mysql_result.GetString(1);
                lookingFriengsTB.IsChecked = mysql_result.GetBoolean(2);
                nickNameTB.Text = mysql_result.GetString(3);
                mySkinTB.Text = mysql_result.GetString(4);
                nameImg = mysql_result.GetString(4);
                loginTB.Text = mysql_result.GetString(5);
                subscriptionUser = mysql_result.GetInt32(6);
                if (!mysql_result.IsDBNull(7) && !mysql_result.IsDBNull(8))
                {
                    dateT = mysql_result.GetDateTime(7).ToString(String.Format("dd.MM.yyyy"));
                    dateF = mysql_result.GetDateTime(8).ToString(String.Format("dd.MM.yyyy"));
                }
                if (!mysql_result.IsDBNull(9))
                {
                    loadSkinImage(mysql_result.GetStream(9));
                }

                mysql_result.Close();
                mysql_connection.Close();

                loadSub();

                BtnProfile.IsChecked = true;
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при загрузке данных пользователя: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void loadSkinImage(Stream stream)
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
                imgSkin.Source = image;
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при загрузке изображения скина: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void loadSub()
        {
            try
            {
                mysql_connection.Open();
                dataSub.Clear();
                Subcriptions.Children.Clear();
                MySqlCommand mysql_query_sub = mysql_connection.CreateCommand();
                mysql_query_sub.CommandText = "SELECT * FROM subscriptions ORDER BY price;";
                MySqlDataReader mysql_result_sub = mysql_query_sub.ExecuteReader();

                while (mysql_result_sub.Read())
                {
                    if (mysql_result_sub.GetString(1) != "нет подписки")
                    {
                        if (mysql_result_sub.GetInt32(0) == subscriptionUser)
                        {
                            UserControlSubscription userControlSubscription = new UserControlSubscription();
                            userControlSubscription.NameSub = mysql_result_sub.GetString(1);
                            userControlSubscription.Description = mysql_result_sub.GetString(2);
                            userControlSubscription.Price = mysql_result_sub.GetFloat(3).ToString() + " руб";
                            userControlSubscription.BtnText = "оформлено";
                            userControlSubscription.OpacityBtn = "0.8";
                            userControlSubscription.IsEnabledBtn = false;
                            userControlSubscription.Click += BtnSub_Click;
                            userControlSubscription.TagBtn = mysql_result_sub.GetInt32(0).ToString();
                            Subcriptions.Children.Add(userControlSubscription);
                            dataSub.Add(mysql_result_sub.GetInt32(0), [mysql_result_sub.GetString(2), mysql_result_sub.GetFloat(3).ToString() + " руб"]);
                        }
                        else
                        {
                            UserControlSubscription userControlSubscription = new UserControlSubscription();
                            userControlSubscription.NameSub = mysql_result_sub.GetString(1);
                            userControlSubscription.Description = mysql_result_sub.GetString(2);
                            userControlSubscription.Price = mysql_result_sub.GetFloat(3).ToString() + " руб";
                            if (!mysql_result_sub.GetBoolean(4))
                            {
                                userControlSubscription.OpacityBtn = "0.8";
                                userControlSubscription.IsEnabledBtn = false;
                                userControlSubscription.tooltip.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                userControlSubscription.IsEnabledBtn = true;
                            }
                            userControlSubscription.BtnText = "оформить";
                            userControlSubscription.Click += BtnSub_Click;
                            userControlSubscription.TagBtn = mysql_result_sub.GetInt32(0).ToString();
                            Subcriptions.Children.Add(userControlSubscription);
                            dataSub.Add(mysql_result_sub.GetInt32(0), [mysql_result_sub.GetString(2), mysql_result_sub.GetFloat(3).ToString() + " руб"]);
                        }
                    }
                }
                mysql_result_sub.Close();
                mysql_connection.Close();
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при загрузке подписок: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
}

        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            if (errorMessage != null)
            {
                errorMessage.Visibility = Visibility.Hidden;
            }
            profilePage.Visibility = Visibility.Visible;
            subscriptionPage.Visibility = Visibility.Hidden;
            subscriptionProcessPage.Visibility = Visibility.Hidden;
            passwordChangePage.Visibility = Visibility.Hidden;
            settingPage.Visibility = Visibility.Hidden;
        }

        private void BtnSubscription_Click(object sender, RoutedEventArgs e)
        {
            if (dateF != null || dateT != null) 
            {
                textErrMes.Text = $"подписка действует от {dateT} до {dateF}"; errorMessage.Visibility = Visibility.Visible;
            }
            else
            {
                textErrMes.Text = $"у вас нет активной подписки !"; errorMessage.Visibility = Visibility.Visible;
            }
            subscriptionPage.Visibility = Visibility.Visible;
            profilePage.Visibility = Visibility.Hidden;
            subscriptionProcessPage.Visibility = Visibility.Hidden;
            passwordChangePage.Visibility = Visibility.Hidden;
            settingPage.Visibility = Visibility.Hidden;
        }

        private void BtnSub_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenSubProcess(dataSub[int.Parse((string)(sender as Button).Tag)], int.Parse((string)(sender as Button).Tag));
            }
            catch (Exception ex) 
            {
                MassageWindow.Show("Ошибка при выборе подписки: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OpenSubProcess(string[] dataUC, int idSub)
        {
            try
            {
                subscriptionProcessPage.Visibility = Visibility.Visible;
                subscriptionPage.Visibility = Visibility.Hidden;
                if (errorMessage != null)
                {
                    errorMessage.Visibility = Visibility.Hidden;
                }

                dateFrom.Text = DateTime.Now.ToString(String.Format("dd.MM.yyyy"));
                dateTo.Text = DateTime.Now.AddMonths(1).ToString(String.Format("dd.MM.yyyy"));
                subscriptionDescription.Text = dataUC[0];
                AmountText.Text = dataUC[1];
                PayButton.Tag = idSub;
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при открытии процессе оформления подписки: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPasswordChange_Click(object sender, RoutedEventArgs e)
        {
            if (errorMessage != null)
            {
                errorMessage.Visibility = Visibility.Hidden;
            }
            passwordChangePage.Visibility = Visibility.Visible;
            profilePage.Visibility = Visibility.Hidden;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollviewer = sender as ScrollViewer;
            if (e.Delta > 0)
                scrollviewer.LineLeft();
            else
                scrollviewer.LineRight();
            e.Handled = true;
        }

        string fileName;

        private void btnUpdateSkin_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "PNG files (*.png)|*.png",
                Title = "Выберите изображение"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                fileName = openFileDialog.FileName;
                mySkinTB.Text = System.IO.Path.GetFileNameWithoutExtension(fileName) + ".png";
            }
        }

        private void btnUpdateUserInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (nameImg != mySkinTB.Text)
                {
                    if (nickNameTB.Text.Length == 0) { textErrMes.Text = "ник не должен быть пустым !"; errorMessage.Visibility = Visibility.Visible; }
                    else if (telegramTB.Text.Length == 13) { textErrMes.Text = "телеграм не должен быть пустым !"; errorMessage.Visibility = Visibility.Visible; }
                    else
                    {
                        mysql_connection.Open();
                        byte[] _selectedImageBytes = File.ReadAllBytes(fileName);
                        string query = "UPDATE users SET skinImage = @skinImage, skinFileName = @skinFileName, nickname = @nickname, personalInformation = @personalInformation, telegram = @telegram, lookingFriengs = @lookingFriengs WHERE id = @id;";
                        using (var command = new MySqlCommand(query, mysql_connection))
                        {
                            command.Parameters.Add("@skinImage", MySqlDbType.MediumBlob).Value = _selectedImageBytes;
                            command.Parameters.AddWithValue("@skinFileName", System.IO.Path.GetFileNameWithoutExtension(fileName) + ".png");
                            command.Parameters.AddWithValue("@nickname", nickNameTB.Text);
                            command.Parameters.AddWithValue("@personalInformation", personalInformationTB.Text);
                            command.Parameters.AddWithValue("@telegram", telegramTB.Text);
                            command.Parameters.AddWithValue("@lookingFriengs", lookingFriengsTB.IsChecked.GetHashCode());
                            command.Parameters.AddWithValue("@id", id);
                            command.ExecuteNonQuery();
                        }
                        mysql_connection.Close();
                        nameImg = mySkinTB.Text;
                        MassageWindow.Show("Информация успешно обновлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    if (nickNameTB.Text.Length == 0) { textErrMes.Text = "ник не должен быть пустым !"; errorMessage.Visibility = Visibility.Visible; }
                    else if (telegramTB.Text.Length == 13) { textErrMes.Text = "телеграм не должен быть пустым !"; errorMessage.Visibility = Visibility.Visible; }
                    else
                    {
                        mysql_connection.Open();
                        string query = "UPDATE users SET nickname = @nickname, personalInformation = @personalInformation, telegram = @telegram, lookingFriengs = @lookingFriengs WHERE id = @id;";
                        using (var command = new MySqlCommand(query, mysql_connection))
                        {
                            command.Parameters.AddWithValue("@nickname", nickNameTB.Text);
                            command.Parameters.AddWithValue("@personalInformation", personalInformationTB.Text);
                            command.Parameters.AddWithValue("@telegram", telegramTB.Text);
                            command.Parameters.AddWithValue("@lookingFriengs", lookingFriengsTB.IsChecked.GetHashCode());
                            command.Parameters.AddWithValue("@id", id);
                            command.ExecuteNonQuery();
                        }
                        mysql_connection.Close();
                        MassageWindow.Show("Информация успешно обновлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при обновлении информации: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Text_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (errorMessage != null)
            {
                errorMessage.Visibility = Visibility.Hidden;
            }
        }

        private void btnUpdatePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (newPasswordTB.Text.Length < 4) { textErrMes.Text = "минимум 4 символа в пароле !"; errorMessage.Visibility = Visibility.Visible; }
                else if (!newPasswordTB.Text.Any(char.IsDigit)) { textErrMes.Text = "пароль должен содержать цифры !"; errorMessage.Visibility = Visibility.Visible; }
                else if (newPasswordTB.Text != newChangePasswordTB.Text) { textErrMes.Text = "пароли не совпадают !"; errorMessage.Visibility = Visibility.Visible; }
                else
                {
                    mysql_connection.Open();
                    MySqlCommand mysql_query = mysql_connection.CreateCommand();
                    mysql_query.CommandText = "UPDATE users SET password = '" + newChangePasswordTB.Text + "' WHERE id = " + id + ";";
                    mysql_query.ExecuteNonQuery();
                    mysql_connection.Close();

                    string json = File.ReadAllText("userData.json");
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        var updatedData = new
                        {
                            id = root.GetProperty("id").GetInt32(),
                            login = root.GetProperty("login").GetString(),
                            password = newChangePasswordTB.Text,
                            subscription = root.GetProperty("subscription").GetInt32(),
                            nickname = root.GetProperty("nickname").GetString(),
                            directory = root.GetProperty("directory").GetString(),
                            fullScreen = root.GetProperty("fullScreen").GetBoolean(),
                            width = root.GetProperty("width").GetInt32(),
                            height = root.GetProperty("height").GetInt32(),
                            ram = root.GetProperty("ram").GetInt32()
                        };
                        string updatedJson = JsonSerializer.Serialize(updatedData);
                        File.WriteAllText("userData.json", updatedJson);
                    }

                    newPasswordTB.Text = "";
                    newChangePasswordTB.Text = "";

                    passwordChangePage.Visibility = Visibility.Hidden;
                    profilePage.Visibility = Visibility.Visible;
                    MassageWindow.Show("Пароль успешно изменен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при смене пароля: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (errorMessage != null)
            {
                errorMessage.Visibility = Visibility.Hidden;
            }
            passwordChangePage.Visibility = Visibility.Hidden;
            profilePage.Visibility = Visibility.Visible;
        }

        private void newTelegramTB_KeyDown(object sender, KeyEventArgs e)
        {
            if (telegramTB.SelectionStart <= 13 &&
        (e.Key == Key.Back || e.Key == Key.Delete ||
         (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)))
            {
                e.Handled = true;
            }
        }

        private void newTelegramTB_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (telegramTB.CaretIndex < 13)
            {
                e.Handled = true;
            }
        }

        private async void btnDeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var confirmResult = MassageWindow.Show(
                    "Вы уверены, что хотите удалить аккаунт? Это действие нельзя отменить!",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmResult != MessageBoxResult.Yes)
                    return;

                bool success = await DeleteUserAccount(id);
                if (success)
                {
                    MassageWindow.Show("Аккаунт успешно удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    ClearUserData();
                    NavigateToLoginWindow();
                }
                else
                {
                    MassageWindow.Show("Не удалось удалить аккаунт", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при удалении аккаунта: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<bool> DeleteUserAccount(int userId)
        {
            const string connectionString = "Database=launcherDB;Datasource=127.0.0.1;User=root;Password=";

            try
            {
                await using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                await using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    await using var cmdErrorMessages = connection.CreateCommand();
                    cmdErrorMessages.CommandText = "DELETE FROM errormessages WHERE user = @userId";
                    cmdErrorMessages.Parameters.AddWithValue("@userId", userId);
                    await cmdErrorMessages.ExecuteNonQueryAsync();

                    await using var cmdUserRatings = connection.CreateCommand();
                    cmdUserRatings.CommandText = "DELETE FROM userratings WHERE user = @userId";
                    cmdUserRatings.Parameters.AddWithValue("@userId", userId);
                    await cmdUserRatings.ExecuteNonQueryAsync();

                    await using var cmdFriendRequests = connection.CreateCommand();
                    cmdFriendRequests.CommandText = "DELETE FROM friendrequests WHERE sender = @userId OR recipient = @userId";
                    cmdFriendRequests.Parameters.AddWithValue("@userId", userId);
                    await cmdFriendRequests.ExecuteNonQueryAsync();

                    await using var cmdFriends = connection.CreateCommand();
                    cmdFriends.CommandText = "DELETE FROM friends WHERE user1 = @userId OR user2 = @userId";
                    cmdFriends.Parameters.AddWithValue("@userId", userId);
                    await cmdFriends.ExecuteNonQueryAsync();

                    await using var cmdUser = connection.CreateCommand();
                    cmdUser.CommandText = "DELETE FROM users WHERE id = @userId";
                    cmdUser.Parameters.AddWithValue("@userId", userId);
                    int rowsAffected = await cmdUser.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();

                    return rowsAffected > 0;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                MassageWindow.Show($"Ошибка при удалении из базы данных: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void ClearUserData()
        {
            try
            {
                string userDataPath = "userData.json";
                if (File.Exists(userDataPath))
                {
                    File.Delete(userDataPath);
                }

                if (Application.Current.Properties.Contains("UserData"))
                {
                    Application.Current.Properties.Remove("UserData");
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при очистке данных: " + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void NavigateToLoginWindow()
        {
            var loginWindow = new EntryWindow();
            loginWindow.Show();

            var currentWindow = Window.GetWindow(this);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                currentWindow?.Close();
            }), DispatcherPriority.Background);
        }

        private void btnChangeAccount_Click(object sender, RoutedEventArgs e)
        {
            var result = MassageWindow.Show(
                "Вы уверены, что хотите сменить аккаунт?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                EntryWindow admin = new EntryWindow();
                var window = Window.GetWindow(this);
                admin.Show();
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    window?.Close();
                }), DispatcherPriority.Background);
            }
        }

        private void BtnSetting_Click(object sender, RoutedEventArgs e)
        {
            if (errorMessage != null)
            {
                errorMessage.Visibility = Visibility.Hidden;
            }
            subscriptionPage.Visibility = Visibility.Hidden;
            profilePage.Visibility = Visibility.Hidden;
            subscriptionProcessPage.Visibility = Visibility.Hidden;
            passwordChangePage.Visibility = Visibility.Hidden;
            settingPage.Visibility = Visibility.Visible;
        }

        private void PayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateCardData())
            {
                return;
            }

            SimulatePaymentProcessing();

            UpdateSubscriptionInDatabase();

            loadSub();
            EventService.RaiseSubscriptionChanged();
            if (errorMessage != null)
            {
                errorMessage.Visibility = Visibility.Hidden;
            }
            subscriptionPage.Visibility = Visibility.Visible;
            profilePage.Visibility = Visibility.Hidden;
            subscriptionProcessPage.Visibility = Visibility.Hidden;
            passwordChangePage.Visibility = Visibility.Hidden;
            settingPage.Visibility = Visibility.Hidden;
        }

        private bool ValidateCardData()
        {
            if (string.IsNullOrWhiteSpace(CardNumberBox.Text) || CardNumberBox.Text.Length != 16)
            {
                textErrMes.Text = "Введите корректный номер карты (16 цифр)!"; errorMessage.Visibility = Visibility.Visible;
                return false;
            }
            else if (string.IsNullOrWhiteSpace(ExpiryBox.Text) || !ExpiryBox.Text.Contains("/"))
            {
                textErrMes.Text = "Введите корректную дату (мм/гг)!"; errorMessage.Visibility = Visibility.Visible;
                return false;
            }
            else if (string.IsNullOrWhiteSpace(CvvBox.Text) || CvvBox.Text.Length != 3)
            {
                textErrMes.Text = "Введите корректный CVV код (3 цифры)"; errorMessage.Visibility = Visibility.Visible;
                return false;
            }

            CardNumberBox.BorderBrush = ExpiryBox.BorderBrush = CvvBox.BorderBrush = Brushes.White;
            if (errorMessage != null)
            {
                errorMessage.Visibility = Visibility.Hidden;
            }
            return true;
        }

        private void SimulatePaymentProcessing()
        {
            PayButton.IsEnabled = false;
            PayButton.Content = "Обработка платежа...";

            Task.Delay(2000).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    PayButton.Content = "оплатить";
                    PayButton.IsEnabled = true;
                });
            });
        }

        private void UpdateSubscriptionInDatabase()
        {
            try
            {
                mysql_connection.Open();
                string query = "UPDATE users SET subscription = @subscription, dateFromSub = @dateFromSub, dateToSub = @dateToSub WHERE id = @id;";
                using (var command = new MySqlCommand(query, mysql_connection))
                {
                    command.Parameters.AddWithValue("@subscription", PayButton.Tag);
                    command.Parameters.AddWithValue("@dateFromSub", DateTime.Parse(dateFrom.Text).ToString(String.Format("yyyy-MM-dd")));
                    command.Parameters.AddWithValue("@dateToSub", DateTime.Parse(dateTo.Text).ToString(String.Format("yyyy-MM-dd")));
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                }
                mysql_connection.Close();

                string json = File.ReadAllText("userData.json");
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    var updatedData = new
                    {
                        id = root.GetProperty("id").GetInt32(),
                        login = root.GetProperty("login").GetString(),
                        password = root.GetProperty("password").GetString(),
                        subscription = PayButton.Tag,
                        nickname = root.GetProperty("nickname").GetString(),
                        directory = root.GetProperty("directory").GetString(),
                        fullScreen = root.GetProperty("fullScreen").GetBoolean(),
                        width = root.GetProperty("width").GetInt32(),
                        height = root.GetProperty("height").GetInt32(),
                        ram = root.GetProperty("ram").GetInt32()
                    };
                    string updatedJson = JsonSerializer.Serialize(updatedData);
                    File.WriteAllText("userData.json", updatedJson);
                }
                MassageWindow.Show("Оплата прошла успешно! Подписка активирована.",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при обновлении подписки: " + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
