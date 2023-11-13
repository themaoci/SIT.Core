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

        private HashSet<string> _processedPackets { get; } = new HashSet<string>();

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

            ProcessMoveOperation(packet);
            ProcessThrowOperation(packet);

        }

        private void ProcessMoveOperation(Dictionary<string, object> packet)
        {
            if (packet["m"].ToString() != "MoveOperation")
                return;

            var packetJson = packet.ToJson();
            if (_processedPackets.Contains(packetJson))
                return;

            _processedPackets.Add(packetJson);

            //Logger.LogInfo(packetJson);

            var plyr = Players[packet["profileId"].ToString()];

            //MoveOperationPacket moveOperationPacket = new(packet["profileId"].ToString(), null, null);
            //moveOperationPacket.DeserializePacketSIT(packet["data"].ToString());
            MoveOperationPacket moveOperationPacket = JsonConvert.DeserializeObject<MoveOperationPacket>(packetJson);

            Logger.LogInfo(moveOperationPacket.ToJson());
            //Logger.LogInfo(moveOperationPacket.MoveOpJson);
            var moveOpDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(moveOperationPacket.MoveOpJson);
            //if (!moveOpDict.ContainsKey("From"))
            //{
            //    Logger.LogError("No From Key found in MoveOperation");
            //    return;
            //}
            if (!moveOpDict.ContainsKey("To"))
            {
                Logger.LogError("No To Key found in MoveOperation");
                return;
            }
            AbstractDescriptor fromAD = null;
            
            if (packet.ContainsKey("FromAddressType") && moveOpDict.ContainsKey("From"))
            {
                switch (packet["FromAddressType"].ToString())
                {
                    case "SlotItemAddress":
                        fromAD = JsonConvert.DeserializeObject<SlotItemAddressDescriptor>(moveOpDict["From"].ToString());
                        break;
                    case "StackSlotItemAddress":
                        fromAD = JsonConvert.DeserializeObject<StackSlotItemAddressDescriptor>(moveOpDict["From"].ToString());
                        break;
                    case "GridItemAddress":
                        fromAD = JsonConvert.DeserializeObject<GridItemAddressDescriptor>(moveOpDict["From"].ToString());
                        break;
                    default:
                        Logger.LogError($"Unknown FromAddressType {packet["FromAddressType"].ToString()}");
                        break;
                }
            }

            //Logger.LogInfo(packet["ToAddressType"].ToString());
            AbstractDescriptor toAD = null;
            switch(packet["ToAddressType"].ToString())
            {
                case "SlotItemAddress":
                    toAD = JsonConvert.DeserializeObject<SlotItemAddressDescriptor>(moveOpDict["To"].ToString());
                    break;
                case "StackSlotItemAddress":
                    toAD = JsonConvert.DeserializeObject<StackSlotItemAddressDescriptor>(moveOpDict["To"].ToString());
                    break;
                case "GridItemAddress":
                    toAD = JsonConvert.DeserializeObject<GridItemAddressDescriptor>(moveOpDict["To"].ToString());
                    break;
                default:
                    Logger.LogError($"Unknown ToAddressType {packet["ToAddressType"].ToString()}");
                    break;
            }
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
                // This is a bad way to handle the error that the item doesn't exist on PlayerInventoryController
                try
                {
                    MoveInternalOperation moveOperation = new MoveInternalOperation(moveOpDesc.OperationId, pic, item, pic.ToItemAddress(moveOpDesc.To), new List<ItemsCount>());
                    pic.ReceiveExecute(moveOperation, packetJson);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"{packetJson}");
                    Logger.LogError($"{ex}");

                    ItemController itemController = null;
                    if(ItemFinder.TryFindItemController(moveOpDesc.To.Container.ParentId, ref itemController))
                    {
                        Logger.LogError("ItemController not found! Falling back to To Container");
                        MoveInternalOperation moveOperation = new MoveInternalOperation(moveOpDesc.OperationId, itemController, item, itemController.ToItemAddress(moveOpDesc.To), new List<ItemsCount>());
                        pic.ReceiveExecute(moveOperation, packetJson);
                    }
                    else if (ItemFinder.TryFindItemController(moveOpDesc.From.Container.ParentId, ref itemController))
                    {
                        Logger.LogError("ItemController not found! Falling back to From Container");
                        MoveInternalOperation moveOperation = new MoveInternalOperation(moveOpDesc.OperationId, itemController, item, itemController.ToItemAddress(moveOpDesc.To), new List<ItemsCount>());
                        pic.ReceiveExecute(moveOperation, packetJson);
                    }
                    else
                    {
                        Logger.LogError("ItemController not found!");
                    }
                }
            }
            //pic.ReceiveDoOperation(moveOpDesc);
        }

        private void ProcessThrowOperation(Dictionary<string, object> packet)
        {
            if (packet["m"].ToString() != "ThrowOperation")
                return;

            var packetJson = packet.ToJson();
            if (_processedPackets.Contains(packetJson))
                return;

            _processedPackets.Add(packetJson);

            var plyr = Players[packet["profileId"].ToString()];
            MoveOperationPacket moveOperationPacket = JsonConvert.DeserializeObject<MoveOperationPacket>(packetJson);

            var moveOpDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(moveOperationPacket.MoveOpJson);
            ThrowOperationDescriptor throwOpDesc = JsonConvert.DeserializeObject<ThrowOperationDescriptor>(JsonConvert.SerializeObject(moveOpDict));

            var pic = ItemFinder.GetPlayerInventoryController(plyr) as CoopInventoryController;
            if (pic == null)
            {
                Logger.LogError("Player Inventory Controller is null");
                return;
            }

            if (ItemFinder.TryFindItem(throwOpDesc.ItemId, out var item))
            {
                pic.ReceiveExecute((MoveInternalOperation2)plyr.ToThrowOperation(throwOpDesc).Value, packetJson);
            }
        }
    }
}
