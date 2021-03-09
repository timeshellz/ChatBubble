using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using System.Security;
using ChatBubble;


namespace ChatBubbleClientWPF.ViewModels.Windows
{
    class LoginWindowViewModel : BaseViewModel
    {
        public enum ErrorStatus { Neutral, Success, Failure }

        public event EventHandler<UserForms.UserFormEventArgs> CredentialsRequested;

        Models.ClientFrontDoor clientFrontDoorModel;

        string nameForm = String.Empty;
        string usernameForm = String.Empty;
        public SecureString PasswordForm { private get; set; }
        public SecureString RepeatPasswordForm { private get; set; }

        string statusPrompt;

        private ErrorStatus[] formCorrectnessStatuses = new ErrorStatus[4]
        {
            ErrorStatus.Neutral,
            ErrorStatus.Neutral,
            ErrorStatus.Neutral,
            ErrorStatus.Neutral,
        };

        private ErrorStatus loginStatus;
        private ErrorStatus signUpStatus;

        ICommand loginCommand;
        ICommand signupCommand;

        public ICommand LoginCommand
        {
            get
            {
                if(loginCommand == null)
                {
                    loginCommand = new Command(p => DoOnLoginCommand());
                }
                return loginCommand;
            }
        }

        public ICommand SignupCommand
        {
            get
            {
                if (signupCommand == null)
                {
                    signupCommand = new Command(p => DoOnSignupCommand());
                }
                return signupCommand;
            }
        }

        public string NameForm
        {
            get { return nameForm; }
            set
            {
                nameForm = value;
                OnPropertyChanged();
            }
        }

        public string UsernameForm
        {
            get { return usernameForm; }
            set
            {
                usernameForm = value;
                OnPropertyChanged();
            }
        }

        public string StatusPrompt
        {
            get { return statusPrompt; }
            set
            {
                statusPrompt = value;
                OnPropertyChanged();
            }
        }

        public ErrorStatus LoginStatus
        {
            get { return loginStatus; }
            set
            {
                loginStatus = value;
                OnPropertyChanged();
            }
        }

        public ErrorStatus SignUpStatus
        {
            get { return signUpStatus; }
            set
            {
                signUpStatus = value;
                OnPropertyChanged();
            }
        }

        private ErrorStatus[] FormCorrectnessStatuses         // Set-only private property to call OnPropertyChanged()
        {
            set
            {
                formCorrectnessStatuses = value;
                OnPropertyChanged();
            }
        }

        public ReadOnlyCollection<ErrorStatus> GetFormStatuses()
        {
            return new ReadOnlyCollection<ErrorStatus>(formCorrectnessStatuses);
        }

        public void ClearFormStatuses()
        {
            FormCorrectnessStatuses = new ErrorStatus[4]
            {
                ErrorStatus.Neutral,
                ErrorStatus.Neutral,
                ErrorStatus.Neutral,
                ErrorStatus.Neutral,
            };

            StatusPrompt = String.Empty;
        }

        public LoginWindowViewModel(Utility.IWindowFactory windowFactory)
        {
            this.windowFactory = windowFactory;

            clientFrontDoorModel = new Models.ClientFrontDoor();
            clientFrontDoorModel.PropertyChanged += new PropertyChangedEventHandler(OnModelPropertyChanged);

            this.windowFactory.OpenAssociatedWindow(this);
        }

        void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(clientFrontDoorModel.SuccessStatus):
                    HandleStatusChange(clientFrontDoorModel.SuccessStatus);
                    break;
            }
        }

        void HandleStatusChange(Models.ClientFrontDoor.SuccessStatuses status)
        {
            switch(status)
            {
                case Models.ClientFrontDoor.SuccessStatuses.LoginSuccess:
                    FormCorrectnessStatuses = new ErrorStatus[4]
                    {
                        ErrorStatus.Success,
                        ErrorStatus.Success,
                        ErrorStatus.Success,
                        ErrorStatus.Success,
                    };
                    StatusPrompt = "Success!";
                    LoginStatus = ErrorStatus.Success;

                    CreateMainViewModel();

                    break;
                case Models.ClientFrontDoor.SuccessStatuses.SignupSuccess:
                    FormCorrectnessStatuses = new ErrorStatus[4]
                    {
                        ErrorStatus.Success,
                        ErrorStatus.Success,
                        ErrorStatus.Success,
                        ErrorStatus.Success,
                    };
                    StatusPrompt = "Success!";
                    SignUpStatus = ErrorStatus.Success;
                    break;
                case Models.ClientFrontDoor.SuccessStatuses.GenericFailure:
                    FormCorrectnessStatuses = new ErrorStatus[4]
                    {
                        ErrorStatus.Failure,
                        ErrorStatus.Failure,
                        ErrorStatus.Failure,
                        ErrorStatus.Failure,
                    };
                    StatusPrompt = "Error. Service unavailable.";
                    break;
                case Models.ClientFrontDoor.SuccessStatuses.IncorrectNameFailure:
                    FormCorrectnessStatuses = new ErrorStatus[4]
                    {
                        ErrorStatus.Failure,
                        ErrorStatus.Neutral,
                        ErrorStatus.Neutral,
                        ErrorStatus.Neutral,
                    };
                    StatusPrompt = "Name contains forbidden characters!";
                    break;
                case Models.ClientFrontDoor.SuccessStatuses.IncorrectUsernameFailure:
                    FormCorrectnessStatuses = new ErrorStatus[4]
                    {
                        ErrorStatus.Neutral,
                        ErrorStatus.Failure,
                        ErrorStatus.Neutral,
                        ErrorStatus.Neutral,
                    };
                    StatusPrompt = "Username contains forbidden characters!";
                    break;
                case Models.ClientFrontDoor.SuccessStatuses.IncorrectPasswordFailure:
                    FormCorrectnessStatuses = new ErrorStatus[4]
                    {
                        ErrorStatus.Neutral,
                        ErrorStatus.Neutral,
                        ErrorStatus.Failure,
                        ErrorStatus.Neutral,
                    };
                    StatusPrompt = "Password contains forbidden characters!";
                    break;
                case Models.ClientFrontDoor.SuccessStatuses.CredentialFailure:
                    FormCorrectnessStatuses = new ErrorStatus[4]
                    {
                        ErrorStatus.Neutral,
                        ErrorStatus.Failure,
                        ErrorStatus.Failure,
                        ErrorStatus.Neutral,
                    };
                    StatusPrompt = "Wrong username or password!";
                    break;
                case Models.ClientFrontDoor.SuccessStatuses.PasswordMismatchFailure:
                    FormCorrectnessStatuses = new ErrorStatus[4]
                    {
                        ErrorStatus.Neutral,
                        ErrorStatus.Neutral,
                        ErrorStatus.Failure,
                        ErrorStatus.Failure,
                    };
                    StatusPrompt = "Passwords don't match!";
                    break;
                case Models.ClientFrontDoor.SuccessStatuses.UsernameExistsFailure:
                    FormCorrectnessStatuses = new ErrorStatus[4]
                    {
                        ErrorStatus.Neutral,
                        ErrorStatus.Failure,
                        ErrorStatus.Neutral,
                        ErrorStatus.Neutral,
                    };
                    StatusPrompt = "Username already in use.";
                    break;
            }
        }

        void CreateMainViewModel()
        {
            windowFactory.WindowRendered += (o, e) => OnViewModelClosing();

            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(windowFactory, new Utility.PageFactory(), clientFrontDoorModel.LoggedInUserCookie);
        }

        protected void OnCredentialsRequested(object sender, UserForms.UserFormEventArgs e)
        {
            CredentialsRequested?.Invoke(sender, e);
        }

        void DoOnLoginCommand()
        {
            // Raise event to immediately get password from view's passwordbox
            OnCredentialsRequested(this, new UserForms.UserFormEventArgs() { CurrentFormType = typeof(UserForms.LoginForm)});

            clientFrontDoorModel.AttemptLogin(UsernameForm, PasswordForm);
        }

        void DoOnSignupCommand()
        {
            OnCredentialsRequested(this, new UserForms.UserFormEventArgs() { CurrentFormType = typeof(UserForms.SignUpForm) });

            clientFrontDoorModel.AttemptSignup(NameForm, UsernameForm, PasswordForm, RepeatPasswordForm);
        }
    }

    
}
