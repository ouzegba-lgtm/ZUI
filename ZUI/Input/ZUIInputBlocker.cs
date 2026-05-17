using BepInEx.Unity.IL2CPP.Utils;
using ProjectM;
using System;
using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace ZUI.InputBlocking
{
    /// <summary>
    /// Input blocker that blocks game inputs while the user interacts with ZUI panels.
    /// Supports both momentary click blocking and sustained interaction blocking.
    /// </summary>
    public static class ZUIInputBlocker
    {
        private static bool _shouldBlock = false;
        private static bool _isInitialized = false;
        private static Coroutine _unblockCoroutine;
        
        /// <summary>
        /// Tracks whether input is being held blocked by panel interaction
        /// (as opposed to momentary click blocking).
        /// </summary>
        private static bool _isHeldBlock = false;

        /// <summary>
        /// Gets whether game inputs should currently be blocked.
        /// </summary>
        public static bool ShouldBlock => _shouldBlock;

        /// <summary>
        /// Initialize the blocker - ensures we start in unblocked state.
        /// Call this once during plugin initialization.
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            _shouldBlock = false;
            _isHeldBlock = false;
            _isInitialized = true;
            UnityEngine.Debug.Log("[ZUI] InputBlocker initialized — starting UNBLOCKED");
        }

        /// <summary>
        /// Momentarily blocks input for a brief period (default 0.1 seconds).
        /// Call this when the user clicks on ZUI UI elements.
        /// </summary>
        public static void BlockMomentarily(float duration = 0.1f)
        {
            if (!_isInitialized)
            {
                UnityEngine.Debug.LogWarning("[ZUI] BlockMomentarily called before Initialize!");
                Initialize();
            }

            // Don't interrupt a held block with a momentary one
            if (_isHeldBlock)
                return;

            _shouldBlock = true;
            UnityEngine.Debug.Log($"[ZUI] Momentary input block START ({duration}s)");

            // Cancel any existing unblock coroutine
            if (_unblockCoroutine != null)
            {
                Plugin.CoreUpdateBehavior.StopCoroutine(_unblockCoroutine);
            }

            _unblockCoroutine = Plugin.CoreUpdateBehavior.StartCoroutine(UnblockAfterDelay(duration));
        }

        /// <summary>
        /// Sustained block: holds input blocked while the user is interacting 
        /// with a ZUI panel (cursor hovering, clicking, dragging, resizing).
        /// Call BeginInteraction() when pointer enters a panel,
        /// and EndInteraction() when pointer leaves all panels.
        /// 
        /// Uses reference counting so nested panels work correctly.
        /// </summary>
        private static int _interactionRefCount = 0;

        /// <summary>
        /// Call when the pointer enters a ZUI panel or user begins panel interaction.
        /// </summary>
        public static void BeginInteraction()
        {
            if (!_isInitialized) Initialize();

            _interactionRefCount++;
            
            if (!_shouldBlock)
            {
                _shouldBlock = true;
                _isHeldBlock = true;
                
                // Cancel momentary unblock timer if running
                if (_unblockCoroutine != null)
                {
                    Plugin.CoreUpdateBehavior.StopCoroutine(_unblockCoroutine);
                    _unblockCoroutine = null;
                }
                
                UnityEngine.Debug.Log($"[ZUI] Input held-block START (refs: {_interactionRefCount})");
            }
        }

        /// <summary>
        /// Call when the pointer leaves a ZUI panel or user ends panel interaction.
        /// Uses reference counting: only unblocks when ALL interactions end.
        /// </summary>
        public static void EndInteraction()
        {
            if (_interactionRefCount > 0)
                _interactionRefCount--;
            
            if (_interactionRefCount <= 0 && _isHeldBlock)
            {
                _interactionRefCount = 0;
                _shouldBlock = false;
                _isHeldBlock = false;
                UnityEngine.Debug.Log("[ZUI] Input held-block END (all interactions finished)");
            }
        }

        /// <summary>
        /// Forcefully clears all held blocks. Use when UI is closed/hidden.
        /// </summary>
        public static void ForceUnblock()
        {
            _interactionRefCount = 0;
            _isHeldBlock = false;
            
            if (_unblockCoroutine != null)
            {
                Plugin.CoreUpdateBehavior.StopCoroutine(_unblockCoroutine);
                _unblockCoroutine = null;
            }

            if (_shouldBlock)
            {
                _shouldBlock = false;
                UnityEngine.Debug.Log("[ZUI] Input block FORCE cleared");
            }
        }

        /// <summary>
        /// Immediately restores game inputs.
        /// </summary>
        public static void UnblockImmediately()
        {
            if (_unblockCoroutine != null)
            {
                Plugin.CoreUpdateBehavior.StopCoroutine(_unblockCoroutine);
                _unblockCoroutine = null;
            }

            _isHeldBlock = false;
            _interactionRefCount = 0;

            if (_shouldBlock)
            {
                _shouldBlock = false;
                UnityEngine.Debug.Log("[ZUI] Input block CLEARED immediately");
            }
        }

        private static IEnumerator UnblockAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // Only unblock if not held by panel interaction
            if (!_isHeldBlock)
            {
                _shouldBlock = false;
                _unblockCoroutine = null;
                UnityEngine.Debug.Log("[ZUI] Momentary input block END");
            }
        }

        /// <summary>
        /// Legacy method — kept for compatibility but not recommended.
        /// Use BlockMomentarily() for click handling or BeginInteraction()/EndInteraction() for panel interaction.
        /// </summary>
        public static void SetBlocking(bool block)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            if (_shouldBlock != block)
            {
                _shouldBlock = block;
                _isHeldBlock = block;
                if (!block) _interactionRefCount = 0;
                UnityEngine.Debug.Log($"[ZUI] Game input blocking: {(block ? "ENABLED" : "disabled")}");
            }
        }
    }
}
