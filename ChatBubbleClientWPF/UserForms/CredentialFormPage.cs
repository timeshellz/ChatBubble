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
using System.ComponentModel;
using System.Collections.ObjectModel;


namespace ChatBubbleClientWPF.UserForms
{
    public partial class CredentialFormPage : Page
    {
        public event EventHandler<UserFormEventArgs> FormChangePrompted;
        public event EventHandler<UserFormEventArgs> FormRequestPosted;

        private protected ReadOnlyCollection<LoginWindowViewModel.ErrorStatus> errorStatuses;

        protected string defaultStatusPrompt;

        public void OnFormChangePrompted(UserFormEventArgs eventArgs)
        {
            Unsubscribe();
            FormChangePrompted(this, eventArgs);
        }

        protected virtual void OnRequestPosted(UserFormEventArgs eventArgs)
        {
            FormRequestPosted(this, eventArgs);
        }

        protected virtual void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {

        }

        protected virtual void ElementGotFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginWindowViewModel viewModel)
            {
                viewModel.ClearFormStatuses();
            }
        }

        protected virtual void CredentialFormPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginWindowViewModel viewModel)
            {
                viewModel.PropertyChanged += OnViewModelPropertyChanged;
                viewModel.ClearFormStatuses();
            }
        }

        public void Unsubscribe()
        {
            if (DataContext is LoginWindowViewModel viewModel)
            {
                viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
        }
    }


    public interface IDataSecurable
    {
        void PassSecureData(object sender, UserForms.UserFormEventArgs e);
    }

    public class UserFormEventArgs : EventArgs
    {
        public Type PromptedFormType { get; set; }
        public Type CurrentFormType { get; set; }
    }
}
