using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SIT.Coop.Core.Web;
//using SIT.Core.Coop.ItemControllerPatches;
using SIT.Core.Coop.NetworkPacket;
using SIT.Core.Core;
using SIT.Core.Misc;
using SIT.Tarkov.Core;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Core.Coop
{
    internal class CoopInventoryController
        : EFT.Player.PlayerOwnerInventoryController, ICoopInventoryController
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

        public override void InProcess(ItemController executor, Item item, ItemAddress to, bool succeed, IBaseInventoryOperation operation, Callback callback)
        {
            BepInLogger.LogInfo($"InProcess [executor]");

            // Taken from EFT.Player.PlayerInventoryController
            if (!succeed)
            {
                callback.Succeed();
                return;
            }
            base.InProcess(executor, item, to, succeed, operation, callback);
		}

        public override void OutProcess(Item item, ItemAddress from, ItemAddress to, IBaseInventoryOperation operation, Callback callback)
        {
            BepInLogger.LogInfo($"OutProcess [item]");
			base.OutProcess(item, from, to, operation, callback);

		}

		public override void OutProcess(ItemController executor, Item item, ItemAddress from, ItemAddress to, IBaseInventoryOperation operation, Callback callback)
		{
			BepInLogger.LogInfo($"OutProcess [executor]");

			base.OutProcess(executor, item, from, to, operation, callback);	
		}


		public Dictionary<string, (AbstractInternalOperation, Callback, Action)> OperationCallbacks { get; } = new();
		public HashSet<string> SentExecutions { get; } = new();

        public override void Execute(AbstractInternalOperation operation, [CanBeNull] Callback callback)
        {
            BepInLogger.LogInfo($"Execute");
            BepInLogger.LogInfo($"{operation}");

            if (callback == null)
            {
                callback = delegate
                {
                };
            }
            //EOperationStatus? localOperationStatus = null;
            if (!vmethod_0(operation))
            {
                operation.Dispose();
                callback.Fail("LOCAL: hands controller can't perform this operation");
                return;
            }
            //EOperationStatus? serverOperationStatus;
            //base.Execute(operation, callback);

            var json = SendExecute(operation);
			if(json == null)
				return;

            OperationCallbacks.Add(json, (operation, callback, new Action(() => {

                //if (result.Succeed)
                //{
                    BepInLogger.LogInfo("ActionCallback");
                    operation.vmethod_0(delegate (IResult executeResult)
                    {
                        BepInLogger.LogInfo($"operation.vmethod_0 : {executeResult}");
                        if (executeResult.Succeed)
                        {
                            ReflectionHelpers.SetFieldOrPropertyFromInstance<CommandStatus>(operation, "commandStatus_0", CommandStatus.Succeed);
                            
                            var baseInvOp = (BaseInventoryOperation)ReflectionHelpers.GetFieldFromTypeByFieldType(operation.GetType(), typeof(BaseInventoryOperation))?.GetValue(operation);
                            if(baseInvOp != null)
                            {
                                baseInvOp.RaiseEvents(CommandStatus.Succeed);
                            }
                            var oneItemOp = (OneItemOperation)ReflectionHelpers.GetFieldFromTypeByFieldType(operation.GetType(), typeof(OneItemOperation))?.GetValue(operation);
                            if(oneItemOp != null)
                            {
                                oneItemOp.RaiseEvents(CommandStatus.Succeed);
                            }
                            callback.Invoke(executeResult);

                            operation.Dispose();
                        }
                        else
                        {
                            ReflectionHelpers.SetFieldOrPropertyFromInstance<CommandStatus>(operation, "commandStatus_0", CommandStatus.Failed);
                        }
                        operation.Dispose();

                    }, false);

                //}


            }
            )
            ));

   //         var vm = ReflectionHelpers.GetMethodForType(operation.GetType(), "vmethod_0");
			//if(vm != null)
			//	vm.Invoke(operation, new object[] { callback, false });
      
        }


        private string SendExecute(AbstractInternalOperation operation)
		{
			string json = null;
            BepInLogger.LogInfo($"SendExecute");
            BepInLogger.LogInfo($"{operation.GetType()}");
            BepInLogger.LogInfo($"{operation}");
            ReflectionHelpers.SetFieldOrPropertyFromInstance<CommandStatus>(operation, "commandStatus_0", CommandStatus.Begin);

            if (operation is MoveInternalOperation moveOperation)
            {
                MoveOperationDescriptor moveOperationDescriptor = new MoveOperationDescriptor();
                // From Packet
                Dictionary<string, object> fromPacket = new();
                ItemAddressHelpers.ConvertItemAddressToDescriptor(moveOperation.From
                    , ref fromPacket
                    , out var gridItemAddressDescriptorFrom
                    , out var slotItemAddressDescriptorFrom
                    , out var stackSlotItemAddressDescriptorFrom);

                moveOperationDescriptor.From = gridItemAddressDescriptorFrom != null ? gridItemAddressDescriptorFrom
                    : slotItemAddressDescriptorFrom != null ? slotItemAddressDescriptorFrom
                    : stackSlotItemAddressDescriptorFrom;

                // To Packet
                Dictionary<string, object> toPacket = new();
                ItemAddressHelpers.ConvertItemAddressToDescriptor(moveOperation.To
                    , ref toPacket
                    , out var gridItemAddressDescriptorTo
                    , out var slotItemAddressDescriptorTo
                    , out var stackSlotItemAddressDescriptorTo);

                moveOperationDescriptor.To = gridItemAddressDescriptorTo != null ? gridItemAddressDescriptorTo 
                    : slotItemAddressDescriptorTo != null ? slotItemAddressDescriptorTo
                    : stackSlotItemAddressDescriptorTo;

                moveOperationDescriptor.OperationId = moveOperation.Id;
                moveOperationDescriptor.ItemId = moveOperation.Item.Id;

                var moveOpJson = moveOperationDescriptor.SITToJson();
                MoveOperationPacket moveOperationPacket = new MoveOperationPacket(player.ProfileId, moveOperation.Item.Id, moveOperation.Item.TemplateId, moveOperation.To.GetType().ToString(), moveOperation.From != null ? moveOperation.From.GetType().ToString() : null);
                moveOperationPacket.MoveOpJson = moveOpJson;

				json = moveOperationPacket.SITToJson();
              
            }
			// Throw/Discard operation
			else if (operation is MoveInternalOperation2 throwOperation)
			{
                ThrowOperationDescriptor throwOperationDescriptor = new ThrowOperationDescriptor();

                throwOperationDescriptor.OperationId = throwOperation.Id;
                throwOperationDescriptor.ItemId = throwOperation.Item.Id;

                var moveOpJson = throwOperationDescriptor.SITToJson();
                MoveOperationPacket moveOperationPacket = new MoveOperationPacket(player.ProfileId, throwOperation.Item.Id, throwOperation.Item.TemplateId, null, null);
				moveOperationPacket.Method = "ThrowOperation";
                moveOperationPacket.MoveOpJson = moveOpJson;

                json = moveOperationPacket.SITToJson();
            }


            else 
            {
                var oneitemoperation = operation as IOneItemOperation;
                if (oneitemoperation != null)
                {
                    BepInLogger.LogInfo("SendExecute:IOneItemOperation");

                    MoveOperationDescriptor moveOperationDescriptor = new MoveOperationDescriptor();
                    // From Packet
                    Dictionary<string, object> fromPacket = new();
                    ItemAddressHelpers.ConvertItemAddressToDescriptor(oneitemoperation.From1
                        , ref fromPacket
                        , out var gridItemAddressDescriptorFrom
                        , out var slotItemAddressDescriptorFrom
                        , out var stackSlotItemAddressDescriptorFrom);

                    moveOperationDescriptor.From = gridItemAddressDescriptorFrom != null ? gridItemAddressDescriptorFrom
                        : slotItemAddressDescriptorFrom != null ? slotItemAddressDescriptorFrom
                        : stackSlotItemAddressDescriptorFrom;

                    // To Packet
                    Dictionary<string, object> toPacket = new();
                    ItemAddressHelpers.ConvertItemAddressToDescriptor(oneitemoperation.To1
                        , ref toPacket
                        , out var gridItemAddressDescriptorTo
                        , out var slotItemAddressDescriptorTo
                        , out var stackSlotItemAddressDescriptorTo);

                    moveOperationDescriptor.To = gridItemAddressDescriptorTo != null ? gridItemAddressDescriptorTo
                        : slotItemAddressDescriptorTo != null ? slotItemAddressDescriptorTo
                        : stackSlotItemAddressDescriptorTo;

                    moveOperationDescriptor.OperationId = operation.Id;
                    moveOperationDescriptor.ItemId = oneitemoperation.Item1.Id;

                    var moveOpJson = moveOperationDescriptor.SITToJson();

                    MoveOperationPacket moveOperationPacket = new MoveOperationPacket(
                        player.ProfileId
                        , oneitemoperation.Item1.Id
                        , oneitemoperation.Item1.TemplateId
                        , oneitemoperation.To1 != null ? oneitemoperation.To1.GetType().ToString() : null
                        , oneitemoperation.From1 != null ? oneitemoperation.From1.GetType().ToString() : null
                        , oneitemoperation.GetType().FullName);
                    moveOperationPacket.MoveOpJson = moveOpJson;

                    json = moveOperationPacket.SITToJson();
                }
            }

            if (json == null)
                return null;

			if (OperationCallbacks.ContainsKey(json))
				return null;

			if (SentExecutions.Contains(json)) 
				return null;

            AkiBackendCommunication.Instance.PostDownWebSocketImmediately(json);
            SentExecutions.Add(json);
            return json;
        }

		//private AbstractInternalOperation ReceivedOperationPacket { get; set; }
		private Dictionary<string, bool> ReceivedOperations { get; } = new Dictionary<string, bool>();

        public void ReceiveExecute(AbstractInternalOperation operation, string packetJson)
        {
            ReceivedOperations.Add(packetJson, false);

            BepInLogger.LogInfo($"ReceiveExecute");
            BepInLogger.LogInfo($"{packetJson}");
            BepInLogger.LogInfo($"{operation}");
            //ReceivedOperationPacket = operation;
            ReflectionHelpers.SetFieldOrPropertyFromInstance<CommandStatus>(operation, "commandStatus_0", CommandStatus.Begin);
            if (OperationCallbacks.ContainsKey(packetJson))
            {
                OperationCallbacks[packetJson].Item2.Succeed();
                OperationCallbacks[packetJson].Item3();
                OperationCallbacks.Remove(packetJson);
            }
            else
            {
                operation.vmethod_0(delegate (IResult result)
                {
                    ReflectionHelpers.SetFieldOrPropertyFromInstance<CommandStatus>(operation, "commandStatus_0", CommandStatus.Succeed);
                });

            }

        }

        public override void Execute(SearchContentOperation operation, Callback callback)
        {
            BepInLogger.LogInfo($"CoopInventoryController: Execute");
            BepInLogger.LogInfo($"CoopInventoryController: {operation}");
            base.Execute(operation, callback);
        }

        public override void ExecuteStop(SearchContentOperation operation)
        {
            BepInLogger.LogInfo($"CoopInventoryController: ExecuteStop");
            BepInLogger.LogInfo($"CoopInventoryController: {operation}");
            base.ExecuteStop(operation);
        }

        //public override void InProcess(ItemController executor, Item item, ItemAddress to, bool succeed, IBaseInventoryOperation operation, Callback callback)
        //{
        //    //BepInLogger.LogInfo($"CoopInventoryController: InProcess");
        //    //BepInLogger.LogInfo($"CoopInventoryController: {item}");
        //    //BepInLogger.LogInfo($"CoopInventoryController: {to}");
        //    //BepInLogger.LogInfo($"CoopInventoryController: {operation}");
        //    base.InProcess(executor, item, to, succeed, operation, callback);
        //}

        //public virtual void ReceiveInProcess(ItemController executor, Item item, ItemAddress to, bool succeed, IBaseInventoryOperation operation, Callback callback)
        //{
        //    //BepInLogger.LogInfo($"ReceiveInProcess");
        //    //BepInLogger.LogInfo($"{item}");
        //    //BepInLogger.LogInfo($"{to}");
        //    //BepInLogger.LogInfo($"{operation}");
        //    base.InProcess(executor, item, to, succeed, operation, callback);
        //}

        /// <summary>
        /// General Operations
        /// </summary>
        /// <param name="operation"></param>
        /// <returns></returns>
        public override bool vmethod_0(AbstractInternalOperation operation)
        {
            //BepInLogger.LogInfo($"CoopInventoryController: vmethod_0");
            //BepInLogger.LogInfo($"CoopInventoryController: {operation}");


			//return true;
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



			//return base.vmethod_0(operation);
			return true;
		}

		public virtual void ReceiveDoOperation(AbstractDescriptor2 descriptor)
        {
			var invOp = player.ToInventoryOperation(descriptor);
			BepInLogger.LogInfo("ReceiveDoOperation");
            BepInLogger.LogInfo(invOp);
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
            //ItemControllerHandler_Move_Patch.DisableForPlayer.Add(Profile.ProfileId);

            BepInLogger.LogInfo("UnloadMagazine");
            UnloadMagazinePacket unloadMagazinePacket = new(Profile.ProfileId, magazine.Id, magazine.TemplateId);
            var serialized = unloadMagazinePacket.Serialize();

            //if (AlreadySent.Contains(serialized))
            {
                result = base.UnloadMagazine(magazine);
                //ItemControllerHandler_Move_Patch.DisableForPlayer.Remove(Profile.ProfileId);
            }

            //AlreadySent.Add(serialized);

            AkiBackendCommunication.Instance.SendDataToPool(serialized);
            result = base.UnloadMagazine(magazine);
            //ItemControllerHandler_Move_Patch.DisableForPlayer.Remove(Profile.ProfileId);
            return result;
        }

        Random randomGenThrowNumber = new Random();

        public override void ThrowItem(Item item, IEnumerable<ItemsCount> destroyedItems, Callback callback = null, bool downDirection = false)
        {
            //BepInLogger.LogInfo("ThrowItem");
            //destroyedItems = new List<ItemsCount>();
            //base.ThrowItem(item, destroyedItems, callback, downDirection);
            Execute(new MoveInternalOperation2(ushort_0++, this, item, destroyedItems, player, downDirection), callback);
        }

        public override SOperationResult3<bool> TryThrowItem(Item item, Callback callback = null, bool silent = false)
        {
            return base.TryThrowItem(item, callback, silent);
        }

        public void ReceiveUnloadMagazineFromServer(UnloadMagazinePacket unloadMagazinePacket)
        {
            BepInLogger.LogInfo("ReceiveUnloadMagazineFromServer");
            if (ItemFinder.TryFindItem(unloadMagazinePacket.MagazineId, out Item magazine))
            {
                //ItemControllerHandler_Move_Patch.DisableForPlayer.Add(unloadMagazinePacket.ProfileId);
                base.UnloadMagazine((MagazineClass)magazine);
                //ItemControllerHandler_Move_Patch.DisableForPlayer.Remove(unloadMagazinePacket.ProfileId);

            }
        }
    }


    public interface ICoopInventoryController
    {
        public void ReceiveUnloadMagazineFromServer(UnloadMagazinePacket unloadMagazinePacket);
    }
}
