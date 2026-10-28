
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class TankSpawner : NetworkBehaviour
{
    [Header("Spawn Setup")]
    public TankDamageSystem enemyTankPrefab;
    public Transform[] spawnPoints;

    [Min(0f)]
    public float nextSpawnDelay = 2f;

    private TankDamageSystem activeTank;
    private int lastSpawnIndex = -1;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            SpawnTank();
    }

    void SpawnTank()
    {
        // Only the server can create enemy tanks.
        if (!IsServer)
            return;

        if (enemyTankPrefab == null ||
            spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            Debug.LogWarning(
                "TankSpawner: Assign an enemy prefab and spawn points."
            );
            return;
        }

        int index = Random.Range(0, spawnPoints.Length);

        // Avoid repeating the same spawn point.
        if (spawnPoints.Length > 1 && index == lastSpawnIndex)
            index = (index + 1) % spawnPoints.Length;

        Transform point = spawnPoints[index];

        if (point == null)
        {
            Debug.LogWarning("TankSpawner: Missing spawn point.");
            return;
        }

        lastSpawnIndex = index;

        activeTank = Instantiate(
            enemyTankPrefab,
            point.position,
            point.rotation
        );

        NetworkObject networkObject =
            activeTank.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError(
                "Enemy tank prefab needs a NetworkObject!"
            );

            Destroy(activeTank.gameObject);
            activeTank = null;
            return;
        }

        networkObject.Spawn();

        activeTank.OnDestroyed += HandleDestroyed;

        Debug.Log("NETWORK ENEMY TANK SPAWNED");
    }

    void HandleDestroyed(TankDamageSystem tank)
    {
        if (!IsServer)
            return;

        tank.OnDestroyed -= HandleDestroyed;

        if (tank == activeTank)
            activeTank = null;

        StartCoroutine(
            SpawnAfterDelay(
                tank.despawnDelay + nextSpawnDelay
            )
        );
    }

    IEnumerator SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (IsServer)
            SpawnTank();
    }
}
