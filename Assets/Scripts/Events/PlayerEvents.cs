using System;
using UnityEngine;

public static class PlayerEvents
{
    // Combat
    public static event Action<Vector2, Vector2> OnShoot;
    public static event Action<Vector2, Vector2> OnMeleeAttack;
    public static event Action OnCrosshairShoot;

    // Player state
    public static event Action OnPlayerDied;
    public static event Action<bool> OnPlayerMoveStateChanged;

    // Weapon / animation
    public static event Action<WeaponData> OnWeaponEquipped;
    public static event Action OnWeaponUnequipped;

    // Optional utility events
    public static event Action<string> OnWeaponPickedUp;
    public static event Action<string> OnWeaponDropped;

    public static void RaiseShoot(Vector2 pos, Vector2 dir)
    {
        OnShoot?.Invoke(pos, dir);
    }

    public static void RaiseMeleeAttack(Vector2 pos, Vector2 dir)
    {
        OnMeleeAttack?.Invoke(pos, dir);
    }

    public static void RaiseCrosshairShoot()
    {
        OnCrosshairShoot?.Invoke();
    }

    public static void RaisePlayerDied()
    {
        OnPlayerDied?.Invoke();
    }

    public static void RaisePlayerMoveStateChanged(bool isMoving)
    {
        OnPlayerMoveStateChanged?.Invoke(isMoving);
    }

    public static void RaiseWeaponEquipped(WeaponData weaponData)
    {
        OnWeaponEquipped?.Invoke(weaponData);
    }

    public static void RaiseWeaponUnequipped()
    {
        OnWeaponUnequipped?.Invoke();
    }

    public static void RaiseWeaponPickedUp(string weaponID)
    {
        OnWeaponPickedUp?.Invoke(weaponID);
    }

    public static void RaiseWeaponDropped(string weaponID)
    {
        OnWeaponDropped?.Invoke(weaponID);
    }

}