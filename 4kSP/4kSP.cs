using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using KSP.UI.Screens.Mapview;

namespace FourkSP
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FourkSP_FlightL : FourkSP
    {

    }

    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class FourkSP_TrackingStation : FourkSP
    {

    }
    public class FourkSP : MonoBehaviour
    {
        public const float MinIconSize = 0.75f;
        public const float MaxIconSize = 1.5f;

        public const float MinScale = 0.5f;
        public const float MaxScale = 2f;

        public const float MinFontSize = 9f;
        public const float MaxFontSize = 20f;



        public List<MapNode> scaledNodes = new List<MapNode>();
        private float nextActionTime = 0.0f;
        public float period = 2f;

        float UI_Scale = 0;
        float fontSize = 0;

        private static FourkSP current;
        internal const string MODID = "4kSP";
        internal const string MODNAME = "4kSP";

        void Start()
        {
            current = this;
            UpdateSizes();
            GameEvents.OnGameSettingsApplied.Add(OnGameSettingsApplied);
        }

        void OnGameSettingsApplied()
        {
            UpdateSizes();
        }

        void OnDestroy()
        {
            if (ReferenceEquals(current, this)) current = null;
            GameEvents.OnGameSettingsApplied.Remove(OnGameSettingsApplied);
        }


        public static void RefreshMapSettings()
        {
            if (ReferenceEquals(current, null) || HighLogic.CurrentGame == null) return;
            current.UpdateSizes();
            current.nextActionTime = 0f;
            if (MapView.MapIsEnabled) current.UpdateNodes();
        }

        public void UpdateSizes()
        {
            if (HighLogic.CurrentGame == null) return;
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
            //if (!HighLogic.LoadedSceneHasPlanetarium) return;
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

#if false
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
#endif
    }
}