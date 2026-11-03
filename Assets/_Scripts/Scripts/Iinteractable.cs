using Unity.Netcode;

public interface IInteractable
{
    void ToggleSelection(bool IsSelected);
    NetworkObject NetworkObject { get; }
}
