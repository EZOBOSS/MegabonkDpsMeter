using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;


[assembly: MelonInfo(typeof(megabonkDpsMeter.Main), "megabonkDpsMeter", "1.0.0", "pasele")]
[assembly: MelonGame("Ved", "Megabonk")] 

namespace megabonkDpsMeter
{
    public class Main : MelonMod
    {
        public static KeyCode toggleKey = KeyCode.F1;
        public static bool disableMeter = false;

        private GameObject statsParent;
        private GameObject damageWindow;
        private GameObject statsWindow;
        private GameObject questsWindow;
        private Vector2 windowPos;
        private MelonPreferences_Category cat;
        private MelonPreferences_Entry<float> posX;
        private MelonPreferences_Entry<float> posY;
        private MelonPreferences_Entry<float> widthEntry;
        private MelonPreferences_Entry<float> heightEntry;
        private Vector2 windowSize;

        private MelonPreferences_Entry<float> fontSizeEntry;
        private float fontSize;

        private float refreshTimer = 0f;
        private float refreshRate = 2f; // every 2 seconds
        public override void OnInitializeMelon()
        {
            // Load / create config
            cat = MelonPreferences.CreateCategory("megabonkDpsMeter", "DPS Meter Settings");
            posX = cat.CreateEntry("PosX", 400f);
            posY = cat.CreateEntry("PosY", -200f);
            widthEntry = cat.CreateEntry("Width", 800f);
            heightEntry = cat.CreateEntry("Height", 400f);
            windowPos = new Vector2(posX.Value, posY.Value);
            windowSize = new Vector2(widthEntry.Value, heightEntry.Value);
            fontSizeEntry = cat.CreateEntry("FontSize", 4f);
            fontSize = fontSizeEntry.Value;

            MelonLogger.Msg("megabonkDpsMeter loaded!");
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(toggleKey) && !disableMeter)
            {
                ToggleStatsWindow();
            }
            // Only update if window is visible
            if (statsParent != null && statsParent.activeSelf)
            {
                refreshTimer += Time.deltaTime;
                if (refreshTimer >= refreshRate)
                {
                    var ui = statsParent.GetComponentInChildren<GameOverDamageSourcesUi>();
                    ui?.Start(); // refresh data
                    
                    refreshTimer = 0f;
                    
                }
            }

            // font size
            if (Input.GetKey(KeyCode.PageUp)) { fontSize += 10f * Time.deltaTime; ApplyFontSize(); }
            if (Input.GetKey(KeyCode.PageDown)) { fontSize -= 10f * Time.deltaTime; ApplyFontSize(); }
            

            // move window
            float moveSpeed = 200f * Time.deltaTime;
            if (Input.GetKey(KeyCode.UpArrow)) { windowPos.y += moveSpeed; }
            if (Input.GetKey(KeyCode.DownArrow)) { windowPos.y -= moveSpeed;  }
            if (Input.GetKey(KeyCode.LeftArrow)) { windowPos.x -= moveSpeed;  }
            if (Input.GetKey(KeyCode.RightArrow)) { windowPos.x += moveSpeed;  }

            // Resize window
            float resizeSpeed = 300f * Time.deltaTime;
            if (Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.KeypadPlus))
            {
                windowSize += new Vector2(resizeSpeed, resizeSpeed);
                MelonLogger.Msg($"Increasing size: {windowSize}");
                
            }
            if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
            {
                windowSize -= new Vector2(resizeSpeed, resizeSpeed);
                MelonLogger.Msg($"Decreasing size: {windowSize}");
                
            }
            

            windowSize.x = Mathf.Max(windowSize.x, 200f);
            windowSize.y = Mathf.Max(windowSize.y, 200f);

            
            // Apply movement + resizing
            if (statsParent != null && statsParent.activeSelf)
            {
                var rect = statsParent.GetComponent<RectTransform>();
                if (rect != null)
                    rect.anchoredPosition = windowPos;

                if (damageWindow != null)
                {
                    var dmgRect = damageWindow.GetComponent<RectTransform>();
                    if (dmgRect != null)
                    {
                        // Try direct resizing
                        dmgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, windowSize.x);
                        dmgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, windowSize.y);

                        // Some Unity prefabs ignore anchors; use scaling as fallback
                        if (Mathf.Abs(dmgRect.rect.width - windowSize.x) > 5f)
                            dmgRect.localScale = new Vector3(windowSize.x / 800f, windowSize.y / 400f, 1f);
                    }
                }
            }

        }

        private void ToggleStatsWindow()
        {
            if (statsParent == null)
            {
                statsParent = GameObject.Find("GameUI/GameUI/DeathScreen/StatsWindows");
                if (statsParent != null)
                {
                    damageWindow = statsParent.transform.Find("W_Damage")?.gameObject;
                    statsWindow = statsParent.transform.Find("W_Stats")?.gameObject;
                    questsWindow = statsParent.transform.Find("W_Quests")?.gameObject;
                }
            }

            if (statsParent != null)
            {
                bool newState = !statsParent.activeSelf;
                statsParent.SetActive(newState);
                statsWindow?.SetActive(!newState);
                questsWindow?.SetActive(!newState);
                

                if (newState)
                {
                    var ui = statsParent.GetComponentInChildren<GameOverDamageSourcesUi>();
                    ApplyFontSize();
                    ui?.Start();
                }
            }
        }
        public override void OnApplicationQuit()
        {
            // Save position on exit
            posX.Value = windowPos.x;
            posY.Value = windowPos.y;
            widthEntry.Value = windowSize.x;
            heightEntry.Value = windowSize.y;
            fontSizeEntry.Value = fontSize;
            MelonPreferences.Save();
        }
        private void ApplyFontSize()
        {
            // Check if the damage window GameObject is found
            if (damageWindow == null) return;

            // Look for all TextMeshProUGUI components within the damageWindow, 
            // including inactive ones (though they should be active when this runs)
            var texts = damageWindow.GetComponentsInChildren<TextMeshProUGUI>(true);
                        
            foreach (var t in texts)
            {
                // Only set the font size if the value has been configured (fontSize > 0)
                // You might want to add a check for the min/max font size here
                if (t.fontSize != fontSize)
                {
                    t.fontSize = fontSize;
                }
            }
        }
    }


    [HarmonyPatch(typeof(GameOverDamageSourcesUi), nameof(GameOverDamageSourcesUi.Start))]
    public static class Patch_GameOverDamageSourcesUi_Start
    {
        private static void Prefix(GameOverDamageSourcesUi __instance)
        {
            var contentEntries = GameObject.Find("GameUI/GameUI/DeathScreen/StatsWindows/W_Damage/WindowLayers/Content/ScrollRect/ContentEntries");
            if (contentEntries == null) return;

            Transform contentTransform = contentEntries.transform;
            for (int i = contentTransform.childCount - 1; i >= 3; i--)
            {
                GameObject.Destroy(contentTransform.GetChild(i).gameObject);
            }
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.StartPlaying))]
    public static class Patch_GameManager_StartPlaying
    {
        private static void Postfix(GameManager __instance)
        {
            Main.disableMeter = false;
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.OnDied))]
    public static class Patch_GameManager_OnDied
    {
        private static void Postfix(GameManager __instance)
        {
            Main.disableMeter = true;
        }
    }
}
