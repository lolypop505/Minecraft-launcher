using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace launch.Views.UserControls
{
    public partial class UserControlNewSubscription : UserControl
    {
        string hostDB = "127.0.0.1"; // Имя хоста
        string databaseDB = "launcherDB"; // Имя базы данных
        string userDB = "root"; // Имя пользователя
        string passwordDB = ""; // Пароль пользователя

        MySqlConnection mysql_connection;
        public UserControlNewSubscription()
        {
            InitializeComponent();
            DataContext = this;
            string Connect = "Database=" + databaseDB + ";Datasource=" + hostDB + ";User=" + userDB + ";Password=" + passwordDB;
            mysql_connection = new MySqlConnection(Connect);
        }

        public event RoutedEventHandler ClickSave
        {
            add { btnSave.AddHandler(ButtonBase.ClickEvent, value); }
            remove { btnSave.AddHandler(ButtonBase.ClickEvent, value); }
        }

        public event RoutedEventHandler ClickDel
        {
            add { btnDelete.AddHandler(ButtonBase.ClickEvent, value); }
            remove { btnDelete.AddHandler(ButtonBase.ClickEvent, value); }
        }

        public string NameSub { get; set; }
        public string Description { get; set; }
        public string Price { get; set; }
        public string TagBtnDel { get; set; }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            mysql_connection.Open();
            string query = "UPDATE subscriptions SET name = @name, description = @bescription, price = @price WHERE id = @id;";
            using (var command = new MySqlCommand(query, mysql_connection))
            {
                command.Parameters.AddWithValue("@name", nameTB.Text);
                command.Parameters.AddWithValue("@bescription", descTB.Text);
                command.Parameters.AddWithValue("@price", Convert.ToDouble(priceTB.Text));
                command.Parameters.AddWithValue("@id", btnDelete.Tag);
                command.ExecuteNonQuery();
            }
            mysql_connection.Close();
        }

        private void btnIaActive_Click(object sender, RoutedEventArgs e)
        {
            if(btnIaActive.Content == "не активна")
            {
                mysql_connection.Open();
                string query = "UPDATE subscriptions SET isActive = 1 WHERE id = @id;";
                using (var command = new MySqlCommand(query, mysql_connection))
                {
                    command.Parameters.AddWithValue("@id", btnDelete.Tag);
                    command.ExecuteNonQuery();
                }
                mysql_connection.Close();

                btnIaActive.Content = "активна";
            }
            else
            {
                mysql_connection.Open();
                string query = "UPDATE subscriptions SET isActive = 0 WHERE id = @id;";
                using (var command = new MySqlCommand(query, mysql_connection))
                {
                    command.Parameters.AddWithValue("@id", btnDelete.Tag);
                    command.ExecuteNonQuery();
                }
                mysql_connection.Close();

                btnIaActive.Content = "не активна";
            }
        }
    }
}
