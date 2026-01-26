using UnityEngine;
public interface IWeapon
{
    string WeaponID { get; }
    void Use();
    void OnEquip(Transform weaponHolder);
    void OnDrop(Vector2 dropDirection);
    bool IsRanged { get; } // 🔹 Eklenen özellik
}
