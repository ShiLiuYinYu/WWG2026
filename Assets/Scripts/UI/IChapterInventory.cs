namespace WWG
{
    /// <summary>State and actions supplied by the later chapters to GameplayOverlay.</summary>
    public interface IChapterInventory
    {
        bool IsGameplayModalOpen { get; }
        bool CloseGameplayModal();
        string ClockText { get; }
        void OpenRecord();
        SaveSlotSnapshot CaptureSaveSlot();
        bool HasItem(string itemId);
        void UseInventoryItem(string itemId);
        void InspectInventoryItem(string itemId);
    }
}
