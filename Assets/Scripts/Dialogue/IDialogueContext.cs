using System.Collections;

namespace WWG
{
    /// <summary>Operational surface a chapter exposes to the shared dialogue layer.</summary>
    public interface IDialogueContext
    {
        bool HasFlag(string id);
        bool HasItem(string id);
        bool HasSeenDialogue(string nodeId);
        void MarkDialogueSeen(string nodeId);

        int San { get; }
        int Health { get; }
        string Route { get; }
        string View { get; }

        void SetFlag(string id);
        void ClearFlag(string id);
        void AddItem(string id);
        void ApplyShock(string id, int sanDelta, int healthDelta = 0);
        void ChooseRoute(string choice);
        void ChooseView(string choice);

        IEnumerator ShowDocument(string documentId);
        IEnumerator ShowPhoto();

        void RequestCheckpoint();
        void RequestGoTo(string locationId, string variant);
        void RefreshHud();
    }
}
