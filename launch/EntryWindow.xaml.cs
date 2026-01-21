using CmlLib.Core;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace launch
{
    public class wind
    {
        public int id { get; set; }
        public string login { get; set; }
        public string password { get; set; }
        public int subscription { get; set; }
        public string nickname { get; set; }
        public string directory { get; set; }
        public bool fullScreen { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int ram { get; set; }
    }
    public partial class EntryWindow : Window
    {
        string hostDB = "127.0.0.1"; // Имя хоста
        string databaseDB = "launcherDB"; // Имя базы данных
        string userDB = "root"; // Имя пользователя
        string passwordDB = ""; // Пароль пользователя

        MySqlConnection mysql_connection;
        DataSet dataSet = new DataSet();
        DataRowCollection tableRow;
        const string fileName = "userData.json";

        public EntryWindow()
        {
            InitializeComponent();

            try
            {
                if (!File.Exists(fileName))
                {
                    string minecraftPath = System.IO.Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            ".LauncherLS",
                            ".minecraft");
                    if (!Directory.Exists(minecraftPath))
                    {
                        Directory.CreateDirectory(minecraftPath);
                    }
                    var userData = new wind
                    {
                        id = -1,
                        login = "",
                        password = "",
                        subscription = -1,
                        nickname = "",
                        directory = minecraftPath,
                        width = 925,
                        height = 530,
                        fullScreen = false,
                        ram = 4000
                    };
                    string jsonString = JsonSerializer.Serialize(userData);
                    File.WriteAllText(fileName, jsonString);
                }
                else
                {
                    string jsonString = File.ReadAllText(fileName);
                    wind userData = JsonSerializer.Deserialize<wind>(jsonString)!;

                    loginTB.Text = userData.login;
                    passwordTB.Password = userData.password;

                }

                string Connect = "Database=" + databaseDB + ";Datasource=" + hostDB + ";User=" + userDB + ";Password=" + passwordDB;
                mysql_connection = new MySqlConnection(Connect);

                MySqlDataAdapter adapter = new MySqlDataAdapter();
                adapter.SelectCommand = new MySqlCommand("SELECT id, login, password, subscription FROM users;", mysql_connection);
                adapter.Fill(dataSet);
                tableRow = dataSet.Tables[0].Rows;
            }
            catch (Exception ex) {// MessageBox.Show("произошла ошибка !" + ex.Message);
                                  }
        }
        private void Clouze_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Collapse_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) { DragMove(); }
        }

        private void loginTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (errorMessage != null)
            {
                errorMessage.Visibility = Visibility.Hidden;
            }
        }

        private void passwordTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (errorMessage != null)
            {
                errorMessage.Visibility = Visibility.Hidden;
            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (loginTB.Text == "admin" && passwordTB.Password == "0000")
            {
                AdminWindow admin = new AdminWindow();
                admin.Show();
                this.Close();
            }
            else
            {
                mysql_connection.Open();
                MySqlCommand mysql_query = mysql_connection.CreateCommand();
                mysql_query.CommandText = "SELECT id, login, password, subscription, nickname, dateToSub FROM users WHERE login = '" + loginTB.Text + "' AND password = '" + passwordTB.Password + "';";
                MySqlDataReader mysql_result = mysql_query.ExecuteReader();
                
                if (mysql_result.HasRows)
                {
                    mysql_result.Read();
                    int id = mysql_result.GetInt32(0);

                    if (!mysql_result.IsDBNull(5)) { 
                        if (mysql_result.GetDateTime(5) < DateTime.Now)
                        {
                            MessageBox.Show($"действие вашей подписки закончилось !");
                            mysql_result.Close();

                            string query = "UPDATE users SET subscription = @subscription, dateFromSub = @dateFromSub, dateToSub = @dateToSub WHERE id = @id;";
                            using (var command = new MySqlCommand(query, mysql_connection))
                            {
                                command.Parameters.AddWithValue("@subscription", 0);
                                command.Parameters.AddWithValue("@dateFromSub", null);
                                command.Parameters.AddWithValue("@dateToSub", null);
                                command.Parameters.AddWithValue("@id", id);
                                command.ExecuteNonQuery();
                            }

                            mysql_query.CommandText = "SELECT id, login, password, subscription, nickname, dateToSub FROM users WHERE id = " + id + ";";
                            mysql_result = mysql_query.ExecuteReader();
                            mysql_result.Read();
                        }
                    }

                    string json = File.ReadAllText(fileName);
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        var updatedData = new
                        {
                            id = mysql_result.GetInt32(0),
                            login = "",
                            password = "",
                            subscription = mysql_result.GetInt32(3),
                            nickname = mysql_result.GetString(4),
                            directory = root.GetProperty("directory").GetString(),
                            fullScreen = root.GetProperty("fullScreen").GetBoolean(),
                            width = root.GetProperty("width").GetInt32(),
                            height = root.GetProperty("height").GetInt32(),
                            ram = root.GetProperty("ram").GetInt32()
                        };
                        if ((bool)btnRememberMe.IsChecked)
                        {
                            updatedData = new
                            {
                                id = mysql_result.GetInt32(0),
                                login = mysql_result.GetString(1),
                                password = mysql_result.GetString(2),
                                subscription = mysql_result.GetInt32(3),
                                nickname = mysql_result.GetString(4),
                                directory = root.GetProperty("directory").GetString(),
                                fullScreen = root.GetProperty("fullScreen").GetBoolean(),
                                width = root.GetProperty("width").GetInt32(),
                                height = root.GetProperty("height").GetInt32(),
                                ram = root.GetProperty("ram").GetInt32()
                            };
                        }
                        string updatedJson = JsonSerializer.Serialize(updatedData);
                        File.WriteAllText(fileName, updatedJson);
                    }

                    MainWindow window = new MainWindow(mysql_result.GetInt32(0));
                    window.Show();

                    mysql_result.Close();
                    this.Close();
                }
                else
                {
                    errorMessage.Visibility = Visibility.Visible;
                }
                mysql_result.Close();
                mysql_connection.Close();
            }
        }

        private void btnRegistration_Click(object sender, RoutedEventArgs e)
        {
            registrationPage1.Visibility = Visibility.Visible;
            loginInPage.Visibility = Visibility.Hidden;
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool log = true;
                //bool nick = true;
                foreach (DataRow dataRow in tableRow)
                {
                    if (dataRow[1] == newLoginTB.Text)
                    {
                        log = false;
                    }
                    //if (dataRow[0] == newNicknameTB.Text)
                    //{
                    //    nick = false;
                    //}
                }

                if (newLoginTB.Text.Length == 0) { textErrMes.Text = "введите логин !"; errorMessage.Visibility = Visibility.Visible; }
                else if (!log) { textErrMes.Text = "данный логин уже существует !"; errorMessage.Visibility = Visibility.Visible; }
                else if (newPasswordTB.Password.Length < 4) { textErrMes.Text = "минимум 4 символа в пароле !"; errorMessage.Visibility = Visibility.Visible; }
                else if (!newPasswordTB.Password.Any(char.IsDigit)) { textErrMes.Text = "пароль должен содержать цифры !"; errorMessage.Visibility = Visibility.Visible; }
                else if (newPasswordTB.Password != newPasswordTwoTB.Password) { textErrMes.Text = "пароли не совпадают !"; errorMessage.Visibility = Visibility.Visible; }
                else if (newNicknameTB.Text.Length == 0) { textErrMes.Text = "введите никнейм !"; errorMessage.Visibility = Visibility.Visible; }
                //else if (!nick) { textErrMes.Text = "данный ник уже существует !"; }
                else
                {
                    registrationPage2.Visibility = Visibility.Visible;
                    registrationPage1.Visibility = Visibility.Hidden;
                }
            }
            catch { MessageBox.Show("произошла ошибка !"); }
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (newTelegramTB.Text.Length == 13) { textErrMes.Text = "введите свой телеграм !"; errorMessage.Visibility = Visibility.Visible; }
                else if (btnConsentPersonalData.IsChecked == false) { textErrMes.Text = "без согласия регистрация невозможна !"; errorMessage.Visibility = Visibility.Visible; }
                else
                {
                    if (errorMessage != null)
                    {
                        errorMessage.Visibility = Visibility.Hidden;
                    }

                    mysql_connection.Open();
                    MySqlCommand mysql_query = mysql_connection.CreateCommand();
                    mysql_query.CommandText = @"INSERT INTO users (login, password, nickname, personalInformation, telegram, lookingFriengs) VALUES ('" + newLoginTB.Text + "', '" + newPasswordTB.Password + "', '" + newNicknameTB.Text + "', '" + newPersonalInformationTB.Text + "', '" + newTelegramTB.Text + "', " + btnLookingFriengs.IsChecked.GetHashCode() + ");";
                    mysql_query.ExecuteNonQuery();
                    mysql_connection.Close();

                    loginTB.Text = newLoginTB.Text;
                    passwordTB.Password = newPasswordTB.Password;
                    registrationPage2.Visibility = Visibility.Hidden;
                    loginInPage.Visibility = Visibility.Visible;
                }
            }
            catch { MessageBox.Show("произошла ошибка !"); }
        }

        private void btnBack1_Click(object sender, RoutedEventArgs e)
        {
            if (errorMessage != null)
            {
                errorMessage.Visibility = Visibility.Hidden;
            }
            registrationPage1.Visibility = Visibility.Hidden;
            loginInPage.Visibility = Visibility.Visible;
        }

        private void btnBack2_Click(object sender, RoutedEventArgs e)
        {
            if (errorMessage != null)
            {
                errorMessage.Visibility = Visibility.Hidden;
            }
            registrationPage1.Visibility = Visibility.Visible;
            registrationPage2.Visibility = Visibility.Hidden;
        }

        private void viewHTTP(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.minecraft.net/ru-ru/terms/r2") { UseShellExecute = true });
        }

        private void newTelegramTB_KeyDown(object sender, KeyEventArgs e)
        {
            if (newTelegramTB.SelectionStart <= 13 &&
        (e.Key == Key.Back || e.Key == Key.Delete ||
         (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)))
            {
                e.Handled = true;
            }
        }

        private void newTelegramTB_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (newTelegramTB.CaretIndex < 13)
            {
                e.Handled = true;
            }
        }
    }
}
