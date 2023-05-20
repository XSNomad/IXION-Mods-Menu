using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using BulwarkStudios.GameSystems.Ui;
using BulwarkStudios.Stanford.Core.GameStates;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

namespace Olympia.IXIONModsMenu {

    [BepInPlugin(GUID, NAME, VERSION)]
    public class ModsMenu : BasePlugin {

        public const string GUID = "Olympia.IXIONModsMenu";
        public const string NAME = "IXION Mods Menu";
        public const string VERSION = "0.2.0";

        public static Phase CurrentPhase { get; private set; }
        public static ConfigEntry<string> titleText;
        internal static new ManualLogSource Log;

        [Flags]
        public enum Phase {
            None = 0,
            Init = 1,
            Splashes = 2,
            Loading = 4,
            MainMenu = 8,
            Interior = 16,
            ShipView = 32,
            StarSys = 64,
            Cutscene = 128,
            Prologue = 256,
            Chapter1 = 512,
            Chapter2 = 1024,
            Chapter3 = 2048,
            Chapter4 = 4096,
            Chapter5 = 8192,
            Unknown = 16384,
            AnyTorus = Interior | ShipView,
            AnyGame = Interior | ShipView | StarSys,
            AnyLevel = Prologue | Chapter1 | Chapter2 | Chapter3 | Chapter4 | Chapter5
        }

        public override void Load() {
            Log = base.Log;
            titleText = Config.Bind("Settings", "Title Text", "Mods", "Title text used in buttons and headers");
            AddComponent<ModsMenuMono>();
        }

        public static bool InAnyPhase(Phase phaseFlags) => (CurrentPhase & phaseFlags) != 0;
        public static bool InAllPhases(Phase phaseFlags) => CurrentPhase.HasFlag(phaseFlags);

        public class ModsMenuMono : MonoBehaviour {

            private readonly Dictionary<string, Phase> sceneLookup = new Dictionary<string, Phase> {
                ["Initialize"] = Phase.Splashes,
                ["GameSetup"] = Phase.Loading,
                ["Chapter0"] = Phase.Prologue,
                ["Chapter0End"] = Phase.Cutscene
            };
            private readonly Dictionary<Game.VIEW, Phase> viewLookup = new Dictionary<Game.VIEW, Phase> {
                [Game.VIEW.TORUS] = Phase.Interior,
                [Game.VIEW.SHIP] = Phase.ShipView,
                [Game.VIEW.SOLAR_SYSTEM] = Phase.StarSys
            };

            private GameObject btn, window;
            private List<Tuple<ConfigEntry<bool>, TMP_Dropdown>> disabledConfigs;

            public void Awake() {
                DontDestroyOnLoad(gameObject);
                disabledConfigs = new List<Tuple<ConfigEntry<bool>, TMP_Dropdown>>();
                SceneManager.activeSceneChanged += (UnityAction<Scene, Scene>)OnSceneChanged;
            }

            private void OnSceneChanged(Scene _, Scene __) {
                string sceneName = SceneManager.GetActiveScene().name;
                Log.LogInfo(sceneName);
                Phase prevPhase = CurrentPhase;
                CurrentPhase = Phase.None;
                if (Enum.TryParse(sceneName, out Phase phase))
                    CurrentPhase = phase;
                else if (sceneLookup.ContainsKey(sceneName))
                    CurrentPhase = sceneLookup[sceneName];
                if (viewLookup.ContainsKey(Game.view))
                    CurrentPhase |= viewLookup[Game.view];
                if (CurrentPhase == Phase.None)
                    CurrentPhase = Phase.Unknown;
                if (prevPhase == CurrentPhase) return;
                Log.LogInfo(prevPhase + " -> " + CurrentPhase);
                if (InAnyPhase(Phase.MainMenu) && btn == null) SetupUI();
            }

            private void Update() {
                if (!viewLookup.ContainsKey(Game.view)) return;
                if (InAnyPhase(viewLookup[Game.view])) return;
                Phase prevPhase = CurrentPhase;
                CurrentPhase &= ~Phase.AnyGame;
                CurrentPhase |= viewLookup[Game.view];
                Log.LogInfo(prevPhase + " -> " + CurrentPhase);
                if (btn == null) SetupUI();
            }

            private void SetupUI() {
                GameObject btnTemplate = GameObject.Find(InAnyPhase(Phase.MainMenu) ? "Button_LoadGame" : "Container/Content/Settings");
                btn = Instantiate(btnTemplate, btnTemplate.transform.parent);
                btn.transform.SetSiblingIndex(InAnyPhase(Phase.MainMenu) ? 3 : 6);
                UpdateText(btn.transform.GetChild(1));
                btn.GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
                Action modBtnAction = delegate { ToggleModList(true); };
                btn.GetComponent<UiButton>().add_OnTriggered(modBtnAction);

                GameObject windowTemplate = GameObject.Find("UI Window Load Game");
                window = Instantiate(windowTemplate, windowTemplate.transform.parent);
                window.GetComponent<CanvasGroup>().alpha = 1;
                window.GetComponent<Canvas>().enabled = true;
                window.GetComponent<GraphicRaycaster>().enabled = true;
                UpdateText(window.transform.GetChild(0).GetChild(1).GetChild(2));
                window.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
                Action xBtnAction = delegate { ToggleModList(false); };
                window.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<UiButton>().add_OnTriggered(xBtnAction);
                Transform listParent = window.transform.GetChild(0).GetChild(2).GetChild(0).GetChild(0).transform;
                GameObject tempCont = GameObject.Find("UI Window Settings").transform.GetChild(0).GetChild(2).GetChild(0).GetChild(0).GetChild(1).gameObject;
                tempCont.SetActive(true);
                GameObject entryTemplate = GameObject.Find("Entry_Tutorial");
                GameObject separator = GameObject.Find("Game/Separator");
                foreach (PluginInfo plugin in IL2CPPChainloader.Instance.Plugins.Values.OrderBy(p => p.Metadata.GUID != GUID).ThenBy(p => p.Metadata.Name)) {
                    GameObject listItem = Instantiate(entryTemplate, listParent);
                    listItem.transform.GetChild(0).GetComponent<Localize>().enabled = false;
                    listItem.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = plugin.Metadata.Name + " - " + plugin.Metadata.Version;
                    if (separator != null) Instantiate(separator, listParent);

                    TMP_Dropdown dropDown = listItem.transform.GetChild(1).gameObject.GetComponent<TMP_Dropdown>();
                    if (dropDown == null) continue;
                    ConfigEntry<bool> configEntry = plugin.Instance.GetType().GetField("disabledInMenu")?.GetValue(plugin.Instance) as ConfigEntry<bool>;
                    if (configEntry == null) {
                        dropDown.SetValueWithoutNotify(1);
                        dropDown.enabled = false;
                        listItem.transform.GetChild(1).GetChild(1).gameObject.SetActive(false);
                        continue;
                    }
                    dropDown.onValueChanged.AddListener((UnityAction<int>)UpdateModDisabledConfigs);
                    dropDown.SetValueWithoutNotify(configEntry.Value ? 0 : 1);
                    disabledConfigs.Add(Tuple.Create(configEntry, dropDown));
                }
                tempCont.SetActive(false);
                window.SetActive(false);
            }

            private void UpdateText(Transform transform) {
                transform.GetComponent<Localize>().enabled = false;
                transform.GetComponent<TextMeshProUGUI>().text = titleText.Value;
            }

            private void UpdateModDisabledConfigs(int _) {
                foreach (var tuple in disabledConfigs)
                    if (!tuple.Item1.Value && tuple.Item2.value == 0)
                        tuple.Item1.BoxedValue = true;
                    else if (tuple.Item1.Value && tuple.Item2.value == 1)
                        tuple.Item1.BoxedValue = false;
            }

            private void ToggleModList(bool enabled) {
                window.SetActive(enabled);
            }


        }

    }
}
