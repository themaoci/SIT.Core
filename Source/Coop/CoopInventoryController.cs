using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Newtonsoft.Json;
using SIT.Coop.Core.Web;
using SIT.Core.Coop.ItemControllerPatches;
using SIT.Core.Coop.NetworkPacket;
using SIT.Core.Core;
using SIT.Tarkov.Core;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Core.Coop
{
    internal class CoopInventoryController
        : EFT.Player.PlayerInventoryController, ICoopInventoryController
    {
        ManualLogSource BepInLogger { get; set; }

        public HashSet<string> AlreadySent = new();

		private EFT.Player player { get; set; }


        public CoopInventoryController(EFT.Player player, Profile profile, bool examined) : base(player, profile, examined)
        {
            BepInLogger = BepInEx.Logging.Logger.CreateLogSource(nameof(CoopInventoryController));
			this.player = player;
        }

        public override void AddDiscardLimits(Item rootItem, IEnumerable<ItemsCount> destroyedItems)
        {
        }

        public override void SubtractFromDiscardLimits(Item rootItem, IEnumerable<ItemsCount> destroyedItems)
        {
        }

        protected override void Execute(SearchContentOperation operation, Callback callback)
        {
            BepInLogger.LogInfo($"CoopInventoryController: Execute");
            BepInLogger.LogInfo($"CoopInventoryController: {operation}");
            base.Execute(operation, callback);
        }

        public override void InProcess(ItemController executor, Item item, ItemAddress to, bool succeed, IBaseInventoryOperation operation, Callback callback)
        {
            BepInLogger.LogInfo($"CoopInventoryController: InProcess");
            BepInLogger.LogInfo($"CoopInventoryController: {item}");
            BepInLogger.LogInfo($"CoopInventoryController: {to}");
            BepInLogger.LogInfo($"CoopInventoryController: {operation}");
            base.InProcess(executor, item, to, succeed, operation, callback);
        }

        /// <summary>
        /// General Operations
        /// </summary>
        /// <param name="operation"></param>
        /// <returns></returns>
        public override bool vmethod_0(AbstractInternalOperation operation)
        {
            BepInLogger.LogInfo($"CoopInventoryController: vmethod_0");
            BepInLogger.LogInfo($"CoopInventoryController: {operation}");

            // Send to the Echo (Server)
            // GClass2698 (Item Move Operation?)
			if(operation is GClass2698 moveOperation)
			{
                BepInLogger.LogInfo("Sending...");

                Dictionary<string, object> packet = new Dictionary<string, object>();
				
				MoveOperationDescriptor moveOperationDescriptor = new MoveOperationDescriptor();
				// From Packet
				Dictionary<string, object> fromPacket = new();
				ItemAddressHelpers.ConvertItemAddressToDescriptor(moveOperation.From
					, ref fromPacket
					, out var gridItemAddressDescriptorFrom
					, out var slotItemAddressDescriptorFrom
					, out var stackSlotItemAddressDescriptorFrom);
				//ItemAddressHelpers.ConvertDictionaryToAddress(fromPacket, out var grid, out var slot, out var stack);
				moveOperationDescriptor.From = gridItemAddressDescriptorFrom;
				// To Packet
				Dictionary<string, object> toPacket = new();
				ItemAddressHelpers.ConvertItemAddressToDescriptor(moveOperation.To
                    , ref toPacket
                    , out var gridItemAddressDescriptorTo
                    , out var slotItemAddressDescriptorTo
                    , out var stackSlotItemAddressDescriptorTo);
				//ItemAddressHelpers.ConvertDictionaryToAddress(toPacket, out var gridTo, out var slotTo, out var stackTo);
				moveOperationDescriptor.To = gridItemAddressDescriptorTo;

				moveOperationDescriptor.OperationId = moveOperation.Id;
				moveOperationDescriptor.ItemId = moveOperation.Item.Id;

				var moveOpJson = moveOperationDescriptor.SITToJson();
				MoveOperationPacket moveOperationPacket = new MoveOperationPacket(player.ProfileId, moveOperation.Item.Id, moveOperation.Item.TemplateId);
				moveOperationPacket.MoveOpJson = moveOpJson;
				var str = moveOperationPacket.ToString();
                BepInLogger.LogInfo(str);	
				var bytes = moveOperationPacket.Serialize();
                AkiBackendCommunication.Instance.SendDataToPool(bytes);
            }
			// GClass2721 (Load Mag Operation?)
			// GClass2730 (Split Bullet Operation?)
            // GClass2734 (Unload Mag Operation?)
			// 


            /*
             * if (descriptor is AddOperationDescriptor addOperationDescriptor)
		{
			AddOperationDescriptor descriptor2 = addOperationDescriptor;
			return ToAddOperation(descriptor2);
		}
		if (descriptor is LoadMagOperationDescriptor loadMagOperationDescriptor)
		{
			LoadMagOperationDescriptor descriptor3 = loadMagOperationDescriptor;
			return ToLoadMagOperation(descriptor3);
		}
		if (descriptor is UnloadMagOperationDescriptor unloadMagOperationDescriptor)
		{
			UnloadMagOperationDescriptor descriptor4 = unloadMagOperationDescriptor;
			return ToUnloadMagOperation(descriptor4);
		}
		if (descriptor is RemoveOperationDescriptor removeOperationDescriptor)
		{
			RemoveOperationDescriptor descriptor5 = removeOperationDescriptor;
			return ToRemoveOperation(descriptor5);
		}
		if (descriptor is MoveOperationDescriptor moveOperationDescriptor)
		{
			MoveOperationDescriptor descriptor6 = moveOperationDescriptor;
			return ToMoveOperation(descriptor6);
		}
		if (descriptor is MoveAllOperationDescriptor moveAllOperationDescriptor)
		{
			MoveAllOperationDescriptor descriptor7 = moveAllOperationDescriptor;
			return ToMoveAllOperation(descriptor7);
		}
		if (descriptor is SplitOperationDescriptor splitOperationDescriptor)
		{
			SplitOperationDescriptor descriptor8 = splitOperationDescriptor;
			return ToSplitOperation(descriptor8);
		}
		if (descriptor is MergeOperationDescriptor mergeOperationDescriptor)
		{
			MergeOperationDescriptor descriptor9 = mergeOperationDescriptor;
			return ToMergeOperation(descriptor9);
		}
		if (descriptor is TransferOperationDescriptor transferOperationDescriptor)
		{
			TransferOperationDescriptor descriptor10 = transferOperationDescriptor;
			return ToTransferOperation(descriptor10);
		}
		if (descriptor is SwapOperationDescriptor swapOperationDescriptor)
		{
			SwapOperationDescriptor descriptor11 = swapOperationDescriptor;
			return ToSwapOperation(descriptor11);
		}
		if (descriptor is ThrowOperationDescriptor throwOperationDescriptor)
		{
			ThrowOperationDescriptor descriptor12 = throwOperationDescriptor;
			return ToThrowOperation(descriptor12);
		}
		if (descriptor is ToggleOperationDescriptor toggleOperationDescriptor)
		{
			ToggleOperationDescriptor descriptor13 = toggleOperationDescriptor;
			return ToToggleOperation(descriptor13);
		}
		if (descriptor is FoldOperationDescriptor foldOperationDescriptor)
		{
			FoldOperationDescriptor descriptor14 = foldOperationDescriptor;
			return ToFoldOperation(descriptor14);
		}
		if (descriptor is ShotOperationDescriptor shotOperationDescriptor)
		{
			ShotOperationDescriptor descriptor15 = shotOperationDescriptor;
			return ToShotOperation(descriptor15);
		}
		if (descriptor is ApplyOperationDescriptor applyOperationDescriptor)
		{
			ApplyOperationDescriptor descriptor16 = applyOperationDescriptor;
			return ToApplyKeyOperation(descriptor16);
		}
		if (descriptor is CreateMapMarkerOperationDescriptor createMapMarkerOperationDescriptor)
		{
			CreateMapMarkerOperationDescriptor descriptor17 = createMapMarkerOperationDescriptor;
			return ToCreateMapMarkerOperation(descriptor17);
		}
		if (descriptor is EditMapMarkerOperationDescriptor editMapMarkerOperationDescriptor)
		{
			EditMapMarkerOperationDescriptor descriptor18 = editMapMarkerOperationDescriptor;
			return ToEditMapMarkerOperation(descriptor18);
		}
		if (descriptor is DeleteMapMarkerOperationDescriptor deleteMapMarkerOperationDescriptor)
		{
			DeleteMapMarkerOperationDescriptor descriptor19 = deleteMapMarkerOperationDescriptor;
			return ToDeleteMapMarkerOperation(descriptor19);
		}
		if (descriptor is AddNoteOperationDescriptor addNoteOperationDescriptor)
		{
			AddNoteOperationDescriptor descriptor20 = addNoteOperationDescriptor;
			return ToAddNoteOperation(descriptor20);
		}
		if (descriptor is EditNoteOperationDescriptor editNoteOperationDescriptor)
		{
			EditNoteOperationDescriptor descriptor21 = editNoteOperationDescriptor;
			return ToEditNoteOperation(descriptor21);
		}
		if (descriptor is DeleteNoteOperationDescriptor deleteNoteOperationDescriptor)
		{
			DeleteNoteOperationDescriptor descriptor22 = deleteNoteOperationDescriptor;
			return ToDeleteNoteOperation(descriptor22);
		}
		if (descriptor is ExamineOperationDescriptor examineOperationDescriptor)
		{
			ExamineOperationDescriptor descriptor23 = examineOperationDescriptor;
			return ToExamineOperation(descriptor23);
		}
		if (descriptor is ExamineMalfunctionOperationDescriptor examineMalfunctionOperationDescriptor)
		{
			ExamineMalfunctionOperationDescriptor descriptor24 = examineMalfunctionOperationDescriptor;
			return ToExamineMalfunctionOperation(descriptor24);
		}
		if (descriptor is ExamineMalfTypeOperationDescriptor examineMalfTypeOperationDescriptor)
		{
			ExamineMalfTypeOperationDescriptor descriptor25 = examineMalfTypeOperationDescriptor;
			return ToExamineMalfTypeOperation(descriptor25);
		}
		if (descriptor is CheckMagazineOperationDescriptor checkMagazineOperationDescriptor)
		{
			CheckMagazineOperationDescriptor descriptor26 = checkMagazineOperationDescriptor;
			return ToCheckMagazineOperation(descriptor26);
		}
		if (descriptor is BindItemOperationDescriptor bindItemOperationDescriptor)
		{
			BindItemOperationDescriptor descriptor27 = bindItemOperationDescriptor;
			return method_1(descriptor27);
		}
		if (descriptor is UnbindItemOperationDescriptor unbindItemOperationDescriptor)
		{
			UnbindItemOperationDescriptor descriptor28 = unbindItemOperationDescriptor;
			return method_2(descriptor28);
		}
		if (descriptor is InsureItemsOperationDescriptor insureItemsOperationDescriptor)
		{
			InsureItemsOperationDescriptor descriptor29 = insureItemsOperationDescriptor;
			return method_3(descriptor29);
		}
		if (descriptor is SetupItemOperationDescriptor setupItemOperationDescriptor)
		{
			SetupItemOperationDescriptor descriptor30 = setupItemOperationDescriptor;
			return ToSetupItemOperation(descriptor30);
		}
		if (descriptor is TagOperationDescriptor tagOperationDescriptor)
		{
			TagOperationDescriptor descriptor31 = tagOperationDescriptor;
			return ToTagOperation(descriptor31);
		}
		if (descriptor is OperateStationaryWeaponOperationDescriptor operateStationaryWeaponOperationDescriptor)
		{
			OperateStationaryWeaponOperationDescriptor descriptor32 = operateStationaryWeaponOperationDescriptor;
			return ToStationaryOperation(descriptor32);
		}
		if (descriptor is WeaponRechamberOperationDescriptor weaponRechamberOperationDescriptor)
		{
			WeaponRechamberOperationDescriptor descriptor33 = weaponRechamberOperationDescriptor;
			return ToWeaponRechamberOperation(descriptor33);
		}
		if (descriptor is Descriptor descriptor34)
		{
			Descriptor descriptor35 = descriptor34;
			return ToQuestAcceptOperation(descriptor35);
		}
		if (descriptor is QuestFinishDescriptor questFinishDescriptor)
		{
			QuestFinishDescriptor descriptor36 = questFinishDescriptor;
			return ToQuestFinishOperation(descriptor36);
		}
		if (descriptor is QuestHandoverDescriptor questHandoverDescriptor)
		{
			QuestHandoverDescriptor descriptor37 = questHandoverDescriptor;
			return ToQuestHandoverOperation(descriptor37);
		}
		if (descriptor is InventoryLogicOperationsCreateItemsDescriptor inventoryLogicOperationsCreateItemsDescriptor)
		{
			InventoryLogicOperationsCreateItemsDescriptor descriptor38 = inventoryLogicOperationsCreateItemsDescriptor;
			return ToCreateItemsOperation(descriptor38);
		}
             */



            return base.vmethod_0(operation);
        }

		public virtual void ReceiveDoOperation(AbstractDescriptor2 descriptor)
        {
			var invOp = player.ToInventoryOperation(descriptor);
			base.vmethod_0(invOp.Value);
		}

        public override Task<IResult> LoadMagazine(BulletClass sourceAmmo, MagazineClass magazine, int loadCount, bool ignoreRestrictions)
        {
            //BepInLogger.LogInfo("LoadMagazine");
            return base.LoadMagazine(sourceAmmo, magazine, loadCount, ignoreRestrictions);
        }

        public override Task<IResult> UnloadMagazine(MagazineClass magazine)
        {
            Task<IResult> result;
            ItemControllerHandler_Move_Patch.DisableForPlayer.Add(Profile.ProfileId);

            BepInLogger.LogInfo("UnloadMagazine");
            UnloadMagazinePacket unloadMagazinePacket = new(Profile.ProfileId, magazine.Id, magazine.TemplateId);
            var serialized = unloadMagazinePacket.Serialize();

            //if (AlreadySent.Contains(serialized))
            {
                result = base.UnloadMagazine(magazine);
                ItemControllerHandler_Move_Patch.DisableForPlayer.Remove(Profile.ProfileId);
            }

            //AlreadySent.Add(serialized);

            AkiBackendCommunication.Instance.SendDataToPool(serialized);
            result = base.UnloadMagazine(magazine);
            ItemControllerHandler_Move_Patch.DisableForPlayer.Remove(Profile.ProfileId);
            return result;
        }

        public override void ThrowItem(Item item, IEnumerable<ItemsCount> destroyedItems, Callback callback = null, bool downDirection = false)
        {
            //BepInLogger.LogInfo("ThrowItem");
            destroyedItems = new List<ItemsCount>();
            base.ThrowItem(item, destroyedItems, callback, downDirection);
        }

        public void ReceiveUnloadMagazineFromServer(UnloadMagazinePacket unloadMagazinePacket)
        {
            BepInLogger.LogInfo("ReceiveUnloadMagazineFromServer");
            if (ItemFinder.TryFindItem(unloadMagazinePacket.MagazineId, out Item magazine))
            {
                ItemControllerHandler_Move_Patch.DisableForPlayer.Add(unloadMagazinePacket.ProfileId);
                base.UnloadMagazine((MagazineClass)magazine);
                ItemControllerHandler_Move_Patch.DisableForPlayer.Remove(unloadMagazinePacket.ProfileId);

            }
        }
    }


    public interface ICoopInventoryController
    {
        public void ReceiveUnloadMagazineFromServer(UnloadMagazinePacket unloadMagazinePacket);
    }
}
