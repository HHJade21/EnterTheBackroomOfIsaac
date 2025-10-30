using UnityEngine;

// Boss-specific controller with multiple attack patterns and phases
// Responsibilities:
// - Manage phase transitions based on hp thresholds
// - Schedule and execute attack patterns (pattern interface if needed)
// - Coordinate telegraphs, summons, and arena mechanics
// - Emit events for UI (boss hp bar)
// SOLID:
// - Strategy for attack patterns (IAttackPattern) if needed

public class BossController : MonoBehaviour
{
    // [Phases] Configure thresholds and current phase
    // [Patterns] Execute current pattern logic and switch patterns
    // [Damage] Handle large hp pool and death sequence
}


