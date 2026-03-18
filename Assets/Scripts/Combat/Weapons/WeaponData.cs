using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Weapon Data", fileName = "NewWeaponData")]
public class WeaponData : ScriptableObject
{
    public string weaponID = "gun_default";
    public GameObject bulletPrefab;
    public int maxAmmo = 6;
    public float shootForce = 15f;
    public float fireRate = 0.3f;
    public float dropForce = 6f;
    public float drag = 7f;
}