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
    /// Логика взаимодействия для UserControlHelpdeskMessage.xaml
    /// </summary>
    public partial class UserControlHelpdeskMessage : UserControl
    {
        public UserControlHelpdeskMessage()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event RoutedEventHandler Click
        {
            add { btnDelete.AddHandler(ButtonBase.ClickEvent, value); }
            remove { btnDelete.AddHandler(ButtonBase.ClickEvent, value); }
        }

        public event RoutedEventHandler ClickView
        {
            add { btnViewTelegram.AddHandler(ButtonBase.ClickEvent, value); }
            remove { btnViewTelegram.AddHandler(ButtonBase.ClickEvent, value); }
        }

        public string messageText { get; set; }
        public BitmapImage ImageSourse { get; set; }
        public string user { get; set; }
        public string userTelegram { get; set; }
        public string tagBtn { get; set; }
    }
}
