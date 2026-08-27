using EFT;
using EFT.UI.BattleTimer;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using TMPro;

namespace Pause
{
    public class WorldTickPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GameWorld), nameof(GameWorld.DoWorldTick));

        [PatchPrefix]
        internal static bool Prefix(GameWorld __instance, float dt)
        {
            if (!PauseController.IsPaused)
            {
                return true;
            }

            typeof(GameWorld).GetMethod("PlayerTick", BindingFlags.Instance | BindingFlags.Public)
                ?.Invoke(__instance, new object[] { dt });

            return false;
        }
    }

    public class OtherWorldTickPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GameWorld), nameof(GameWorld.DoOtherWorldTick));

        [PatchPrefix]
        internal static bool Prefix(GameWorld __instance)
        {
            return !PauseController.IsPaused;
        }
    }

    public class TimerPanelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TimerPanel), nameof(TimerPanel.UpdateTimer));

        [PatchPrefix]
        internal static bool Prefix(TextMeshProUGUI ____timerText)
        {
            if (!PauseController.IsPaused)
            {
                return true;
            }

            ____timerText.SetMonospaceText("PAUSED", false);
            return false;
        }
    }

    public class PlayerUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Player), nameof(Player.UpdateTick));

        [PatchPrefix]
        internal static bool Prefix()
        {
            return !PauseController.IsPaused;
        }
    }

    public class EndByTimerScenarioUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EndByTimerScenario), nameof(EndByTimerScenario.Update));

        [PatchPrefix]
        internal static bool Prefix()
        {
            return !PauseController.IsPaused;
        }
    }
}
