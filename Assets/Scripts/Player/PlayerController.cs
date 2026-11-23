using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TempleOfBoredTitans.Core;

namespace TempleOfBoredTitans.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4.2f;
        [SerializeField] private float sprintSpeed = 6.2f;
        [SerializeField] private float jumpHeight = 1.0f;
        [SerializeField] private float gravity = -18f;
        [Header("Look")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float mouseSensitivity = 0.08f;
        [SerializeField] private float pitchLimit = 82f;

        private CharacterController controller;
        private float verticalVelocity;
        private float pitch;
        private float footstepTimer;
        private float hitKick;
        private float hitRoll;
        private Vector3 cameraHomePosition;
        private Quaternion cameraHomeRotation;
        private Coroutine deathFallRoutine;
        private TempleOfBoredTitans.Player.PlayerAvatarLocomotion avatarLocomotion;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            avatarLocomotion = GetComponentInChildren<TempleOfBoredTitans.Player.PlayerAvatarLocomotion>(true);
            if (GetComponent<PlayerWeapon>() == null) gameObject.AddComponent<PlayerWeapon>();
            TempleOfBoredTitans.Core.TempleHorrorAtmosphere.Apply();
            TempleOfBoredTitans.Titans.TitanEncounterDirector.EnsureRuntimeEncounter(transform);
            if (cameraPivot == null && Camera.main != null)
                cameraPivot = Camera.main.transform;
            if (cameraPivot != null)
            {
                cameraHomePosition = cameraPivot.localPosition;
                cameraHomeRotation = cameraPivot.localRotation;
            }
        }

        private void OnEnable() => SetCursorLocked(true);

        private void Update()
        {
            hitKick = Mathf.MoveTowards(hitKick, 0f, Time.deltaTime * 55f);
            hitRoll = Mathf.MoveTowards(hitRoll, 0f, Time.deltaTime * 30f);
            if (Keyboard.current == null || Mouse.current == null)
                return;

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
                return;
            if (Cursor.lockState != CursorLockMode.Locked)
                return;

            Look();
            Move();
        }

        private void Look()
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(Vector3.up * delta.x);
            pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
            if (cameraPivot != null)
                cameraPivot.localRotation = Quaternion.Euler(pitch - hitKick, 0f, hitRoll) * cameraHomeRotation;
        }

        public void PlayHitReaction()
        {
            hitKick = Mathf.Max(hitKick, 13f);
            hitRoll = Random.Range(-1, 1) < 0 ? -6f : 6f;
        }

        public void PlayDeathFall()
        {
            if (deathFallRoutine != null) StopCoroutine(deathFallRoutine);
            deathFallRoutine = StartCoroutine(DeathFallCamera());
        }

        private IEnumerator DeathFallCamera()
        {
            if (cameraPivot == null) yield break;
            Vector3 startPosition = cameraPivot.localPosition;
            Quaternion startRotation = cameraPivot.localRotation;
            Vector3 endPosition = cameraHomePosition + Vector3.down * .42f + Vector3.forward * .08f;
            Quaternion endRotation = Quaternion.Euler(66f, 0f, 24f) * cameraHomeRotation;
            float timer = 0f;
            while (timer < .85f)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timer / .85f));
                cameraPivot.localPosition = Vector3.Lerp(startPosition, endPosition, t);
                cameraPivot.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
                yield return null;
            }
            deathFallRoutine = null;
        }

        public IEnumerator PlayEscapeRun(float distance, float duration)
        {
            if (cameraPivot == null) yield break;
            bool controllerWasEnabled = controller != null && controller.enabled;
            if (controllerWasEnabled) controller.enabled = false;
            Vector3 startPosition = transform.position;
            Vector3 direction = transform.forward;
            float timer = 0f;
            duration = Mathf.Max(.1f, duration);
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / duration);
                transform.position = startPosition + direction * (distance * t);
                float bob = Mathf.Sin(timer * 18f) * .045f;
                cameraPivot.localPosition = cameraHomePosition + Vector3.up * bob;
                cameraPivot.localRotation = Quaternion.Euler(-2f - Mathf.Abs(bob) * 20f, 0f, Mathf.Sin(timer * 9f) * 1.2f) * cameraHomeRotation;
                yield return null;
            }
            if (cameraPivot != null)
            {
                cameraPivot.localPosition = cameraHomePosition;
                cameraPivot.localRotation = cameraHomeRotation;
            }
            if (controllerWasEnabled && controller != null) controller.enabled = true;
        }

        private void Move()
        {
            Vector2 input = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            float speed = Keyboard.current.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;
            Vector3 planar = (transform.right * input.x + transform.forward * input.y) * speed;
            if (avatarLocomotion != null)
                avatarLocomotion.SetMovement(input.sqrMagnitude > .01f, Keyboard.current.leftShiftKey.isPressed);

            if (controller.isGrounded && input.sqrMagnitude > 0.1f)
            {
                footstepTimer -= Time.deltaTime;
                if (footstepTimer <= 0f)
                {
                    if (TempleAudioDirector.Instance != null) TempleAudioDirector.Instance.PlayFootstep();
                    footstepTimer = Keyboard.current.leftShiftKey.isPressed ? 0.34f : 0.52f;
                }
            }
            else footstepTimer = 0.1f;

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            if (controller.isGrounded && Keyboard.current.spaceKey.wasPressedThisFrame)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalVelocity += gravity * Time.deltaTime;

            controller.Move((planar + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        public void ResetMotion()
        {
            if (deathFallRoutine != null) StopCoroutine(deathFallRoutine);
            deathFallRoutine = null;
            verticalVelocity = 0f;
            pitch = 0f;
            hitKick = 0f;
            hitRoll = 0f;
            if (cameraPivot != null)
            {
                cameraPivot.localPosition = cameraHomePosition;
                cameraPivot.localRotation = cameraHomeRotation;
            }
        }

        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}


