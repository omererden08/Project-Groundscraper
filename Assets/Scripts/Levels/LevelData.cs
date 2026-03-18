using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/LevelData")]
public class LevelData : ScriptableObject
{
    [Header("Identity")]
    public string levelId = "Level 1";
    public int buildIndex; // opsiyonel, level sýrasý için

    [Header("Level Content")]
    public GameObject levelPrefab;

    [Header("Audio (Optional)")]
    public AudioClip music;

}
