using UnityEngine;
public interface IMeleeAttacker
{
    Transform Transform { get; }
    Vector2 AimDirection { get; }
    float MeleeRange { get; }
    float MeleeRadius { get; }
}
