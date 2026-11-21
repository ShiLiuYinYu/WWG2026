using System.Collections.Generic;
using UnityEngine;

namespace WWG
{
    internal sealed class InventorySlotContents
    {
        private readonly Behaviour slot;
        private readonly List<GameObject> artwork = new List<GameObject>();

        public InventorySlotContents(Behaviour slot)
        {
            this.slot = slot;
            foreach (Transform child in slot.transform)
                if (child.gameObject.activeSelf) artwork.Add(child.gameObject);
        }

        public void SetOccupied(bool occupied)
        {
            if (slot.enabled != occupied) slot.enabled = occupied;
            foreach (GameObject child in artwork)
                if (child.activeSelf != occupied) child.SetActive(occupied);
        }
    }
}
