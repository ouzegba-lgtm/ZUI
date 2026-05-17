using HarmonyLib;
using UnityEngine;
using ZUI.InputBlocking;

namespace ZUI.Patches
{
    /// <summary>
    /// Patches ZUI panel lifecycle to attach input blocking detectors
    /// and coordinate sustained input blocking during panel interaction.
    /// 
    /// When a ZUI panel is shown: attaches ZUIPanelInteractionDetector
    /// When the entire UI is hidden: force-clears any held input block
    /// </summary>
    public static class PanelInteractionPatch
    {
        /// <summary>
        /// Patch to attach interaction detectors when panels are enabled/shown.
        /// Targets the base panel Enable method used by UniverseLib panels.
        /// </summary>
        [HarmonyPatch(typeof(ZUI.UI.UniverseLib.UI.PanelBase), "OnEnable")]
        [HarmonyPostfix]
        private static void PanelOnEnable_Postfix(ZUI.UI.UniverseLib.UI.PanelBase __instance)
        {
            try
            {
                if (__instance == null || __instance.gameObject == null) return;
                
                // Attach interaction detector to panel root
                ZUIPanelInteractionDetector.AttachTo(__instance.gameObject);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[ZUI] Failed to attach detector: {ex.Message}");
            }
        }

        /// <summary>
        /// Also handle dynamic content inside panels (scroll views, input fields).
        /// Patches UIPanel to also attach detectors to content root.
        /// </summary>
        [HarmonyPatch(typeof(ZUI.UI.UniverseLib.UI.UIPanel), "SetActive")]
        [HarmonyPostfix]
        private static void PanelSetActive_Postfix(ZUI.UI.UniverseLib.UI.UIPanel __instance, bool active)
        {
            try
            {
                if (!active)
                {
                    // Panel hidden - release any held input block
                    ZUIInputBlocker.ForceUnblock();
                    return;
                }

                if (__instance == null || __instance.GameObject == null) return;

                // Ensure detector is attached
                var detector = __instance.GameObject.GetComponent<ZUIPanelInteractionDetector>();
                if (detector == null)
                {
                    ZUIPanelInteractionDetector.AttachTo(__instance.GameObject);
                }

                // Also scan child objects for interactive elements
                // and attach detectors to draggable title bars, resize handles, etc.
                AttachToInteractiveChildren(__instance.GameObject);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[ZUI] PanelSetActive error: {ex.Message}");
            }
        }

        /// <summary>
        /// Recursively attaches interaction detectors to key interactive elements.
        /// </summary>
        private static void AttachToInteractiveChildren(GameObject root)
        {
            if (root == null) return;

            // Attach to direct children that are likely interactive regions
            foreach (Transform child in root.transform)
            {
                var name = child.name.ToLower();
                
                // Title bar (dragging), resize handles, scroll views
                if (name.Contains("title") || name.Contains("header") ||
                    name.Contains("drag") || name.Contains("resize") ||
                    name.Contains("scroll") || name.Contains("content"))
                {
                    if (child.gameObject.GetComponent<ZUIPanelInteractionDetector>() == null)
                    {
                        ZUIPanelInteractionDetector.AttachTo(child.gameObject);
                    }
                }

                // Recurse one level for nested containers
                if (child.childCount > 0 && child.childCount < 20)
                {
                    AttachToInteractiveChildren(child.gameObject);
                }
            }
        }
    }
}
