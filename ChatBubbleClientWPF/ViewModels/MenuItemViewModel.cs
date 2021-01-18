using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChatBubbleClientWPF.ViewModels
{
    class MenuItemViewModel : BaseViewModel
    {
        string itemHeader;
        ICommand itemCommand;

        public string Header
        {
            get { return itemHeader; }
            set
            {
                itemHeader = value;
                OnPropertyChanged();
            }
        }

        public ICommand Command
        {
            get { return itemCommand; }
            set
            {
                itemCommand = value;
                OnPropertyChanged();
            }
        }

        public MenuItemViewModel(string header, ICommand command)
        {
            Header = header;
            Command = command;
        }
    }
}
