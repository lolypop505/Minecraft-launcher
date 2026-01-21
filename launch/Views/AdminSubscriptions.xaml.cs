using launch.Views.Other;
using launch.Views.UserControls;
using MySql.Data.MySqlClient;
using System;
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

namespace launch.Views
{
    public partial class AdminSubscriptions : Page
    {
        string hostDB = "127.0.0.1"; // Имя хоста
        string databaseDB = "launcherDB"; // Имя базы данных
        string userDB = "root"; // Имя пользователя
        string passwordDB = ""; // Пароль пользователя

        MySqlConnection mysql_connection;
        public AdminSubscriptions()
        {
            InitializeComponent();
            LoadSub();
        }

        public void LoadSub()
        {
            try
            {
                SPSubscriptions.Children.Clear();
                string Connect = "Database=" + databaseDB + ";Datasource=" + hostDB + ";User=" + userDB + ";Password=" + passwordDB;
                mysql_connection = new MySqlConnection(Connect);

                mysql_connection.Open();

                MySqlCommand mysql_query = mysql_connection.CreateCommand();
                mysql_query.CommandText = "SELECT * FROM subscriptions WHERE id != 0 ORDER BY price;";
                MySqlDataReader mysql_result = mysql_query.ExecuteReader();

                while (mysql_result.Read())
                {
                    UserControlNewSubscription userControlFriendsSearch = new UserControlNewSubscription();
                    userControlFriendsSearch.NameSub = mysql_result.GetString(1);
                    userControlFriendsSearch.Description = mysql_result.GetString(2);
                    userControlFriendsSearch.Price = mysql_result.GetFloat(3).ToString();
                    userControlFriendsSearch.ClickDel += Delete_Click;
                    userControlFriendsSearch.TagBtnDel = mysql_result.GetInt32(0).ToString();
                    if (!mysql_result.GetBoolean(4))
                    {
                        userControlFriendsSearch.btnIaActive.Content = "не активна";
                    }
                    SPSubscriptions.Children.Add(userControlFriendsSearch);
                }
                mysql_connection.Close();
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка загрузки подписок: " + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public async void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                mysql_connection.Open();
                using (var checkCommand = new MySqlCommand("SELECT COUNT(*) FROM users WHERE subscription = @subscriptionId;", mysql_connection))
                {
                    checkCommand.Parameters.AddWithValue("@subscriptionId", (sender as Button).Tag);
                    int userCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (userCount > 0)
                    {
                        var confirmResult = MassageWindow.Show(
                            $"Невозможно удалить подписку, так как она активна у {userCount} пользователей. Хотите запретить ее приобретение? (Советуем после этого создать новость для пользователей о прекращении действия данной подписки)",
                            "Предупреждение",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);
                        if (confirmResult == MessageBoxResult.Yes)
                        {
                            using (var deactivateCommand = new MySqlCommand(
                                "UPDATE subscriptions SET isActive = 0 WHERE id = @id;",
                                mysql_connection))
                            {
                                deactivateCommand.Parameters.AddWithValue("@id", (sender as Button).Tag);
                                deactivateCommand.ExecuteNonQuery();
                            }

                            MassageWindow.Show(
                                "Подписка деактивирована успешно!",
                                "Информация",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                            LoadSub();
                        }
                        return;
                    }
                    mysql_connection.Close();
                }

                var result = MassageWindow.Show(
                            "Вы уверены, что хотите удалить эту подписку?",
                            "Подтверждение",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    mysql_connection.Open();
                    MySqlCommand mysql_query_assa = mysql_connection.CreateCommand();
                    mysql_query_assa.CommandText = "UPDATE assembly SET requiredSubscription = 0 WHERE requiredSubscription = " + (sender as Button).Tag + ";";
                    int updatedAssemblies = mysql_query_assa.ExecuteNonQuery();
                   
                    MySqlCommand mysql_query_sub = mysql_connection.CreateCommand();
                    mysql_query_sub.CommandText = "DELETE FROM subscriptions WHERE id = " + (sender as Button).Tag + ";";
                    mysql_query_sub.ExecuteNonQuery();
                    mysql_connection.Close();

                    EventService.RaiseSubscriptionListChanged();
                    EventService.RaiseNewsChanged();
                    LoadSub();
                    MassageWindow.Show(
                        $"Подписка удалена.\nОбновлено сборок: {updatedAssemblies}",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при удалении или деактивации подписки: " + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ClearAddForm()
        {
            newNameTB.Text = string.Empty;
            newDescTB.Text = string.Empty;
            newPriceTB.Text = string.Empty;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                mysql_connection.Open();
                string query = "INSERT INTO subscriptions (name, description, price) VALUES (@name, @bescription, @price);";
                using (var command = new MySqlCommand(query, mysql_connection))
                {
                    command.Parameters.AddWithValue("@name", newNameTB.Text);
                    command.Parameters.AddWithValue("@bescription", newDescTB.Text);
                    command.Parameters.AddWithValue("@price", newPriceTB.Text);
                    command.ExecuteNonQuery();
                }
                mysql_connection.Close();
                ClearAddForm();
                EventService.RaiseSubscriptionListChanged();
                EventService.RaiseNewsChanged();
                LoadSub();
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при добавлении подписки: " + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void btnViewSub_Click(object sender, RoutedEventArgs e)
        {
            viewSubPage.Visibility = Visibility.Visible;
            addSubPage.Visibility = Visibility.Hidden;
        }

        private void btnAddSub_Click(object sender, RoutedEventArgs e)
        {
            viewSubPage.Visibility = Visibility.Hidden;
            addSubPage.Visibility = Visibility.Visible;
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

        private void newNameTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            NameSub.Text = string.IsNullOrEmpty(newNameTB.Text)
                ? "название"
                : newNameTB.Text;
        }

        private void newDescTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            DescriptionSub.Text = string.IsNullOrEmpty(newDescTB.Text)
                ? "описание"
                : newDescTB.Text;
        }

        private void newPriceTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            PriceSub.Text = string.IsNullOrEmpty(newPriceTB.Text)
                ? "цена"
                : newPriceTB.Text;
        }
    }
}
