using UnityEngine;

namespace ButecoDosDevs.Systems
{
    /// <summary>
    /// Smooth-damped 2D camera follow, clamped to a map bounds BoxCollider2D.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private BoxCollider2D mapBounds;
        [SerializeField] private float smoothTime = 0.12f;

        private Camera cam;
        private Vector3 velocity;

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
            transform.position = snapped;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
            Vector3 smoothed = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
            transform.position = ClampToBounds(smoothed);
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
