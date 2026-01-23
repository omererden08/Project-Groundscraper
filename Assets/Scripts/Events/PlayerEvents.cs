using System;
using UnityEngine;

public static class PlayerEvents
{
    /// <summary>
    /// Player ateþ ettiðinde tetiklenir — Parametre: pozisyon, yön
    /// </summary>
    public static event Action<Vector2, Vector2> OnShoot;
    public static void RaiseShoot(Vector2 position, Vector2 direction)
    {
        OnShoot?.Invoke(position, direction);
    }

    /// <summary>
    /// Player yakýn dövüþ yaptýðýnda tetiklenir — Parametre: pozisyon, yön
    /// </summary>
    public static event Action<Vector2, Vector2> OnMeleeAttack;
    public static void RaiseMeleeAttack(Vector2 position, Vector2 direction)
    {
        OnMeleeAttack?.Invoke(position, direction);
    }

    /// <summary>
    /// Player öldüðünde tetiklenir
    /// </summary>
    public static event Action OnPlayerDied;
    public static void RaisePlayerDied()
    {
        OnPlayerDied?.Invoke();
    }

    /// <summary>
    /// Silah alýndýðýnda tetiklenir (ID opsiyonel)
    /// </summary>
    public static event Action<string> OnWeaponPickedUp;
    public static void RaiseWeaponPickedUp(string weaponId)
    {
        OnWeaponPickedUp?.Invoke(weaponId);
    }

    /// <summary>
    /// Silah yere býrakýldýðýnda/fýrlatýldýðýnda tetiklenir
    /// </summary>
    public static event Action<string> OnWeaponDropped;
    public static void RaiseWeaponDropped(string weaponId)
    {
        OnWeaponDropped?.Invoke(weaponId);
    }
}
