using System.Collections;
using UnityEngine;

namespace Mistforge
{
    /// Freeze-frame beat before big hits land (§6.3): brief Time.timeScale pulse.
    public class HitPause : MonoBehaviour
    {
        static HitPause instance;
        static int depth;

        public static void Do(float seconds = 0.08f)
        {
            if (instance == null)
            {
                var go = new GameObject("HitPause");
                instance = go.AddComponent<HitPause>();
            }
            instance.StartCoroutine(instance.Pulse(seconds));
        }

        IEnumerator Pulse(float seconds)
        {
            depth++;
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(seconds);
            depth--;
            if (depth <= 0)
            {
                depth = 0;
                Time.timeScale = 1f;
            }
        }

        void OnDestroy()
        {
            depth = 0;
            Time.timeScale = 1f;
        }
    }
}
