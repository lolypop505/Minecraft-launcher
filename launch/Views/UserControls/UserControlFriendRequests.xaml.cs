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
    public partial class UserControlFriendRequests : UserControl
    {
        public UserControlFriendRequests()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event RoutedEventHandler ClickAcc
        {
            add { btnAcceptRequest.AddHandler(ButtonBase.ClickEvent, value); }
            remove { btnAcceptRequest.AddHandler(ButtonBase.ClickEvent, value); }
        }
        public event RoutedEventHandler ClickDel
        {
            add { btnDeleteRequest.AddHandler(ButtonBase.ClickEvent, value); }
            remove { btnDeleteRequest.AddHandler(ButtonBase.ClickEvent, value); }
        }

        public string Text1 { get; set; }
        public string Text2 { get; set; }
        public string tagBtnDel { get; set; }
        public string tagBtnAcc { get; set; }
    }
}
