using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ChatBubble;


namespace ChatBubbleClientWPF
{
    class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
    }

    class Command : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly Action<object> execute = null;
        private readonly Predicate<object> canExecute = null;

        public Command(Action<object> execute)
        {
            this.execute = execute;
        }

        public Command(Action<object> execute, Predicate<object> canExecute)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object obj)
        {
            if (canExecute != null)
                return canExecute(obj);
            else return true;
        }

        public void Execute(object obj)
        {
            execute?.Invoke(obj);
        }
    }
}
