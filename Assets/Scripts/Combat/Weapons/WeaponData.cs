using UnityEngine;

using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Weapon Data", fileName = "NewWeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string weaponID = "gun_default";

    [Header("Visual")]
    public Sprite bodySprite;

    [Header("Projectile")]
    public GameObject bulletPrefab;

    [Header("Ammo / Fire")]
    public int maxAmmo = 6;
    public float shootForce = 15f;
    public float fireRate = 0.3f;

    [Header("Drop")]
    public float dropForce = 6f;
    public float drag = 7f;
}