using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using GearSwapPlugin.GearSwap;
using HarmonyLib;
using UnhollowerRuntimeLib;

namespace GearSwapPlugin
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    [BepInProcess("GTFO.exe")]
    public class GearSwapCore : BasePlugin
    {
        public const string
            NAME = "Gear Swap Plugin",
            MODNAME = "GearSwapPlugin",
            AUTHOR = "Cookie_K",
            GUID = "com." + AUTHOR + "." + MODNAME,
            VERSION = "1.0.1";
        
        internal static ManualLogSource log;
        
        private Harmony HarmonyPatches { get; set; }

        public override void Load()
        {
            log = Log;
                        
            ClassInjector.RegisterTypeInIl2Cpp<GearSwapConsistencyManager>();
            ClassInjector.RegisterTypeInIl2Cpp<GearLoadingObserver>();
            ClassInjector.RegisterTypeInIl2Cpp<GearSwapManager>();
            
            HarmonyPatches = new Harmony(GUID);
            HarmonyPatches.PatchAll();
        }
    }
}