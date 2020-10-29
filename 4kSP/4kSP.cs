using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using KSP.UI.Screens.Mapview;
using ToolbarControl_NS;
using ClickThroughFix;

namespace FourkSP
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class FourkSP : MonoBehaviour
    {
        internal const float MinIconSize = 0.75f;
        internal const float MaxIconSize = 1.5f;

        internal const float MinScale = 0.5f;
        internal const float MaxScale = 2f;

        internal const float MinFontSize = 9f;
        internal const float MaxFontSize = 20f;



        public List<MapNode> scaledNodes = new List<MapNode>();
        private float nextActionTime = 0.0f;
        public float period = 2f;

        float UI_Scale = 0;
        float fontSize = 0;

        internal static String _AssemblyName { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; } }

        int baseWindowID;

        float tmpUI_Scale, tmpFontSize, tmpIconSize;

        void Start()
        {
            if (!HighLogic.LoadedSceneHasPlanetarium) return;

            UpdateSizes();
            GameEvents.OnGameSettingsApplied.Add(OnGameSettingsApplied);
            AddToolbarButton();
            baseWindowID = UnityEngine.Random.Range(1000, 2000000) + _AssemblyName.GetHashCode();

        }

        void OnGameSettingsApplied()
        {
            UpdateSizes();
        }

        void OnDestroy()
        {
            if (!HighLogic.LoadedSceneHasPlanetarium) return;

            GameEvents.OnGameSettingsApplied.Remove(OnGameSettingsApplied);

            if (toolbarControl != null)
            {
                toolbarControl.OnDestroy();
                GameObject.Destroy(toolbarControl);
                toolbarControl = null;
            }

        }

        ToolbarControl toolbarControl;
        internal const string MODID = "4kSP";
        internal const string MODNAME = "4kSP";
        private bool windowShown = false;
        private Rect windowPosition = new Rect(Screen.width / 2 - 250, Screen.height / 2 - 250, 400, 150);


        void AddToolbarButton()
        {
            if (toolbarControl == null)
            {
                GameObject gameObject = new GameObject();
                toolbarControl = gameObject.AddComponent<ToolbarControl>();
                toolbarControl.AddToAllToolbars(windowToggle, windowToggle,
                    ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.TRACKSTATION,
                    MODID,
                    "4kSPBtn",
                    "4kSP/PluginData/4kSP-38",
                    "4kSP/PluginData/4kSP-24",
                    MODNAME
                );
            }

        }
        private void windowToggle()
        {
            windowShown = true;
            if (windowShown)
            {
                tmpUI_Scale = HighLogic.CurrentGame.Parameters.CustomParams<_4kSP>().UI_IconSize;
                tmpIconSize = HighLogic.CurrentGame.Parameters.CustomParams<_4kSP>().UI_IconSize;
                tmpFontSize = HighLogic.CurrentGame.Parameters.CustomParams<_4kSP>().UI_FontSize;
            }
            else
                RevertToSaved();
        }

        void RevertToSaved()
        {
            HighLogic.CurrentGame.Parameters.CustomParams<_4kSP>().UI_IconSize = tmpUI_Scale;
            HighLogic.CurrentGame.Parameters.CustomParams<_4kSP>().UI_IconSize = tmpIconSize;
            HighLogic.CurrentGame.Parameters.CustomParams<_4kSP>().UI_FontSize = tmpFontSize;
        }

        void OnGUI()
        {
            GUI.skin = HighLogic.Skin;
            if (windowShown)
                windowPosition = ClickThruBlocker.GUILayoutWindow(baseWindowID, windowPosition, drawWindow, "4kSP");
        }

        void DrawSlider(string label, ref float data, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label+" (" + data.ToString("F2") + ")", GUILayout.Width(170));
            float newData = GUILayout.HorizontalSlider(data, min, max, GUILayout.Width(200));
            if (newData != data)
            {
                data = newData;
                UpdateSizes();
                nextActionTime = 0;
            }
            GUILayout.EndHorizontal();

        }
        void drawWindow(int id)
        {
            DrawSlider("Overall UI Scale: ", ref HighLogic.CurrentGame.Parameters.CustomParams<_4kSP>().UI_Scale, MinScale, MaxScale);
            DrawSlider("Icon Size: ", ref HighLogic.CurrentGame.Parameters.CustomParams<_4kSP>().UI_IconSize, MinIconSize, MaxIconSize);
            DrawSlider("Font Size: ", ref HighLogic.CurrentGame.Parameters.CustomParams<_4kSP>().UI_FontSize, MinFontSize, MaxFontSize);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Accept"))
            {
                windowShown = false;
            }
            if (GUILayout.Button("Cancel"))
            {
                RevertToSaved();
                windowShown = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Click <B>Accept</b> to save");
            GUI.DragWindow();
        }
        public void UpdateSizes()
        {
            if (HighLogic.CurrentGame.Parameters.CustomParams<_4kSP>().useStockUIScale)
            {
                UI_Scale = /* baseIconScale *  */GameSettings.UI_SCALE;
                fontSize = 12 * GameSettings.UI_SCALE;
            }
            else
            {
                UI_Scale = HighLogic.CurrentGame.Parameters.CustomParams<_4kSP>().UI_IconSize * HighLogic.CurrentGame.Parameters.CustomParams<_4kSP>().UI_Scale;
                fontSize = HighLogic.CurrentGame.Parameters.CustomParams<_4kSP>().UI_FontSize * GameSettings.UI_SCALE;

            }
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneHasPlanetarium) return;
            if (!scaledNodes.SequenceEqual(MapNode.AllMapNodes) && MapView.MapIsEnabled)
            {
                UpdateNodes();
                scaledNodes = new List<MapNode>(MapNode.AllMapNodes);
            }
            if (Time.time > nextActionTime)
            {
                nextActionTime += period;
                UpdateNodes();
            }
        }
        public void UpdateNodes()
        {
            foreach (MapNode node in MapNode.AllMapNodes)
            {
                var caption = node.transform.Find("OverrideSortingCanvas").Find("Caption");
                var sprite = node.transform.Find("Sprite");
                var iconLabel = node.transform.Find("iconLabel(Clone)");
                sprite.localScale = new Vector3(UI_Scale, UI_Scale, UI_Scale);
                caption.GetComponent<TMPro.TextMeshProUGUI>().fontSize = fontSize;
                if (iconLabel != null)
                {
                    iconLabel.GetComponent<TMPro.TextMeshProUGUI>().fontSize = fontSize;
                }

            }
        }

        public void LogTree(Transform transform, int lvl = 0)
        {
            if (lvl == 0)
            {
                Debug.Log(transform.name);
                LogTree(transform, lvl += 1);
            }
            else
            {
                foreach (Transform child in transform)
                {
                    Debug.Log(String.Concat(Enumerable.Repeat("-", lvl).ToArray()) + child.name);
                    LogTree(child.transform, lvl + 1);
                }
            }
        }
    }
}