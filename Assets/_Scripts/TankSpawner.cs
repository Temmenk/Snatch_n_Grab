
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TankSpawner : NetworkBehaviour
{
    [Header("Spawn Setup")]
    public TankDamageSystem enemyTankPrefab;
    public Transform[] spawnPoints;

    [Header("Enemy Settings")]
    [Min(1)]
    public int enemyTankCount = 3;

    [Min(0f)]
    public float nextSpawnDelay = 2f;

    private List<TankDamageSystem> activeTanks =
        new List<TankDamageSystem>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        for (int i = 0; i < enemyTankCount; i++)
        {
            SpawnTank();
        }
    }

    private void SpawnTank()
    {
        if (!IsServer)
            return;

        if (enemyTankPrefab == null ||
            spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            Debug.LogWarning(
                "TankSpawner: Assign enemy prefab and spawn points.");
            return;
        }

        // Choose a random spawn point.
        Transform point =
            spawnPoints[Random.Range(0, spawnPoints.Length)];

        if (point == null)
            return;

        TankDamageSystem tank = Instantiate(
            enemyTankPrefab,
            point.position,
            point.rotation
        );

        NetworkObject networkObject =
            tank.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError(
                "Enemy tank prefab needs a NetworkObject!");

            Destroy(tank.gameObject);
            return;
        }

        networkObject.Spawn();

        activeTanks.Add(tank);
        tank.OnDestroyed += HandleDestroyed;

        Debug.Log(
            $"Enemy spawned. Active tanks: {activeTanks.Count}");
    }

    private void HandleDestroyed(TankDamageSystem tank)
    {
        if (!IsServer)
            return;

        tank.OnDestroyed -= HandleDestroyed;
        activeTanks.Remove(tank);

        StartCoroutine(SpawnAfterDelay(
            tank.despawnDelay + nextSpawnDelay));
    }

    private IEnumerator SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (IsServer && IsSpawned)
            SpawnTank();
    }
}
