using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using System.Text.Json;
using launch.Views.UserControls;

namespace launch.Views
{
    public partial class Friends : Page
    {
        private int id;
        private List<string[]> friends = new List<string[]>();
        private List<int> friendsRequest = new List<int>();
        private List<string[]> users = new List<string[]>();

        private readonly string host = "127.0.0.1";
        private readonly string database = "launcherDB";
        private readonly string user = "root";
        private readonly string password = "";

        public Friends()
        {
            InitializeComponent();
            try
            {
                string fileName = "userData.json";
                string jsonString = File.ReadAllText(fileName);
                var userData = JsonSerializer.Deserialize<wind>(jsonString);
                id = userData?.id ?? 0;

                if (id == 0)
                {
                    MessageBox.Show("Ошибка загрузки данных пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                LoadPage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при загрузке страницы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPage()
        {
            try
            {
                ClearAllPanels();

                using (var mysql_connection = new MySqlConnection($"Database={database};Datasource={host};User={user};Password={password}"))
                {
                    mysql_connection.Open();

                    LoadFriends(mysql_connection);
                    LoadFriendRequests(mysql_connection);
                    LoadPotentialFriends(mysql_connection);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearAllPanels()
        {
            SPfriendRequests.Children.Clear();
            SPfriendRequestsUser.Children.Clear();
            SPfriendSearch.Children.Clear();
            SPmyFriends.Children.Clear();
            friends.Clear();
            friendsRequest.Clear();
            users.Clear();
        }

        private void LoadFriends(MySqlConnection connection)
        {
            var query = $"SELECT u.id, u.nickname, u.personalInformation, u.telegram FROM users u " +
                        $"INNER JOIN friends f ON (f.user1 = u.id OR f.user2 = u.id) " +
                        $"WHERE (f.user1 = {id} OR f.user2 = {id}) AND u.id != {id}";

            using (var command = new MySqlCommand(query, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    friends.Add(new[] {
                        reader.GetInt32(0).ToString(),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetString(3)
                    });

                    var userControl = new UserControlMyFriends
                    {
                        Text1 = reader.GetString(1),
                        Text2 = reader.GetString(2),
                        tagBtnDel = reader.GetInt32(0).ToString(),
                        tagBtnView = reader.GetInt32(0).ToString()
                    };
                    userControl.ClickDel += BtnDeleteFriends_Click;
                    userControl.ClickTg += BtnViewTGFriends_Click;

                    SPmyFriends.Children.Add(userControl);
                }
            }
            UpdateEmptyStateVisibility();
        }

        private void LoadFriendRequests(MySqlConnection connection)
        {
            // Incoming requests
            LoadRequestsSection(connection,
                $"SELECT u.id, u.nickname, u.personalInformation FROM users u " +
                $"INNER JOIN friendrequests fr ON fr.sender = u.id " +
                $"WHERE fr.recipient = {id}",
                SPfriendRequests);

            // Outgoing requests
            LoadRequestsSection(connection,
                $"SELECT u.id, u.nickname, u.personalInformation FROM users u " +
                $"INNER JOIN friendrequests fr ON fr.recipient = u.id " +
                $"WHERE fr.sender = {id}",
                SPfriendRequestsUser, false);
            UpdateEmptyStateVisibility();
        }

        private void LoadRequestsSection(MySqlConnection connection, string query, StackPanel panel, bool showAcceptButton = true)
        {
            using (var command = new MySqlCommand(query, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    friendsRequest.Add(reader.GetInt32(0));

                    var userControl = new UserControlFriendRequests
                    {
                        Text1 = reader.GetString(1),
                        Text2 = reader.GetString(2),
                        tagBtnDel = reader.GetInt32(0).ToString(),
                        tagBtnAcc = reader.GetInt32(0).ToString()
                    };

                    userControl.ClickDel += btnDeleteRequest_Click;
                    if (showAcceptButton)
                    {
                        userControl.ClickAcc += btnAcceptRequest_Click;
                    }
                    else
                    {
                        userControl.btnAcceptRequest.Visibility = Visibility.Hidden;
                    }

                    panel.Children.Add(userControl);
                }
            }
        }

        private void LoadPotentialFriends(MySqlConnection connection)
        {
            var excludedIds = new List<string> { id.ToString() };
            excludedIds.AddRange(friends.Select(f => f[0]));
            excludedIds.AddRange(friendsRequest.Select(fr => fr.ToString()));

            var query = $"SELECT id, nickname, personalInformation, telegram FROM users " +
                        $"WHERE id NOT IN ({string.Join(",", excludedIds)}) " +
                        $"AND lookingFriengs = 1";

            using (var command = new MySqlCommand(query, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    users.Add(new[] {
                        reader.GetInt32(0).ToString(),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetString(3)
                    });

                    var userControl = new UserControlFriendsSearch
                    {
                        Text1 = reader.GetString(1),
                        Text2 = reader.GetString(2),
                        tagBtnAdd = reader.GetInt32(0).ToString()
                    };
                    userControl.ClickAdd += btnAddFriend_Click;

                    SPfriendSearch.Children.Add(userControl);
                }
            }
            UpdateEmptyStateVisibility();
        }

        private void UpdateEmptyStateVisibility()
        {
            // Для друзей
            NoFriendsText.Visibility = SPmyFriends.Children.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // Для входящих заявок
            NoIncomingRequestsText.Visibility = SPfriendRequests.Children.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // Для исходящих заявок
            NoOutgoingRequestsText.Visibility = SPfriendRequestsUser.Children.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // Для результатов поиска
            NoSearchResultsText.Visibility = SPfriendSearch.Children.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void btnAddFriend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse((sender as Button)?.Tag?.ToString(), out int idFriend))
                {
                    MessageBox.Show("Неверный идентификатор пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (var connection = new MySqlConnection($"Database={database};Datasource={host};User={user};Password={password}"))
                {
                    connection.Open();
                    var query = $"INSERT INTO friendrequests (sender, recipient) VALUES ({id}, {idFriend})";
                    new MySqlCommand(query, connection).ExecuteNonQuery();
                }

                LoadPage();
                MessageBox.Show("Заявка в друзья отправлена", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке заявки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAcceptRequest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse((sender as Button)?.Tag?.ToString(), out int idFriend))
                {
                    MessageBox.Show("Неверный идентификатор пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (var connection = new MySqlConnection($"Database={database};Datasource={host};User={user};Password={password}"))
                {
                    connection.Open();

                    // Remove friend request
                    var deleteQuery = $"DELETE FROM friendrequests WHERE sender = {idFriend} AND recipient = {id}";
                    new MySqlCommand(deleteQuery, connection).ExecuteNonQuery();

                    // Add friend
                    var insertQuery = $"INSERT INTO friends (user1, user2) VALUES ({id}, {idFriend})";
                    new MySqlCommand(insertQuery, connection).ExecuteNonQuery();
                }

                LoadPage();
                MessageBox.Show("Пользователь добавлен в друзья", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при принятии заявки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDeleteRequest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse((sender as Button)?.Tag?.ToString(), out int idFriend))
                {
                    MessageBox.Show("Неверный идентификатор пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (var connection = new MySqlConnection($"Database={database};Datasource={host};User={user};Password={password}"))
                {
                    connection.Open();
                    var query = $"DELETE FROM friendrequests WHERE (sender = {id} AND recipient = {idFriend}) OR (sender = {idFriend} AND recipient = {id})";
                    new MySqlCommand(query, connection).ExecuteNonQuery();
                }

                LoadPage();
                MessageBox.Show("Заявка в друзья отменена", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отмене заявки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeleteFriends_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse((sender as Button)?.Tag?.ToString(), out int idFriend))
                {
                    MessageBox.Show("Неверный идентификатор пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show("Вы уверены, что хотите удалить пользователя из друзей?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                using (var connection = new MySqlConnection($"Database={database};Datasource={host};User={user};Password={password}"))
                {
                    connection.Open();
                    var query = $"DELETE FROM friends WHERE (user1 = {id} AND user2 = {idFriend}) OR (user1 = {idFriend} AND user2 = {id})";
                    new MySqlCommand(query, connection).ExecuteNonQuery();
                }

                LoadPage();
                MessageBox.Show("Пользователь удален из друзей", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении из друзей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnViewTGFriends_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse((sender as Button)?.Tag?.ToString(), out int idFriend))
                {
                    MessageBox.Show("Неверный идентификатор пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var friend = friends.FirstOrDefault(f => int.Parse(f[0]) == idFriend);
                if (friend == null || string.IsNullOrWhiteSpace(friend[3]))
                {
                    MessageBox.Show("Телеграм пользователя не указан", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Process.Start(new ProcessStartInfo(friend[3]) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии Telegram: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnFriendRequests_Click(object sender, RoutedEventArgs e)
        {
            myFriendsPage.Visibility = Visibility.Hidden;
            friendRequestsPage.Visibility = Visibility.Visible;
            friendSearchPage.Visibility = Visibility.Hidden;
        }

        private void BtnFriendSearch_Click(object sender, RoutedEventArgs e)
        {
            myFriendsPage.Visibility = Visibility.Hidden;
            friendRequestsPage.Visibility = Visibility.Hidden;
            friendSearchPage.Visibility = Visibility.Visible;
        }

        private void BtnMyFriend_Click(object sender, RoutedEventArgs e)
        {
            myFriendsPage.Visibility = Visibility.Visible;
            friendRequestsPage.Visibility = Visibility.Hidden;
            friendSearchPage.Visibility = Visibility.Hidden;
        }

        private void btnSearchNickname_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SPfriendSearch.Children.Clear();

                var excludedIds = new List<string> { id.ToString() };
                excludedIds.AddRange(friends.Select(f => f[0]));
                excludedIds.AddRange(friendsRequest.Select(fr => fr.ToString()));

                var baseQuery = $"SELECT id, nickname, personalInformation, telegram FROM users " +
                                $"WHERE id NOT IN ({string.Join(",", excludedIds)}) " +
                                $"AND lookingFriengs = 1";

                var query = string.IsNullOrWhiteSpace(searchNicknameTB.Text)
                    ? baseQuery
                    : $"{baseQuery} AND nickname LIKE '%{searchNicknameTB.Text}%'";

                using (var connection = new MySqlConnection($"Database={database};Datasource={host};User={user};Password={password}"))
                {
                    connection.Open();
                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var userControl = new UserControlFriendsSearch
                            {
                                Text1 = reader.GetString(1),
                                Text2 = reader.GetString(2),
                                tagBtnAdd = reader.GetInt32(0).ToString()
                            };
                            userControl.ClickAdd += btnAddFriend_Click;

                            SPfriendSearch.Children.Add(userControl);
                        }
                    }
                }

                UpdateEmptyStateVisibility();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}