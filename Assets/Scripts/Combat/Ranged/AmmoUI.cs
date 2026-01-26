using TMPro;
using UnityEngine;

public class AmmoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ammoText;

    private RangedWeapon equippedWeapon;

    private void Awake()
    {
        ammoText = GetComponent<TextMeshProUGUI>();
    }

    public void SetWeapon(RangedWeapon weapon)
    {
        equippedWeapon = weapon;
        UpdateAmmo(); // İlk güncelleme
    }

    private void Update()
    {
        if (equippedWeapon == null)
        {
            if (!string.IsNullOrEmpty(ammoText.text))
                ammoText.text = "";
            return;
        }

        UpdateAmmo();
    }


    private void UpdateAmmo()
    {
        ammoText.text = $"Ammo: {equippedWeapon.CurrentAmmo} / {equippedWeapon.MaxAmmo}";
    }

    public void Clear()
    {
        equippedWeapon = null;
        ammoText.text = "";
    }
}
