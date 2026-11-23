using UnityEngine;
using UnityEngine.InputSystem;
using TempleOfBoredTitans.Environment;
using TempleOfBoredTitans.UI;

namespace TempleOfBoredTitans.Interaction
{
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float interactionDistance = 4.5f;
        [SerializeField, Range(0f, 1f)] private float proximityFacingThreshold = 0f;
        [SerializeField] private float focusGracePeriod = 0.25f;
        private Interactable focused;
        private float lastFocusTime;

        private void Awake()
        {
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            if (playerCamera == null || (Keyboard.current == null && Mouse.current == null))
                return;

            Interactable next = FindLookTarget();
            if (next == null) next = FindNearbyTarget();
            if (next != null) lastFocusTime = Time.unscaledTime;
            else if (focused != null && Time.unscaledTime - lastFocusTime <= focusGracePeriod) next = focused;

            if (next != focused)
            {
                focused = next;
                if (UIManager.Instance != null)
                    UIManager.Instance.SetPrompt(focused != null ? focused.Prompt : string.Empty);
            }

            bool keyboardPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool mouseInteractionAllowed = mousePressed && !(focused is DoorController);
            if (focused != null && (keyboardPressed || mouseInteractionAllowed))
                focused.Interact(this);
        }

        private Interactable FindLookTarget()
        {
            RaycastHit[] hits = Physics.RaycastAll(playerCamera.transform.position, playerCamera.transform.forward,
                interactionDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, delegate(RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); });
            for (int i = 0; i < hits.Length; i++)
            {
                Transform hitTransform = hits[i].collider.transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform)) continue;
                return hits[i].collider.GetComponentInParent<Interactable>();
            }
            return null;
        }

        private Interactable FindNearbyTarget()
        {
            Vector3 origin = playerCamera.transform.position;
            Collider[] nearby = Physics.OverlapSphere(origin, interactionDistance, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            Interactable best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < nearby.Length; i++)
            {
                Transform hitTransform = nearby[i].transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform)) continue;
                Interactable candidate = nearby[i].GetComponentInParent<Interactable>();
                if (candidate == null) continue;
                Vector3 point = nearby[i].ClosestPoint(origin);
                Vector3 offset = point - origin;
                float distance = offset.magnitude;
                if (distance > interactionDistance || distance >= bestDistance) continue;
                if (distance > 0.01f && Vector3.Dot(playerCamera.transform.forward, offset / distance) < proximityFacingThreshold) continue;
                if (!HasClearPath(candidate, point, distance)) continue;
                best = candidate;
                bestDistance = distance;
            }
            return best;
        }

        private bool HasClearPath(Interactable candidate, Vector3 point, float distance)
        {
            if (distance <= 0.01f) return true;
            RaycastHit[] hits = Physics.RaycastAll(playerCamera.transform.position, (point - playerCamera.transform.position).normalized,
                distance + 0.05f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, delegate(RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); });
            for (int i = 0; i < hits.Length; i++)
            {
                Transform hitTransform = hits[i].collider.transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform)) continue;
                return hits[i].collider.GetComponentInParent<Interactable>() == candidate;
            }
            return false;
        }
    }
}


