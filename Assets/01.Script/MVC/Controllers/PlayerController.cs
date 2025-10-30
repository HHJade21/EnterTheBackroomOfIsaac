using UnityEngine;

// Orchestrates player behavior (Controller in MVC)
// Responsibilities:
// - Read input (WASD, LMB fire, RMB reload, Space roll, E interact, Q skill)
// - Move player using physics/CharacterController
// - Manage rolling with temporary invincibility frames
// - Coordinate with IWeapon for firing/reloading and attack speed
// - Interact with IInteractable objects
// - Apply damage via IDamageable and PlayerStats (hp/defense)
// - Emit events for UI (hp change, ammo change)
// SOLID:
// - SRP: Only orchestrates; no rendering, no direct input system details
// - DIP: Depend on abstractions (IWeapon, IInteractable, IDamageable)

public class PlayerController : MonoBehaviour
{
    // [References] Link to PlayerView, PlayerStats, current IWeapon
    // [Movement] Handle WASD movement, movement speed from stats
    // [Combat] Handle fire, reload, skill cooldowns, projectile size modifier
    // [Roll] Implement roll state, duration, cooldown, i-frames
    // [Interaction] Detect interactables and invoke their Interact()
    // [Damage] Calculate final damage taken using defense stat
}


