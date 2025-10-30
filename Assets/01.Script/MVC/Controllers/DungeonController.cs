using UnityEngine;

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
    // [Rooms] Data for current room, neighbors, and visited state
    // [Spawning] Trigger enemy waves and boss spawn
    // [State] Track combat active/cleared flags
}


