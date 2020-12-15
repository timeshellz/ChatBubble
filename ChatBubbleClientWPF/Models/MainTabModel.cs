using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using ChatBubble;

namespace ChatBubbleClientWPF.Models
{
    class MainTabModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        int userID;
        string userFullName;
        string username;
        string userStatus;
        string userDescription;
        

    }
}
