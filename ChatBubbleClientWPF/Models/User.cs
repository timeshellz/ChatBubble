using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ChatBubble;

namespace ChatBubbleClientWPF.Models
{
    class User
    {
        public int ID { get; }
        public string FullName { get; protected set; }
        public string Username { get; protected set; }
        public string Status { get; protected set; }
        public string Description { get; protected set; }
        public int BubScore { get; protected set; }

        public User(int userID)
        {
            ID = userID;
            RequestUserSummary();
        }

        public User(int userID, string fullName, string userName)
        {
            ID = userID;
            FullName = fullName;
            Username = userName;
        }

        public void RequestUserSummary()
        {
            string[] profileInfoSplitstrings = { "id=", "login=", "name=", "status=", "main=", "bubscore=" };
            string profileInfoString = NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.GetUserSummaryRequest, "reqid=" + ID.ToString(), true, true);

            string[] allProfileData = profileInfoString.Split(profileInfoSplitstrings, StringSplitOptions.RemoveEmptyEntries);
            //[0] = id, [1] = login, [2] = name, [3] = status summary, [4] = main summary, [5] = bubscore

            for (int i = 0; i < allProfileData.Length; i++)
            {
                allProfileData[i] = allProfileData[i].Replace("[eqlsgn]", "=");
            }

            Username = allProfileData[1];
            FullName = allProfileData[2];
            Status = allProfileData[3];
            Description = allProfileData[4];
            BubScore = Convert.ToInt32(allProfileData[5]);
        }      

    }
}
