using EFT;
using Newtonsoft.Json;
using SIT.Core.Coop.NetworkPacket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SIT.Core.Coop.Components
{
    internal class MoveOperationPlayerPacketHandler : IPlayerPacketHandlerComponent
    {
        private CoopGameComponent CoopGameComponent { get; set; }
        public ConcurrentDictionary<string, EFT.Player> Players => CoopGameComponent.Players;

        private BepInEx.Logging.ManualLogSource Logger { get; set; }

        public MoveOperationPlayerPacketHandler()
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(MoveOperationPlayerPacketHandler));
        }

        public void ProcessPacket(Dictionary<string, object> packet)
        {
            if (!packet.ContainsKey("profileId"))
                return;

            if (!packet.ContainsKey("m"))
                return;

            if (packet["m"].ToString() != "MoveOperation")
                return;

            Logger.LogInfo(packet.ToJson());

            var plyr = Players[packet["profileId"].ToString()];
            var pic = ItemFinder.GetPlayerInventoryController(plyr) as CoopInventoryController;

            MoveOperationPacket moveOperationPacket = new(packet["profileId"].ToString(), null, null);
            moveOperationPacket.DeserializePacketSIT(packet["data"].ToString());

            var moveOpDesc = JsonConvert.DeserializeObject<MoveOperationDescriptor>(moveOperationPacket.MoveOpJson);
            pic.ReceiveDoOperation(moveOpDesc);
        }
    }
}
