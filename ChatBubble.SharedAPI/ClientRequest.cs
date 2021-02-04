using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace ChatBubble.SharedAPI
{
    [ProtoContract]
    [ProtoInclude(2, typeof(HandshakeRequest))]
    [ProtoInclude(3, typeof(LoginRequest))]
    [ProtoInclude(4, typeof(SignupRequest))]
    [ProtoInclude(5, typeof(SearchRequest))]
    [ProtoInclude(6, typeof(AddFriendRequest))]
    [ProtoInclude(7, typeof(GetFriendListRequest))]
    [ProtoInclude(8, typeof(RemoveFriendRequest))]
    [ProtoInclude(9, typeof(GetUserRequest))]
    [ProtoInclude(10, typeof(EditSummaryRequest))]
    [ProtoInclude(11, typeof(SendMessageRequest))]
    [ProtoInclude(12, typeof(GetPendingMessagesRequest))]
    [ProtoInclude(13, typeof(ChangeNameRequest))]
    [ProtoInclude(14, typeof(ChangePasswordRequest))]
    public abstract class ClientRequest : NetTransferObject
    {
        [ProtoMember(1)]
        public Cookie Cookie { get; private set; }

        protected ClientRequest() { }

        protected ClientRequest(string requestFlag) : base(requestFlag)
        {

        }

        protected ClientRequest(Cookie cookie, string requestFlag) : this(requestFlag)
        {
            Cookie = cookie;
        }
    } 

    [ProtoContract]
    [ProtoInclude(2, typeof(HandshakeType))]
    public sealed class HandshakeRequest : ClientRequest
    {
        [ProtoContract]
        public enum HandshakeType { OngoingSession, NewSession }

        [ProtoMember(1, IsRequired = true)]
        public HandshakeType Type { get; private set; }

        public HandshakeRequest(Cookie cookie) : base(cookie, ConnectionCodes.HandshakeRequest)
        {
            Type = HandshakeType.OngoingSession;
        }

        public HandshakeRequest() : base(ConnectionCodes.HandshakeRequest)
        {
            Type = HandshakeType.NewSession;
        }
    }

    [ProtoContract]
    public sealed class LoginRequest : ClientRequest
    {
        [ProtoMember(1)]
        public string Login { get; private set; }
        [ProtoMember(2)]
        public string Password { get; private set; }

        private LoginRequest() { }

        public LoginRequest(string login, string password) : base(ConnectionCodes.LoginRequest)
        {
            Login = login;
            Password = password;
        }

        public LoginRequest(Cookie cookie) : base(cookie, ConnectionCodes.CookieLoginRequest) { }
    }

    [ProtoContract]
    public sealed class SignupRequest : ClientRequest
    {
        [ProtoMember(1)]
        public string Name { get; private set; }
        [ProtoMember(2)]
        public string Login { get; private set; }
        [ProtoMember(3)]
        public string Password { get; private set; }

        private SignupRequest() { }

        public SignupRequest(string name, string login, string password) : base(ConnectionCodes.SignUpRequest)
        {
            Name = name;
            Login = login;
            Password = password;
        }
    }

    [ProtoContract]
    public sealed class SearchRequest : ClientRequest
    {
        [ProtoMember(1)]
        public string SearchParameter { get; private set; }

        private SearchRequest() { }

        public SearchRequest(Cookie cookie, string searchParameter) : base(cookie, ConnectionCodes.SearchRequest)
        {
            SearchParameter = searchParameter;
        }
    }

    [ProtoContract]
    public sealed class AddFriendRequest : ClientRequest
    {
        [ProtoMember(1)]
        public int ID { get; private set; }

        private AddFriendRequest() { }

        public AddFriendRequest(Cookie cookie, int newFriendID) : base(cookie, ConnectionCodes.AddFriendRequest)
        {
            ID = newFriendID;
        }
    }

    [ProtoContract]
    public sealed class GetFriendListRequest : ClientRequest
    {
        [ProtoMember(1)]
        public int ID { get; private set; }

        private GetFriendListRequest() { }

        public GetFriendListRequest(Cookie cookie) : base(cookie, ConnectionCodes.GetFriendListRequest)
        {
            ID = cookie.ID;
        }

        public GetFriendListRequest(Cookie cookie, int userID) : base(cookie, ConnectionCodes.GetFriendListRequest)
        {
            ID = userID;
        }
    }

    [ProtoContract]
    public sealed class RemoveFriendRequest : ClientRequest
    {
        [ProtoMember(1)]
        public int FriendID { get; private set; }

        private RemoveFriendRequest() { }
        
        public RemoveFriendRequest(Cookie cookie, int friendID) : base(cookie, ConnectionCodes.RemoveFriendRequest)
        {
            FriendID = friendID;
        }
    }

    [ProtoContract]
    public sealed class GetUserRequest : ClientRequest
    {
        [ProtoMember(1)]
        public int ID { get; private set; }

        private GetUserRequest() { }

        public GetUserRequest(Cookie cookie, int userID) : base(cookie, ConnectionCodes.GetUserSummaryRequest)
        {
            ID = userID;
        }
    }

    [ProtoContract]
    public sealed class EditSummaryRequest : ClientRequest
    {
        [ProtoMember(1)]
        public string NewUserStatus { get; private set; }
        [ProtoMember(2)]
        public string NewDescription { get; private set; }

        private EditSummaryRequest() { }

        public EditSummaryRequest(Cookie cookie, string status, string description) : base(cookie, ConnectionCodes.EditUserSummaryRequest)
        {
            NewUserStatus = status;
            NewDescription = description;
        }
    }

    [ProtoContract]
    public sealed class SendMessageRequest : ClientRequest
    {
        [ProtoMember(1)]
        public int MessageSenderID { get; private set; }
        [ProtoMember(2)]
        public int MessageRecipientID { get; private set; }
        [ProtoMember(3)]
        public Message Message { get; private set; }

        private SendMessageRequest() { }

        public SendMessageRequest(Message message, int messageSenderID, int messageRecepientID) : base(ConnectionCodes.SendNewMessageRequest)
        {
            MessageSenderID = messageSenderID;
            MessageRecipientID = messageRecepientID;
            Message = message;
        }
    }

    [ProtoContract]
    public sealed class GetPendingMessagesRequest : ClientRequest
    {
        private GetPendingMessagesRequest() { }

        public GetPendingMessagesRequest(Cookie cookie) : base(cookie, ConnectionCodes.GetPendingMessageRequest)
        {

        }
    }

    [ProtoContract]
    public sealed class ChangeNameRequest : ClientRequest
    {
        [ProtoMember(1)]
        public string NewName { get; set;}

        private ChangeNameRequest() { }

        public ChangeNameRequest(Cookie cookie, string newName) : base(cookie, ConnectionCodes.ChangeNameRequest)
        {
            NewName = newName;
        }
    }

    [ProtoContract]
    public sealed class ChangePasswordRequest : ClientRequest
    {
        [ProtoMember(1)]
        public string OldPassword { get; private set; }
        [ProtoMember(2)]
        public string NewPassword { get; private set; }

        private ChangePasswordRequest() { }

        public ChangePasswordRequest(Cookie cookie, string oldPassword, string newPassword) : base(cookie, ConnectionCodes.ChangePasswdRequest)
        {
            OldPassword = oldPassword;
            NewPassword = newPassword;
        }
    }
}
