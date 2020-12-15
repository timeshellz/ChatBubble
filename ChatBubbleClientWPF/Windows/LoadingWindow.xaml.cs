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
using System.Windows.Threading;
using WpfAnimatedGif;

namespace ChatBubbleClientWPF
{
    /// <summary>
    /// Interaction logic for LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : Window
    {
        LoadingWindowViewModel viewModel;
        Image animationBox;
        ImageAnimationController animationBoxController;

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
            viewModel.ConnectionFailed += OnConnectionFailed;

            animationBox = (Image)FindName("AnimationBox");
        }

        private void OnConnectionEstablished(object sender, ConnectionEventArgs e)
        {
            if(e.ConnectionType == ConnectionEventArgs.ConnectionTypes.Expired)
            {
                Window currentWindow = GetWindow(this);
                LoginWindow loginWindow = new LoginWindow(currentWindow);
                loginWindow.Top = currentWindow.Top;
                loginWindow.Left = currentWindow.Left;
                loginWindow.Show();
            }
        }

        private void OnConnectionFailed(object sender, ConnectionEventArgs e)
        {
            Task.Run(PromptAndDisplayError);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Animation_Loaded(object sender, RoutedEventArgs e)
        {
            animationBoxController = ImageBehavior.GetAnimationController(animationBox);
        }

        private async void PromptAndDisplayError()
        {
            bool isAnimationReady = false;

            while (true)
            {                                                           //Displays error when animation can smoothly transition
                await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => 
                {
                    if ((animationBoxController.CurrentFrame >= 186 && animationBoxController.CurrentFrame <= 200) ||
                        (animationBoxController.CurrentFrame >= 402 && animationBoxController.CurrentFrame <= 414) ||
                        (animationBoxController.CurrentFrame >= 582))
                    {
                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.UriSource = new Uri("../Animations/ErrorAnimation.gif", UriKind.Relative);
                        image.EndInit();

                        ImageBehavior.SetAnimatedSource(animationBox, image);
                        isAnimationReady = true;

                        Background = (SolidColorBrush)Application.Current.Resources["ErrorColorBrush"];
                    }

                    return;
                }));

                if (isAnimationReady) return;
                else await Task.Delay(100);
            }
        }
    }
}
