using System.Collections;
using UnityEngine;

namespace ButecoDosDevs.Systems
{
    /// <summary>
    /// Smooth-damped 2D camera follow, clamped to a map bounds BoxCollider2D. Shake()
    /// adds a small, self-decaying offset on top of the clamped position without ever
    /// feeding back into the smoothing/clamp math, so the camera always lands back on
    /// the exact clamped position once a shake finishes.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private BoxCollider2D mapBounds;
        [SerializeField] private float smoothTime = 0.12f;
        [SerializeField] private float maxShakeIntensity = 0.12f;

        private Camera cam;
        private Vector3 velocity;
        private Vector3 basePosition;
        private Vector3 shakeOffset;
        private Coroutine shakeRoutine;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            if (target == null)
            {
                return;
            }

            Vector3 snapped = ClampToBounds(new Vector3(target.position.x, target.position.y, transform.position.z));
            basePosition = snapped;
            transform.position = snapped;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = new Vector3(target.position.x, target.position.y, basePosition.z);
            Vector3 smoothed = Vector3.SmoothDamp(basePosition, desired, ref velocity, smoothTime);
            basePosition = ClampToBounds(smoothed);
            transform.position = basePosition + shakeOffset;
        }

        /// <summary>
        /// Brief, self-decaying screen shake. Intensity is clamped to maxShakeIntensity
        /// so the offset never visibly pushes the camera out of its clamped bounds.
        /// </summary>
        public void Shake(float intensity, float duration)
        {
            float clamped = Mathf.Min(Mathf.Abs(intensity), maxShakeIntensity);
            if (clamped <= 0f)
            {
                return;
            }
            if (shakeRoutine != null)
            {
                StopCoroutine(shakeRoutine);
            }
            shakeRoutine = StartCoroutine(ShakeRoutine(clamped, Mathf.Max(duration, 0.01f)));
        }

        private IEnumerator ShakeRoutine(float intensity, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float falloff = 1f - Mathf.Clamp01(t / duration);
                shakeOffset = (Vector3)(Random.insideUnitCircle * intensity * falloff);
                yield return null;
            }
            shakeOffset = Vector3.zero;
            shakeRoutine = null;
        }

        private Vector3 ClampToBounds(Vector3 position)
        {
            if (mapBounds == null || cam == null)
            {
                return position;
            }

            Bounds bounds = mapBounds.bounds;

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            float minX = bounds.min.x + halfWidth;
            float maxX = bounds.max.x - halfWidth;
            float minY = bounds.min.y + halfHeight;
            float maxY = bounds.max.y - halfHeight;

            float x = minX > maxX ? bounds.center.x : Mathf.Clamp(position.x, minX, maxX);
            float y = minY > maxY ? bounds.center.y : Mathf.Clamp(position.y, minY, maxY);

            return new Vector3(x, y, position.z);
        }
    }
}
