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

using ChatBubbleClientWPF.ViewModels.Basic;

namespace ChatBubbleClientWPF.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged, IDisposable
    {
        private bool isDisposed = false;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler ViewModelClosing;

        internal Utility.IWindowFactory windowFactory;

        public BaseViewModel()
        {
            
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void OnViewModelClosing()
        {
            ViewModelClosing?.Invoke(this, new EventArgs());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }

            if (disposing)
            {
                ViewModelClosing = null;
                PropertyChanged = null;
                windowFactory = null;
            }

            isDisposed = true;
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

    interface IContextMenuTileContainer
    {
        void OnTileAction(object sender, TileInteractionEventArgs e);
    }

    interface IContextMenuTile
    {
        EventHandler<TileInteractionEventArgs> TileActionTriggered { get; set; }
        List<MenuItemViewModel> ContextMenuItems { get; set; }
    }

    class TileInteractionEventArgs : EventArgs
    {
        public enum TileAction 
        { OpenProfile, OpenPicture, 
            RemoveMessage, CopyMessage, SendMessage,
            RemoveFriend, AddFriend, 
            RemoveDialogue, OpenDialogue, 
            Select, ReadMessage}

        public TileAction Action { get; private set; }
        public int InteractionID { get; private set; }

        public object InteractionParameters { get; private set; }

        public TileInteractionEventArgs(TileAction action, int interactionID)
        {
            Action = action;
            InteractionID = interactionID;
        }

        public TileInteractionEventArgs(TileAction action, int interactionID, params object[] parameters) : this(action, interactionID)
        {
            InteractionParameters = parameters;
        }
    }
}
