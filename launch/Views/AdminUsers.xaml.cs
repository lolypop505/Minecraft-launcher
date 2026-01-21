using launch.Views.UserControls;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public partial class AdminUsers : Page
    {
        string hostDB = "127.0.0.1"; // Имя хоста
        string databaseDB = "launcherDB"; // Имя базы данных
        string userDB = "root"; // Имя пользователя
        string passwordDB = ""; // Пароль пользователя
        MySqlConnection mysql_connection;
        public AdminUsers()
        {
            InitializeComponent();
            string Connect = "Database=" + databaseDB + ";Datasource=" + hostDB + ";User=" + userDB + ";Password=" + passwordDB;
            mysql_connection = new MySqlConnection(Connect);

            mysql_connection.Open();
            MySqlCommand mysql_query = mysql_connection.CreateCommand();
            mysql_query.CommandText = "SELECT id, nickname, personalInformation, telegram FROM users;";
            MySqlDataReader mysql_result = mysql_query.ExecuteReader();

            while (mysql_result.Read())
            {
                UserControlMyFriends userControlFriendsSearch = new UserControlMyFriends();
                userControlFriendsSearch.Text1 = mysql_result.GetString(1);
                userControlFriendsSearch.Text2 = mysql_result.GetString(2);
                userControlFriendsSearch.ClickTg += BtnViewTGFriends_Click;
                userControlFriendsSearch.btnViewTGFriend.HorizontalAlignment = HorizontalAlignment.Right;
                userControlFriendsSearch.btnDeleteFriend.Visibility = Visibility.Hidden;
                userControlFriendsSearch.tagBtnView = mysql_result.GetString(3);
                usersScroll.Children.Add(userControlFriendsSearch);
            }
            mysql_result.Close();
            mysql_connection.Close();
        }

        private void BtnViewTGFriends_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo((string)(sender as Button).Tag) { UseShellExecute = true });
            }
            catch { MessageBox.Show("произошла ошибка !"); }
        }
    }
}
