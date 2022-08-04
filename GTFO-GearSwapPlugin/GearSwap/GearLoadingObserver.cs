using Player;
using UnityEngine;

namespace GearSwapPlugin.GearSwap
{
    /// <summary>
    /// Observes loading of new gear after gear swapping
    /// </summary>
    public class GearLoadingObserver : MonoBehaviour
    {
        public static event Action<InventorySlot> OnGearLoaded;
        private readonly List<InventorySlot> _unloadedSlots = new ();

        private void Start()
        {
            GearSwapManager.OnGearUnLoaded += AddToUnloadedSlots;
        }

        private void Update()
        {
            if (_unloadedSlots.Count <= 0) return;
            
            var loadedSlots = new List<InventorySlot>();
            foreach (var slot in _unloadedSlots.Where(slot => PlayerBackpackManager.GetLocalItem(slot).IsLoaded))
            {
                OnGearLoaded?.Invoke(slot);
                loadedSlots.Add(slot);
            }
            _unloadedSlots.RemoveAll(loadedSlots.Contains);

        }

        private void AddToUnloadedSlots(InventorySlot slot)
        {
            _unloadedSlots.Add(slot);
        }

        private void OnDestroy()
        {
            GearSwapManager.OnGearUnLoaded -= AddToUnloadedSlots;
        }
    }
}