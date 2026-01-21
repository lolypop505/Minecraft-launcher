using CommunityToolkit.Mvvm.Input;
using launch.Views;
using launch.Views.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace launch
{
    public partial class MainWindow : Window
    {
        public MainWindow(int userId)
        {
            InitializeComponent();
        }

        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) { DragMove(); }
        }

        private void Clouze_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Collapse_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}