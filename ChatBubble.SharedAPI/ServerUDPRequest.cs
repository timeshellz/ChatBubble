using System;
using System.Collections.Generic;
using System.Text;

namespace ChatBubble.SharedAPI
{
    [Serializable]
    public class ServerUDPRequest : NetTransferObject
    {
        public ServerUDPRequest(string flag) : base(flag)
        {

        }
    }
}
