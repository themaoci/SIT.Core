//using EFT;
//using EFT.InventoryLogic;
//using EFT.UI;
//using SIT.Core.Coop.NetworkPacket;
//using SIT.Core.Core;
//using SIT.Core.Misc;
//using SIT.Tarkov.Core;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Threading.Tasks;

//namespace SIT.Core.Coop.Player
//{
//    internal class PlayerInventoryController_ThrowItem_Patch : ModuleReplicationPatch
//    {
//        public override Type InstanceType => ReflectionHelpers.SearchForType("EFT.Player+PlayerInventoryController", false);

//        public override string MethodName => "PlayerInventoryController_ThrowItem";

//        public static List<string> CallLocally = new();

//        protected override MethodBase GetTargetMethod()
//        {
//            var method = ReflectionHelpers.GetMethodForType(InstanceType, "ThrowItem", false, true);
//            return method;
//        }

//        [PatchPrefix]
//        public static bool PrePatch(object __instance, Item item, Profile ___profile_0)
//        {
//            Logger.LogInfo("PlayerInventoryController_ThrowItem_Patch:PrePatch");

//            if (CallLocally.Contains(___profile_0.ProfileId))
//                return true;

//            return false;
//        }

//        [PatchPostfix]
//        public static void PostPatch(object __instance, Item item, Profile ___profile_0)
//        {
//            Logger.LogInfo("PlayerInventoryController_ThrowItem_Patch:PostPatch");

//            if (CallLocally.Contains(___profile_0.ProfileId))
//            {
//                CallLocally.Remove(___profile_0.ProfileId);
//                return;
//            }

//            ItemPlayerPacket itemPacket = new(___profile_0.ProfileId, item.Id, item.TemplateId, "PlayerInventoryController_ThrowItem");
//            var serialized = itemPacket.Serialize();
//            AkiBackendCommunication.Instance.SendDataToPool(serialized);
//        }

//        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
//        {
//            var taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
//            taskScheduler.Do(async (s) =>
//            {
//                Logger.LogInfo($"PlayerInventoryController_ThrowItem_Patch.Replicated");

//                if (!dict.ContainsKey("data"))
//                    return;

//                ItemPlayerPacket itemPacket = new(null, null, null, "");
//                itemPacket = itemPacket.DeserializePacketSIT(dict["data"].ToString());

//                if (HasProcessed(GetType(), player, itemPacket))
//                    return;

//                ItemController itemController = null;
//                if (ItemFinder.TryFindItemController(player.ProfileId, ref itemController))
//                {
//                    List<ItemsCount> destroyedItems = new();

//                    if (ItemFinder.TryFindItem(itemPacket.ItemId, out Item item))
//                    {
//                        if (player.IsYourPlayer)
//                        {
//                            if (item.HasDiscardLimits(item.OriginalAddress, out int itemDiscardLimit) && item.StackObjectsCount > itemDiscardLimit)
//                                destroyedItems.Add(new ItemsCount(item, item.StackObjectsCount - itemDiscardLimit, itemDiscardLimit));

//                            if (item.IsContainer && destroyedItems.Count == 0)
//                            {
//                                Item[] itemsInContainer = item.GetAllItems()?.ToArray();
//                                if (itemsInContainer != null)
//                                {
//                                    Dictionary<string, int> discardItems = new();

//                                    for (int i = 0; i < itemsInContainer.Count(); i++)
//                                    {
//                                        Item itemInContainer = itemsInContainer[i];
//                                        if (itemInContainer == item)
//                                            continue;

//                                        if (itemInContainer.HasDiscardLimits(itemInContainer.OriginalAddress, out int itemInContainerDiscardLimit))
//                                        {
//                                            if (!destroyedItems.Any(x => x.Item.TemplateId == itemInContainer.TemplateId))
//                                            {
//                                                string templateId = itemInContainer.TemplateId;
//                                                if (discardItems.ContainsKey(templateId))
//                                                {
//                                                    discardItems[templateId] += itemInContainer.StackObjectsCount;
//                                                }
//                                                else
//                                                {
//                                                    discardItems.Add(templateId, itemInContainer.StackObjectsCount);
//                                                }

//                                                if (discardItems[templateId] > itemInContainerDiscardLimit)
//                                                {
//                                                    destroyedItems.Add(new ItemsCount(itemInContainer, discardItems[templateId] - itemInContainerDiscardLimit, itemInContainer.StackObjectsCount - (discardItems[templateId] - itemInContainerDiscardLimit)));
//                                                }
//                                            }
//                                            else
//                                            {
//                                                destroyedItems.Add(new ItemsCount(itemInContainer, itemInContainer.StackObjectsCount, 0));
//                                            }
//                                        }
//                                    }
//                                }
//                            }

//                            if (destroyedItems.Count != 0)
//                            {
//                                Logger.LogDebug($"PlayerInventoryController_ThrowItem_Patch.Replicated. Found {destroyedItems.Count} item(s) has hit LimitedDiscard.");

//                                ReflectionHelpers.GetTypeAndMethodWhereMethodExists("GetFullLocalizedDescription", out _, out var GetFullLocalizedDescriptionMI);
//                                //if (await ItemUiContext.Instance.ShowScrolledMessageWindow(out _, GClass3045.GetFullLocalizedDescription(destroyedItems), "InventoryWarning/ItemsToBeDestroyed".Localized(), true))
//                                if (await ItemUiContext.Instance.ShowScrolledMessageWindow(out _, (string)GetFullLocalizedDescriptionMI.Invoke(null, new object[] { destroyedItems }), "InventoryWarning/ItemsToBeDestroyed".Localized(), true))
//                                {
//                                    if (destroyedItems[0].Item == item)
//                                    {
//                                        Logger.LogWarning($"PlayerInventoryController_ThrowItem_Patch.Replicated. The item {itemPacket.TemplateId} cannot be thrown, destroyed.");
//                                        itemController.DestroyItem(item);
//                                        return;
//                                    }
//                                }
//                                else
//                                {
//                                    Logger.LogWarning($"PlayerInventoryController_ThrowItem_Patch.Replicated. The player doesn't agree to destroy their LimitedDiscard item(s), ThrowItem failed.");
//                                    return;
//                                }
//                            }
//                        }

//                        CallLocally.Add(player.ProfileId);
//                        Logger.LogInfo($"PlayerInventoryController_ThrowItem_Patch.Replicated. Calling ThrowItem ({itemPacket.ItemId})");
//                        itemController.ThrowItem(item, destroyedItems);
//                    }
//                    else
//                    {
//                        Logger.LogError($"PlayerInventoryController_ThrowItem_Patch.Replicated. Unable to find Inventory Controller item {itemPacket.ItemId}");
//                    }
//                }
//                else
//                {
//                    Logger.LogError("PlayerInventoryController_ThrowItem_Patch.Replicated. Unable to find Item Controller");
//                }
//            });
//        }
//    }
//}