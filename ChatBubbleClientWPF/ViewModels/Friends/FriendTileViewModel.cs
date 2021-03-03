using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using ChatBubble;
using ChatBubble.SharedAPI;

using ChatBubbleClientWPF.ViewModels.Windows;
using ChatBubbleClientWPF.ViewModels.Basic;

namespace ChatBubbleClientWPF.ViewModels.Friends
{
    class FriendTileViewModel : UserTileViewModel
    {
        ICommand removeFriendCommand;

        public ICommand RemoveFriendCommand
        {
            get
            {
                if (removeFriendCommand == null)
                {
                    removeFriendCommand = new Command(p => OnFriendRemoval());
                }
                return removeFriendCommand;
            }
        }

        public FriendTileViewModel(User userModel) : base(userModel)
        {
            ContextMenuItems.Add(new MenuItemViewModel("Remove friend", RemoveFriendCommand));
        }

        public FriendTileViewModel(User userModel, params UserContextMenuActions[] contextMenuActions) : base(userModel, contextMenuActions)
        {
            ContextMenuItems.Add(new MenuItemViewModel("Remove friend", RemoveFriendCommand));
        }

        void OnFriendRemoval()
        {
            TileActionTriggered?.Invoke(this, new TileInteractionEventArgs(TileInteractionEventArgs.TileAction.RemoveFriend, UserID));
        }
    }

}
