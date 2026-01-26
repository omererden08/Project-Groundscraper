using System;
using UnityEngine;

public static class PlayerEvents
{
    public static event Action<Vector2, Vector2> OnShoot;
    public static void RaiseShoot(Vector2 pos, Vector2 dir) => OnShoot?.Invoke(pos, dir);

    public static event Action<Vector2, Vector2> OnMeleeAttack;
    public static void RaiseMeleeAttack(Vector2 pos, Vector2 dir) => OnMeleeAttack?.Invoke(pos, dir);

    public static event Action OnPlayerDied;
    public static void RaisePlayerDied() => OnPlayerDied?.Invoke();

    public static event Action<string> OnWeaponPickedUp;
    public static void RaiseWeaponPickedUp(string id) => OnWeaponPickedUp?.Invoke(id);

    public static event Action<string> OnWeaponDropped;
    public static void RaiseWeaponDropped(string id) => OnWeaponDropped?.Invoke(id);
}
