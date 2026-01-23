using UnityEngine;

public static class MeleeAttackHandler
{
    public static void DoAttack(IMeleeAttacker attacker)
    {
        Vector2 origin = attacker.Transform.position;
        Vector2 direction = attacker.AimDirection;
        float range = attacker.MeleeRange;
        float radius = attacker.MeleeRadius;

        Vector2 center = origin + direction * range;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, LayerMask.GetMask("Enemy"));

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out IDamageable damageable))
            {
                damageable.Die();
            }
        }
    }
}
