using UnityEngine;
using UnityEngine.UI;

namespace WWG
{
    /// <summary>The common corner controls used by every gameplay scene.</summary>
    public sealed class SharedInteractionHud : MonoBehaviour
    {
        public GameObject topBar;
        public Text timeText;
        public VitalsBars vitals;
        public Button pauseButton;
        public Button recordButton;
        public Button inventoryButton;
    }
}
