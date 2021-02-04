using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using ProtoBuf;

using ChatBubble.SharedAPI;

namespace ChatBubble.ClientAPI
{
    public static class ClientRequestManager
    {
        static object sendLock = new object();
        static object receiveLock = new object();

        public static GenericServerReply PerformHandshake(HandshakeRequest request)
        {
            byte[] handshakeBytes = NetTransferObject.SerializeNetObject(request);
            GenericServerReply serverReply;

            try
            {
                SharedNetworkConfiguration.MainSocket.Connect(ClientNetworkConfiguration.ServerIPEndPoint);

                serverReply = SendClientRequest(request);

                SharedNetworkConfiguration.AuxilarryUDPSocket.Bind(SharedNetworkConfiguration.MainSocket.LocalEndPoint);
            }
            catch (SocketException e)
            {
                if (e.ErrorCode == Convert.ToInt32(SocketError.TimedOut) || e.ErrorCode == Convert.ToInt32(SocketError.ConnectionRefused))
                    throw new RequestException(ConnectionCodes.ConnectionTimeoutStatus);
                else
                    throw new RequestException(ConnectionCodes.ConnectionFailure);
            }
            catch
            {
                throw new RequestException(ConnectionCodes.ConnectionFailure);
            }

            return serverReply;
        }

        public static GenericServerReply SendClientRequest(ClientRequest request)
        {
            byte[] connectionSignature = SharedNetworkConfiguration.Encoding.GetBytes(ConnectionCodes.ConnectionSignature);
            byte[] serializedRequest = NetTransferObject.SerializeNetObject(request);
            byte[] signedSerializedRequest = new byte[serializedRequest.Length + connectionSignature.Length];

            connectionSignature.CopyTo(signedSerializedRequest, 0);
            serializedRequest.CopyTo(signedSerializedRequest, connectionSignature.Length);

            lock (sendLock)
            {
                try
                {
                    SharedNetworkConfiguration.MainSocket.Send(signedSerializedRequest);
                }
                catch (SocketException e)
                {
                    if (e.ErrorCode == Convert.ToInt32(SocketError.TimedOut))
                        throw new RequestException(ConnectionCodes.ConnectionTimeoutStatus);
                    else
                        throw new RequestException(ConnectionCodes.ConnectionFailure);
                }
                catch
                {
                    throw new RequestException(ConnectionCodes.ConnectionFailure);
                }
            }

            byte[] receiveBuffer = new byte[2048];
            byte[] signedSerializedReply = new byte[receiveBuffer.Length];

            lock(receiveLock)
            {
                try
                {
                    if (SharedNetworkConfiguration.MainSocket.Poll(-1, SelectMode.SelectRead))
                    {
                        if (SharedNetworkConfiguration.MainSocket.Available == 0)
                        {
                            throw new SocketException(Convert.ToInt32(SocketError.HostDown));
                        }

                        int bytesRead = 0;
                        int totalBytesRead = 0;
                       
                        while (SharedNetworkConfiguration.MainSocket.Available > 0)
                        {
                            bytesRead = SharedNetworkConfiguration.MainSocket.Receive(receiveBuffer);

                            if (totalBytesRead + bytesRead >= signedSerializedReply.Length)
                                Array.Resize(ref signedSerializedReply, totalBytesRead + bytesRead);

                            Array.Copy(receiveBuffer, 0, signedSerializedReply, totalBytesRead, bytesRead);
                            totalBytesRead += bytesRead;
                        }

                        if (signedSerializedReply.Length != totalBytesRead)
                            Array.Resize(ref signedSerializedReply, totalBytesRead);
                    }
                }
                catch
                {
                    throw new RequestException(ConnectionCodes.ConnectionFailure);
                }
            }

            GenericServerReply serverReply = (GenericServerReply)NetTransferObject.DeserializeNetObject(signedSerializedReply);

            return serverReply;
        }

        

        /*

        /// <summary>
        /// Clientside method.<para/>
        /// Organizes a request to pull all pending messages from the server and save them on the client's device.
        /// </summary>
        /// <returns></returns>
        public static string ClientPendingMessageManager()
        {
            string pendingMessagesString = NetComponents.ClientRequestArbitrary(ConnectionCodes.GetPendingMessageRequest, "", true, true);

            if (pendingMessagesString.Contains(ConnectionCodes.NoPendingMessagesStatus))
                return "";

            string[] pendingMessages = pendingMessagesString.Split(new string[] { "msg=" }, StringSplitOptions.RemoveEmptyEntries);
            string[] messageSplitstrings = new string[] { "sender=", "time=", "message=" };

            foreach (string message in pendingMessages)
            {
                string[] messageSubstrings = message.Split(messageSplitstrings, StringSplitOptions.RemoveEmptyEntries);
                //[0] - senderid, [1] - message time, [2] - message contents

                //ChatID is same as senderID
                GenericFileManager.WriteToFile(GenericFileManager.defaultLocalUserDialoguesDirectory + "chatid=" + messageSubstrings[0] + ".txt",
                    "message==" + "\ntime=" + messageSubstrings[1] + "\nstatus=unread" +
                    "\ncontent=" + messageSubstrings[2] + "\n==message\n", false);
            }

            receivedMessagesCollection.Enqueue(pendingMessagesString);

            return (pendingMessagesString);

        }

        /// <summary>
        /// Listens for UDP server calls.
        /// </summary>
        public static void ClientServerFlagListener()
        {
            byte[] streamBytes;

            while (mainSocket.Connected == true)
            {
                streamBytes = new byte[64];

                try
                {
                    auxilarryUDPSocket.Receive(streamBytes);
                }
                catch
                { return; }

                string serverMessage = us_US.GetString(streamBytes);
                serverMessage = serverMessage.Substring(0, serverMessage.IndexOf('\0'));

                if (serverMessage == ConnectionCodes.AvailablePendingMessagesStatus)
                {
                    NetComponents.ClientPendingMessageManager();
                }
            }
        }*/
    }
}
