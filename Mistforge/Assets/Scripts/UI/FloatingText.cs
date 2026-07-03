using TMPro;
using UnityEngine;

namespace Mistforge
{
    /// World-space floating text: damage numbers, AP gains, overworld toasts.
    /// Arcs upward and fades (§6.3).
    public class FloatingText : MonoBehaviour
    {
        TMP_Text text;
        float life;
        float duration = 0.9f;
        Vector3 velocity;

        public static FloatingText Spawn(Vector3 pos, string content, Color color,
            float fontSize = 8f, bool big = false)
        {
            var go = new GameObject("FloatingText");
            go.transform.position = pos;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = content;
            tmp.fontSize = big ? fontSize * 1.5f : fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(10f, 2f);
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sortingOrder = 60;

            var ft = go.AddComponent<FloatingText>();
            ft.text = tmp;
            ft.velocity = new Vector3(Random.Range(-0.3f, 0.3f), big ? 2.2f : 1.6f, 0f);
            if (big) ft.duration = 1.2f;
            return ft;
        }

        void Update()
        {
            life += Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            velocity.y -= 1.2f * Time.deltaTime;
            float t = life / duration;
            if (text != null)
            {
                var c = text.color;
                c.a = 1f - Mathf.Clamp01(t * t);
                text.color = c;
            }
            if (life >= duration) Destroy(gameObject);
        }
    }
}
