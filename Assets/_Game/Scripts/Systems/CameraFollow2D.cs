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

        [Header("Fit-whole-map mode (Buteco/BarRival: fixed camera showing the whole room)")]
        [Tooltip("When true, ignores 'target' and instead fixes the camera on mapBounds's center, sizing orthographicSize so the whole room fits (recalculated on aspect/resolution changes).")]
        [SerializeField] private bool fitWholeMap;
        [SerializeField] private float fitMargin = 0.5f;

        private Camera cam;
        private Vector3 velocity;
        private Vector3 basePosition;
        private Vector3 shakeOffset;
        private Coroutine shakeRoutine;
        private int lastScreenWidth;
        private int lastScreenHeight;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            if (fitWholeMap)
            {
                ApplyFitWholeMap();
                return;
            }

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
            if (fitWholeMap)
            {
                // Cheap guard: only recompute the fit when the screen/aspect actually changed.
                if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
                {
                    ApplyFitWholeMap();
                }
                transform.position = basePosition + shakeOffset;
                return;
            }

            if (target == null)
            {
                return;
            }

            Vector3 desired = new Vector3(target.position.x, target.position.y, basePosition.z);
            Vector3 smoothed = Vector3.SmoothDamp(basePosition, desired, ref velocity, smoothTime);
            basePosition = ClampToBounds(smoothed);
            transform.position = basePosition + shakeOffset;
        }

        /// <summary>Centers on mapBounds and sizes orthographicSize so the whole room is visible,
        /// regardless of window aspect ratio. No-op (logs nothing, just skips) if mapBounds/cam
        /// aren't wired — never blocks Play Mode.</summary>
        private void ApplyFitWholeMap()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            if (mapBounds == null || cam == null)
            {
                return;
            }

            Bounds bounds = mapBounds.bounds;
            float halfWidth = bounds.extents.x;
            float halfHeight = bounds.extents.y;

            float sizeFromHeight = halfHeight;
            float sizeFromWidth = cam.aspect > 0.0001f ? halfWidth / cam.aspect : halfHeight;
            cam.orthographicSize = Mathf.Max(sizeFromHeight, sizeFromWidth) + fitMargin;

            basePosition = new Vector3(bounds.center.x, bounds.center.y, transform.position.z);
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
