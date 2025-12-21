using UnityEngine;

[System.Serializable]
public struct DamagePayload
{
    [Header("Base Values")]
    public float PhysicalDamage;
    public float EnergyDamage;

    [Header("Penetration (0.0 to 1.0)")]
    // Replaces the simple bools. 1.0 = Ignore 100% of defense.
    public float ShieldPenetration; 
    public float ArmorPenetration;

    [Header("Bonuses (Multipliers)")]
    // 2.0 = Double damage to this layer. Default should be 1.0.
    public float DamageToShieldsMultiplier; 
    public float DamageToArmorMultiplier;

    [Header("Physics & Feedback")]
    public float ImpactForce; // For Rigidbody.AddForce()
    public bool IsCritical;   // For UI floating text
    public GameObject Source; // The ship that fired (for kill credit/vampirism)
    public Vector3 HitPoint;  // Where exactly did we hit? (For spawning sparks)
}

// This allows the weapon to hit an Asteroid, Ship, or Turret without knowing what it is.
// It just looks for "IDamageable".
public interface IDamageable
{
    void TakeDamage(DamagePayload payload);
}