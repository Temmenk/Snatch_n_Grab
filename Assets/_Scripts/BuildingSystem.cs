
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingSystem : MonoBehaviour
{
    public ResourceInventory inventory;
    public GameObject buildingPrefab;
    public Transform player;

    public int woodCost = 20;
    public float buildDistance = 5f;

    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.bKey.wasPressedThisFrame)
        {
            Build();
        }
    }

    void Build()
    {
        if (inventory == null ||
            buildingPrefab == null ||
            player == null)
        {
            Debug.LogWarning("Missing building references!");
            return;
        }

        if (inventory.wood < woodCost)
        {
            Debug.Log("Need " + woodCost + " wood to build!");
            return;
        }

        Vector3 position =
            player.position + player.forward * buildDistance;

        // Find the ground beneath the building position.
        if (!Physics.Raycast(
            position + Vector3.up * 50f,
            Vector3.down,
            out RaycastHit hit,
            100f))
        {
            Debug.Log("No ground found!");
            return;
        }

        position.y = hit.point.y;

        Instantiate(
            buildingPrefab,
            position,
            Quaternion.Euler(0, player.eulerAngles.y, 0)
        );

        inventory.SpendWood(woodCost);

        Debug.Log("Building placed!");
    }
}
