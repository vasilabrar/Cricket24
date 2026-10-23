
using UnityEngine;

public class CricketFieldingCircle : MonoBehaviour
{
    public GameObject markingPrefab;

    [Header("Shape")]
    public float width = 54.864f;
    public float length = 66.864f;
    public float spacing = 0.75f;

    [Header("Markings")]
    public float markingSize = 0.15f;
    public float groundHeight = 0.02f;

    [ContextMenu("Generate Circle")]
    public void GenerateCircle()
    {
        if (markingPrefab == null)
            return;

        ClearCircle();

        float radius = width * 0.5f;
        float halfStraight = (length - width) * 0.5f;

        float perimeter = 2f * (length - width) + 2f * Mathf.PI * radius;
        int count = Mathf.CeilToInt(perimeter / spacing);

        for (int i = 0; i < count; i++)
        {
            float distance = i * perimeter / count;
            Vector3 point;

            if (distance < length - width)
            {
                point = new Vector3(radius, 0f, -halfStraight + distance);
            }
            else if (distance < length - width + Mathf.PI * radius)
            {
                float angle = (distance - (length - width)) / radius;
                angle = Mathf.PI * 0.5f - angle;
                point = new Vector3(radius * Mathf.Cos(angle), 0f, halfStraight + radius * Mathf.Sin(angle));
            }
            else if (distance < 2f * (length - width) + Mathf.PI * radius)
            {
                float d = distance - (length - width + Mathf.PI * radius);
                point = new Vector3(-radius, 0f, halfStraight - d);
            }
            else
            {
                float angle = (distance - (2f * (length - width) + Mathf.PI * radius)) / radius;
                angle = -Mathf.PI * 0.5f - angle;
                point = new Vector3(radius * Mathf.Cos(angle), 0f, -halfStraight + radius * Mathf.Sin(angle));
            }

            point.y = groundHeight;

            GameObject marking = Instantiate(markingPrefab, transform);
            marking.transform.localPosition = point;
            marking.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            SpriteRenderer sr = marking.GetComponent<SpriteRenderer>();

            if (sr != null && sr.sprite != null)
            {
                Vector2 size = sr.sprite.bounds.size;
                marking.transform.localScale = new Vector3(markingSize / size.x, markingSize / size.y, 1f);
            }
        }
    }

    [ContextMenu("Clear Circle")]
    public void ClearCircle()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private void Start()
    {
        if (transform.childCount == 0)
            GenerateCircle();
    }
}
