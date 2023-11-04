using EFT;
using Newtonsoft.Json;
using SIT.Core.Coop.Components;
using SIT.Core.Coop.NetworkPacket;
using SIT.Tarkov.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SIT.Core.Coop.PacketHandlers
{
    internal class MoveOperationPlayerPacketHandler : IPlayerPacketHandlerComponent
    {
        private CoopGameComponent CoopGameComponent { get { CoopGameComponent.TryGetCoopGameComponent(out var coopGC); return coopGC; } }
        public ConcurrentDictionary<string, EFT.Player> Players => CoopGameComponent.Players;

        private BepInEx.Logging.ManualLogSource Logger { get; set; }

        public MoveOperationPlayerPacketHandler()
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(MoveOperationPlayerPacketHandler));
        }

        public void ProcessPacket(Dictionary<string, object> packet)
        {
            string profileId = null;

            if (!packet.ContainsKey("profileId"))
                return;

            profileId = packet["profileId"].ToString();

            if (!packet.ContainsKey("m"))
                return;

            if (packet["m"].ToString() != "MoveOperation")
                return;

            Logger.LogInfo(packet.ToJson());

            var plyr = Players[packet["profileId"].ToString()];

            //MoveOperationPacket moveOperationPacket = new(packet["profileId"].ToString(), null, null);
            //moveOperationPacket.DeserializePacketSIT(packet["data"].ToString());
            MoveOperationPacket moveOperationPacket = JsonConvert.DeserializeObject<MoveOperationPacket>(packet.SITToJson());

            //Logger.LogInfo(moveOperationPacket.ToJson());
            //Logger.LogInfo(moveOperationPacket.MoveOpJson);
            var moveOpDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(moveOperationPacket.MoveOpJson);
            var fromAD = JsonConvert.DeserializeObject<GridItemAddressDescriptor>(moveOpDict["From"].ToString());
            var toAD = JsonConvert.DeserializeObject<GridItemAddressDescriptor>(moveOpDict["To"].ToString());
            moveOpDict.Remove("From");
            moveOpDict.Remove("To");
            MoveOperationDescriptor moveOpDesc = JsonConvert.DeserializeObject<MoveOperationDescriptor>(JsonConvert.SerializeObject(moveOpDict));
            moveOpDesc.From = fromAD;
            moveOpDesc.To = toAD;

            var pic = ItemFinder.GetPlayerInventoryController(plyr) as CoopInventoryController;
            if (pic == null)
            {
                Logger.LogError("Player Inventory Controller is null");
                return;
            }

            if (ItemFinder.TryFindItem(moveOpDesc.ItemId, out var item))
            {
                MoveInternalOperation moveOperation = new MoveInternalOperation(0, pic, item, pic.ToItemAddress(moveOpDesc.To), null);
                //pic.ReceiveOutProcess(pic, item, pic.ToItemAddress(moveOpDesc.From), pic.ToItemAddress(moveOpDesc.To), moveOperation, (result) => { });
                //pic.ReceiveInProcess(pic, item, pic.ToItemAddress(moveOpDesc.To), true, moveOperation, (result) => { });
                pic.ReceiveExecute(moveOperation);
            }
            //pic.ReceiveDoOperation(moveOpDesc);

        }
    }
}
