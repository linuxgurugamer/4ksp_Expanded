using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace _4kSP_ModWindows
{
    // Harmony patch bootstrap. Runs once at MainMenu, mirrors
    // RDScalerHarmonyBootstrap (4kSP-RnD): load config, then PatchAll so
    // the [HarmonyPatch] classes below are picked up automatically.
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class ModWindowScalerHarmonyBootstrap : MonoBehaviour
    {
        private const string HARMONY_ID = "com.fourksp.modwindowscaler";
        private const string TAG = "[4kSP-ModWindows/Harmony]";
        public static bool harmonyLoaded = false;

        void Start()
        {
            try { _4kSP_RnD.ModWindowScalerConfig.Load(); }
            catch (Exception e) { Debug.LogError(TAG + " Load err: " + e); }

            if (harmonyLoaded) return;
            try
            {
                Harmony h = new Harmony(HARMONY_ID);
                h.PatchAll(typeof(ModWindowScalerHarmonyBootstrap).Assembly);
                harmonyLoaded = true;
            }
            catch (Exception e)
            {
                Debug.LogError(TAG + " PatchAll err: " + e);
            }
        }
    }

    // Scales every IMGUI window opened through GUI.Window, GUI.ModalWindow
    // or GUILayout.Window, EXCEPT the ones opened by mods that already
    // scale themselves (see the GUI.matrix check below) and by Unity's own
    // internal wrappers (GUILayout.Window calls GUI.Window once per frame
    // to do the actual drawing; TargetMethods() below patches all three
    // families with a single class since Unity's Window/ModalWindow
    // overloads all share the (id, Rect, WindowFunction, ...) parameter
    // order, id at position 0, Rect at position 1, WindowFunction at
    // position 2 - so the __1/__2 positional Harmony parameters work
    // uniformly across every overload regardless of its declared parameter
    // names (GUI.Window uses "clientRect", GUILayout.Window uses
    // "screenRect", etc).
    //
    // The scale is applied as a GUI.matrix anchored at the window's
    // top-left corner. Unity composes GUI.matrix with the window's own
    // GUIClip, so mouse hit-testing (click, drag) stays aligned with what
    // is drawn - no separate input-remapping is needed.
    [HarmonyPatch]
    public static class Patch_IMGUI_Window
    {
        internal struct State
        {
            public bool Applied;
            public Matrix4x4 Saved;
        }

        private static readonly HashSet<string> loggedAssemblies = new HashSet<string>();

        static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (MethodInfo m in typeof(GUI).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (m.Name == "Window" || m.Name == "ModalWindow")
                    yield return m;
            }
            foreach (MethodInfo m in typeof(GUILayout).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (m.Name == "Window")
                    yield return m;
            }
        }

        static void Prefix(Rect __1, GUI.WindowFunction __2, ref State __state)
        {
            __state.Applied = false;
            try
            {
                if (!_4kSP_RnD.ModWindowScalerConfig.EffectiveEnabled)
                    return;

                // Already scaled by an outer call (or by the mod itself):
                // don't compound. This is also what makes the inner
                // GUI.Window call that GUILayout.Window performs internally
                // a no-op here, since the outer GUILayout.Window prefix has
                // already set the matrix by the time it runs.
                if (GUI.matrix != Matrix4x4.identity)
                    return;

                string asm = null;
                if (__2 != null)
                {
                    MethodInfo mi = __2.Method;
                    if (mi != null && mi.DeclaringType != null)
                        asm = mi.DeclaringType.Assembly.GetName().Name;
                }
                if (asm == null || asm.StartsWith("UnityEngine", StringComparison.Ordinal))
                    return;

                float s = _4kSP_RnD.ModWindowScalerConfig.ScaleFor(asm);

                _4kSP_RnD.ModWindowScalerConfig.TrackWindow(asm, s);

                if (_4kSP_RnD.ModWindowScalerConfig.LogWindows && loggedAssemblies.Add(asm))
                    Debug.Log("[4kSP-ModWindows] window from assembly '" + asm
                        + "' (requested scale: " + s.ToString("0.##") + ")");

                if (s < 0.2f)
                    return;

                // Some mods size their window as a fraction of Screen.width/
                // height directly (a "sidebar" or "fills the screen" pattern
                // rather than auto-sizing from content). Scaling those in
                // full would push them off-screen regardless of translation,
                // clipping content near the far edge (frequently the window's
                // own Close/Exit button). When that would happen, shrink the
                // effective scale just enough to fit instead of overflowing.
                // Never shrinks below 1x: at that point the window is simply
                // left untouched, same as if it were excluded via config.
                if (_4kSP_RnD.ModWindowScalerConfig.KeepOnScreen && s > 1f)
                {
                    float w = Mathf.Max(__1.width, 1f);
                    float h = Mathf.Max(__1.height, 1f);
                    float maxFit = Mathf.Min(Screen.width / w, Screen.height / h);
                    if (maxFit < s)
                        s = Mathf.Max(1f, maxFit);
                }

                if (Mathf.Abs(s - 1f) < 0.005f)
                    return;

                float dx = 0f, dy = 0f;
                if (_4kSP_RnD.ModWindowScalerConfig.KeepOnScreen)
                {
                    float right = __1.x + __1.width * s;
                    float bottom = __1.y + __1.height * s;
                    if (right > Screen.width) dx = Screen.width - right;
                    if (bottom > Screen.height) dy = Screen.height - bottom;
                    if (__1.x + dx < 0f) dx = -__1.x;
                    if (__1.y + dy < 0f) dy = -__1.y;
                }

                __state.Saved = GUI.matrix;
                __state.Applied = true;
                Vector3 t = new Vector3(__1.x * (1f - s) + dx, __1.y * (1f - s) + dy, 0f);
                GUI.matrix = Matrix4x4.TRS(t, Quaternion.identity, new Vector3(s, s, 1f));
            }
            catch (Exception e)
            {
                if (__state.Applied)
                {
                    GUI.matrix = __state.Saved;
                    __state.Applied = false;
                }
                Debug.LogError("[4kSP-ModWindows] Prefix err: " + e);
            }
        }

        static void Postfix(ref State __state)
        {
            if (__state.Applied)
                GUI.matrix = __state.Saved;
        }
    }
}
