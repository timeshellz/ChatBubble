using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace ChatBubble.SharedAPI
{
    [ProtoContract]
    public class ServerUDPRequest : NetTransferObject
    {
        [ProtoMember(1)]
        public int Target { get; private set; }

        private ServerUDPRequest() { }

        public ServerUDPRequest(string flag, int target) : base(flag)
        {
            Target = target;
        }

        public ServerUDPRequest(string flag) : base(flag)
        {
            Target = -1;
        }
    }
}
