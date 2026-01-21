using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace launch
{
    public partial class AdminWindow : Window
    {
        private readonly string connectionString;
        private MySqlConnection mysqlConnection;

        public AdminWindow()
        {
            InitializeComponent();
            connectionString = "Database=launcherDB;Datasource=127.0.0.1;User=root;Password=";
            check();
        }

        public void check()
        {
            try
            {
                using (mysqlConnection = new MySqlConnection(connectionString))
                {
                    mysqlConnection.Open();

                    // Сначала получаем всех пользователей с истекшими подписками
                    string selectQuery = @"SELECT id, login, subscription FROM users 
                                        WHERE dateToSub IS NOT NULL 
                                        AND dateToSub < NOW() 
                                        AND subscription != 0;";

                    using (var command = new MySqlCommand(selectQuery, mysqlConnection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int userId = reader.GetInt32("id");
                            string login = reader.GetString("login");
                            int currentSubscription = reader.GetInt32("subscription");
                        }
                    }

                    // Обновляем всех пользователей с истекшими подписками
                    string updateQuery = @"UPDATE users 
                                        SET subscription = 0
                                        WHERE dateToSub IS NOT NULL 
                                        AND dateToSub < NOW() 
                                        AND subscription != 0;";

                    using (var command = new MySqlCommand(updateQuery, mysqlConnection))
                    {
                        int affectedRows = command.ExecuteNonQuery();
                        if (affectedRows > 0)
                        {
                            MessageBox.Show($"Обнаружено {affectedRows} истекших подписок. Статус обновлен.",
                                "Информация",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке подписок: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) { DragMove(); }
        }


        private void Clouze_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Collapse_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
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
}
