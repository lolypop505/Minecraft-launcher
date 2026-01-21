using Google.Protobuf.WellKnownTypes;
using Mysqlx.Crud;
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
using System.Windows.Shapes;

namespace launch
{
    public partial class MassageWindow : Window
    {
        public MessageBoxResult Result { get; private set; }

        // Конструктор с настройкой сообщения
        private MassageWindow(string messageText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            InitializeComponent();

            this.messageText.Text = messageText;
            title.Text = caption;

            // Настройка кнопок в зависимости от MessageBoxButton
            switch (button)
            {
                case MessageBoxButton.OK:
                    btnCancel.Visibility = Visibility.Collapsed;
                    btnNo.Visibility = Visibility.Collapsed;
                    btnYes.Visibility = Visibility.Collapsed;
                    btnOk.Visibility = Visibility.Visible;
                    break;
                case MessageBoxButton.OKCancel:
                    btnNo.Visibility = Visibility.Collapsed;
                    btnYes.Visibility = Visibility.Collapsed;
                    btnOk.Visibility = Visibility.Visible;
                    btnCancel.Visibility = Visibility.Visible;
                    break;
                case MessageBoxButton.YesNo:
                    btnOk.Visibility = Visibility.Collapsed;
                    btnCancel.Visibility = Visibility.Collapsed;
                    btnYes.Visibility = Visibility.Visible;
                    btnNo.Visibility = Visibility.Visible;
                    break;
                case MessageBoxButton.YesNoCancel:
                    btnOk.Visibility = Visibility.Collapsed;
                    btnYes.Visibility = Visibility.Visible;
                    btnNo.Visibility = Visibility.Visible;
                    btnCancel.Visibility = Visibility.Visible;
                    break;
            }

            // Настройка иконки
            string iconPath = icon switch
            {
                MessageBoxImage.Error => "Views/Images/ErrorIcon.png",
                MessageBoxImage.Warning => "Views/Images/WarningIcon.png",
                MessageBoxImage.Information => "Views/Images/InfoIcon.png",
                MessageBoxImage.Question => "Views/Images/QuestionIcon.png",
                _ => null
            };

            if (!string.IsNullOrEmpty(iconPath))
            {
                messageIcon.Source = new BitmapImage(new Uri(iconPath, UriKind.Relative));
                messageIcon.Visibility = Visibility.Visible;
            }
            else
            {
                messageIcon.Visibility = Visibility.Collapsed;
            }

            btnOk.Click += (s, e) => { Result = MessageBoxResult.OK; Close(); };
            btnCancel.Click += (s, e) => { Result = MessageBoxResult.Cancel; Close(); };
            btnYes.Click += (s, e) => { Result = MessageBoxResult.Yes; Close(); };
            btnNo.Click += (s, e) => { Result = MessageBoxResult.No; Close(); };
        }
        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Collapse_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Clouze_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.None;
            Close();
        }

        public static MessageBoxResult Show(string messageText)
        {
            return Show(messageText, "", MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(string messageText, string caption)
        {
            return Show(messageText, caption, MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(string messageText, string caption, MessageBoxButton button)
        {
            return Show(messageText, caption, button, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(Window owner, string messageText)
        {
            return Show(owner, messageText, "", MessageBoxButton.OK, MessageBoxImage.None);
        }
        public static MessageBoxResult Show(Window owner, string messageText, string caption)
        {
            return Show(owner, messageText, caption, MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(Window owner, string messageText, string caption, MessageBoxButton button)
        {
            return Show(owner, messageText, caption, button, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(string messageText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            var window = new MassageWindow(messageText, caption, button, icon)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            window.ShowDialog();
            return window.Result;
        }

        public static MessageBoxResult Show(Window owner, string messageText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            var window = new MassageWindow(messageText, caption, button, icon)
            {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            window.ShowDialog();
            return window.Result;
        }
    }
}
