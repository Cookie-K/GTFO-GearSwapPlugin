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
        public static event Action<InventorySlot> AfterSwapOnGearUnLoaded;
        public static event Action<InventorySlot> BeforeGearSwap;
        public static readonly List<InventorySlot> SwappableGearSlots = new List<InventorySlot> { InventorySlot.GearMelee, InventorySlot.GearStandard, InventorySlot.GearSpecial, InventorySlot.GearClass };

        private static readonly List<GearIDRange> EquipDelayedGear = new List<GearIDRange>();
        private static readonly Dictionary<string, InventorySlot> SlotByPlayfabID = new Dictionary<string, InventorySlot>();

        public GearSwapper(IntPtr intPtr) : base(intPtr)
        {
            // For Il2CppAssemblyUnhollower
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

        public static bool RequestToEquip(GearIDRange gearId)
        {
            if (GearEquipValidator.CanEquipNow())
            {
                Equip(gearId);
            }
            else
            {
                EquipDelayedGear.Add(gearId);
            }
            return false;
        }
        
        private static void Equip(GearIDRange gearId)
        {
            var currSlot = PlayerManager.GetLocalPlayerAgent().Inventory.WieldedSlot;
            var gearSlot = SlotByPlayfabID[gearId.PlayfabItemId];
            
            BeforeGearSwap?.Invoke(gearSlot);

            if (currSlot == gearSlot)
            {
                PlayerBackpackManager.EquipLocalGear(gearId);
            }
            else
            {
                PlayerBackpackManager.LocalBackpack.SpawnAndEquipGearAsync(gearSlot, gearId);
            }

            AfterSwapOnGearUnLoaded?.Invoke(gearSlot);
        }

    }
}