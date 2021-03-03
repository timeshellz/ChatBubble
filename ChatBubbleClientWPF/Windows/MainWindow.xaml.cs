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

using ChatBubbleClientWPF.ViewModels;
using ChatBubbleClientWPF.ViewModels.Windows;
using ChatBubbleClientWPF.ViewModels.Basic;

namespace ChatBubbleClientWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel viewModel;
        

        bool needsLocationRestore = false;

        public MainWindow(ViewModels.BaseViewModel viewModel)
        {
            InitializeComponent();

            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;

            this.viewModel = (MainWindowViewModel)viewModel;
            this.DataContext = this.viewModel;

            this.viewModel.TabSwitchPrompted += OnTabSwitchPrompted;
            this.viewModel.TabReturnPrompted += OnTabReturnPrompted;
        }

        private void SetHeaderVisibility(Visibility visibility)
        {
            HeaderArc.Visibility = visibility;
            HeaderRectangle.Visibility = visibility;
            HeaderContentEllipse.Visibility = visibility;
            HeaderLabel.Visibility = visibility;
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            var page = ((Frame)sender).Content as Page;

            if (((Frame)sender).Content.GetType() == typeof(Tabs.MainTab)) SetHeaderVisibility(Visibility.Hidden);
            else SetHeaderVisibility(Visibility.Visible);

            viewModel.CurrentTabViewModel = (BaseViewModel)page.DataContext;

            Binding titleBinding = new Binding("Title");
            titleBinding.Source = page;
            titleBinding.Mode = BindingMode.OneWay;
            HeaderLabel.SetBinding(Label.ContentProperty, titleBinding);
        }

        void OnTabSwitchPrompted(object sender, TabNavigationEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                CurrentTab.Content = null;

                Page promptedTab;
                promptedTab = e.PageFactory.GetAssociatedPage(e.PageViewModel);

                CurrentTab.Navigate(promptedTab);
            }

        }

        void OnTabReturnPrompted(object sender, EventArgs e)
        {
            if(DataContext is MainWindowViewModel viewModel)
            {
                CurrentTab.Content = null;
                CurrentTab.GoBack();
            }
        }

        private void PageHeader_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (this.WindowState == WindowState.Maximized)
                {                  
                    needsLocationRestore = true;
                }

                DragMove();
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {                
                this.BorderThickness = new Thickness(8);                
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if(needsLocationRestore)
            {
                this.BorderThickness = new Thickness(0);
                needsLocationRestore = false;

                var point = PointToScreen(e.MouseDevice.GetPosition(this));

                Left = point.X - (RestoreBounds.Width * 0.5);
                Top = point.Y;
            
                this.WindowState = WindowState.Normal;

                try { DragMove(); }
                catch { };
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left)
                needsLocationRestore = false;
        }

        private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (viewModel.CloseSessionCommand.CanExecute(null))
                viewModel.CloseSessionCommand.Execute(null);
        }
    }
}
