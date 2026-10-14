using EditorGizmos;
using HarmonyLib;
using UnityEngine;

namespace _4kSP_RnD
{
    [HarmonyPatch(typeof(ManeuverGizmoBase), "LateUpdate")]
    public static class Patch_ManeuverGizmoBase_LateUpdate
    {
        [HarmonyPostfix]
        public static void Postfix(ManeuverGizmoBase __instance)
        {
            if (__instance == null) return;

            float factor = GizmoScalerConfig.CurrentScale();
            if (factor == 1f) return;

            // Stock writes a fresh root scale every frame. Scale that result,
            // leaving the child handles' collapse and hover animation intact.
            __instance.transform.localScale *= factor;
        }
    }

    [HarmonyPatch(typeof(GizmoOffset), "Attach")]
    public static class Patch_GizmoOffset_Attach
    {
        private static readonly AccessTools.FieldRef<GizmoOffset, ScreenSpaceObjectScaling> Scaling =
            AccessTools.FieldRefAccess<GizmoOffset, ScreenSpaceObjectScaling>("ssScaling");

        [HarmonyPostfix]
        public static void Postfix(GizmoOffset __result)
        {
            if (__result == null) return;
            EditorGizmoSizeDriver.Attach(Scaling(__result));
        }
    }

    [HarmonyPatch(typeof(GizmoRotate), "Attach")]
    public static class Patch_GizmoRotate_Attach
    {
        private static readonly AccessTools.FieldRef<GizmoRotate, ScreenSpaceObjectScaling> Scaling =
            AccessTools.FieldRefAccess<GizmoRotate, ScreenSpaceObjectScaling>("ssScaling");

        [HarmonyPostfix]
        public static void Postfix(GizmoRotate __result)
        {
            if (__result == null) return;
            EditorGizmoSizeDriver.Attach(Scaling(__result));
        }
    }

    // Only factory-created editor gizmos receive this driver. The stock native
    // scaler still owns the transform and lerp; we change its size input only.
    public sealed class EditorGizmoSizeDriver : MonoBehaviour
    {
        private ScreenSpaceObjectScaling _scaler;
        private float _baselineScreenSize;
        private bool _initialized;

        internal static void Attach(ScreenSpaceObjectScaling scaler)
        {
            if (scaler == null) return;

            var driver = scaler.gameObject.GetComponent<EditorGizmoSizeDriver>();
            if (driver == null)
                driver = scaler.gameObject.AddComponent<EditorGizmoSizeDriver>();
            driver.Initialize(scaler);
        }

        internal void Initialize(ScreenSpaceObjectScaling scaler)
        {
            if (!_initialized)
            {
                if (scaler == null) return;
                _scaler = scaler;
                _baselineScreenSize = scaler.screenSize;
                _initialized = true;
            }

            // Apply at the factory boundary, before the first native LateUpdate.
            ApplyCurrentScale();
        }

        private void Update()
        {
            // Every Update precedes LateUpdate, including after a settings change.
            ApplyCurrentScale();
        }

        private void ApplyCurrentScale()
        {
            if (!_initialized || _scaler == null) return;
            _scaler.screenSize = _baselineScreenSize * GizmoScalerConfig.CurrentScale();
        }

        private void OnDestroy()
        {
            // Unity's null check also skips native components already destroyed
            // as part of their containing gizmo's teardown.
            if (_initialized && _scaler != null)
                _scaler.screenSize = _baselineScreenSize;
        }
    }
}
