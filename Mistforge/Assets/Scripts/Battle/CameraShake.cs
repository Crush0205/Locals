using UnityEngine;

namespace Mistforge
{
    /// Manual transform-jitter shake (§6.3 — the non-Cinemachine option).
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake I { get; private set; }

        Vector3 basePosition;
        float amplitude;
        float timeLeft;

        void Awake()
        {
            I = this;
            basePosition = transform.localPosition;
        }

        void OnDestroy() { if (I == this) I = null; }

        public static void Shake(float amp, float duration)
        {
            if (I == null) return;
            I.amplitude = Mathf.Max(I.amplitude, amp);
            I.timeLeft = Mathf.Max(I.timeLeft, duration);
        }

        void LateUpdate()
        {
            if (timeLeft <= 0f) return;
            timeLeft -= Time.unscaledDeltaTime;
            if (timeLeft <= 0f)
            {
                amplitude = 0f;
                transform.localPosition = basePosition;
                return;
            }
            transform.localPosition = basePosition + (Vector3)(Random.insideUnitCircle * amplitude);
        }
    }
}
