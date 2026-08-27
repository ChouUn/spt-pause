using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.UI.BattleTimer;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Pause
{
    public class PauseController : MonoBehaviour
    {
        internal static bool IsPaused { get; private set; }

        private DateTime? _pausedDate;
        private DateTime? _unpausedDate;
        private GameTimer _gameTimer;
        private MainTimerPanel _mainTimerPanel;
        private AbstractGame _abstractGame;
        private List<AudioSource> _pausedAudioSources;

        internal static ManualLogSource Logger;

        private static GameWorld GameWorld;

        private static Player MainPlayer;
        private static FieldInfo TimerPanelField;

        [UsedImplicitly]
        private void Awake()
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(PauseController));

            IsPaused = false;
            _abstractGame = Singleton<AbstractGame>.Instance;
            _mainTimerPanel = FindObjectOfType<MainTimerPanel>();
            _gameTimer = _abstractGame?.GameTimer;
            _pausedAudioSources = new List<AudioSource>(); 

            TimerPanelField = AccessTools.Field(typeof(TimerPanel), "_dateTime");
        }

        [UsedImplicitly]
        private void OnDestroy()
        {
            IsPaused = false;
            GameWorld = null;
            MainPlayer = null;
            Logger = null;
            _pausedAudioSources.Clear();
            TimerPanelField = null;
        }

        [UsedImplicitly]
        private void Update()
        {
            if (!IsKeyPressed(Plugin.TogglePause.Value))
            {
                return;
            }

            IsPaused = !IsPaused;

            if (IsPaused)
            {
                Pause();
            }
            else
            {
                Unpause();
                ResetFov();
            }
        }

        private void Pause()
        {
            Time.timeScale = 0f;
            _pausedDate = DateTime.UtcNow;

            MainPlayer.enabled = false;
            MainPlayer.PauseAllEffectsOnPlayer();

            foreach (var player in GetPlayers().Where(p => !p.IsYourPlayer))
            {
                Logger.LogInfo($"Deactivating player: {player.name}");
                SetPlayerState(player, false);
            }

            PauseAllAudio();
            ShowTimer();
        }

        private void Unpause()
        {
            Time.timeScale = 1f;
            _unpausedDate = DateTime.UtcNow;

            MainPlayer.enabled = true;
            MainPlayer.UnpauseAllEffectsOnPlayer();

            foreach (var player in GetPlayers().Where(p => !p.IsYourPlayer))
            {
                Logger.LogInfo($"Reactivating player: {player.name}");
                SetPlayerState(player, true);
            }

            ResumeAllAudio();

            if (!_mainTimerPanel.ForcePull)
            {
                StartCoroutine(CoHideTimer());
            }
            UpdateTimers(GetTimePaused());
        }

        private void PauseAllAudio()
        {
            _pausedAudioSources.Clear();
            foreach (var audioSource in FindObjectsOfType<AudioSource>().Where(s => s.isPlaying))
            {
                audioSource.Pause();
                _pausedAudioSources.Add(audioSource);
            }
        }

        private void ResumeAllAudio()
        {
            foreach (var audioSource in _pausedAudioSources)
            {
                audioSource.UnPause();
            }

            _pausedAudioSources.Clear();
        }

        private static IEnumerable<Player> GetPlayers()
        {
            return GameWorld?.AllAlivePlayersList ?? new List<Player>();
        }

        private static void SetPlayerState(Player player, bool active)
        {
            if (player == null)
            {
                return;
            }

            if (player.PlayerBones != null)
            {
                foreach (var r in player.PlayerBones.GetComponentsInChildren<Rigidbody>())
                {
                    if (active)
                    {
                        r.WakeUp();
                    }
                    else
                    {
                        r.Sleep();
                    }
                }
            }

            var weaponRigidBody = player.HandsController?.ControllerGameObject?.GetComponent<Rigidbody>();
            if (weaponRigidBody != null)
            {
                weaponRigidBody.angularVelocity = UnityEngine.Vector3.zero;
                weaponRigidBody.velocity = UnityEngine.Vector3.zero;
                weaponRigidBody.Sleep();
            }

            if (!active)
            {
                player.AIData.BotOwner.DecisionQueue.Clear();
                player.gameObject.SetActive(false);
            }
            else
            {
                player.gameObject.SetActive(true);
                player.AIData.BotOwner.CalcGoal();
            }
        }

        private void ShowTimer()
        {
            _mainTimerPanel?.DisplayTimer();
        }

        private IEnumerator CoHideTimer()
        {
            if (_mainTimerPanel == null)
            {
                yield break;
            }

            yield return new WaitForSeconds(4f);
            _mainTimerPanel.HideTimer();
        }

        private TimeSpan GetTimePaused()
        {
             return _pausedDate.HasValue && _unpausedDate.HasValue ? 
                _unpausedDate.Value - _pausedDate.Value : 
                TimeSpan.Zero;
        }

        private void UpdateTimers(TimeSpan timePaused)
        {
            if (_gameTimer == null || _mainTimerPanel == null ||
                GameWorld?.GameDateTime == null || TimerPanelField == null)
            {
                return;
            }

            var startDateTime = _gameTimer.StartDateTime;
            var escapeDateTime = _gameTimer.EscapeDateTime;
            var timerPanelDate = TimerPanelField.GetValue(_mainTimerPanel) as DateTime?;

            if (!startDateTime.HasValue || !escapeDateTime.HasValue ||
                !timerPanelDate.HasValue)
            {
                return;
            }

            _gameTimer._startDateTime = startDateTime.Value.Add(timePaused);
            _gameTimer._escapeDateTime = escapeDateTime.Value.Add(timePaused);
            _gameTimer.nullable_2 = null;
            GameWorld.GameDateTime._realtimeSinceStartup +=
                (float)timePaused.TotalSeconds;
            TimerPanelField.SetValue(
                _mainTimerPanel,
                timerPanelDate.Value.Add(timePaused));
        }
        
        private static void ResetFov()
        {
            var proceduralWeaponAnimation = MainPlayer?.ProceduralWeaponAnimation;
            if (proceduralWeaponAnimation == null)
            {
                return;
            }

            proceduralWeaponAnimation.OnFovChange(
                (int)proceduralWeaponAnimation.HeadBobbing);
        }

        internal static void Enable()
        {
            if (!Singleton<IBotGame>.Instantiated)
            {
                return;
            }

            GameWorld = Singleton<GameWorld>.Instance;
            GameWorld.GetOrAddComponent<PauseController>();
            MainPlayer = GameWorld.MainPlayer;
            Logger.LogDebug("PauseController enabled.");
        }

        internal static bool IsKeyPressed(KeyboardShortcut key)
        {
            return UnityInput.Current.GetKeyDown(key.MainKey) && key.Modifiers.All(modifier => UnityInput.Current.GetKey(modifier));
        }
    }
}
