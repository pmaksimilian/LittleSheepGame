using UnityEngine;
using TMPro;

public class SheepPen : MonoBehaviour
{
    private int sheepCount = 0;

    [SerializeField] private TMP_Text sheepCountText;

    void Start()
    {
        TextChange();
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        SheepController sheep = collider.GetComponent<SheepController>();
        if (sheep != null)
        {
            sheepCount++;
            Destroy(collider.gameObject);
            Debug.Log("Sheep entered the pen. Total sheep: " + sheepCount);
            TextChange();
        }
    }

    private void TextChange()
    {
        sheepCountText.text = sheepCount + " / " + SheepSpawner.Instance.GetSheepCount();
    }
}
