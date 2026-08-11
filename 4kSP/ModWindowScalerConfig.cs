using System;
using System.Collections.Generic;
using System.IO;
using FourkSP;
using UnityEngine;

namespace _4kSP_RnD
{
    // Persists the mod-window scaler settings to
    // GameData/4kSP/PluginData/ModWindowScaler.cfg using KSP's native
    // ConfigNode format. Follows the same Load()/Save() shape as
    // RDScalerConfig (4kSP-RnD). Per-assembly overrides (exclude / custom
    // scale) are managed from the "Detected mod windows" list in
    // RDSceneScaler's "Mod Window Scaling" tab, or by hand-editing the cfg
    // directly.
    public static class ModWindowScalerConfig
    {
        public const float DefaultScale = 1.5f;
        public const float MinScale = 1.0f;
        public const float MaxScale = 3.0f;

        public static bool Enabled = true;
        public static bool UseStockUIScale = true;   // true: follow GameSettings.UI_SCALE
        public static float Scale = DefaultScale;     // used only when UseStockUIScale = false
        public static bool KeepOnScreen = true;
        public static bool LogWindows = false;

        // Per-assembly scale overrides. A value of 1 acts as an exclude
        // (the assembly's windows are left untouched).
        public static readonly Dictionary<string, float> Overrides =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        // Every assembly Patch_IMGUI_Window has actually seen open a
        // window this session, in display order. Powers the "Detected mod
        // windows" list in RDSceneScaler's "Mod Window Scaling" tab - same
        // information LogWindows already printed to KSP.log, just tracked
        // unconditionally instead of gated behind that toggle, and with a
        // UI on top instead of a grep.
        public sealed class DetectedMod
        {
            public string Assembly;
            public float LastRequestedScale;
        }

        public static readonly List<DetectedMod> Detected = new List<DetectedMod>();
        private static readonly Dictionary<string, DetectedMod> detectedByAssembly =
            new Dictionary<string, DetectedMod>(StringComparer.OrdinalIgnoreCase);

        public static void TrackWindow(string assemblyName, float requestedScale)
        {
            DetectedMod d;
            if (!detectedByAssembly.TryGetValue(assemblyName, out d))
            {
                d = new DetectedMod { Assembly = assemblyName };
                detectedByAssembly[assemblyName] = d;
                Detected.Add(d);
                Detected.Sort((a, b) => string.Compare(a.Assembly, b.Assembly, StringComparison.OrdinalIgnoreCase));
            }
            d.LastRequestedScale = requestedScale;
        }

        public static bool IsExcluded(string assemblyName)
        {
            float s;
            return Overrides.TryGetValue(assemblyName, out s) && Mathf.Approximately(s, 1f);
        }

        public static void SetExcluded(string assemblyName, bool excluded)
        {
            if (excluded)
                Overrides[assemblyName] = 1f;
            else
                Overrides.Remove(assemblyName);
        }

        public static float CurrentScale()
        {
            return UseStockUIScale ? GameSettings.UI_SCALE : Scale;
        }

        // Combines the global on/off switch above (this cfg file, applies
        // pre-game-load too, e.g. MainMenu) with the per-save Difficulty
        // Settings toggle (FourkSP._4kSP.modWindowScalerEnabled, only
        // meaningful once a game is loaded). Both must be true for windows
        // to be scaled. This is what Patch_IMGUI_Window actually checks.
        public static bool EffectiveEnabled
        {
            get
            {
                if (!Enabled) return false;
                Game game = HighLogic.CurrentGame;
                if (game == null) return true;
                return game.Parameters.CustomParams<_4kSP>().modWindowScalerEnabled;
            }
        }

        public static float ScaleFor(string assemblyName)
        {
            float s;
            if (assemblyName != null && Overrides.TryGetValue(assemblyName, out s))
                return s;
            return CurrentScale();
        }

        private static string ConfigPath
        {
            get
            {
                return Path.Combine(
                    KSPUtil.ApplicationRootPath,
                    "GameData/4kSP/PluginData/ModWindowScaler.cfg");
            }
        }

        private const string TAG = "[4kSP-ModWindows/Cfg]";
        private const string ROOT = "MODWINDOWSCALER";

        // Loads the file; creates it with defaults if missing.
        public static void Load()
        {
            try
            {
                string path = ConfigPath;

                if (!File.Exists(path))
                {
                    Save();
                    return;
                }

                ConfigNode root = ConfigNode.Load(path);
                ConfigNode node = root != null ? root.GetNode(ROOT) : null;
                if (node == null)
                {
                    Save();
                    return;
                }

                bool b = false;
                if (node.TryGetValue("enabled", ref b)) Enabled = b;
                if (node.TryGetValue("useStockUIScale", ref b)) UseStockUIScale = b;
                if (node.TryGetValue("keepOnScreen", ref b)) KeepOnScreen = b;
                if (node.TryGetValue("logWindows", ref b)) LogWindows = b;

                float f = 0f;
                if (node.TryGetValue("scale", ref f))
                    Scale = Mathf.Clamp(f, MinScale, MaxScale);

                Overrides.Clear();
                foreach (ConfigNode ov in node.GetNodes("OVERRIDE"))
                {
                    string name = ov.GetValue("assembly");
                    string val = ov.GetValue("scale");
                    float parsed;
                    if (!string.IsNullOrEmpty(name) && val != null
                        && float.TryParse(val, out parsed))
                    {
                        Overrides[name.Trim()] = parsed;
                    }
                }
                foreach (string ex in node.GetValues("exclude"))
                {
                    if (!string.IsNullOrEmpty(ex)) Overrides[ex.Trim()] = 1f;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(TAG + " Load err: " + e);
            }
        }

        // Writes current state to disk, including Overrides (exclude / per-
        // mod scale). The "Detected mod windows" list is now the primary
        // way to manage those, so this always fully regenerates the file
        // from the in-memory state rather than trying to preserve whatever
        // is on disk - simpler, and consistent with how the scalar fields
        // already worked (a Save() from the window was already rewriting
        // those unconditionally). Hand edits to OVERRIDE/exclude survive
        // until the next Save() from the window, same as before; they're
        // just no longer specially protected past that point.
        public static void Save()
        {
            try
            {
                string path = ConfigPath;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                ConfigNode root = new ConfigNode();
                ConfigNode node = root.AddNode(ROOT);

                node.AddValue("enabled", Enabled);
                node.AddValue("useStockUIScale", UseStockUIScale);
                node.AddValue("scale", Scale);
                node.AddValue("keepOnScreen", KeepOnScreen);
                node.AddValue("logWindows", LogWindows);

                List<string> names = new List<string>(Overrides.Keys);
                names.Sort(StringComparer.OrdinalIgnoreCase);
                foreach (string name in names)
                {
                    float s = Overrides[name];
                    if (Mathf.Approximately(s, 1f))
                    {
                        node.AddValue("exclude", name);
                    }
                    else
                    {
                        ConfigNode ov = node.AddNode("OVERRIDE");
                        ov.AddValue("assembly", name);
                        ov.AddValue("scale", s);
                    }
                }

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
            KeepOnScreen = true;
            LogWindows = false;
        }
    }
}
