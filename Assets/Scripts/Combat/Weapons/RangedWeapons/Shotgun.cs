using UnityEngine;

public class Shotgun : RangedWeapon
{
    [Header("Shotgun Settings")]
    [SerializeField] private int pelletCount = 8;
    [SerializeField] private float spreadAngle = 18f;

    [Header("Randomness")]
    [SerializeField] private bool useRandomSpread = true;
    [SerializeField] private bool centerWeightedSpread = true;
    [SerializeField] private int pelletCountVariation = 1;

    public override bool IsAutomatic => false;

    protected override void Fire(Vector2 direction)
    {
        int finalPelletCount = pelletCount;

        if (pelletCountVariation > 0)
        {
            finalPelletCount += Random.Range(-pelletCountVariation, pelletCountVariation + 1);
            finalPelletCount = Mathf.Max(1, finalPelletCount);
        }

        float halfSpread = spreadAngle * 0.5f;

        for (int i = 0; i < finalPelletCount; i++)
        {
            float angleOffset;

            if (useRandomSpread)
            {
                angleOffset = centerWeightedSpread
                    ? GetCenterWeightedRandom(-halfSpread, halfSpread)
                    : Random.Range(-halfSpread, halfSpread);
            }
            else
            {
                float t = finalPelletCount == 1 ? 0.5f : (float)i / (finalPelletCount - 1);
                angleOffset = Mathf.Lerp(-halfSpread, halfSpread, t);
            }

            Vector2 spreadDir = Rotate(direction, angleOffset);
            SpawnBullet(spreadDir);
        }
    }

    private float GetCenterWeightedRandom(float min, float max)
    {
        float a = Random.Range(0f, 1f);
        float b = Random.Range(0f, 1f);
        float t = (a + b) * 0.5f; // merkeze daha sık düşer
        return Mathf.Lerp(min, max, t);
    }

    private Vector2 Rotate(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos
        ).normalized;
    }
}