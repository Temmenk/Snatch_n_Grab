
using UnityEngine;

public class TreeChopper : MonoBehaviour
{
    public Camera playerCamera;
    public Terrain terrain;
    public float chopDistance = 5f;
    public int hitsToChop = 3;

    private int currentHits = 0;
    private int selectedTree = -1;

    public ResourceInventory inventory;
    public int woodPerTree = 5;

    void Update()
    {
        if (KeyboardChop())
        {
            ChopTree();
        }
    }

    bool KeyboardChop()
    {
        return UnityEngine.InputSystem.Keyboard.current != null &&
               UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame;
    }

    void ChopTree()
    {
        if (playerCamera == null || terrain == null)
            return;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(ray, out RaycastHit hit, chopDistance))
        {
            TreeInstance[] trees = terrain.terrainData.treeInstances;

            int closestTree = -1;
            float closestDistance = 2f;

            for (int i = 0; i < trees.Length; i++)
            {
                Vector3 treePosition =
                    Vector3.Scale(
                        trees[i].position,
                        terrain.terrainData.size
                    ) + terrain.transform.position;

                float distance =
                    Vector3.Distance(hit.point, treePosition);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTree = i;
                }
            }

            if (closestTree == -1)
                return;

            if (selectedTree != closestTree)
            {
                selectedTree = closestTree;
                currentHits = 0;
            }

            currentHits++;

            Debug.Log("Tree hit: " + currentHits);

            if (currentHits >= hitsToChop)
            {
                RemoveTree(closestTree);
                currentHits = 0;
                selectedTree = -1;
            }
        }
    }

    
void RemoveTree(int index)
{
    TreeInstance[] trees = terrain.terrainData.treeInstances;

    var treeList =
        new System.Collections.Generic.List<TreeInstance>(trees);

    treeList.RemoveAt(index);

    terrain.terrainData.treeInstances = treeList.ToArray();

    if (inventory != null)
    {
        inventory.AddWood(woodPerTree);
    }

    Debug.Log("Tree chopped down! +5 Wood");
}

}
