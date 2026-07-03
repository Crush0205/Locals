using UnityEngine;

namespace Mistforge
{
    /// Follows the player, clamped to the map bounds. Cinemachine is listed as
    /// optional in the spec; this covers the vertical slice.
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Rect bounds;

        Camera cam;

        void Awake() { cam = GetComponent<Camera>(); }

        void LateUpdate()
        {
            if (target == null || cam == null) return;

            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector3 pos = transform.position;
            pos.x = ClampAxis(target.position.x, bounds.xMin, bounds.xMax, halfW);
            pos.y = ClampAxis(target.position.y + 0.5f, bounds.yMin, bounds.yMax, halfH);
            transform.position = pos;
        }

        static float ClampAxis(float value, float min, float max, float halfExtent)
        {
            if (max - min <= halfExtent * 2f) return (min + max) * 0.5f;
            return Mathf.Clamp(value, min + halfExtent, max - halfExtent);
        }
    }
}
