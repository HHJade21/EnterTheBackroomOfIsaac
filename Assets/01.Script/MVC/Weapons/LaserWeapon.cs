using UnityEngine;

// Laser weapon using a continuous gauge (float) instead of magazine
// Responsibilities:
// - Implement IWeapon interface expectations
// - Track gauge max/current, drain while firing, recharge when idle
// - Control laser beam rendering via WeaponView
// - Raise events for UI on gauge changes

public class LaserWeapon : MonoBehaviour /*, IWeapon*/
{
    // [Gauge] Current value, max, drain and recharge rates
    // [Fire] Continuous firing behavior while input held
}


