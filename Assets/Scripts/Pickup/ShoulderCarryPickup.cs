using LudumDare.Intro;
using UnityEngine;

namespace LudumDare.Pickup
{
    /// <summary>
    /// <see cref="IPickupable"/> that, after pickup, follows the camera with a shoulder-style offset
    /// (typically lower-left of the view). Colliders are disabled so the carry mesh does not block raycasts.
    /// </summary>
    public sealed class ShoulderCarryPickup : MonoBehaviour, IPickupable, IBodyFinaleCarry
    {
        [SerializeField] Transform cameraTransform;
        [Tooltip("Camera-local offset: more negative X/Y = lower-left; keep Z ~0.78–1.1 to stay in front of the near plane.")]
        [SerializeField] Vector3 offsetLocal = new Vector3(-0.76f, -0.62f, 0.82f);
        [Tooltip("Extra rotation in camera-local axes: stronger negative Y/Z helps tuck the torso off-screen and aim the head toward the top-left of the view.")]
        [SerializeField] Vector3 tiltEuler = new Vector3(32f, -58f, -62f);
        [Tooltip("Uniform scale applied when picked up (relative to scale on the ground).")]
        [SerializeField] float carryScaleMultiplier = 0.5f;
        [Tooltip("After tilt, shifts position in tilted local axes (slide along bag length to hide torso in corner).")]
        [SerializeField] Vector3 carryPivotOffsetLocal = new Vector3(0.26f, 0.08f, -0.05f);
        [SerializeField] bool usePickupLerp = true;
        [SerializeField] float lerpSpeed = 3f;
        [SerializeField] string itemId;
        [SerializeField] bool registerCollected = true;

        bool _carried;
        float _lerpT;
        Vector3 _startPos;
        Quaternion _startRot;
        Vector3 _startScale;
        Transform _cam;
        Vector3 _groundScale;

        CollectedItemsRegistry _registry;

        CollectedItemsRegistry Registry =>
            _registry ??= FindFirstObjectByType<CollectedItemsRegistry>();

        void Awake()
        {
            _groundScale = transform.localScale;
        }

        void LateUpdate()
        {
            if (!_carried || _cam == null)
                return;

            var targetRot = _cam.rotation * Quaternion.Euler(tiltEuler);
            var targetPos = _cam.TransformPoint(offsetLocal) + targetRot * carryPivotOffsetLocal;
            var targetScale = _groundScale * Mathf.Max(0.01f, carryScaleMultiplier);

            if (usePickupLerp && _lerpT < 1f)
            {
                _lerpT = Mathf.MoveTowards(_lerpT, 1f, Time.deltaTime * lerpSpeed);
                var t = 1f - Mathf.Pow(1f - _lerpT, 3f);
                transform.SetPositionAndRotation(
                    Vector3.Lerp(_startPos, targetPos, t),
                    Quaternion.Slerp(_startRot, targetRot, t));
                transform.localScale = Vector3.Lerp(_startScale, targetScale, t);
            }
            else
            {
                transform.SetPositionAndRotation(targetPos, targetRot);
                transform.localScale = targetScale;
            }
        }

        public bool CanPickup(PickupContext context)
        {
            if (_carried)
                return false;

            if (!string.IsNullOrEmpty(itemId) && Registry != null && Registry.IsCollected(itemId))
                return false;

            return true;
        }

        public void OnPickup(PickupContext context)
        {
            _cam = cameraTransform != null
                ? cameraTransform
                : context.Instigator != null
                    ? context.Instigator.GetComponentInChildren<Camera>()?.transform
                    : null;

            if (_cam == null && Camera.main != null)
                _cam = Camera.main.transform;

            if (_cam == null)
                return;

            _carried = true;
            _lerpT = 0f;
            _startPos = transform.position;
            _startRot = transform.rotation;
            _startScale = transform.localScale;

            foreach (var c in GetComponentsInChildren<Collider>(true))
                c.enabled = false;

            if (registerCollected && Registry != null && !string.IsNullOrEmpty(itemId))
                Registry.MarkCollected(itemId);
        }

        public bool IsCarriedForFinale => _carried;

        public Transform BodyTransform => transform;

        public void DetachForFinale()
        {
            _carried = false;
            transform.localScale = _groundScale;
            enabled = false;
        }
    }
}
