using UnityEngine;

public static class MeleeAttackHandler
{
    public static void DoAttack(IMeleeAttacker attacker)
    {
        Vector2 origin = attacker.Transform.position;
        Vector2 direction = attacker.AimDirection;

        Vector2 center = origin + direction * attacker.MeleeRange;

        int mask = LayerMask.GetMask("Enemy"); // Layer var mý test et
        Debug.DrawLine(origin, center, Color.red, 1f); // Görsel test

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, attacker.MeleeRadius, mask);


        foreach (var hit in hits)
        {
            if (!hit.enabled) continue;

            if (hit.TryGetComponent(out IDamageable damageable))
            {
                damageable.Die();
            }
        }

    }
}
