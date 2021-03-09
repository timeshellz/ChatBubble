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

using ChatBubbleClientWPF.ViewModels.Windows;
using ChatBubbleClientWPF.ViewModels.Basic;

namespace ChatBubbleClientWPF
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        LoginWindowViewModel viewModel;

        public LoginWindow(ViewModels.BaseViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = (LoginWindowViewModel)viewModel;
            this.DataContext = this.viewModel;

            LoadingWindow ancestorWindow = GetLastLoadingWindow();
            Top = ancestorWindow.Top;
            Left = ancestorWindow.Left;
        }

        private LoadingWindow GetLastLoadingWindow()
        {
            if (App.Current.Windows[0] is LoadingWindow loadingWindow)
                return loadingWindow;
            else return null;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentUserForm.Navigate(new UserForms.LoginForm());

            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            viewModel.ViewModelClosing += (o, ev) => Close();
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
                if (e.PropertyName == nameof(viewModel.SignUpStatus) && viewModel.SignUpStatus == LoginWindowViewModel.ErrorStatus.Success)
                {
                    ((UserForms.CredentialFormPage)CurrentUserForm.Content).OnFormChangePrompted(new UserForms.UserFormEventArgs()
                    { CurrentFormType = typeof(UserForms.SignUpForm), PromptedFormType = typeof(UserForms.LoginForm) });
                }
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
