using UnityEngine;

public class SheepPen : MonoBehaviour
{
    private int sheepCount = 0;

    private void OnTriggerEnter2D(Collider2D collider)
    {
        SheepController sheep = collider.GetComponent<SheepController>();
        if (sheep != null)
        {
            sheepCount++;
            Destroy(collider.gameObject);
            Debug.Log("Sheep entered the pen. Total sheep: " + sheepCount);
        }
    }
}
