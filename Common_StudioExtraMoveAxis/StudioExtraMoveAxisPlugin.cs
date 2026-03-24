using System;
using System.Collections.Generic;
using System.Linq;
#if !HS1
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
#else
using IllusionPlugin;
using Harmony;
#endif
#if !HS1
using Illusion.Extensions;
using KKAPI;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using KKAPI.Utilities;
#endif
using Studio;
using UnityEngine;

namespace StudioExtraMoveAxis
{
#if !HS1
    [BepInPlugin(GUID, Name, Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public partial class StudioExtraMoveAxisPlugin : BaseUnityPlugin
#else
    public partial class StudioExtraMoveAxisPlugin : IPlugin
#endif
    {
        public const string GUID = "StudioExtraMoveAxis";
#if !HS1
        public const string Name = "Extra move axis in bottom right corner";
        public const string Version = "3.0";
#endif

        private static readonly string[] BoneCycleOrder = new string[]
        {
            "cf_j_spine01",
            "cf_j_spine02",
            "cf_j_neck",
            "cf_j_head",
            "cf_j_shoulder_r",
            "cf_j_armup00_r",
            "cf_j_armlow01_r",
            "cf_j_hand_r",
            "cf_j_shoulder_l",
            "cf_j_armup00_l",
            "cf_j_armlow01_l",
            "cf_j_hand_l",
            "cf_j_legup00_r",
            "cf_j_leglow01_r",
            "cf_j_foot01_r",
            "cf_j_toes01_r",
            "cf_j_legup00_l",
            "cf_j_leglow01_l",
            "cf_j_foot01_l",
            "cf_j_toes01_l",
            "cf_j_kosi01"
        };

#if !HS1
        internal static new ManualLogSource Logger;
        
#if HS1
        private static bool _showGizmoValue = false;
        private static bool _referenceToSelectedObjectValue = true;
#else
        private static ConfigEntry<bool> _showGizmo;
#if !PH
        private static ConfigEntry<bool> _referenceToSelectedObject;
#endif
#endif
#else
        public string Name => "StudioExtraMoveAxis";
        public string Version => "2.0";
        public void OnApplicationQuit() { }
        public void OnLevelWasLoaded(int level) { }
        public void OnLevelWasInitialized(int level) { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }

        public static class Logger
        {
            public static void LogDebug(string m) { UnityEngine.Debug.Log("[MoveAxis] DEBUG: " + m); }
            public static void LogInfo(string m) { UnityEngine.Debug.Log("[MoveAxis] INFO: " + m); }
            public static void LogWarning(string m) { UnityEngine.Debug.LogWarning("[MoveAxis] WARNING: " + m); }
            public static void LogError(string m) { UnityEngine.Debug.LogError("[MoveAxis] ERROR: " + m); }
        }

        private static bool _showGizmoValue = true;
        private static bool _showGizmo
        {
            get { return _showGizmoValue; }
            set 
            {
                if (_showGizmoValue != value)
                {
                    _showGizmoValue = value;
                    SetVisibility();
                }
            }
        }
#if !PH
        private static bool _referenceToSelectedObject = true;
#endif
#endif
        
#if HS1
        private static HarmonyInstance _hi;
#else
        private static Harmony _hi;
#endif

        private static GameObject _gizmoRoot;
        private static GameObject _moveObj, _rotObj, _scaleObj;
        private static GuideMove[] _guideMoves;

        private static HashSet<GuideObject> _selectedObjects;
        private static bool _lastAnySelected;

        private static Camera _camera;
        private static float _lastFov;
        private static int _lastScreenWidth;
        private static int _lastScreenHeight;

        private static bool IsStudioLoaded
        {
            get
            {
#if HS1
                return Studio.Studio.Instance != null;
#else
                return StudioAPI.StudioLoaded;
#endif
            }
        }

#if !HS1
        private void Awake()
        {
            Logger = base.Logger;
#else
        public void OnApplicationStart()
        {
            Logger.LogInfo("OnApplicationStart triggered!");
#endif

#if !HS1
            _showGizmo = Config.Bind("Extra gizmos", "Show extra move gizmo", false,
                "Show extra set of gizmos in the bottom right corner of the screen. An object must be selected for gizmo to be visible." +
                "You can use left toolbar to turn the gizmo on or off.");
#if !PH
            _referenceToSelectedObject = Config.Bind("Extra gizmos", "Use selected object as reference", true,
                "If true, using the extra XYZ move gizmo is the same as using the default gizmo on the currently selected object (so direction of the arrow may not be the same as direction of movement).\n" +
                "If false, current camera position is used as the reference frame, so using the extra XYZ gizmo will move the object to where the gizmo arrows are actually pointing.\n" +
                "Change currently selected object to apply the setting.");
#endif
#endif

#if !HS1
            if (StudioAPI.StudioLoaded)
            {
                // for debug purposes, doesn't get called normally
                Initialize();
            }
            else
            {
                var buttonTex = ResourceUtils.GetEmbeddedResource("toolbar_icon.png", typeof(StudioExtraMoveAxisPlugin).Assembly).LoadTexture(TextureFormat.DXT5, false);
                var tgl = CustomToolbarButtons.AddLeftToolbarToggle(buttonTex, _showGizmo.Value, b => _showGizmo.Value = b);
                _showGizmo.SettingChanged += (o, eventArgs) =>
                {
                    tgl.SetValue(_showGizmo.Value, false);
                    SetVisibility();
                };

                StudioAPI.StudioLoadedChanged += (sender, args) => Initialize();
            }
#else
            // In HS1, we will poll for Studio load in the Update loop 
            // since scene name might differ and sceneLoaded hook can be unreliable across BepInEx versions.
#endif
        }

#if DEBUG
        private void OnDestroy()
        {
            UnityEngine.Object.Destroy(_gizmoRoot);
            _hi?.UnpatchAll(_hi.Id);
            _selectedObjects = null;
        }
#endif

#if HS1
        private bool _hs1Initialized = false;
#endif

#if HS1
        private static bool ShowGizmo => _showGizmoValue;
        private static void SetShowGizmo(bool val) { _showGizmoValue = val; SetVisibility(); }
#else
        private static bool ShowGizmo => _showGizmo.Value;
        private static void SetShowGizmo(bool val) { _showGizmo.Value = val; }
#endif

#if HS1
        public void OnUpdate()
#else
        private void Update()
#endif
        {
#if HS1
            if (!_hs1Initialized)
            {
                if (Input.GetKeyDown(KeyCode.K))
                {
                    Logger.LogInfo("Diagnostic Key K pressed!");
                    Logger.LogInfo("IsStudioLoaded: " + IsStudioLoaded);
                    
                    bool guideObjManagerExists = GuideObjectManager.Instance != null;
                    Logger.LogInfo("GuideObjectManager.Instance != null: " + guideObjManagerExists);
                    
                    if (guideObjManagerExists)
                    {
                        try 
                        {
                            Logger.LogInfo("GetGuideObjectOriginal() != null: " + (GetGuideObjectOriginal() != null));
                        } 
                        catch (Exception e) 
                        {
                            Logger.LogError("Error calling GetGuideObjectOriginal(): " + e.Message);
                        }
                    }
                }

                if (IsStudioLoaded && GuideObjectManager.Instance != null && GetGuideObjectOriginal() != null)
                {
                    try
                    {
                        Logger.LogInfo("All initial conditions met, calling Initialize()...");
                        Initialize();
                        _hs1Initialized = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Failed to Initialize: " + ex.ToString());
                    }
                }
                return;
            }
            if (Input.GetKeyDown(KeyCode.M) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                SetShowGizmo(!_showGizmoValue);
                Logger.LogInfo("Toggled Extra Move Axis Gizmo: " + _showGizmoValue);
            }
#endif
            if (_selectedObjects == null)
            {
                Logger.LogWarning("_selectedObjects is null!");
                return;
            }
            if (!ShowGizmo) return;

            var anySelected = _selectedObjects.Count > 0;
            if (_lastAnySelected != anySelected)
            {
                _lastAnySelected = anySelected;
                Logger.LogInfo($"Selection state changed. anySelected={anySelected}, Count={_selectedObjects.Count}");
                SetVisibility();
            }

            if (_lastFov != _camera.fieldOfView || _lastScreenWidth != _camera.pixelWidth || _lastScreenHeight != _camera.pixelHeight)
            {
                AdjustScaleToFov();
                _lastScreenWidth = _camera.pixelWidth;
                _lastScreenHeight = _camera.pixelHeight;
            }
                
            CheckHoverExit();
            HandleBoneScroll();
            HandleCustomDrag();
            ApplyRotationRestrictions();
        }

        internal static bool _customDragActive = false;
        internal static Vector2 _dragStartPos;
        internal static Vector3 _dragStartRot;
        internal static Vector2 _dragDirection;
        internal static bool _dragDirectionSet;
        internal static GuideObject _draggedGuideObject;
        internal static string _draggedConstrainedAxis; // "x", "y", "z"

        private void HandleCustomDrag()
        {
            if (_customDragActive && _draggedGuideObject != null)
            {
                if (!Input.GetMouseButton(0)) // Drop drag if mouse released
                {
                    _customDragActive = false;
                    _draggedGuideObject = null;
                    return;
                }

                Vector2 currentMousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                Vector2 delta = currentMousePos - _dragStartPos;

                if (delta.magnitude > 2f)
                {
                    if (!_dragDirectionSet)
                    {
                        // Lock to the user's ACTUAL initial movement direction (not snapped to H/V).
                        // This creates a full ±90° acceptance cone around the initial gesture,
                        // so any mouse direction that has a component along the first movement contributes.
                        _dragDirection = delta.normalized;
                        _dragDirectionSet = true;
                    }

                    // Project all subsequent mouse movement onto the locked reference direction.
                    // Anything within ±90° of that direction adds rotation; perpendicular = zero.
                    float distance = Vector2.Dot(delta, _dragDirection);
                    float angleDelta = distance * 0.5f; // drag sensitivity

                    Vector3 newRot = _dragStartRot;

                    // Depending on starting rotation, movement direction might feel inverted. 
                    // To handle arbitrary camera angles gracefully, it locks to distance moved.
                    if (_draggedConstrainedAxis == "x")
                        newRot.x += angleDelta;
                    else if (_draggedConstrainedAxis == "y")
                        newRot.y += angleDelta;
                    else if (_draggedConstrainedAxis == "z")
                        newRot.z += angleDelta;

                    _draggedGuideObject.changeAmount.rot = newRot;
                }
            }
        }

        private void ApplyRotationRestrictions()
        {
            if (_selectedObjects == null || _selectedObjects.Count == 0) return;

            bool extraAllowX = false;
            bool extraAllowY = false;
            bool extraAllowZ = false;

            // Apply to the World Gizmos (all selected objects)
            foreach (var g in _selectedObjects)
            {
                if (g == null || g.transformTarget == null) continue;

                string name = g.transformTarget.name.ToLower();

                bool allowX = true;
                bool allowY = true;
                bool allowZ = true;

                // Knees: X only
                if (name == "cf_j_leglow01_r" || name == "cf_j_leglow01_l")
                {
                    allowY = false; allowZ = false;
                }
                // Elbows: Y only
                else if (name == "cf_j_armlow01_r" || name == "cf_j_armlow01_l")
                {
                    allowX = false; allowZ = false;
                }
                // Thumbs outermost: Y only
                else if (name == "cf_j_hand_thumb03_r" || name == "cf_j_hand_thumb03_l")
                {
                    allowX = false; allowZ = false;
                }
                // Others outermost 2: Z only
                else if (name.Contains("index02") || name.Contains("index03") ||
                         name.Contains("middle02") || name.Contains("middle03") ||
                         name.Contains("ring02") || name.Contains("ring03") ||
                         name.Contains("little02") || name.Contains("little03"))
                {
                    allowX = false; allowY = false;
                }

                extraAllowX |= allowX;
                extraAllowY |= allowY;
                extraAllowZ |= allowZ;

                // Restrict the physical UI rings for THIS world bone, but ALWAYS leave the center orb (else = true)
                Transform rot = g.transform.Find("rotation");
                if (rot != null)
                {
                    foreach (Transform t in rot)
                    {
                        string tName = t.name.ToLower();
                        if (tName == "x") t.gameObject.SetActiveIfDifferent(allowX);
                        else if (tName == "y") t.gameObject.SetActiveIfDifferent(allowY);
                        else if (tName == "z") t.gameObject.SetActiveIfDifferent(allowZ);
                        else t.gameObject.SetActiveIfDifferent(true); // Center orb
                    }
                }

                // Mathematically lock the bone — only during ExtraMoveAxis custom drags.
                // Gating on _customDragActive ensures the studio's own FK panel inputs are never snapped back.
                if ((!allowX || !allowY || !allowZ) && _customDragActive && _draggedGuideObject == g)
                {
                    Quaternion q = Quaternion.Euler(g.changeAmount.rot);

                    if (allowX && !allowY && !allowZ)
                    {
                        float num = Mathf.Sqrt(q.w * q.w + q.x * q.x);
                        q = num > 0.0001f ? new Quaternion(q.x / num, 0f, 0f, q.w / num) : Quaternion.identity;
                    }
                    else if (!allowX && allowY && !allowZ)
                    {
                        float num = Mathf.Sqrt(q.w * q.w + q.y * q.y);
                        q = num > 0.0001f ? new Quaternion(0f, q.y / num, 0f, q.w / num) : Quaternion.identity;
                    }
                    else if (!allowX && !allowY && allowZ)
                    {
                        float num = Mathf.Sqrt(q.w * q.w + q.z * q.z);
                        q = num > 0.0001f ? new Quaternion(0f, 0f, q.z / num, q.w / num) : Quaternion.identity;
                    }
                    else
                    {
                        Vector3 currentRot = q.eulerAngles;
                        if (!allowX) currentRot.x = 0f;
                        if (!allowY) currentRot.y = 0f;
                        if (!allowZ) currentRot.z = 0f;
                        q = Quaternion.Euler(currentRot);
                    }

                    g.changeAmount.rot = q.eulerAngles;
                }
            }

            // Apply global combined permissions to the Extra Gizmo
            if (_rotObj != null)
            {
                foreach (Transform t in _rotObj.transform)
                {
                    string tName = t.name.ToLower();
                    if (tName == "x") t.gameObject.SetActiveIfDifferent(extraAllowX);
                    else if (tName == "y") t.gameObject.SetActiveIfDifferent(extraAllowY);
                    else if (tName == "z") t.gameObject.SetActiveIfDifferent(extraAllowZ);
                    else t.gameObject.SetActiveIfDifferent(true);
                }
            }
        }

    // SanitizeAxis removed, using superior Quaternion projection above

        private static GuideObject _originalSelectedBone;
        private static bool _isShiftScrollingActive;
        private static int _shiftScrollDistance;

        private void CheckHoverExit()
        {
            if (!_isShiftScrollingActive || _gizmoRoot == null) return;
            
            // Pillarboxing breaks strict pixel distances. Use a flexible percentage of the screen height.
            Vector3 gizmoScreenPos = _camera.WorldToScreenPoint(_gizmoRoot.transform.position);
            Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            Vector2 gizmo2D = new Vector2(gizmoScreenPos.x, gizmoScreenPos.y);

            // 35% of the screen height acts as a very generous and flexible "bottom right quadrant" exit boundary
            if (Vector2.Distance(mousePos, gizmo2D) > (Screen.height * 0.35f)) 
            {
                // Revert to original selection
                if (_originalSelectedBone != null)
                {
                    foreach (var obj in GetSelectedObjects())
                    {
                        if (obj != _originalSelectedBone)
                        {
                            obj.isActive = false;
                        }
                    }
                    GuideObjectManager.Instance.selectObject = _originalSelectedBone;
                }
                _isShiftScrollingActive = false;
                _originalSelectedBone = null;
                _shiftScrollDistance = 0;
            }
        }

        private void HandleBoneScroll()
        {
            float scroll = 0f;
            try
            {
                scroll = Input.GetAxis("Mouse ScrollWheel");
            }
            catch { }
            
            if (Mathf.Abs(scroll) < 0.01f)
            {
                scroll = Input.mouseScrollDelta.y;
            }
            
            if (Mathf.Abs(scroll) < 0.01f) return;
            // Logger.LogDebug($"[Scroll] Detected scroll input: {scroll}");

            // If a constrained-bone drag is active, cancel it so the scroll can cycle bones cleanly.
            // Without this, HandleCustomDrag() overwrites the old bone's rotation every frame and
            // makes bone cycling appear to do nothing.
            if (_customDragActive)
            {
                _customDragActive = false;
                _draggedGuideObject = null;
            }

            if (!IsStudioLoaded) 
            {
                Logger.LogDebug("[Scroll] Studio is not loaded");
                return;
            }
            if (_selectedObjects == null || _selectedObjects.Count == 0) 
            {
                Logger.LogDebug("[Scroll] No selected objects");
                return;
            }

            // Restrict scrolling to when the mouse is physically near the gizmo on screen
            if (_gizmoRoot != null)
            {
                Vector3 gizmoScreenPos = _camera.WorldToScreenPoint(_gizmoRoot.transform.position);
                Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                Vector2 gizmo2D = new Vector2(gizmoScreenPos.x, gizmoScreenPos.y);

                float dist = Vector2.Distance(mousePos, gizmo2D);
                // About 20% of the screen height gives a nice, generous hover zone around the widget
                if (dist > (Screen.height * 0.20f)) 
                {
                    Logger.LogDebug($"[Scroll] Mouse too far from widget. Distance: {dist}, Threshold: {Screen.height * 0.20f}");
                    return;
                }
            }
            else 
            {
                Logger.LogDebug("[Scroll] Gizmo root is null");
            }

            var guide = _selectedObjects.FirstOrDefault(x => x.isActive);
            if (guide == null || guide.transformTarget == null) 
            {
                Logger.LogDebug("[Scroll] Selected guide or transformTarget is null");
                return;
            }

            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (shiftHeld)
            {
                if (!_isShiftScrollingActive)
                {
                    _isShiftScrollingActive = true;
                    _originalSelectedBone = guide; // Record the starting bone
                    _shiftScrollDistance = 0;
                }
            }
            else
            {
                if (_isShiftScrollingActive)
                {
                    // They let go of shift, but haven't moused away. If they scroll now without shift, cancel multi-select.
                    _isShiftScrollingActive = false;
                    _originalSelectedBone = null;
                    _shiftScrollDistance = 0;
                }
            }

            GuideObject refGuide = _isShiftScrollingActive && _originalSelectedBone != null ? _originalSelectedBone : guide;

            // Try to find if this is part of a character FK
            List<OCIChar.BoneInfo> charBones = GetFKBonesFromGuide(refGuide);
            if (charBones == null || charBones.Count == 0) 
            {
                Logger.LogDebug($"[Scroll] GetFKBonesFromGuide returned null/empty for guide {(refGuide.transformTarget != null ? refGuide.transformTarget.name : "null")}");
                return;
            }

            // Determine our current bone's place in the custom order list
            string currentName = refGuide.transformTarget.name;
            int currentIndex = GetBoneOrderIndex(currentName);
            Logger.LogDebug($"[Scroll] Scroll success. currentName={currentName}, currentIndex={currentIndex}, shiftHeld={shiftHeld}");
            
            int step = scroll > 0 ? 1 : -1; // scroll up usually > 0
            
            if (shiftHeld)
            {
                _shiftScrollDistance += step;
                // Allow up to 2 additional bones beyond the original selection
                if (_shiftScrollDistance > 2) _shiftScrollDistance = 2;
                if (_shiftScrollDistance < -2) _shiftScrollDistance = -2;

                // Reset to 1 object first
                GuideObjectManager.Instance.selectObject = _originalSelectedBone;

                int dir = Math.Sign(_shiftScrollDistance);
                int count = Math.Abs(_shiftScrollDistance);

                if (currentIndex == -1) currentIndex = 0;

                for (int i = 1; i <= count; i++)
                {
                    int nextIndex = (currentIndex + dir * i) % BoneCycleOrder.Length;
                    if (nextIndex < 0) nextIndex += BoneCycleOrder.Length;
                    
                    string targetName = BoneCycleOrder[nextIndex].ToLower();
                    
                    var nextBone = charBones.FirstOrDefault(b => b.guideObject != null && b.guideObject.transformTarget != null && b.guideObject.transformTarget.name.ToLower() == targetName);
                    
                    if (nextBone != null)
                    {
                        nextBone.guideObject.isActive = true;
                        var selectedObjects = GetSelectedObjects();
                        if (!selectedObjects.Contains(nextBone.guideObject))
                        {
                            selectedObjects.Add(nextBone.guideObject);
                        }
                    }
                }
            }
            else
            {
                for (int i = 1; i <= BoneCycleOrder.Length; i++)
                {
                    int nextIndex = 0;
                    if (currentIndex == -1) 
                    {
                        nextIndex = 0;
                        currentIndex = 0;
                    }
                    else
                    {
                        nextIndex = (currentIndex + step * i) % BoneCycleOrder.Length;
                        if (nextIndex < 0) nextIndex += BoneCycleOrder.Length;
                    }
                    
                    string targetName = BoneCycleOrder[nextIndex].ToLower();
                    
                    var nextBone = charBones.FirstOrDefault(b => b.guideObject != null && b.guideObject.transformTarget != null && b.guideObject.transformTarget.name.ToLower() == targetName);
                    
                    if (nextBone != null)
                    {
                        GuideObjectManager.Instance.selectObject = nextBone.guideObject;
                        break;
                    }
                }
            }
        }

        private List<OCIChar.BoneInfo> GetFKBonesFromGuide(GuideObject guide)
        {
            GuideObject currentGuide = guide;
            while (currentGuide != null)
            {
                ObjectCtrlInfo tempSel;
                if (Studio.Studio.Instance.dicObjectCtrl.TryGetValue(currentGuide.dicKey, out tempSel))
                {
                    if (tempSel is OCIChar ociChar && ociChar.fkCtrl.enabled)
                    {
                        return ociChar.listBones;
                    }
#if !HS1
                    if (tempSel is OCIItem ociItem && ociItem.isFK && ociItem.itemFKCtrl.enabled)
                    {
                        return ociItem.listBones;
                    }
#else
                    if (tempSel is OCIItem ociItem)
                    {
                        var isFkField = Traverse.Create(ociItem).Field("isFK");
                        var itemFkCtrlField = Traverse.Create(ociItem).Field("itemFKCtrl");
                        
                        if (isFkField.FieldExists() && isFkField.GetValue<bool>() && 
                            itemFkCtrlField.FieldExists() && itemFkCtrlField.GetValue<FKCtrl>().enabled)
                        {
                            return Traverse.Create(ociItem).Field("listBones").GetValue<List<OCIChar.BoneInfo>>();
                        }
                    }
#endif
                }
                
                // The FK bone itself doesn't directly map to the Character.
                // We must traverse up the parent chain to find the Root guide object.
                currentGuide = currentGuide.parentGuide;
            }

            // Fallback for games like Koikatsu where `parentGuide` chain might be broken or nonexistent for FK bones.
            foreach (var kvp in Studio.Studio.Instance.dicObjectCtrl)
            {
                if (kvp.Value is OCIChar fallbackChar && fallbackChar.fkCtrl.enabled)
                {
                    if (fallbackChar.listBones != null)
                    {
                        foreach (var bone in fallbackChar.listBones)
                        {
                            if (bone != null && bone.guideObject == guide)
                            {
                                return fallbackChar.listBones;
                            }
                        }
                    }
                }
#if !HS1
                else if (kvp.Value is OCIItem fallbackItem && fallbackItem.isFK && fallbackItem.itemFKCtrl.enabled)
                {
                    if (fallbackItem.listBones != null)
                    {
                        foreach (var bone in fallbackItem.listBones)
                        {
                            if (bone != null && bone.guideObject == guide)
                            {
                                return fallbackItem.listBones;
                            }
                        }
                    }
                }
#else
                else if (kvp.Value is OCIItem fallbackItem)
                {
                    var isFkField = Traverse.Create(fallbackItem).Field("isFK");
                    var itemFkCtrlField = Traverse.Create(fallbackItem).Field("itemFKCtrl");
                    
                    if (isFkField.FieldExists() && isFkField.GetValue<bool>() && 
                        itemFkCtrlField.FieldExists() && itemFkCtrlField.GetValue<FKCtrl>().enabled)
                    {
                        var listBones = Traverse.Create(fallbackItem).Field("listBones").GetValue<List<OCIChar.BoneInfo>>();
                        if (listBones != null)
                        {
                            foreach (var bone in listBones)
                            {
                                if (bone != null && bone.guideObject == guide)
                                {
                                    return listBones;
                                }
                            }
                        }
                    }
                }
#endif
            }

            return null;
        }

        private int GetBoneOrderIndex(string boneName)
        {
            string lowerBoneName = boneName.ToLower();
            for (int i = 0; i < BoneCycleOrder.Length; i++)
            {
                if (lowerBoneName == BoneCycleOrder[i].ToLower()) return i;
            }
            return -1;
        }

        private static GameObject GetGuideObjectOriginal()
        {
#if HS1
            return Traverse.Create(GuideObjectManager.Instance).Field("objectOriginal").GetValue<GameObject>();
#else
            return GuideObjectManager.Instance.objectOriginal;
#endif
        }

        private static HashSet<GuideObject> GetSelectedObjects()
        {
#if HS1
            return Traverse.Create(GuideObjectManager.Instance).Field("hashSelectObject").GetValue<HashSet<GuideObject>>();
#else
            return GuideObjectManager.Instance.hashSelectObject;
#endif
        }

        private static void Initialize()
        {
            if (!IsStudioLoaded) return;

            _camera = Camera.main;
            if (_camera == null) throw new ArgumentException("Camera.main not found");

            var origRoot = GetGuideObjectOriginal();
            if (origRoot == null) throw new ArgumentException("origRoot not found");

            _selectedObjects = GetSelectedObjects() ?? throw new ArgumentException("Couldn't get hashSelectObject");

            _gizmoRoot = UnityEngine.Object.Instantiate(origRoot, _camera.transform.position, _camera.transform.rotation) as GameObject;
            if (_gizmoRoot != null)
            {
                _gizmoRoot.transform.SetParent(_camera.transform, false);
            }
            _gizmoRoot.gameObject.name = "CustomManipulatorGizmo";

            var go = _gizmoRoot.GetComponent<GuideObject>();
            UnityEngine.Object.Destroy(go);

            AdjustScaleToFov();

            _gizmoRoot.transform.localEulerAngles = new Vector3(17f, 150f, 343f);

            var visibleLayer = LayerMask.NameToLayer("Studio/Select");
            foreach (Transform rootChild in _gizmoRoot.transform)
            {
                switch (rootChild.name)
                {
                    case "move":
                        _moveObj = rootChild.gameObject;
                        _guideMoves = _moveObj.GetComponentsInChildren<GuideMove>();
                        break;

                    case "rotation":
                        _rotObj = rootChild.gameObject;
                        break;

                    case "scale":
                        _scaleObj = rootChild.gameObject;
                        break;

                    default:
                        UnityEngine.Object.Destroy(rootChild.gameObject);
                        break;
                }

                // todo configurable? can work between 0.75 and 1.5
                rootChild.localScale = Vector3.one;

                foreach (var subChild in rootChild.GetComponentsInChildren<Transform>(true))
                {
#if !HS1
                    subChild.gameObject.layer = visibleLayer;
#endif
                    // Fix center point gizmos being disabled in some games
                    subChild.gameObject.SetActiveIfDifferent(true);
                }
            }

            if (_moveObj == null) throw new ArgumentException("_moveObj not found");
            if (_rotObj == null) throw new ArgumentException("_rotObj not found");
            if (_scaleObj == null) throw new ArgumentException("_scaleObj not found");

            SetVisibility(GuideObjectManager.Instance.mode);

            _gizmoRoot.SetActiveIfDifferent(true);

#if HS1
            // Intentional blank: UI injection logic removed since it was placing it in a hidden/wrong layout.
            // Using Ctrl+M hotkey instead.
#endif

#if HS1
            _hi = HarmonyInstance.Create(GUID);
            _hi.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
#else
            _hi = Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
#endif
        }

        private static void AdjustScaleToFov()
        {
            // Calculate using the camera's actual pixel viewport rect (accommodates letterboxing/aspect ratios)
            var screenPos = new Vector3(_camera.pixelRect.xMin + _camera.pixelWidth * 0.9f, _camera.pixelRect.yMin + _camera.pixelHeight * 0.14f, 6f);
            
            _gizmoRoot.transform.position = _camera.ScreenToWorldPoint(screenPos);
            var fov = _camera.fieldOfView;
            _gizmoRoot.transform.localScale = Vector3.one * (fov / 23f);
            _lastFov = fov;
        }

#if !PH
#if HS1
        private static void SetMoveRootTr(Transform rootTransform)
        {
            if (!_referenceToSelectedObject)
                rootTransform = _gizmoRoot.transform;

            for (var i = 0; i < _guideMoves.Length; i++)
            {
                var guideMove = _guideMoves[i];
                Traverse.Create(guideMove).Field("moveCalc").SetValue(2); // Equivalent to MoveCalc.TYPE3 in HS1 which is an int or enum 2
                Traverse.Create(guideMove).Field("transformRoot").SetValue(rootTransform);
            }
        }
#else
        private static void SetMoveRootTr(Transform rootTransform)
        {
            if (!_referenceToSelectedObject.Value)
                rootTransform = _gizmoRoot.transform;

            for (var i = 0; i < _guideMoves.Length; i++)
            {
                var guideMove = _guideMoves[i];
                guideMove.moveCalc = GuideMove.MoveCalc.TYPE3;
                guideMove.transformRoot = rootTransform;
            }
        }
#endif
#endif

        private static void SetVisibility()
        {
            SetVisibility(GuideObjectManager.Instance.mode);
        }

        private static void SetVisibility(int value)
        {
            //todo add setting
            if (!_lastAnySelected || !ShowGizmo)
            {
                _moveObj.SetActiveIfDifferent(false);
                _rotObj.SetActiveIfDifferent(false);
                _scaleObj.SetActiveIfDifferent(false);
                return;
            }

            switch (value)
            {
                case 0:
#if !PH
                    // Some objects can't be moved
#if HS1
                    bool moveIsVisible = true;
                    try 
                    {
                        var workInfo = Traverse.Create(Singleton<Studio.Studio>.Instance).Field("workInfo");
                        if (workInfo != null && workInfo.FieldExists() && workInfo.Field("visibleAxisTranslation").FieldExists())
                        {
                            moveIsVisible = workInfo.Field("visibleAxisTranslation").GetValue<bool>();
                        }
                        else
                        {
                             Logger.LogWarning("visibleAxisTranslation field not found on workInfo!");
                        }
                    } 
                    catch (Exception ex) 
                    {
                        Logger.LogError("Error getting moveIsVisible: " + ex);
                    }
                    Logger.LogInfo($"SetVisibility: moveIsVisible={moveIsVisible}");
#else
                    var moveIsVisible = Singleton<Studio.Studio>.Instance.workInfo?.visibleAxisTranslation ?? true;
#endif
                    _moveObj.SetActiveIfDifferent(moveIsVisible);
#else
                    _moveObj.SetActiveIfDifferent(true);
#endif
                    _rotObj.SetActiveIfDifferent(false);
                    _scaleObj.SetActiveIfDifferent(false);
                    break;

                case 1:
                    _moveObj.SetActiveIfDifferent(false);
                    _rotObj.SetActiveIfDifferent(true);
                    _scaleObj.SetActiveIfDifferent(false);
                    break;

                case 2:
                    // todo some objects can't be scaled
                    _moveObj.SetActiveIfDifferent(false);
                    _rotObj.SetActiveIfDifferent(false);
                    _scaleObj.SetActiveIfDifferent(true);
                    break;

                default:
                    Logger.LogWarning("Unknown GuideObject mode - " + value);
                    break;
            }
        }

        private static class Hooks
        {
            #region Cursor lock when dragging

            private static bool _locked;

            [HarmonyPostfix]
            [HarmonyPatch(typeof(GuideBase), nameof(GuideBase.OnBeginDrag))]
            private static void OnBeginDragHook(GuideBase __instance/*, PointerEventData eventData*/)
            {
                if (_gizmoRoot != null && __instance.transform.parent?.parent == _gizmoRoot.transform)
                {
                    _locked = true;
                    var gc = GameCursor.Instance;
                    // Save current cursor position and lock it
                    gc.SetCursorLock(true);
                    // Stop the game resetting cursor position to the center of the screen on every frame, which breaks how gizmo dragging works
                    gc.enabled = false;
                    // Prevent camera script from unlocking the cursor on every frame
                    UnityEngine.Object.FindObjectOfType<Studio.CameraControl>().isCursorLock = false;
                }

                // Simplify check: As long as it's not a translation (move) arrow, and we are dragging a constrained bone, trigger constraint!
                if (__instance is GuideBase && __instance.name.ToLower() != "x" && __instance.name.ToLower() != "y" && __instance.name.ToLower() != "z")
                {
                    GuideObject go = __instance.guideObject;
                    if (go != null && go.transformTarget != null)
                    {
                        string name = go.transformTarget.name.ToLower();
                        string constrainedAxis = null;
                        
                        // Discover if bone is constrained
                        if (name.Contains("leglow01_r") || name.Contains("leglow01_l")) constrainedAxis = "x";
                        else if (name.Contains("armlow01_r") || name.Contains("armlow01_l")) constrainedAxis = "y";
                        else if (name.Contains("hand_thumb03_r") || name.Contains("hand_thumb03_l")) constrainedAxis = "y";
                        else if (name.Contains("index02") || name.Contains("index03") ||
                                 name.Contains("middle02") || name.Contains("middle03") ||
                                 name.Contains("ring02") || name.Contains("ring03") ||
                                 name.Contains("little02") || name.Contains("little03")) constrainedAxis = "z";

                        if (constrainedAxis != null)
                        {
                            _customDragActive = true;
                            _draggedGuideObject = go;
                            _dragStartPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                            _dragStartRot = go.changeAmount.rot;
                            _dragDirectionSet = false;
                            _dragDirection = Vector2.zero;
                            _draggedConstrainedAxis = constrainedAxis;
                        }
                    }
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(GuideBase), nameof(GuideBase.OnEndDrag))]
            private static void OnEndDragHook(/*GuideBase __instance*/)
            {
                if (_locked)
                {
                    GameCursor.Instance.SetCursorLock(false);
                    _locked = false;
                    GameCursor.Instance.enabled = true;
                    UnityEngine.Object.FindObjectOfType<Studio.CameraControl>().isCursorLock = true;
                }
            }

            #endregion

            #region Attaching our gizmo to stock gizmo code

            [HarmonyPrefix]
            [HarmonyPatch(typeof(GuideObjectManager), nameof(GuideObjectManager.mode), MethodType.Setter)]
            private static void SetModeHook(int value, int ___m_Mode)
            {
                if (value != ___m_Mode && _moveObj != null)
                {
                    SetVisibility(value);
                }
            }

#if !PH && !HS1
            [HarmonyPostfix]
            [HarmonyPatch(typeof(GuideObjectManager), nameof(GuideObjectManager.SetVisibleTranslation))]
            private static void SetVisibleTranslationHook()
            {
                SetVisibility();
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(GuideObjectManager), nameof(GuideObjectManager.AddObject))]
            [HarmonyPatch(typeof(GuideObjectManager), nameof(GuideObjectManager.SetDeselectObject))]
            private static void AddObjectHook(GuideObject _object)
            {
                if (_object == null || _selectedObjects == null) return;

                var selectedObj = _selectedObjects.FirstOrDefault(x => x.isActive);
                if (selectedObj != null)
                    SetMoveRootTr(selectedObj.transformTarget);

                SetVisibility();
            }
#endif
            #endregion
        }
    }
    
#if HS1
    internal static class HS1Extensions
    {
        public static void SetActiveIfDifferent(this GameObject gameObject, bool active)
        {
            if (gameObject.activeSelf != active)
                gameObject.SetActive(active);
        }
    }
#endif
}
