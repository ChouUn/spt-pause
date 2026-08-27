using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT;
using JetBrains.Annotations;
using SPT.Reflection.Patching;
using UnityEngine;

namespace Pause
{
    [BepInPlugin("com.netVnum.pause", "PAUSE", "1.5.0")]
    public class Plugin : BaseUnityPlugin
    {

        internal static ConfigEntry<KeyboardShortcut> TogglePause;
        internal static ManualLogSource Log;

        [UsedImplicitly]
        private void Awake()
        {
            Log = Logger;
            TogglePause = Config.Bind("Keybinds", "Toggle Pause", new KeyboardShortcut(KeyCode.F9));
            Logger.LogInfo("PAUSE: Loading");

            new NewGamePatch().Enable();

            new WorldTickPatch().Enable();
            new OtherWorldTickPatch().Enable();
            new TimerPanelPatch().Enable();
            new PlayerUpdatePatch().Enable();
            new EndByTimerScenarioUpdatePatch().Enable();

            new BaseLocalGameUpdatePatch().Enable();
        }

        internal class NewGamePatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

            [PatchPrefix]
            private static void PatchPrefix()
            {
                PauseController.Enable();
            }
        }
    }
}
