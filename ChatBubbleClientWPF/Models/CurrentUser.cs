using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ChatBubble;

namespace ChatBubbleClientWPF.Models
{
    class CurrentUser : User
    {

        public CurrentUser(int userID) : base(userID)
        {

        }

        public string ChangeFullName(string newName)
        {
            return NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.ChangeNameRequest, "" + ID.ToString(), true, true);
        }

        public string ChangeUsername(string username)
        {
            return "";
        }

        public string ChangeDescription(string newStatus, string newDescription)
        {
            string descriptionChangeRequest = "newsummary=\nstatus=" + newStatus + "\nmain=" + newDescription + "\n";

            string reply = NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.EditUserSummaryRequest, descriptionChangeRequest, true, true);

            if (reply == NetComponents.ConnectionCodes.DescEditSuccess)
            {
                Status = newStatus;
                Description = newDescription;
            }

            return reply;
        }

        public string ChangeBubScore(int bubScore)
        {
            return "";
        }

        public string RemoveFriend(int friendID)
        {
            return NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.RemoveFriendRequest, "fid=" + friendID.ToString(), true, true);
        }

        public string AddFriend(int userID)
        {
            return NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.AddFriendRequest, "addid=" + userID.ToString(), true, true);
        }
    }
}
