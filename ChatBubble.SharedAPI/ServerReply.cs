using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace ChatBubble.SharedAPI
{
    [ProtoContract]
    [ProtoInclude(1, typeof(ServerHandshakeReply))]
    [ProtoInclude(2, typeof(ServerLoginReply))]
    [ProtoInclude(3, typeof(ServerSearchReply))]
    [ProtoInclude(4, typeof(ServerGetUserReply))]
    [ProtoInclude(5, typeof(ServerFriendListReply))]
    [ProtoInclude(6, typeof(ServerPendingMessagesReply))]
    public class GenericServerReply : NetTransferObject
    {
        protected GenericServerReply() { }

        public GenericServerReply(string flag) : base(flag)
        { }
    }

    [ProtoContract]
    public sealed class ServerHandshakeReply : GenericServerReply
    {
        [ProtoMember(1)]
        public int ConnectionSessionID { get; private set; }

        private ServerHandshakeReply() { }

        public ServerHandshakeReply(int newSessionID, string flag) : base(flag)
        {
            ConnectionSessionID = newSessionID;
        }
    }

    [ProtoContract]
    public sealed class ServerLoginReply : GenericServerReply
    {
        [ProtoMember(1)]
        public Cookie NewCookie { get; private set; }

        private ServerLoginReply() { }

        public ServerLoginReply(string flag, Cookie newCookie) : base(flag)
        {
            NewCookie = newCookie;
        }
    }

    [ProtoContract]
    public sealed class ServerSearchReply : GenericServerReply
    {
        [ProtoMember(1)]
        public List<User> SearchResults { get; private set; }

        private ServerSearchReply() { }

        public ServerSearchReply(string flag, List<User> searchResults) : base(flag)
        {
            SearchResults = searchResults;
        }
    }

    [ProtoContract]
    public class ServerGetUserReply : GenericServerReply
    {
        [ProtoMember(1)]
        public User User { get; private set; }

        private ServerGetUserReply() { }

        public ServerGetUserReply(string flag, User user) : base(flag)
        {
            User = user;
        }
    }

    [ProtoContract]
    public sealed class ServerFriendListReply : GenericServerReply
    {
        [ProtoMember(1)]
        public List<User> FriendList { get; private set; }

        private ServerFriendListReply() { }

        public ServerFriendListReply(string flag, List<User> friendList) : base(flag)
        {
            FriendList = friendList;
        }
    }

    [ProtoContract]
    public sealed class ServerPendingMessagesReply : GenericServerReply
    {
        [ProtoMember(1)]
        public Dictionary<int, List<Message>> PendingMessages { get; private set; }

        private ServerPendingMessagesReply() { }

        public ServerPendingMessagesReply(string flag, Dictionary<int, List<Message>> pendingMessagesDictionary) : base(flag)
        {
            PendingMessages = pendingMessagesDictionary;
        }
    }
}
