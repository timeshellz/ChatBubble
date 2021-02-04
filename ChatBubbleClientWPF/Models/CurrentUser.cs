using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ChatBubble;
using ChatBubble.ClientAPI;
using ChatBubble.SharedAPI;

namespace ChatBubbleClientWPF.Models
{
    class CurrentUser : User
    {
        public Cookie Cookie { get; private set; }

        public CurrentUser(Cookie currentUserCookie) : base(currentUserCookie.ID)
        {
            GetUserRequest summaryRequest = new GetUserRequest(currentUserCookie, currentUserCookie.ID);
            ServerGetUserReply serverReply = (ServerGetUserReply)ClientRequestManager.SendClientRequest(summaryRequest);

            FullName = serverReply.User.FullName;
            Username = serverReply.User.Username;
            Status = serverReply.User.Status;
            Description = serverReply.User.Description;
            BubScore = serverReply.User.BubScore;
            Cookie = currentUserCookie;
        }

        public GenericServerReply ChangeFullName(string newName)
        {
            return ClientRequestManager.SendClientRequest(new ChangeNameRequest(Cookie, newName));
        }

        public string ChangeUsername(string username)
        {
            return "";
        }

        public GenericServerReply ChangeDescription(string newStatus, string newDescription)
        {
            string descriptionChangeRequest = "newsummary=\nstatus=" + newStatus + "\nmain=" + newDescription + "\n";

            GenericServerReply serverReply = ClientRequestManager.SendClientRequest(new EditSummaryRequest(Cookie, newStatus, newDescription));

            if (serverReply.NetFlag == ConnectionCodes.DescEditSuccess)
            {
                Status = newStatus;
                Description = newDescription;
            }

            return serverReply;
        }

        public string ChangeBubScore(int bubScore)
        {
            return "";
        }

        public GenericServerReply RemoveFriend(int friendID)
        {
            return ClientRequestManager.SendClientRequest(new RemoveFriendRequest(Cookie, friendID));
        }

        public GenericServerReply AddFriend(int userID)
        {
            return ClientRequestManager.SendClientRequest(new AddFriendRequest(Cookie, userID));
        }
    }
}
