using UnityEngine;

public static class MeleeAttackHandler
{
    public static void DoAttack(IMeleeAttacker attacker)
    {
        Vector2 origin = attacker.Transform.position;
        Vector2 direction = attacker.AimDirection;
        Vector2 center = origin + direction * attacker.MeleeRange;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, attacker.MeleeRadius);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out IDamageable dmg))
            {
                if (hit.transform != attacker.Transform)
                {
                    dmg.Die();
                }
            }
        }
    }
}
