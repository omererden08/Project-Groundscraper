using UnityEngine;

public class Pistol : RangedWeapon
{
    protected override void Fire(Vector2 direction)
    {
        SpawnBullet(direction);
    }
}