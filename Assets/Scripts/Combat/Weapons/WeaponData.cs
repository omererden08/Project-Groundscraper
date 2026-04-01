using UnityEngine;

public enum WeaponFireAnimationType
{
    None,
    Melee,
    Pistol,
    Rifle,
    Shotgun
}

[CreateAssetMenu(menuName = "Weapons/Weapon Data", fileName = "NewWeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string weaponID = "gun_default";

    [Header("Visual")]
    public AnimatorOverrideController bodyOverrideController;

    [Header("Projectile")]
    public GameObject bulletPrefab;

    [Header("Ammo / Fire")]
    public int maxAmmo = 6;
    public float shootForce = 15f;
    public float fireRate = 0.3f;

    [Header("Drop")]
    public float dropForce = 6f;
    public float drag = 7f;

    [Header("Animation")]
    [SerializeField] private WeaponFireAnimationType fireAnimationType = WeaponFireAnimationType.None;
    public WeaponFireAnimationType FireAnimationType => fireAnimationType;
}