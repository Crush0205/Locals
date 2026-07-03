using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mistforge
{
    /// Boot scene entry point: stands up the persistent singletons, then
    /// hands off to town.
    public class BootLoader : MonoBehaviour
    {
        void Start()
        {
            GameManager.Ensure();
            SceneManager.LoadScene(SceneFlow.TownScene);
        }
    }
}
