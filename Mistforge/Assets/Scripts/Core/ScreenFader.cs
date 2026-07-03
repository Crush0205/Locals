using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Mistforge
{
    /// Persistent full-screen black CanvasGroup used for all scene fades (§5).
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader I { get; private set; }

        CanvasGroup group;

        public static ScreenFader Ensure()
        {
            if (I != null) return I;
            var go = new GameObject("ScreenFader");
            I = go.AddComponent<ScreenFader>();
            DontDestroyOnLoad(go);
            I.Build();
            return I;
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            var imageGO = new GameObject("Black");
            imageGO.transform.SetParent(transform, false);
            var image = imageGO.AddComponent<Image>();
            image.color = Color.black;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public IEnumerator FadeOut(float duration) { yield return Fade(1f, duration); }
        public IEnumerator FadeIn(float duration) { yield return Fade(0f, duration); }

        IEnumerator Fade(float target, float duration)
        {
            group.blocksRaycasts = target > 0.5f;
            float start = group.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
                yield return null;
            }
            group.alpha = target;
        }
    }
}
