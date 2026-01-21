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
    /// <summary>
    /// Логика взаимодействия для UserControlFriendsSearch.xaml
    /// </summary>
    public partial class UserControlFriendsSearch : UserControl
    {
        public UserControlFriendsSearch() //  (string[] ArrayTag)
        {
            InitializeComponent();
            DataContext = this;

            //foreach (string item in ArrayTag)
            //{
            //    UserControlFriendsTag userControlFriendsTag = new UserControlFriendsTag();
            //    userControlFriendsTag.TextTag = item;
            //    stackPanel.Children.Add(userControlFriendsTag);
            //}
        }
        public event RoutedEventHandler ClickAdd
        {
            add { btnAddFriend.AddHandler(ButtonBase.ClickEvent, value); }
            remove { btnAddFriend.AddHandler(ButtonBase.ClickEvent, value); }
        }

        public string Text1 { get; set; }
        public string Text2 { get; set; }
        public string tagBtnAdd { get; set; }
    }
}
