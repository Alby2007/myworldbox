using HarmonyLib;
using NeoModLoader.api;

namespace CivMindOverhaul
{
    public class ModClass : BasicMod<ModClass>
    {
        protected override void OnModLoad()
        {
            Cfg.Load(GetConfig());
            new Harmony("devin.civmind").PatchAll();
            Ref.Report();
            Tuning.TryApply();
            LogInfo("[CivMind] Civ Mind Overhaul loaded");
        }
    }
}
