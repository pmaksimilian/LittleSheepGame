using UnityEngine;

public class SheepSpawner : MonoBehaviour
{
    [SerializeField] private GameObject sheepPrefab;
    [SerializeField] private int sheepCount = 15;

    [SerializeField] private Vector2 spawnAreaMin = new Vector2(-12f, -8f);
    [SerializeField] private Vector2 spawnAreaMax = new Vector2(14f, 4f);

    void Start()
    {
        for (int i = 0; i < sheepCount; i++)
        {
            Vector2 pos = new Vector2(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                Random.Range(spawnAreaMin.y, spawnAreaMax.y)
            );

            Instantiate(sheepPrefab, pos, Quaternion.identity);
        }
    }
}
