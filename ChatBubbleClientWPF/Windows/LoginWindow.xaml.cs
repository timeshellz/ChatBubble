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
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        LoginWindowViewModel viewModel;

        public LoginWindow()
        {
            InitializeComponent();
            viewModel = new LoginWindowViewModel();
            this.DataContext = viewModel;         
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentUserForm.Navigate(new UserForms.LoginForm());
        }

        void ChangeCredentialForm(object sender, UserForms.UserFormEventArgs e)
        {
            if (e.PromptedFormType == typeof(UserForms.LoginForm)) CurrentUserForm.Navigate(new UserForms.LoginForm());
            if (e.PromptedFormType == typeof(UserForms.SignUpForm)) CurrentUserForm.Navigate(new UserForms.SignUpForm());
        }

        private void CurrentUserForm_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            UserForms.CredentialFormPage page = (UserForms.CredentialFormPage)CurrentUserForm.Content;

            page.FormChangePrompted += ChangeCredentialForm;
            page.DataContext = CurrentUserForm.DataContext;

            CurrentUserForm.NavigationService.RemoveBackEntry();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }      
    }
}
