public interface ISaveable
{
    // Server calls this to collect data before writing JSON
    void CaptureState(GameSaveData save, ulong clientId = 0);

    // Server calls this to push data back after loading JSON
    void RestoreState(GameSaveData save, ulong clientId = 0);
}