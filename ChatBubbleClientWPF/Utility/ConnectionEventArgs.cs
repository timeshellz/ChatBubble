using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBubbleClientWPF.Utility
{
    class ConnectionEventArgs : EventArgs
    {
        public enum ConnectionTypes { Expired, Fresh, None }
        public ConnectionTypes ConnectionType { get; set; } = ConnectionTypes.None;
    }
}
