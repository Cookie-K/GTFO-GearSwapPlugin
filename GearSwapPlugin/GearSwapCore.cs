using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;

namespace GearSwapPlugin
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    [BepInProcess("GTFO.exe")]
    public class GearSwapCore : BasePlugin
    {
        public const string
            NAME = "Gear Swap",
            MODNAME = "Gear Swap Plugin",
            AUTHOR = "Cookie_K",
            GUID = "com." + AUTHOR + "." + MODNAME,
            VERSION = "0.0.0";
        
        public static ManualLogSource log;
        
        public Harmony HarmonyPatches { get; private set; }

        public override void Load()
        {
            log = Log;
        }
    }
}