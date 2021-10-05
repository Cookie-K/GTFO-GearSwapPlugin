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
            VERSION = "0.0.0";
        
        public static ManualLogSource log;
        
        public Harmony HarmonyPatches { get; private set; }

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