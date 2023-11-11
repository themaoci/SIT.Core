using JetBrains.Annotations;
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

        public string DynamicOperationType { get; set; }

        public MoveOperationPacket(string profileId, string itemId, string templateId, string toAddressType, string fromAddressType, [CanBeNull] string dynamicOperationType = null)
            : base(profileId, itemId, templateId, "MoveOperation")
        {
            ToAddressType = toAddressType;
            FromAddressType = fromAddressType;
            DynamicOperationType = dynamicOperationType;
        }
    }
}
