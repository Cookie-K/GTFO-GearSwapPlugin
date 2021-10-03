using System;
using System.Collections.Generic;
using System.Linq;
using Enemies;
using Gear;
using Player;
using UnityEngine;

namespace GearSwapPlugin.GearSwap
{
    public class GearSwapper : MonoBehaviour
    {
        public static event Action<InventorySlot> OnGearUnLoaded;
        public static event Action<InventorySlot> BeforeGearSwap;

        public static readonly List<InventorySlot> SwappableGearSlots = new List<InventorySlot> {InventorySlot.GearMelee, InventorySlot.GearStandard, InventorySlot.GearSpecial, InventorySlot.GearClass};

        private static readonly List<GearIDRange> EquipDelayedGear = new List<GearIDRange>();
        private static readonly Dictionary<string, InventorySlot> SlotByPlayfabID = new Dictionary<string, InventorySlot>();

        public GearSwapper(IntPtr intPtr) : base(intPtr)
        {
            // For Il2CppAssemblyUnhollower
        }

        /// <summary>
        /// Requests to equip the given gearID to the local player on the next possible opportunity.
        /// See GearEquipValidator for what criteria delays equipping of a gear 
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
                EquipDelayedGear.Add(gearId);
            }
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
            
            var equippedGear = new List<GearIDRange>();
            foreach (var gearId in EquipDelayedGear)
            {
                Equip(gearId);
                equippedGear.Add(gearId);
            }
            EquipDelayedGear.RemoveAll(equippedGear.Contains);
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