using UnityEngine;

// Base controller for enemies (melee and ranged)
// Responsibilities:
// - Hold EnemyData, manage AI state (idle, chase, attack, dead)
// - Movement toward/around player
// - Attack timing (cooldowns), call into specific attack implementation
// - Apply/receive damage via IDamageable
// Extension:
// - Derived classes implement DoAttack() (melee or ranged)
// SOLID:
// - OCP: Extend for new enemy types without modifying base

public class EnemyController : MonoBehaviour
{
    // [AI] State management and transitions
    // [Navigation] Move towards player or patrol
    // [Combat] Cooldown and attack triggers
    // [Damage] Handle hp and death
}


