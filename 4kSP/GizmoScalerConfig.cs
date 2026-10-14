using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace _4kSP_RnD
{
    public static class GizmoScalerConfig
    {
        public const float MinScale = 0.5f;
        public const float MaxScale = 3f;
        public const float DefaultScale = 1f;

        public static bool Enabled = true;
        public static bool UseStockUIScale = true;
        public static float Scale = DefaultScale;

        private const string TAG = "[4kSP-Gizmos/Cfg]";
        private const string ROOT = "GIZMOSCALER";

        private static string ConfigPath
        {
            get
            {
                return Path.Combine(KSPUtil.ApplicationRootPath,
                    "GameData/4kSP/PluginData/GizmoScaler.cfg");
            }
        }

        public static float CurrentScale()
        {
            if (!Enabled) return 1f;
            if (!UseStockUIScale) return ValidatedManualScale(Scale);

            float stockScale = GameSettings.UI_SCALE;
            return float.IsNaN(stockScale) || float.IsInfinity(stockScale) || stockScale <= 0f
                ? DefaultScale : stockScale;
        }

        private static float ValidatedManualScale(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? DefaultScale : Mathf.Clamp(value, MinScale, MaxScale);
        }

        public static void Load()
        {
            try
            {
                string path = ConfigPath;
                if (!File.Exists(path))
                {
                    ResetDefaults();
                    Save();
                    return;
                }

                ConfigNode root = ConfigNode.Load(path);
                ConfigNode node = root != null ? root.GetNode(ROOT) : null;
                if (node == null)
                {
                    ResetDefaults();
                    Save();
                    return;
                }

                bool b = false;
                if (node.TryGetValue("enabled", ref b)) Enabled = b;
                if (node.TryGetValue("useStockUIScale", ref b)) UseStockUIScale = b;
                float f = 0f;
                if (float.TryParse(node.GetValue("scale"), NumberStyles.Float, CultureInfo.InvariantCulture, out f))
                    Scale = ValidatedManualScale(f);
            }
            catch (Exception e)
            {
                Debug.LogError(TAG + " Load err: " + e);
            }
        }

        public static void Save()
        {
            try
            {
                string path = ConfigPath;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                Scale = ValidatedManualScale(Scale);
                ConfigNode root = new ConfigNode();
                ConfigNode node = root.AddNode(ROOT);
                node.AddValue("enabled", Enabled);
                node.AddValue("useStockUIScale", UseStockUIScale);
                node.AddValue("scale", Scale);
                root.Save(path);
            }
            catch (Exception e)
            {
                Debug.LogError(TAG + " Save err: " + e);
            }
        }

        public static void ResetDefaults()
        {
            Enabled = true;
            UseStockUIScale = true;
            Scale = DefaultScale;
        }
    }
}
