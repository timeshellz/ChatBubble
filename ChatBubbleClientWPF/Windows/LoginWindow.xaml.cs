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
using System.ComponentModel;
using System.Threading;

namespace ChatBubbleClientWPF
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        LoginWindowViewModel viewModel;
        Window previousWindow;

        public LoginWindow()
        {
            InitializeComponent();
            viewModel = new LoginWindowViewModel();
            this.DataContext = viewModel;         
        }

        public LoginWindow(Window previousWindow) : this()
        {
            this.previousWindow = previousWindow;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentUserForm.Navigate(new UserForms.LoginForm());
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
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

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (DataContext is LoginWindowViewModel viewModel)
            {
                if (e.PropertyName == nameof(viewModel.LoginStatus) && viewModel.LoginStatus == LoginWindowViewModel.ErrorStatus.Success)
                {
                    OpenMainWindow();
                }
                if (e.PropertyName == nameof(viewModel.SignUpStatus) && viewModel.SignUpStatus == LoginWindowViewModel.ErrorStatus.Success)
                {
                    ((UserForms.CredentialFormPage)CurrentUserForm.Content).OnFormChangePrompted(new UserForms.UserFormEventArgs()
                    { CurrentFormType = typeof(UserForms.SignUpForm), PromptedFormType = typeof(UserForms.LoginForm) });
                }
            }
        }

        private void OpenMainWindow()
        {
            Window currentWindow = GetWindow(this);
            MainWindow mainWindow = new MainWindow();

            mainWindow.Show();
            currentWindow.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (previousWindow != null) previousWindow.Close();
        }       
    }
}
