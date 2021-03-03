using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ChatBubble.SharedAPI;
using ChatBubble.ClientAPI;

namespace ChatBubbleClientWPF.Models
{
    public class ServerFlagReceiver
    {
        public event EventHandler<ServerFlagEventArgs> ServerFlagReceived;


        public ServerFlagReceiver()
        {
            Thread messageReceiverThread = new Thread(DispatchServerFlags);
            messageReceiverThread.Start();
        }

        public void DispatchServerFlags()
        {
            while(SharedNetworkConfiguration.MainSocket.Connected)
            {
                ServerUDPRequest serverReply = ClientRequestManager.ReceiveServerFlags();

                if (serverReply.NetFlag == ConnectionCodes.MessagesPendingStatus)
                    ServerFlagReceived?.Invoke(this, new ServerFlagEventArgs(ServerFlagEventArgs.FlagTypes.MessagesPending, serverReply.Target));
                if (serverReply.NetFlag == ConnectionCodes.MessagesReceivedStatus)
                    ServerFlagReceived?.Invoke(this, new ServerFlagEventArgs(ServerFlagEventArgs.FlagTypes.MessageStatusReceived, serverReply.Target));
                if (serverReply.NetFlag == ConnectionCodes.MessagesReadStatus)
                    ServerFlagReceived?.Invoke(this, new ServerFlagEventArgs(ServerFlagEventArgs.FlagTypes.MessageStatusRead, serverReply.Target));
            }
        }
    }

    public class ServerFlagEventArgs : EventArgs
    {
        public enum FlagTypes { MessagesPending, PendingFriend, MessageStatusReceived, MessageStatusRead }
        public FlagTypes FlagType { get; private set; }
        public int EventTargetID { get; private set; }

        public ServerFlagEventArgs(FlagTypes flagType, int targetID)
        {
            FlagType = flagType;
            EventTargetID = targetID;
        }
    }
}
