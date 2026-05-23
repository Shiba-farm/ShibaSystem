/// <summary>
/// Interface for all farming-related commands (Command Pattern).
/// Allows for execution and potential undo functionality.
/// </summary>
public interface IFarmCommand
{
    bool Execute();
    void Undo();
}
