using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace ChatBubble.SharedAPI
{
    [Serializable]
    [ProtoContract]
    public sealed class Cookie
    {
        [ProtoMember(1)]
        public int ID { get; private set; }
        [ProtoMember(2)]
        public string Hash { get; private set; }

        private Cookie() { }

        public Cookie(int id, string hash)
        {
            ID = id;
            Hash = hash;
        }
    }
}
