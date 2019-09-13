using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens.Mapview;

namespace FourkSP
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class FourkSP : MonoBehaviour
    {
        public List<MapNode> scaledNodes = new List<MapNode>();
        private float nextActionTime = 0.0f;
        public float period = 2f;
        public float baseIconScale = 1f;
        public void Update()
        {
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
                sprite.localScale = new Vector3(baseIconScale * GameSettings.UI_SCALE, baseIconScale * GameSettings.UI_SCALE, baseIconScale * GameSettings.UI_SCALE);
                caption.GetComponent<TMPro.TextMeshProUGUI>().fontSize = 12 * GameSettings.UI_SCALE;
                if (iconLabel != null)
                {
                    iconLabel.GetComponent<TMPro.TextMeshProUGUI>().fontSize = 12 * GameSettings.UI_SCALE;
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