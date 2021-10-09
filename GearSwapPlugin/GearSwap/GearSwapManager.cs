using System;
using System.Collections.Generic;
using System.Linq;
using Enemies;
using Gear;
using LibCpp2IL;
using Player;
using UnityEngine;

namespace GearSwapPlugin.GearSwap
{
    public class GearSwapManager : MonoBehaviour
    {
        public static event Action<InventorySlot> OnGearUnLoaded;
        public static event Action<InventorySlot> BeforeGearSwap;
        public static readonly List<InventorySlot> SwappableGearSlots = new List<InventorySlot> {InventorySlot.GearMelee, InventorySlot.GearStandard, InventorySlot.GearSpecial, InventorySlot.GearClass};

        private static readonly Dictionary<InventorySlot, GearIDRange> EquipDelayedGear = new Dictionary<InventorySlot, GearIDRange>();
        private static readonly Dictionary<string, InventorySlot> SlotByPlayfabID = new Dictionary<string, InventorySlot>();

        public GearSwapManager(IntPtr intPtr) : base(intPtr)
        {
            // For Il2CppAssemblyUnhollower
        }

        /// <summary>
        /// Requests to equip the given gearID to the local player on the next possible opportunity.
        /// Any slots that already have an item pending to be equipped will be over written by the passed gear.
        /// See GearEquipValidator for what criteria delays equipping of a gear.
        /// </summary>
        /// <param name="gearId"></param>
        public static void RequestToEquip(GearIDRange gearId)
        {
            if (GearEquipValidator.CanEquipNow())
            {
                Equip(gearId);
            }
            else
            {
                EquipDelayedGear[SlotByPlayfabID[gearId.PlayfabItemId]] = gearId;
            }
        }

        /// <summary>
        /// Whether or not to pick up any deployed sentries on tool change: default true. 
        /// If disabled, sentries will not be picked up and remain deployed regardless of the player's tool item.
        /// If sentry is deployed, the player will have zero tool ammo (can be reclaimed by picking up sentry)
        /// 
        /// </summary>
        /// <param name="pickUp">Whether or not to pick up deployed sentries on tool change</param>
        public static void SetPickUpSentryOnToolChange(bool pickUp)
        {
            GearSwapConsistencyManager.PickUpSentryOnToolChange = pickUp;
        }

        private void Start()
        {
            foreach (var slot in SwappableGearSlots)
            {
                foreach (var gearId in GearManager.GetAllGearForSlot(slot))
                {
                    SlotByPlayfabID[gearId.PlayfabItemId] = slot;
                }
            }
        }

        private void Update()
        {
            if (EquipDelayedGear.Count <= 0 || !GearEquipValidator.CanEquipNow()) return;
            
            var equippedSlot = new List<InventorySlot>();
            foreach (var (slot, gearId) in EquipDelayedGear)
            {
                Equip(gearId);
                equippedSlot.Add(slot);
            }
            foreach (var slot in equippedSlot)
            {
                EquipDelayedGear.Remove(slot);
            }
        }

        private static void Equip(GearIDRange gearId)
        {
            var currSlot = PlayerManager.GetLocalPlayerAgent().Inventory.WieldedSlot;
            var gearSlot = SlotByPlayfabID[gearId.PlayfabItemId];
            
            BeforeGearSwap?.Invoke(gearSlot);

            if (currSlot == gearSlot)
            {
                PlayerManager.GetLocalPlayerAgent().Inventory.UnWield();
                PlayerBackpackManager.EquipLocalGear(gearId);
            }
            else
            {
                PlayerBackpackManager.LocalBackpack.SpawnAndEquipGearAsync(gearSlot, gearId);
            }

            OnGearUnLoaded?.Invoke(gearSlot);
        }

    }
}