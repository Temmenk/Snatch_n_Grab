using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using StarterAssets;

// Attach to the root Player prefab alongside NetworkObject.
public class NetworkPlayerSetup : NetworkBehaviour
{
    [Header("Assign from PlayerCapsule")]
    [SerializeField] private FirstPersonController firstPersonController;
    [SerializeField] private StarterAssetsInputs starterAssetsInputs;
    [SerializeField] private PlayerInput playerInput;

    [Header("Local-only objects (cameras, UI event system)")]
    [SerializeField] private GameObject[] localOnlyObjects;

    private void Awake()
    {
        // Keep local input and cameras off until network ownership is established.
        SetLocalControl(false);
    }

    public override void OnNetworkSpawn()
    {
        SetLocalControl(IsOwner);
    }

    public override void OnNetworkDespawn()
    {
        SetLocalControl(false);
    }

    private void SetLocalControl(bool enabled)
    {
        if (firstPersonController != null)
            firstPersonController.enabled = enabled;

        if (starterAssetsInputs != null)
            starterAssetsInputs.enabled = enabled;

        if (playerInput != null)
            playerInput.enabled = enabled;

        if (localOnlyObjects != null)
        {
            foreach (GameObject obj in localOnlyObjects)
            {
                if (obj != null)
                    obj.SetActive(enabled);
            }
        }

        if (enabled)
            Debug.Log($"Local player ready: client {OwnerClientId}");
    }
}
