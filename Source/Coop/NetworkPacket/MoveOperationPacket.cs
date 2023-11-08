using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Core.Coop.NetworkPacket
{
    internal class MoveOperationPacket : ItemPlayerPacket
    {
        public string MoveOpJson { get; set; }
        public string FromAddressType { get; set; }
        public string ToAddressType { get; set; }

        public MoveOperationPacket(string profileId, string itemId, string templateId, string toAddressType, string fromAddressType)
            : base(profileId, itemId, templateId, "MoveOperation")
        {
            ToAddressType = toAddressType;
            FromAddressType = fromAddressType;
        }
    }
}
