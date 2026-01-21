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
    public partial class UserControlSubscription : UserControl
    {
        public UserControlSubscription()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event RoutedEventHandler Click
        {
            add { btnSub.AddHandler(ButtonBase.ClickEvent, value); }
            remove { btnSub.AddHandler(ButtonBase.ClickEvent, value); }
        }

        public string NameSub { get; set; }
        public string Description { get; set; }
        public string Price { get; set; }
        public string BtnText { get; set; }
        public string OpacityBtn { get; set; }
        public bool IsEnabledBtn { get; set; }
        public string TagBtn { get; set; }
    }
}
