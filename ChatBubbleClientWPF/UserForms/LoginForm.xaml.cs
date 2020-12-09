using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using System.ComponentModel;

namespace ChatBubbleClientWPF.UserForms
{
    /// <summary>
    /// Interaction logic for LoginForm.xaml
    /// </summary>
    public partial class LoginForm : CredentialFormPage, IDataSecurable
    {

        public LoginForm()
        {
            InitializeComponent();
            defaultStatusPrompt = "Log in";
        }
        private void CredentialFormPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginWindowViewModel viewModel)
            {
                viewModel.CredentialsRequested += PassSecureData;
                viewModel.PropertyChanged += OnViewModelPropertyChanged;
                viewModel.ClearFormStatuses();
                //viewModel.PropertyChanged += HandleErrorStatusChange;
            }     
        }

        private void SignupButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginWindowViewModel viewModel)
            {
                viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            OnFormChangePrompted(new UserFormEventArgs() { PromptedFormType = typeof(SignUpForm), CurrentFormType = typeof(LoginForm)});
        }

        public void PassSecureData(object sender, UserFormEventArgs e)
        {
            if(e.CurrentFormType == typeof(LoginForm))
            {
                if(DataContext is LoginWindowViewModel viewModel)
                {
                    viewModel.PasswordForm = ((PasswordBox)PasswordBox.Template.FindName(nameof(PasswordBox), PasswordBox)).SecurePassword;
                }
            }
        }

        protected override void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {          
            if (DataContext is LoginWindowViewModel viewModel)
            {
                if (e.PropertyName == "FormCorrectnessStatuses")
                {
                    errorStatuses = viewModel.GetFormStatuses();

                    if (errorStatuses.ElementAt(1) == LoginWindowViewModel.ErrorStatus.Success) UsernameBox.Style = Application.Current.Resources["SuccessRoundedTextBoxStyle"] as Style;
                    if (errorStatuses.ElementAt(2) == LoginWindowViewModel.ErrorStatus.Success) PasswordBox.Style = Application.Current.Resources["SuccessRoundedPasswordBoxStyle"] as Style;

                    if (errorStatuses.ElementAt(1) == LoginWindowViewModel.ErrorStatus.Neutral) UsernameBox.Style = Application.Current.FindResource(typeof(Controls.RoundedRectangleTextBox)) as Style;
                    if (errorStatuses.ElementAt(2) == LoginWindowViewModel.ErrorStatus.Neutral) PasswordBox.Style = Application.Current.FindResource(typeof(Controls.RoundedRectanglePasswordBox)) as Style;

                    if (errorStatuses.ElementAt(1) == LoginWindowViewModel.ErrorStatus.Failure) UsernameBox.Style = Application.Current.Resources["ErrorRoundedTextBoxStyle"] as Style;
                    if (errorStatuses.ElementAt(2) == LoginWindowViewModel.ErrorStatus.Failure) PasswordBox.Style = Application.Current.Resources["ErrorRoundedPasswordBoxStyle"] as Style;
                }
                if (e.PropertyName == nameof(viewModel.StatusPrompt))
                {
                    if (viewModel.StatusPrompt == String.Empty)
                       StatusLabel.Content = defaultStatusPrompt;
                }
            }
        }
    }
}
