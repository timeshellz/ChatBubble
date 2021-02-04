using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace ChatBubble.SharedAPI
{
    [ProtoContract]
    [ProtoInclude(2, typeof(ClientRequest))]
    [ProtoInclude(3, typeof(GenericServerReply))]
    public abstract class NetTransferObject
    {
        [ProtoMember(1)]
        public string NetFlag { get; private set; }

        protected NetTransferObject() { }

        protected NetTransferObject(string flag)
        {
            NetFlag = flag;
        }

        public static byte[] SerializeNetObject(NetTransferObject netObject)
        {
            MemoryStream stream = new MemoryStream();

            Serializer.Serialize(stream, netObject);

            return stream.ToArray();
        }

        public static NetTransferObject DeserializeNetObject(byte[] serializedObject)
        {
            MemoryStream stream = new MemoryStream(serializedObject);

            return Serializer.Deserialize<NetTransferObject>(stream);
        }
    }
}
