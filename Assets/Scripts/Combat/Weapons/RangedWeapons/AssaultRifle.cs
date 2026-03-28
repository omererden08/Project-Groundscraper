using UnityEngine;

public class AssaultRifle : RangedWeapon
{
    public override bool IsAutomatic => true;
    protected override void Fire(Vector2 direction)
    {
        SpawnBullet(direction);
    }
}
