using System;
using System.Collections.Generic;
using System.Text;

namespace ChatBubble.SharedAPI
{
    public interface INetworkConfigurator
    {
        void InitializeSockets();
        void DisconnectSockets(bool reuse);
    }
}
