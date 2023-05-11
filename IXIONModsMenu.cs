using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using BulwarkStudios.GameSystems.Ui;
using BulwarkStudios.Stanford;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
// using ConfigManager.UI;
using BepInEx.Configuration;

namespace IXIONModsMenu {

    [BepInPlugin("com.Olympia.IXIONModsMenu", "IXION Mods Menu", "0.1")]
    public class IXIONModsMenuPlugin : BasePlugin {

        internal static new ManualLogSource Log;
        internal static ConfigEntry<string> buttonText;

        public override void Load() {
            Log = base.Log;
            buttonText = Config.Bind("Settings", "Button Text", "Mods", "Text used on the button");
            AddComponent<IXIONModsMenuMono>();
        }

        public class IXIONModsMenuMono : MonoBehaviour {

            bool uiSetup;
            bool setupInProgress;
            GameObject btn;
            GameObject window;

            public void Awake() => DontDestroyOnLoad(gameObject);
            
            public void Update() {
                if (setupInProgress) return;
                // Log.LogInfo("active: " + SceneManager.GetActiveScene().name + " / is sub: " + SceneManager.GetActiveScene().isSubScene + " / total: " + SceneManager.sceneCount);
                string sceneName = SceneManager.GetActiveScene().name;
                if (uiSetup && sceneName != "MainMenu") uiSetup = false;
                if (sceneName != "MainMenu" || uiSetup) return;
                setupInProgress = true;
                AddModsBtn();
                SetupModListUI();
                setupInProgress = false;
                uiSetup = true;
            }

            private void AddModsBtn() {
                Log.LogInfo("Adding Mod Button...");
                GameObject stnsBtn = GameObject.Find("Button_Settings");
                btn = Instantiate(stnsBtn, stnsBtn.transform.parent);
                btn.transform.SetSiblingIndex(3);
                btn.transform.GetChild(1).GetComponent<Localize>().enabled = false;
                btn.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = buttonText.Value;
                btn.GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
                Action modBtnAction = delegate { ToggleModList(true); }; // UIManager.ShowMenu = !UIManager.ShowMenu;
                btn.GetComponent<UiButton>().add_OnTriggered(modBtnAction);
                Log.LogInfo("Button Added");
            }
            private void SetupModListUI() {
                Log.LogInfo("Setting Up Mod List...");
                GameObject loadWindow = GameObject.Find("UI Window Load Game");
                window = Instantiate(loadWindow, loadWindow.transform.parent);
                window.GetComponent<CanvasGroup>().alpha = 1;
                window.GetComponent<Canvas>().enabled = true;
                window.GetComponent<GraphicRaycaster>().enabled = true;
                window.transform.GetChild(0).GetChild(1).GetChild(2).GetComponent<Localize>().enabled = false;
                window.transform.GetChild(0).GetChild(1).GetChild(2).GetComponent<TextMeshProUGUI>().text = buttonText.Value;
                window.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
                Action xBtnAction = delegate { ToggleModList(false); };
                window.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<UiButton>().add_OnTriggered(xBtnAction);
                Transform listParent = window.transform.GetChild(0).GetChild(2).GetChild(0).GetChild(0).transform;
                GameObject tempCont = GameObject.Find("UI Window Settings").transform.GetChild(0).GetChild(2).GetChild(0).GetChild(0).GetChild(1).gameObject;
                tempCont.SetActive(true);
                GameObject entryTemplate = GameObject.Find("Entry_Language");
                GameObject separator = GameObject.Find("Separator");
                foreach (BepInPlugin plugin in IL2CPPChainloader.Instance.Plugins.Select(p => p.Value.Metadata)) {
                    GameObject listItem = Instantiate(entryTemplate, listParent);
                    listItem.transform.GetChild(1).gameObject.SetActive(false);
                    listItem.transform.GetChild(0).GetComponent<Localize>().enabled = false;
                    listItem.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = plugin.Name + " - " + plugin.Version;
                    if (separator != null) Instantiate(separator, listParent);
                }
                tempCont.SetActive(false);
                window.SetActive(false);
                Log.LogInfo("Mod List Setup Complete");
            }

            private void ToggleModList(bool enabled) {
                window.SetActive(enabled);
            }

        }
    }

    


}
