using UnityEngine;

namespace Mistforge
{
    /// Cheap stand-in for the shared mist/fog overlay (§3.4): a few large,
    /// near-transparent sprites drifting across the map and wrapping.
    public class MistDrift : MonoBehaviour
    {
        public Rect area;
        public float speed = 0.4f;

        public static void Scatter(Transform parent, Rect area, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Mist");
                go.transform.SetParent(parent, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = PlaceholderArt.Centered(96, 48, new Color(0.85f, 0.9f, 0.95f, 1f), false);
                sr.color = new Color(1f, 1f, 1f, 0.07f);
                sr.sortingOrder = 30;
                go.transform.position = new Vector3(
                    Random.Range(area.xMin, area.xMax),
                    Random.Range(area.yMin, area.yMax), 0f);
                var drift = go.AddComponent<MistDrift>();
                drift.area = area;
                drift.speed = Random.Range(0.25f, 0.55f);
            }
        }

        void Update()
        {
            transform.position += Vector3.right * (speed * Time.deltaTime);
            if (transform.position.x > area.xMax + 4f)
                transform.position = new Vector3(area.xMin - 4f,
                    Random.Range(area.yMin, area.yMax), 0f);
        }
    }
}
