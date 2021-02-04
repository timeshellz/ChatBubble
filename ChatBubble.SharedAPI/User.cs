using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProtoBuf;

using ChatBubble;

namespace ChatBubble.SharedAPI
{
    [ProtoContract]
    public class User
    {
        [ProtoMember(1)]
        public int ID { get; private set; }
        [ProtoMember(2)]
        public string FullName { get; protected set; }
        [ProtoMember(3)]
        public string Username { get; protected set; }
        [ProtoMember(4)]
        public string Status { get; protected set; }
        [ProtoMember(5)]
        public string Description { get; protected set; }
        [ProtoMember(6)]
        public int BubScore { get; protected set; }

        private User() { }

        protected User(int userID)
        {
            ID = userID;
        }

        public User(int userID, string fullName, string userName)
        {
            ID = userID;
            FullName = fullName;
            Username = userName;
        }

        public User(int userID, string fullName, string userName, string status, string description, int bubscore) : this(userID, fullName, userName)
        {
            Status = status;
            Description = description;
            BubScore = bubscore;
        }
    }
}
