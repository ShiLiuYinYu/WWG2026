using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WWG
{
    /// <summary>Locks only the scene buttons while their chapter has a modal open.</summary>
    internal sealed class SceneHotspotInput : MonoBehaviour
    {
        private readonly List<CanvasGroup> groups = new List<CanvasGroup>();
        private Func<bool> isBlocked;
        private bool blocked;

        internal static void Install(MonoBehaviour chapter, Func<bool> isBlocked, params Button[] buttons)
        {
            var input = chapter.gameObject.AddComponent<SceneHotspotInput>();
            input.isBlocked = isBlocked;
            foreach (Button button in buttons)
                if (button != null) input.groups.Add(GetOrAddGroup(button.gameObject));
            input.LateUpdate();
        }

        internal static CanvasGroup GetOrAddGroup(GameObject target) =>
            target.TryGetComponent(out CanvasGroup group) ? group : target.AddComponent<CanvasGroup>();

        private void LateUpdate()
        {
            bool next = isBlocked() || GameplayOverlay.IsOverlayModalOpen || GameplayOverlay.IsInventoryOpen;
            if (next == blocked) return;
            blocked = next;
            foreach (CanvasGroup group in groups)
            {
                if (group == null) continue;
                group.interactable = !blocked;
                group.blocksRaycasts = !blocked;
            }
        }
    }
}
