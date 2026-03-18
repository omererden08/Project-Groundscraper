using UnityEngine;

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "Scriptable Objects/LevelDatabase")]
public class LevelDatabase : ScriptableObject
{
    [SerializeField] private LevelData[] levels;

    public LevelData[] Levels => levels;

    public LevelData GetById(string id)
    {
        if (levels == null) return null;

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] != null && levels[i].levelId == id)
                return levels[i];
        }

        Debug.LogWarning($"LevelDatabase: LevelId bulunamadý -> {id}");
        return null;
    }

    public LevelData GetByIndex(int index)
    {
        if (levels == null || index < 0 || index >= levels.Length)
        {
            Debug.LogWarning($"LevelDatabase: Index geçersiz -> {index}");
            return null;
        }

        return levels[index];
    }

    public int GetLevelIndex(string id)
    {
        if (levels == null) return -1;

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] != null && levels[i].levelId == id)
                return i;
        }

        return -1;
    }
}
