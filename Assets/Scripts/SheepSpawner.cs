using UnityEngine;

public class SheepSpawner : MonoBehaviour
{
    public static SheepSpawner Instance { get; private set; }

    [SerializeField] private GameObject sheepPrefab;
    [SerializeField] private int sheepCount = 15;

    [SerializeField] private Collider2D spawnArea;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        for (int i = 0; i < sheepCount; i++)
        {
            Vector2 pos = GetRandomPositionInArea();

            Instantiate(sheepPrefab, pos, Quaternion.identity);
        }
    }

    private Vector2 GetRandomPositionInArea()
    {
        Bounds bounds = spawnArea.bounds;
        Vector2 point;
        int attempts = 0;

        do
        {
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y, bounds.max.y);
            point = new Vector2(x, y);
            attempts++;
        }
        while (!spawnArea.OverlapPoint(point) && attempts < 30);

        return point;
    }

    public int GetSheepCount()
    {
        return sheepCount;
    }
}
