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

namespace ChatBubbleClientWPF
{
    /// <summary>
    /// Interaction logic for LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : Window
    {
        LoadingWindowViewModel viewModel;

        public LoadingWindow()
        {
            InitializeComponent();
            viewModel = new LoadingWindowViewModel();
            this.DataContext = viewModel;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.InitializeClientLogic();
            viewModel.ConnectionEstablished += OnConnectionEstablished;
        }

        private void OnConnectionEstablished(object sender, ConnectionEventArgs e)
        {
            if(e.ConnectionType == ConnectionEventArgs.ConnectionTypes.Expired)
            {
                Window currentWindow = GetWindow(this);
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Top = currentWindow.Top;
                loginWindow.Left = currentWindow.Left;
                loginWindow.Show();
               
                currentWindow.Close();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
