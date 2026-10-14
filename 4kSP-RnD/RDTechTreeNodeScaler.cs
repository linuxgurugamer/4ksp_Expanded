using HarmonyLib;
using KSP.UI;
using KSP.UI.Screens;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Vectrosity;

namespace _4kSP_RnD
{
    // Stock reparents newly created nodes with worldPositionStays=true. Their
    // graphics therefore miss the canvas's initial UI multiplier, but DO inherit
    // subsequent canvas/graph scale changes. Capture the missing factor once.
    internal static class RDTechTreeNodeScaler
    {
        private sealed class NodeState
        {
            internal float InitialUIScale;
            internal float EffectiveScale;
            internal bool Initialized;
            internal EdgeAppearance[] Edges;
        }

        private struct EdgeAppearance
        {
            internal bool Visible;
            internal Color LineColor;
            internal bool HeadEnabled;
            internal Color HeadColor;
        }

        private static readonly ConditionalWeakTable<RDNode, NodeState> States =
            new ConditionalWeakTable<RDNode, NodeState>();
        private static readonly AccessTools.FieldRef<RDNode, float> Width =
            AccessTools.FieldRefAccess<RDNode, float>("width");
        private static readonly AccessTools.FieldRef<RDNode, float> Height =
            AccessTools.FieldRefAccess<RDNode, float>("height");
        private static readonly AccessTools.FieldRef<RDNode, bool> ArrowsSetup =
            AccessTools.FieldRefAccess<RDNode, bool>("arrows_setup");
        private static readonly Action<RDNode, Material> DrawArrow =
            (Action<RDNode, Material>)Delegate.CreateDelegate(typeof(Action<RDNode, Material>),
                AccessTools.Method(typeof(RDNode), "DrawArrow"));

        internal static void Capture(RDNode node, bool setup)
        {
            // Native Setup recursively sets up parents and is itself idempotent.
            if (!node.treeNode || setup || States.TryGetValue(node, out _)) return;
            States.Add(node, new NodeState
            {
                InitialUIScale = UIMasterController.Instance.uiScale
            });
        }

        internal static void Initialize(RDNode node)
        {
            if (node.treeNode && States.TryGetValue(node, out var state))
                Resize(node, state, RDSceneScaler.ScalerEnabled);
        }

        private static bool Resize(RDNode node, NodeState state, bool enabled)
        {
            float effective = node.scale * (enabled ? state.InitialUIScale : 1f);
            if (state.Initialized && state.EffectiveScale == effective) return false;
            node.graphics.SetScale(effective);
            Width(node) = (node.graphics.width - node.graphics.offset.right) * effective;
            Height(node) = (node.graphics.height - node.graphics.offset.bottom) * effective;
            state.EffectiveScale = effective;
            state.Initialized = true;
            return true;
        }

        internal static void Apply(RDController rd, bool enabled)
        {
            if (rd == null || rd.nodes == null) return;
            bool changed = false;
            // Every footprint must be current before an edge reads either endpoint.
            for (int i = 0; i < rd.nodes.Count; i++)
            {
                var node = rd.nodes[i];
                if (node != null && node.treeNode && node.graphics != null &&
                    States.TryGetValue(node, out var state) && state.Initialized)
                    changed |= Resize(node, state, enabled);
            }
            if (!changed) return;
            for (int i = 0; i < rd.nodes.Count; i++)
            {
                var node = rd.nodes[i];
                if (node != null && node.treeNode && ArrowsSetup(node) &&
                    States.TryGetValue(node, out var state) && state.Initialized)
                    Redraw(node, state);
            }
        }

        private static void Redraw(RDNode node, NodeState state)
        {
            var parents = node.parents;
            if (parents.Length == 0) return;
            if (state.Edges == null || state.Edges.Length != parents.Length)
                state.Edges = new EdgeAppearance[parents.Length];
            for (int i = 0; i < parents.Length; i++)
            {
                var edge = parents[i];
                state.Edges[i] = new EdgeAppearance
                {
                    Visible = edge.line.rectTransform.gameObject.activeSelf,
                    LineColor = edge.line.color,
                    HeadEnabled = edge.arrowHead.enabled,
                    HeadColor = edge.arrowHead.color
                };
                VectorLine.Destroy(ref edge.line);
                UnityEngine.Object.Destroy(edge.arrowHead.gameObject);
                edge.arrowHead = null;
            }
            // Native GetVectorArray, routing, chamfers, line parenting and heads.
            // Do not run UpdateGraphics: resizing must not change research state,
            // visibility, selection, callbacks or available-parts indicators.
            DrawArrow(node, node.controller.gridArea.LineMaterial);
            for (int i = 0; i < parents.Length; i++)
            {
                var edge = parents[i];
                var appearance = state.Edges[i];
                edge.line.color = appearance.LineColor;
                edge.line.rectTransform.gameObject.SetActive(appearance.Visible);
                edge.arrowHead.enabled = appearance.HeadEnabled;
                edge.arrowHead.color = appearance.HeadColor;
            }
        }

        internal static bool HasFootprint(RDNode node)
        {
            return node.treeNode && node.graphics != null &&
                States.TryGetValue(node, out var state) && state.Initialized;
        }

        internal static void ScaleAnchor(RDNode node, RDNode.Anchor anchor,
            Vector3 toAnchorPos, ref Vector3 position)
        {
            if (!HasFootprint(node) ||
                (anchor != RDNode.Anchor.TOP && anchor != RDNode.Anchor.BOTTOM &&
                 anchor != RDNode.Anchor.LEFT && anchor != RDNode.Anchor.RIGHT)) return;
            // Native anchors use TransformDirection, which ignores scale. Their
            // cached width/height are graphics-root units. Convert the native
            // base displacement (including the 1-unit inset) to world units once,
            // using the actual root, not current stock UI scale. The native side
            // offset's sign must then use this corrected anchor, not its old base.
            Vector3 origin = node.transform.position;
            Vector3 local = node.transform.InverseTransformDirection(position - origin);
            position = origin + node.graphics.transform.TransformVector(local);
            if (toAnchorPos != Vector3.zero &&
                (anchor == RDNode.Anchor.LEFT || anchor == RDNode.Anchor.RIGHT))
                position += node.graphics.transform.TransformVector(
                    Vector3.up * (5f * Math.Sign((toAnchorPos - position).y)));
        }
    }

    [HarmonyPatch(typeof(RDNode), nameof(RDNode.Setup))]
    internal static class Patch_RDNode_Setup
    {
        static void Prefix(RDNode __instance, bool ___setup)
        {
            RDTechTreeNodeScaler.Capture(__instance, ___setup);
        }
    }

    [HarmonyPatch(typeof(RDNode), "InitializeArrows")]
    internal static class Patch_RDNode_InitializeArrows
    {
        static void Prefix(RDNode __instance)
        {
            // Graphics.Setup and world-preserving parenting have finished; native
            // dimensions are replaced before the very first DrawArrow runs.
            // Authored RDNode.scale is never modified, including during Save.
            RDTechTreeNodeScaler.Initialize(__instance);
        }
    }

    [HarmonyPatch(typeof(RDNode), nameof(RDNode.GetPosition))]
    internal static class Patch_RDNode_GetPosition
    {
        static void Prefix(RDNode __instance, RDNode.Anchor anchor,
            ref Vector3 toAnchorPos, out Vector3 __state)
        {
            __state = Vector3.zero;
            if (!RDTechTreeNodeScaler.HasFootprint(__instance) ||
                (anchor != RDNode.Anchor.LEFT && anchor != RDNode.Anchor.RIGHT)) return;
            __state = toAnchorPos;
            toAnchorPos = Vector3.zero;
        }

        static void Postfix(RDNode __instance, RDNode.Anchor anchor,
            Vector3 __state, ref Vector3 __result)
        {
            RDTechTreeNodeScaler.ScaleAnchor(__instance, anchor, __state, ref __result);
        }
    }
}
