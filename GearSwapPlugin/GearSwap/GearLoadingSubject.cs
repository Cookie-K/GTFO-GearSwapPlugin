using System;
using System.Collections.Generic;
using System.Linq;
using Player;
using UnityEngine;

namespace GearSwapPlugin.GearSwap
{
    public class GearLoadingSubject : MonoBehaviour
    {
        private List<InventorySlot> _unloadedSlots = new List<InventorySlot>();
        public static event Action<InventorySlot> AfterSwapOnGearLoaded;

        public GearLoadingSubject(IntPtr intPtr) : base(intPtr)
        {
            // For Il2CppAssemblyUnhollower
        }
        
        private void Start()
        {
            GearSwapper.AfterSwapOnGearUnLoaded += AddToUnloadedSlots;
        }

        private void Update()
        {
            if (_unloadedSlots.Count <= 0) return;
            
            var loadedSlots = new List<InventorySlot>();
            foreach (var slot in _unloadedSlots.Where(slot => PlayerBackpackManager.GetLocalItem(slot).IsLoaded))
            {
                AfterSwapOnGearLoaded?.Invoke(slot);
                loadedSlots.Add(slot);
            }
            _unloadedSlots.RemoveAll(loadedSlots.Contains);

        }

        public void AddToUnloadedSlots(InventorySlot slot)
        {
            _unloadedSlots.Add(slot);
        }

        private void OnDestroy()
        {
            GearSwapper.AfterSwapOnGearUnLoaded -= AddToUnloadedSlots;
        }
    }
}