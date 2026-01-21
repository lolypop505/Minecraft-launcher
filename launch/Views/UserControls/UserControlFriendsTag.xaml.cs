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

namespace launch.Views.UserControls
{
    /// <summary>
    /// Логика взаимодействия для UserControlFriendsTag.xaml
    /// </summary>
    public partial class UserControlFriendsTag : UserControl
    {
        public UserControlFriendsTag()
        {
            InitializeComponent();
            DataContext = this;
        }
        public string TextTag { get; set; }
    }
}
