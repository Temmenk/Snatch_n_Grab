using System.Collections;
using UnityEngine;

public class TankSpawner : MonoBehaviour
{
    [Header("Spawn Setup")]
    public TankDamageSystem enemyTankPrefab;
    public Transform[] spawnPoints;
    [Min(0f)] public float nextSpawnDelay = 2f;

    private TankDamageSystem activeTank;
    private int lastSpawnIndex = -1;

    void Start()
    {
        SpawnTank();
    }

    void SpawnTank()
    {
        if (enemyTankPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("TankSpawner: Assign an enemy prefab and at least one spawn point.");
            return;
        }

        int index = Random.Range(0, spawnPoints.Length);
        if (spawnPoints.Length > 1 && index == lastSpawnIndex)
            index = (index + 1) % spawnPoints.Length;

        Transform point = spawnPoints[index];
        if (point == null)
        {
            Debug.LogWarning("TankSpawner: A spawn point is missing.");
            return;
        }

        lastSpawnIndex = index;
        activeTank = Instantiate(enemyTankPrefab, point.position, point.rotation);
        activeTank.OnDestroyed += HandleDestroyed;
    }

    void HandleDestroyed(TankDamageSystem tank)
    {
        tank.OnDestroyed -= HandleDestroyed;
        if (tank == activeTank) activeTank = null;
        StartCoroutine(SpawnAfterDelay(tank.despawnDelay + nextSpawnDelay));
    }

    IEnumerator SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnTank();
    }
}
