using HarmonyLib;
using InventorySystem.Items.Armor;
using InventorySystem.Items;
using LabApi.Events.Arguments.Scp914Events;
using LabApi.Events.Handlers;
using PlayerRoles.FirstPersonControl;
using Scp914;
using Scp914.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NorthwoodLib.Pools;
using LabApi.Features.Console;
using UnityEngine;
using Logger = LabApi.Features.Console.Logger;

namespace SCPSLBugPatch.Patches
{
    [HarmonyPatch(typeof(Scp914Upgrader), nameof(Scp914Upgrader.ProcessPlayer))]
    internal class Scp914UpgraderPatch
    {
        private static bool Prefix(ReferenceHub ply, bool upgradeInventory, bool heldOnly, Scp914KnobSetting setting)
        {
            try
            {
                if (Physics.Linecast(ply.transform.position, Scp914Controller.Singleton.IntakeChamber.position, Scp914Upgrader.SolidObjectMask))
                {
                    return false;
                }
                Vector3 vector = ply.transform.position + Scp914Controller.MoveVector;
                Scp914ProcessingPlayerEventArgs scp914ProcessingPlayerEventArgs = new Scp914ProcessingPlayerEventArgs(vector, setting, ply);
                Scp914Events.OnProcessingPlayer(scp914ProcessingPlayerEventArgs);
                if (!scp914ProcessingPlayerEventArgs.IsAllowed)
                {
                    return false;
                }
                setting = scp914ProcessingPlayerEventArgs.KnobSetting;
                vector = scp914ProcessingPlayerEventArgs.NewPosition;
                ply.TryOverridePosition(vector);
                if (!upgradeInventory)
                {
                    return false;
                }
                HashSet<ushort> hashSet = HashSetPool<ushort>.Shared.Rent();
                foreach (KeyValuePair<ushort, ItemBase> keyValuePair in ply.inventory.UserInventory.Items)
                {
                    if (!heldOnly || keyValuePair.Key == ply.inventory.CurItem.SerialNumber)
                    {
                        hashSet.Add(keyValuePair.Key);
                    }
                }
                foreach (ushort key in hashSet)
                {
                    ItemBase itemBase;
                    Scp914ItemProcessor scp914ItemProcessor;
                    if (ply.inventory.UserInventory.Items.TryGetValue(key, out itemBase) && Scp914Upgrader.TryGetProcessor(itemBase.ItemTypeId, out scp914ItemProcessor))
                    {
                        ItemType itemTypeId = itemBase.ItemTypeId;
                        Scp914ProcessingInventoryItemEventArgs scp914ProcessingInventoryItemEventArgs = new Scp914ProcessingInventoryItemEventArgs(itemBase, setting, ply);
                        Scp914Events.OnProcessingInventoryItem(scp914ProcessingInventoryItemEventArgs);
                        if (scp914ProcessingInventoryItemEventArgs.IsAllowed)
                        {
                            setting = scp914ProcessingInventoryItemEventArgs.KnobSetting;
                            Scp914Upgrader.OnInventoryItemUpgraded?.Invoke(itemBase, setting);
                            Scp914Result scp914Result = scp914ItemProcessor.UpgradeInventoryItem(setting, itemBase);
                            ((Action<Scp914Result, Scp914KnobSetting>)typeof(Scp914Upgrader).Field(nameof(Scp914Upgrader.OnUpgraded)).GetValue(null))?.Invoke(scp914Result, setting);
                            if (scp914Result.ResultingItems == null || !scp914Result.ResultingItems.TryGet(0, out ItemBase itemBase2))
                            {
                                itemBase2 = null;
                            }
                            if (itemBase2 != null)
                            {
                                Scp914Events.OnProcessedInventoryItem(new Scp914ProcessedInventoryItemEventArgs(itemTypeId, itemBase2, setting, ply));
                            }
                        }
                    }
                }
                HashSetPool<ushort>.Shared.Return(hashSet);
                BodyArmorUtils.SetPlayerDirty(ply);
                Scp914Events.OnProcessedPlayer(new Scp914ProcessedPlayerEventArgs(vector, setting, ply));
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            return false;
        }
    }
}
