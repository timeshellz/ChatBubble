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

namespace ChatBubbleClientWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CurrentTab.Navigate(new UserForms.MainTab());
        }

        private void SetHeaderVisibility(Visibility visibility)
        {
            HeaderArc.Visibility = visibility;
            HeaderRectangle.Visibility = visibility;
            HeaderContentEllipse.Visibility = visibility;
            HeaderLabel.Visibility = visibility;
        }

        private void SetHeaderTitle(String title="")
        {
            HeaderLabel.Content = title;
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            var page = ((Frame)sender).Content as Page;

            if (((Frame)sender).Content.GetType() == typeof(UserForms.MainTab)) SetHeaderVisibility(Visibility.Hidden);
            else SetHeaderVisibility(Visibility.Visible);
                      
            SetHeaderTitle(page.Name);
        }

        private void MainTabButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentTab.Navigate(new UserForms.MainTab());
        }

        private void DialoguesButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentTab.Navigate(new UserForms.DialoguesTab());
        }

        private void FriendsButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentTab.Navigate(new UserForms.FriendsTab());
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentTab.Navigate(new UserForms.SearchTab());
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentTab.Navigate(new UserForms.SettingsTab());
        }
    }
}
