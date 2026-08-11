using ClickThroughFix;
using FourkSP;
using KSP.UI.Screens;
using System;
using System.Collections;
using System.Reflection;
using ToolbarControl_NS;
using UnityEngine;
using UnityEngine.UI;

namespace _4kSP_RnD
{
    // Scales the R&D Complex UI for high-DPI monitors:
    //   1) raises the tech tree zoom cap (UIGridArea.zoomMax)
    //   2) scales the parts list tiles (RDPartList GridLayoutGroup)
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class RDSceneScaler : MonoBehaviour
    {
        private const string TAG = "[4kSP-RD]";

        private const string TB_ICON_BIG = "4kSP/PluginData/4kSP-38";
        private const string TB_ICON_SMALL = "4kSP/PluginData/4kSP-24";
        private const string TB_BTN_ID = "4kSPRDBtn";
        private const string TB_TOOLTIP = "4kSP R&D Scaler";

        private static readonly ApplicationLauncher.AppScenes TB_SCENES =
            ApplicationLauncher.AppScenes.SPACECENTER;

        private ToolbarControl _toolbar;
        private GameObject _toolbarGO;
        private bool _windowShown;
        private int _windowId;
        private Rect _windowRect = new Rect(200, 200, 380, 220);

        private float _tmpMaxZoom;
        private float _tmpPartsScale;
        private bool _inRnD;
        bool harmonyLoaded = false;
        void Start()
        {
            RDScalerConfig.Load();

            _tmpMaxZoom = RDScalerConfig.MaxZoom;
            _tmpPartsScale = RDScalerConfig.PartsScale;

            _windowId = UnityEngine.Random.Range(10000, 2000000) + GetType().Name.GetHashCode();

            GameEvents.onGUIRnDComplexSpawn.Add(OnRnDSpawn);
            GameEvents.onGUIRnDComplexDespawn.Add(OnRnDDespawn);
            GameEvents.OnGameSettingsApplied.Add(OnGameSettingsApplied);

            harmonyLoaded = GetStaticBool(
                    "4kSP-ModWindows",
                    "_4kSP_ModWindows.ModWindowScalerHarmonyBootstrap",
                    "harmonyLoaded");
            if ( harmonyLoaded ) 
                AddToolbarButton();
        }

        void OnDestroy()
        {
            GameEvents.onGUIRnDComplexSpawn.Remove(OnRnDSpawn);
            GameEvents.onGUIRnDComplexDespawn.Remove(OnRnDDespawn);
            GameEvents.OnGameSettingsApplied.Remove(OnGameSettingsApplied);

            if (_toolbarGO != null)
            {
                Destroy(_toolbarGO);
                _toolbarGO = null;
                _toolbar = null;
            }
        }


        public static bool GetStaticBool(
            string assemblyName,
            string className,
            string fieldName,
            bool defaultValue = false)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name != assemblyName)
                    continue;

                Type type = assembly.GetType(className);
                if (type == null)
                    return defaultValue;

                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Public |
                    BindingFlags.Static);

                if (field == null || field.FieldType != typeof(bool))
                    return defaultValue;

                return (bool)field.GetValue(null);
            }

            return defaultValue;
        }
        // Fires when the player closes the Difficulty Settings dialog with
        // changes applied - including toggling "Enable R&D Scaler". Only
        // acts while actually inside the R&D complex; ApplyAll() itself
        // decides whether that means scaling up or reverting to stock.
        void OnGameSettingsApplied()
        {
            if (_inRnD) ApplyAll();
        }

        // True unless the player turned the scaler off in Difficulty
        // Settings (Section "4kSP" - see FourkSP._4kSP.rdScalerEnabled).
        // Defaults to enabled if there's no active game yet, which in
        // practice never happens here since this whole component only
        // runs inside the R&D complex of a loaded save.
        internal static bool ScalerEnabled
        {
            get
            {
                var game = HighLogic.CurrentGame;
                if (game == null) return true;
                return game.Parameters.CustomParams<_4kSP>().rdScalerEnabled;
            }
        }

        void OnRnDSpawn()
        {
            _inRnD = true;
            StartCoroutine(ApplyWhenReady());
        }

        void OnRnDDespawn()
        {
            _inRnD = false;
        }

        IEnumerator ApplyWhenReady()
        {
            int guard = 0;
            while (RDController.Instance == null && guard++ < 300) yield return null;
            yield return null;

            if (RDController.Instance == null) yield break;

            ApplyAll();
        }

        void ApplyAll()
        {
            if (!ScalerEnabled)
            {
                ResetToStock();
                return;
            }
            PatchZoomLimit();
            ScalePartsGrid();
        }

        void PatchZoomLimit()
        {
            var rd = RDController.Instance;
            var grid = rd != null ? rd.gridArea : null;
            if (grid == null) return;

            if (!_baseZoomCached)
            {
                _baseZoomMax = grid.zoomMax;
                _baseZoomCached = true;
            }

            grid.zoomMax = RDScalerConfig.MaxZoom;
        }

        // Undoes everything ApplyAll() does: restores the stock zoom cap,
        // GridLayoutGroup geometry and tile scale. Called when the scaler
        // is turned off (OnGameSettingsApplied) and, harmlessly, on entry
        // to the R&D complex when it was never turned on this session -
        // in that case _baseZoomCached/_baseGridCached are still false so
        // the zoom/grid writes are skipped, and the tile loop just sets
        // localScale to the Vector3.one it already was.
        void ResetToStock()
        {
            var rd = RDController.Instance;
            if (rd == null) return;

            if (_baseZoomCached)
            {
                var grid = rd.gridArea;
                if (grid != null) grid.zoomMax = _baseZoomMax;
            }

            if (rd.partList == null) return;
            var mask = rd.partList.partTransformMask;
            if (mask == null) return;

            if (_baseGridCached)
            {
                var glg = mask.GetComponent<GridLayoutGroup>()
                          ?? mask.GetComponentInChildren<GridLayoutGroup>(true);
                if (glg != null)
                {
                    glg.cellSize = _baseCell;
                    glg.spacing = _baseSpacing;
                    glg.constraint = _baseConstraint;
                    glg.constraintCount = _baseConstraintCount;
                    glg.padding = new RectOffset(
                        _basePadding.left, _basePadding.right,
                        _basePadding.top, _basePadding.bottom);
                }
            }

            var items = rd.partList.listItems;
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var it = items[i];
                    if (it == null || it.transform == null) continue;
                    var wrapper = it.transform.parent;
                    if (wrapper != null) wrapper.localScale = Vector3.one;
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(mask as RectTransform);
        }

        // Tile hierarchy under RDPartList.partTransformMask:
        //
        //   [PartList] GridLayoutGroup              <- glg
        //     [PartListItem(Clone)] UIListItem      <- wrapper 50x72 (real GLG item)
        //       [StateButton] RDPartListItem        <- listItems[i] (50x50 icon)
        //       [Text] TextMeshProUGUI "poss."      <- sibling of the button
        //
        // rd.partList.listItems[i] returns the RDPartListItem (MonoBehaviour
        // sitting on the StateButton), not the wrapper. Scaling the button
        // alone scales only the icon and leaves the "poss." label at base
        // size; scaling the parent (UIListItem wrapper) scales icon and
        // label together, preserving the stock prefab geometry.
        //
        // cellSize is kept at its base value so GLG keeps laying out with
        // the original cell footprint, while the wrapper localScale = p
        // inflates rendering. Spacing, padding and constraint count are
        // compensated to avoid overlap and keep everything inside the
        // container:
        //
        //   spacing    += baseCell * (p - 1)     // visible gap = baseSpacing
        //   padding    += baseCell * (p - 1)/2   // pivot (0.5, 0.5) -> symmetric
        //   constraint =  floor(baseCount / p)   // fewer columns
        //
        void ScalePartsGrid()
        {
            var rd = RDController.Instance;
            if (rd == null || rd.partList == null) return;

            var mask = rd.partList.partTransformMask;
            if (mask == null) return;

            var glg = mask.GetComponent<GridLayoutGroup>()
                      ?? mask.GetComponentInChildren<GridLayoutGroup>(true);
            if (glg == null) return;

            CacheGridBase(glg);

            float p = RDScalerConfig.PartsScale;

            glg.cellSize = _baseCell;

            glg.spacing = new Vector2(
                _baseSpacing.x + _baseCell.x * (p - 1f),
                _baseSpacing.y + _baseCell.y * (p - 1f));

            int extraXhalf = Mathf.RoundToInt(_baseCell.x * (p - 1f) * 0.5f);
            int extraYhalf = Mathf.RoundToInt(_baseCell.y * (p - 1f) * 0.5f);
            glg.padding = new RectOffset(
                _basePadding.left + extraXhalf,
                _basePadding.right + extraXhalf,
                _basePadding.top + extraYhalf,
                _basePadding.bottom + extraYhalf);

            if (_baseConstraintCount > 0)
            {
                glg.constraint = _baseConstraint;
                int cc = Mathf.FloorToInt(_baseConstraintCount / Mathf.Max(0.01f, p));
                glg.constraintCount = Mathf.Max(1, cc);
            }

            // Scale existing items. New items created during
            // RDPartList.Refresh() are caught by the Harmony postfix.
            var items = rd.partList.listItems;
            if (items != null)
            {
                var sv = new Vector3(p, p, 1f);
                for (int i = 0; i < items.Count; i++)
                {
                    var it = items[i];
                    if (it == null || it.transform == null) continue;
                    var wrapper = it.transform.parent;
                    if (wrapper != null) wrapper.localScale = sv;
                }
            }

            // Unity would rebuild the layout next frame; force it now
            // so live slider changes show up without lag.
            LayoutRebuilder.ForceRebuildLayoutImmediate(mask as RectTransform);
        }

        // Initial snapshot of the GridLayoutGroup so ApplyAll always
        // recomputes from a clean base instead of compounding on the
        // current state (which would drift as the slider moves).
        private static Vector2 _baseCell;
        private static Vector2 _baseSpacing;
        private static GridLayoutGroup.Constraint _baseConstraint;
        private static int _baseConstraintCount;
        private static RectOffset _basePadding;
        private static bool _baseGridCached;

        // Same idea, for the tech tree zoom cap touched by PatchZoomLimit().
        private static float _baseZoomMax;
        private static bool _baseZoomCached;

        private static void CacheGridBase(GridLayoutGroup glg)
        {
            if (_baseGridCached) return;
            _baseCell = glg.cellSize;
            _baseSpacing = glg.spacing;
            _baseConstraint = glg.constraint;
            _baseConstraintCount = glg.constraintCount;
            // Deep copy the RectOffset: glg.padding is a live reference
            // and would be mutated by subsequent writes.
            var pad = glg.padding;
            _basePadding = new RectOffset(
                pad != null ? pad.left : 0,
                pad != null ? pad.right : 0,
                pad != null ? pad.top : 0,
                pad != null ? pad.bottom : 0);
            _baseGridCached = true;
        }

        void AddToolbarButton()
        {
            if (_toolbar != null) return;

            _toolbarGO = new GameObject("4kSPRDScalerToolbar");
            _toolbar = _toolbarGO.AddComponent<ToolbarControl>();
            _toolbar.AddToAllToolbars(
                OnToolbarTrue, OnToolbarFalse,
                TB_SCENES,
                "4kSP",
                TB_BTN_ID,
                TB_ICON_BIG, TB_ICON_SMALL,
                TB_TOOLTIP);
        }

        private bool _tmpEnabled;
        private bool _tmpUseStock;
        private float _tmpScale;
        private bool _tmpKeepOnScreen;
        private bool _tmpLogWindows;

        private bool _showDetected = true;
        private Vector2 _detectedScroll;


        void OnToolbarTrue()
        {
            _tmpMaxZoom = RDScalerConfig.MaxZoom;
            _tmpPartsScale = RDScalerConfig.PartsScale;
            _windowShown = true;

            ModWindowScalerConfig.Load();
            _tmpEnabled = ModWindowScalerConfig.Enabled;
            _tmpUseStock = ModWindowScalerConfig.UseStockUIScale;
            _tmpScale = ModWindowScalerConfig.Scale;
            _tmpKeepOnScreen = ModWindowScalerConfig.KeepOnScreen;
            _tmpLogWindows = ModWindowScalerConfig.LogWindows;

        }

        void OnToolbarFalse()
        {
            _windowShown = false;
        }

        void ApplyTmp()
        {
            _4kSP_RnD.ModWindowScalerConfig.Enabled = _tmpEnabled;
            _4kSP_RnD.ModWindowScalerConfig.UseStockUIScale = _tmpUseStock;
            _4kSP_RnD.ModWindowScalerConfig.Scale = _tmpScale;
            _4kSP_RnD.ModWindowScalerConfig.KeepOnScreen = _tmpKeepOnScreen;
            _4kSP_RnD.ModWindowScalerConfig.LogWindows = _tmpLogWindows;
        }

        //Tooltip variables
        //Store the tooltip text from throughout the code
        String strToolTipText = "";
        String strLastTooltipText = "";
        //is it displayed and where
        Boolean blnToolTipDisplayed = false;
        Rect rectToolTipPosition; //, finalToolTipPos;
        Int32 intTooltipVertOffset = -30;
        Int32 intTooltipMaxWidth = 250;
        //timer so it only displays for a preriod of time
        float fltTooltipTime = 0f;

        const float TOOLTIP_TIME = 5f;
        internal static GUIStyle styleTooltipStyle;
        internal static Texture2D texTooltip = new Texture2D(9, 9, TextureFormat.ARGB32, false);

        internal void SetTooltipText()
        {
            if (Event.current.type == EventType.Repaint)
            {
                strToolTipText = GUI.tooltip;
            }
        }
        //internal static Texture2D texBox = new Texture2D(9, 9, TextureFormat.ARGB32, false);
        private static Vector2 GetMousePositionInWindow(Rect underlyingWindowRect)
        {
            // Convert from the current GUI context, such as the tooltip window,
            // into absolute screen coordinates.
            Vector2 screenMousePosition =
                GUIUtility.GUIToScreenPoint(Event.current.mousePosition);

            // Convert screen coordinates into coordinates local to the
            // underlying window.
            return new Vector2(
                screenMousePosition.x - underlyingWindowRect.x,
                screenMousePosition.y - underlyingWindowRect.y
            );
        }
        private void DrawToolTip()
        {
            if (styleTooltipStyle == null)
            {
                Color[] colors = new Color[9 * 9];
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = new Color(0.2f, 0.2f, 0.2f, 1f);
                }

                texTooltip.SetPixels(colors);
                texTooltip.Apply(); // Apply pushes the pixel data to the GPU
                styleTooltipStyle = new GUIStyle(GUI.skin.label);
                styleTooltipStyle.fontSize = 12;
                styleTooltipStyle.normal.textColor = new Color32(207, 207, 207, 255);
                styleTooltipStyle.stretchHeight = true;
                styleTooltipStyle.wordWrap = true;
                styleTooltipStyle.normal.background = texTooltip;
                //Extra border to prevent bleed of color - actual border is only 1 pixel wide
                styleTooltipStyle.border = new RectOffset(3, 3, 3, 3);
                styleTooltipStyle.padding = new RectOffset(4, 4, 6, 4);
                styleTooltipStyle.alignment = TextAnchor.MiddleCenter;

                //texTooltip = texBox;


            }
            //reset display time if text changed
            if (strToolTipText != strLastTooltipText)
                fltTooltipTime = Time.unscaledTime + TOOLTIP_TIME;
            if (strToolTipText != "" && (Time.unscaledTime <= fltTooltipTime))
            {
                GUIContent contTooltip = new GUIContent(strToolTipText);
                if (!blnToolTipDisplayed || (strToolTipText != strLastTooltipText))
                {
                    //Calc the size of the Tooltip
                    Vector2 mousePosition = GetMousePositionInWindow(_windowRect);
                    rectToolTipPosition = new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y + intTooltipVertOffset, 0, 0);
                    float minwidth, maxwidth;
                    styleTooltipStyle.CalcMinMaxWidth(contTooltip, out minwidth, out maxwidth); // figure out how wide one line would be
                    rectToolTipPosition.width = Math.Min(intTooltipMaxWidth - styleTooltipStyle.padding.horizontal, maxwidth); //then work out the height with a max width
                    rectToolTipPosition.height = styleTooltipStyle.CalcHeight(contTooltip, rectToolTipPosition.width); // here's the result
                    //Make sure its not off the right of the screen
                    if (rectToolTipPosition.x + rectToolTipPosition.width > Screen.width) rectToolTipPosition.x = Screen.width - rectToolTipPosition.width;
                }
                //Draw the Tooltip
                GUI.Label(rectToolTipPosition, contTooltip, styleTooltipStyle);
                //On top of everything
                GUI.depth = 0;

                //reset the flags
                blnToolTipDisplayed = true;
            }
            else
            {
                //clear the flags
                blnToolTipDisplayed = false;
            }
            strLastTooltipText = strToolTipText;
        }


        void OnGUI()
        {
            if (!_windowShown || !harmonyLoaded) return;
            GUI.skin = HighLogic.Skin;
            _windowRect = ClickThruBlocker.GUILayoutWindow(
                _windowId, _windowRect, DrawWindow, "4kSP Scaler");
            //if (settings.ShowTooltips)
            DrawToolTip();

        }

        private GUIStyle horizontalLineStyle = null;

        private void InitializeStyles()
        {
            horizontalLineStyle = new GUIStyle
            {
                normal =
                {
                    background = Texture2D.whiteTexture
                },
                margin = new RectOffset(0, 0, 5, 5),
                fixedHeight = 1
            };
        }

        private void DrawHorizontalLine(float space, float thickness = 1f)
        {
            if (horizontalLineStyle == null)
                InitializeStyles();
            Color previousColor = GUI.color;

            GUI.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            GUILayout.Space(space);
            GUILayout.Box(
                GUIContent.none,
                horizontalLineStyle,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(thickness)
            );
            GUILayout.Space(space);

            GUI.color = previousColor;
        }


        private readonly string[] tabNames =
            {
                "R & D Scaling",
                "Mod Window Scaling"
            };
        private int selectedTab;

        void DrawWindow(int id)
        {
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

            GUILayout.BeginVertical();
            switch (selectedTab)
            {
                case 0:
                    if (!ScalerEnabled)
                        GUILayout.Label("<i>Disabled in Difficulty Settings (\"Enable R & D Scaler\").</i>");

                    GUI.enabled = ScalerEnabled;
                    GUILayout.Label("<color=yellow><b>R & D Scaling</b></color>");
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(string.Format("Max Zoom: {0:F2}", _tmpMaxZoom),
                        GUILayout.Width(150));
                    float newZoom = GUILayout.HorizontalSlider(_tmpMaxZoom,
                        RDScalerConfig.MinZoomLimit, RDScalerConfig.MaxZoomLimit,
                        GUILayout.Width(180));
                    if (!Mathf.Approximately(newZoom, _tmpMaxZoom))
                    {
                        _tmpMaxZoom = newZoom;
                        RDScalerConfig.MaxZoom = newZoom;
                        if (_inRnD) ApplyAll();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(string.Format("Parts Scale: {0:F2}", _tmpPartsScale),
                        GUILayout.Width(150));
                    float newParts = GUILayout.HorizontalSlider(_tmpPartsScale,
                        RDScalerConfig.MinPartsScale, RDScalerConfig.MaxPartsScale,
                        GUILayout.Width(180));
                    if (!Mathf.Approximately(newParts, _tmpPartsScale))
                    {
                        _tmpPartsScale = newParts;
                        RDScalerConfig.PartsScale = newParts;
                        if (_inRnD) ApplyAll();
                    }
                    GUILayout.EndHorizontal();

                    GUI.enabled = true;

                    GUILayout.Space(8);

                    if (!_inRnD)
                        GUILayout.Label("<i>Enter the R & D Complex to see live changes.</i>");
                    else
                        GUILayout.Label("<b>Live changes active</b>");
                    break;

                case 1:
                    GUILayout.Label("<color=yellow><b>Mod Window Scaling</b></color>");
                    // Only flag the Difficulty Settings toggle specifically - if
                    // the checkbox right below is what's off, that's self-evident.
                    if (ModWindowScalerConfig.Enabled && !ModWindowScalerConfig.EffectiveEnabled)
                        GUILayout.Label("<i>Disabled in Difficulty Settings (\"Enable Mod Window Scaler\").</i>");

                    _tmpEnabled = GUILayout.Toggle(_tmpEnabled, "Enabled");

                    _tmpUseStock = GUILayout.Toggle(_tmpUseStock, new GUIContent("Use stock UI Scale", "Use the stock UI scale instead of a custom scale.  Note that AnyRes also has a UI scale setting which updates the stock scale settings"));

                    GUI.enabled = !_tmpUseStock;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(string.Format("Scale: {0:F2}", _tmpScale), GUILayout.Width(150));
                    _tmpScale = GUILayout.HorizontalSlider(_tmpScale,
                        ModWindowScalerConfig.MinScale, ModWindowScalerConfig.MaxScale,
                        GUILayout.Width(150));
                    GUILayout.EndHorizontal();
                    GUI.enabled = true;

                    _tmpKeepOnScreen = GUILayout.Toggle(_tmpKeepOnScreen, new GUIContent("Keep windows on screen", "Auto-shrinks windows that don't fit even scaled"));

                    _tmpLogWindows = GUILayout.Toggle(_tmpLogWindows, new GUIContent(
                        "Log windows to KSP.log",
                        "Use this to find the assembly name for an OVERRIDE / exclude entry"));

                    GUILayout.Space(8);
                    _showDetected = GUILayout.Toggle(_showDetected,
                        "Detected mod windows (" + ModWindowScalerConfig.Detected.Count + ")");
                    if (_showDetected)
                        DrawDetectedMods();
                    break;
            }

            DrawHorizontalLine(4);


            GUILayout.Space(8);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply"))
            {
                RDScalerConfig.MaxZoom = _tmpMaxZoom;
                RDScalerConfig.PartsScale = _tmpPartsScale;
                if (_inRnD) ApplyAll();
                ApplyTmp();
            }

            if (GUILayout.Button("Save"))
            {
                RDScalerConfig.MaxZoom = _tmpMaxZoom;
                RDScalerConfig.PartsScale = _tmpPartsScale;
                if (_inRnD) ApplyAll();
                RDScalerConfig.Save();
                ModWindowScalerConfig.Enabled = _tmpEnabled;
                ModWindowScalerConfig.UseStockUIScale = _tmpUseStock;
                ModWindowScalerConfig.Scale = _tmpScale;
                ModWindowScalerConfig.KeepOnScreen = _tmpKeepOnScreen;
                ModWindowScalerConfig.LogWindows = _tmpLogWindows;
                ModWindowScalerConfig.Save();
                ApplyTmp();
            }

            if (GUILayout.Button("Default"))
            {
                _tmpMaxZoom = RDScalerConfig.DefaultMaxZoom;
                _tmpPartsScale = RDScalerConfig.DefaultPartsScale;
                RDScalerConfig.ResetDefaults();
                if (_inRnD) ApplyAll();
            }

            if (GUILayout.Button("Close"))
            {
                _windowShown = false;
                if (_toolbar != null) _toolbar.SetFalse(false);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUI.DragWindow();
            SetTooltipText();
        }

        // One row per assembly seen opening a window this session (see
        // ModWindowScalerConfig.TrackWindow), with a checkbox to exclude
        // it. Toggling takes effect immediately (Overrides is read live by
        // Patch_IMGUI_Window in 4kSP-ModWindows.dll); hit "Save" above to
        // persist it to disk.
        void DrawDetectedMods()
        {
            var detected = ModWindowScalerConfig.Detected;
            if (detected.Count == 0)
            {
                GUILayout.Label("<i>No mod windows seen yet this session.</i>");
                return;
            }

            _detectedScroll = GUILayout.BeginScrollView(_detectedScroll, GUILayout.Height(140));
            for (int i = 0; i < detected.Count; i++)
            {
                var d = detected[i];
                using (new GUILayout.HorizontalScope())
                {
                    bool excluded = ModWindowScalerConfig.IsExcluded(d.Assembly);
                    bool newExcluded = GUILayout.Toggle(excluded, "Exclude");
                    GUILayout.FlexibleSpace();
                    if (newExcluded != excluded)
                        ModWindowScalerConfig.SetExcluded(d.Assembly, newExcluded);

                    GUILayout.Label(d.Assembly + "  (" + d.LastRequestedScale.ToString("0.##") + "x)");
                }
            }
            GUILayout.EndScrollView();
        }
    }
}
