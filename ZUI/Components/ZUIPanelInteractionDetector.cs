using UnityEngine;
using UnityEngine.EventSystems;

namespace ZUI.InputBlocking
{
    /// <summary>
    /// Attached to ZUI panel root GameObjects. Detects pointer enter/exit 
    /// and coordinates with ZUIInputBlocker to hold game input blocked 
    /// while the user is interacting with the panel.
    /// 
    /// Uses Unity's EventSystem for reliable pointer detection across 
    /// all Canvas rendering modes.
    /// </summary>
    public class ZUIPanelInteractionDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private bool _isPointerOver = false;
        private CanvasGroup _canvasGroup;

        void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                _canvasGroup.blocksRaycasts = true;
            }
        }

        void OnEnable()
        {
            // If the panel was re-enabled while pointer was over it,
            // we'll catch it on the next pointer event
        }

        void OnDisable()
        {
            // Panel hidden/closed — release input block
            if (_isPointerOver)
            {
                _isPointerOver = false;
                ZUIInputBlocker.EndInteraction();
                UnityEngine.Debug.Log($"[ZUI] Panel '{gameObject.name}' hidden — releasing input block");
            }
        }

        void OnDestroy()
        {
            if (_isPointerOver)
            {
                _isPointerOver = false;
                ZUIInputBlocker.EndInteraction();
            }
        }

        /// <summary>
        /// Called by Unity EventSystem when pointer enters this GameObject's rect.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isPointerOver)
            {
                _isPointerOver = true;
                ZUIInputBlocker.BeginInteraction();
                UnityEngine.Debug.Log($"[ZUI] Pointer entered panel '{gameObject.name}' — blocking game input");
            }
        }

        /// <summary>
        /// Called by Unity EventSystem when pointer exits this GameObject's rect.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isPointerOver)
            {
                _isPointerOver = false;
                ZUIInputBlocker.EndInteraction();
                UnityEngine.Debug.Log($"[ZUI] Pointer left panel '{gameObject.name}' — releasing block");
            }
        }

        /// <summary>
        /// Apply this component to a ZUI panel root GameObject.
        /// Ensures it has a CanvasGroup for raycast targeting.
        /// </summary>
        public static ZUIPanelInteractionDetector AttachTo(GameObject panelRoot)
        {
            if (panelRoot == null) return null;

            var existing = panelRoot.GetComponent<ZUIPanelInteractionDetector>();
            if (existing != null) return existing;

            // Ensure the panel can receive pointer events
            var canvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panelRoot.AddComponent<CanvasGroup>();
            }
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            var detector = panelRoot.AddComponent<ZUIPanelInteractionDetector>();
            UnityEngine.Debug.Log($"[ZUI] Interaction detector attached to '{panelRoot.name}'");
            return detector;
        }
    }
}
