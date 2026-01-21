using launch.Views.Other;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace launch.Views
{
    public partial class AdminHome : Page
    {
        string hostDB = "127.0.0.1";
        string databaseDB = "launcherDB"; 
        string userDB = "root"; 
        string passwordDB = ""; 

        MySqlConnection mysql_connection;

        Dictionary<int, int[]> arrayNews = new Dictionary<int, int[]>();

        public AdminHome()
        {
            InitializeComponent();
            EventService.NewsChanged += OnNewsChanged;
            LoadPage();
        }

        private void OnNewsChanged(object sender, EventArgs e)
        {
            LoadPage();
        }

        public void LoadPage()
        {
            try
            {
                string Connect = "Database=" + databaseDB + ";Datasource=" + hostDB + ";User=" + userDB + ";Password=" + passwordDB;
                mysql_connection = new MySqlConnection(Connect);

                mysql_connection.Open();

                MySqlCommand mysql_query_users = new MySqlCommand("SELECT COUNT(*) FROM users;", mysql_connection);
                usersCount.Text = mysql_query_users.ExecuteScalar().ToString();

                MySqlCommand mysql_query_news = new MySqlCommand("SELECT COUNT(*) FROM news;", mysql_connection);
                newsCount.Text = mysql_query_news.ExecuteScalar().ToString();

                MySqlCommand mysql_query_assembly = new MySqlCommand("SELECT COUNT(*) FROM assembly;", mysql_connection);
                assemblyCount.Text = mysql_query_assembly.ExecuteScalar().ToString();


                MySqlCommand mysql_query_news_like = mysql_connection.CreateCommand();
                mysql_query_news_like.CommandText = @"SELECT n.name, COUNT(*) as like_count
                FROM news n
                JOIN userratings r ON n.id = r.news
                WHERE r.liked = 1
                GROUP BY n.id
                ORDER BY like_count DESC
                LIMIT 1;";
                MySqlDataReader mysql_result_news_like = mysql_query_news_like.ExecuteReader();
                mysql_result_news_like.Read();
                nameNewsLiked.Text = mysql_result_news_like["name"].ToString();
                likeCount.Text = WordDeclensionHelper.GetLikesText(Convert.ToInt32(mysql_result_news_like["like_count"]));
                mysql_result_news_like.Close();

                MySqlCommand mysql_query_news_dis = mysql_connection.CreateCommand();
                mysql_query_news_dis.CommandText = @"
                SELECT n.name, COUNT(*) as dislike_count
                FROM news n
                JOIN userratings r ON n.id = r.news
                WHERE r.liked = 0
                GROUP BY n.id
                ORDER BY dislike_count DESC
                LIMIT 1";
                MySqlDataReader mysql_result_news_dis = mysql_query_news_dis.ExecuteReader();
                mysql_result_news_dis.Read();
                nameNewsDisliked.Text = mysql_result_news_dis["name"].ToString();
                dislikeCount.Text = WordDeclensionHelper.GetDislikesText(Convert.ToInt32(mysql_result_news_dis["dislike_count"]));
                mysql_result_news_dis.Close();

                MySqlCommand mysql_query_sub = mysql_connection.CreateCommand();
                mysql_query_sub.CommandText = @"
                SELECT s.name, COUNT(*) as usage_count
                FROM subscriptions s
                JOIN users u ON s.id = u.subscription
                WHERE s.id != 0
                GROUP BY s.id
                ORDER BY usage_count DESC
                LIMIT 1";
                MySqlDataReader mysql_result_sub = mysql_query_sub.ExecuteReader();
                mysql_result_sub.Read();
                nameSub.Text = mysql_result_sub["name"].ToString();
                subCount.Text = WordDeclensionHelper.GetSubscriptionsText(Convert.ToInt32(mysql_result_sub["usage_count"]));
                mysql_result_sub.Close();

                mysql_connection.Close();
            }
            catch (Exception ex)
            {
                MassageWindow.Show("Ошибка при загрузке данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
}

        ~AdminHome()
        {
            EventService.NewsChanged -= OnNewsChanged;
        }
    }
}
