using UnityEngine;
using System.Collections;
using System.Collections.Generic;
// Controls dungeon stage flow and room transitions
// Responsibilities:
// - Generate/track rooms (2 normal, 1 boss) and connections
// - Handle entering rooms, spawning enemies, and locking doors during combat
// - Detect room clear and open exits; transition to boss room
// - Signal GameManager when stage completed or player died
// SOLID:
// - SRP: Focus on dungeon logic; use factories/services for spawning

public class DungeonController : MonoBehaviour
{
    [Header("Enemy Spawning")]
    public GameObject enemyPrefab;

    // [Rooms] Data for current room, neighbors, and visited state
    // [Spawning] Trigger enemy waves and boss spawn
    public void SpawnEnemy(Transform transform)
    {
        Instantiate(enemyPrefab, transform.position, Quaternion.identity);
    }
    // [State] Track combat active/cleared flags
}


