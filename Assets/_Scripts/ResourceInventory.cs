using UnityEngine;

public class ResourceInventory : MonoBehaviour
{
    public int wood = 0;

    public void AddWood(int amount)
    {
        wood += amount;
        Debug.Log("Wood collected! Total: " + wood);
    }

    public bool SpendWood(int amount)
    {
        if (wood < amount)
        {
            Debug.Log("Not enough wood!");
            return false;
        }

        wood -= amount;
        Debug.Log("Wood remaining: " + wood);
        return true;
    }
}
