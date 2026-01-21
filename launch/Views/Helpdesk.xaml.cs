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

namespace launch.Views
{
    // поменять картинку в отчете !!!!
    public partial class Helpdesk : Page
    {
        int id;
        string fileName;
        string host = "127.0.0.1"; // Имя хоста
        string database = "launcherDB"; // Имя базы данных
        string user = "root"; // Имя пользователя
        string password = ""; // Пароль пользователя
        public Helpdesk()
        {
            InitializeComponent();
            try
            {
                string jsonString = File.ReadAllText("userData.json");
                wind userData = JsonSerializer.Deserialize<wind>(jsonString)!;
                id = userData.id;
            }
            catch (Exception ex) { //MessageBox.Show("произошла ошибка !" + ex.Message);
                                   }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            //try
            //{
                string Connect = "Database=" + database + ";Datasource=" + host + ";User=" + user + ";Password=" + password;
                MySqlConnection mysql_connection = new MySqlConnection(Connect);
                mysql_connection.Open();
                string query = "INSERT INTO errorMessages (message, image, user) VALUES (@message, @image, @user);";
                if (imageTB.Text.Length == 0)
                {
                    using (var command = new MySqlCommand(query, mysql_connection))
                    {
                        command.Parameters.Add("@image", MySqlDbType.MediumBlob).Value = null;
                        command.Parameters.AddWithValue("@message", messageTB.Text);
                        command.Parameters.AddWithValue("@user", id);
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    byte[] _selectedImageBytes = File.ReadAllBytes(fileName);
                    using (var command = new MySqlCommand(query, mysql_connection))
                    {
                        command.Parameters.Add("@image", MySqlDbType.MediumBlob).Value = _selectedImageBytes;
                        command.Parameters.AddWithValue("@message", messageTB.Text);
                        command.Parameters.AddWithValue("@user", id);
                        command.ExecuteNonQuery();
                    }
                }

                mysql_connection.Close();
                messageTB.Text = "";
                imageTB.Text = "";
            //}
            //catch { //MessageBox.Show("произошла ошибка !");
            //        }
        }

        private void btnViewFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "PNG files (*.png)|*.png",
                Title = "Выберите изображение"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                fileName = openFileDialog.FileName;
                imageTB.Text = System.IO.Path.GetFileNameWithoutExtension(fileName) + ".png";
            }
        }
    }
}
