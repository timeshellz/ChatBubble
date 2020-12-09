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

namespace ChatBubbleClientWPF
{
    class LoginWindowViewModel : BaseViewModel
    {
        public enum ErrorStatus { Neutral, Success, Failure }

        public event EventHandler<UserForms.UserFormEventArgs> CredentialsRequested;

        ClientFrontDoor clientFrontDoorModel;

        string nameForm = String.Empty;
        string usernameForm = String.Empty;
        SecureString passwordForm = new SecureString();
        SecureString passwordRepeatForm = new SecureString();

        string statusPrompt;

        ErrorStatus[] formCorrectnessStatuses = new ErrorStatus[4]
        {
            ErrorStatus.Neutral,
            ErrorStatus.Neutral,
            ErrorStatus.Neutral,
            ErrorStatus.Neutral,
        };

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

        public SecureString PasswordForm
        {
            get { return passwordForm; }
            set
            {
                passwordForm = value;
                //OnPropertyChanged();
            }
        }

        public SecureString RepeatPasswordForm
        {
            get { return passwordRepeatForm; }
            set
            {
                passwordRepeatForm = value;
                //OnPropertyChanged();
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

        public LoginWindowViewModel()
        {
            clientFrontDoorModel = new ClientFrontDoor();
            clientFrontDoorModel.PropertyChanged += new PropertyChangedEventHandler(OnModelPropertyChanged);
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

        void HandleStatusChange(ClientFrontDoor.SuccessStatuses status)
        {
            switch(status)
            {
                case ClientFrontDoor.SuccessStatuses.LoginSuccess:
                    FormCorrectnessStatuses = new ErrorStatus[4]
                    {
                        ErrorStatus.Success,
                        ErrorStatus.Success,
                        ErrorStatus.Success,
                        ErrorStatus.Success,
                    };
                    StatusPrompt = "Success!";
                    break;
                case ClientFrontDoor.SuccessStatuses.SignupSuccess:
                    FormCorrectnessStatuses = new ErrorStatus[4]
                    {
                        ErrorStatus.Success,
                        ErrorStatus.Success,
                        ErrorStatus.Success,
                        ErrorStatus.Success,
                    };
                    StatusPrompt = "Success!";
                    break;
                case ClientFrontDoor.SuccessStatuses.GenericFailure:
                    FormCorrectnessStatuses = new ErrorStatus[4]
                    {
                        ErrorStatus.Failure,
                        ErrorStatus.Failure,
                        ErrorStatus.Failure,
                        ErrorStatus.Failure,
                    };
                    StatusPrompt = "Error. Service unavailable.";
                    break;
                case ClientFrontDoor.SuccessStatuses.IncorrectNameFailure:
                    FormCorrectnessStatuses = new ErrorStatus[4]
                    {
                        ErrorStatus.Failure,
                        ErrorStatus.Neutral,
                        ErrorStatus.Neutral,
                        ErrorStatus.Neutral,
                    };
                    StatusPrompt = "Name contains forbidden characters!";
                    break;
                case ClientFrontDoor.SuccessStatuses.IncorrectUsernameFailure:
                    FormCorrectnessStatuses = new ErrorStatus[4]
                    {
                        ErrorStatus.Neutral,
                        ErrorStatus.Failure,
                        ErrorStatus.Neutral,
                        ErrorStatus.Neutral,
                    };
                    StatusPrompt = "Username contains forbidden characters!";
                    break;
                case ClientFrontDoor.SuccessStatuses.IncorrectPasswordFailure:
                    FormCorrectnessStatuses = new ErrorStatus[4]
                    {
                        ErrorStatus.Neutral,
                        ErrorStatus.Neutral,
                        ErrorStatus.Failure,
                        ErrorStatus.Neutral,
                    };
                    StatusPrompt = "Password contains forbidden characters!";
                    break;
                case ClientFrontDoor.SuccessStatuses.CredentialFailure:
                    FormCorrectnessStatuses = new ErrorStatus[4]
                    {
                        ErrorStatus.Neutral,
                        ErrorStatus.Failure,
                        ErrorStatus.Failure,
                        ErrorStatus.Neutral,
                    };
                    StatusPrompt = "Wrong username or password!";
                    break;
                case ClientFrontDoor.SuccessStatuses.PasswordMismatchFailure:
                    FormCorrectnessStatuses = new ErrorStatus[4]
                    {
                        ErrorStatus.Neutral,
                        ErrorStatus.Neutral,
                        ErrorStatus.Failure,
                        ErrorStatus.Failure,
                    };
                    StatusPrompt = "Passwords don't match!";
                    break;
                case ClientFrontDoor.SuccessStatuses.UsernameExistsFailure:
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
