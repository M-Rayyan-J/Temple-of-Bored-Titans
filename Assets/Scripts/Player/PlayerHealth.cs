using System.Collections;
using UnityEngine;
using TempleOfBoredTitans.Core;
using TempleOfBoredTitans.UI;

namespace TempleOfBoredTitans.Player
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumHealth = 180;
        [SerializeField, Min(1)] private int startingLives = 1;
        [SerializeField] private float respawnDelay = 2.6f;
        [SerializeField] private float respawnInvulnerability = 4f;
        [SerializeField] private Transform checkpointTransform;

        public int CurrentHealth { get; private set; }
        public int MaximumHealth { get { return maximumHealth; } }
        public int RemainingLives { get { return remainingLives; } }
        public bool IsInvulnerable { get { return Time.time < invulnerableUntil; } }

        public void SetCheckpoint(Vector3 position, Quaternion rotation)
        {
            checkpoint = position;
            checkpointRotation = rotation;
        }

        private CharacterController controller;
        private PlayerController playerController;
        private Vector3 checkpoint;
        private Quaternion checkpointRotation;
        private bool recovering;
        private int remainingLives;
        private float invulnerableUntil;

        private void Awake()
        {
            // Keep the player's health pool at the requested 180 in every scene.
            maximumHealth = 180;
            controller = GetComponent<CharacterController>();
            playerController = GetComponent<PlayerController>();
            CurrentHealth = maximumHealth;
            remainingLives = startingLives;
        }

        private void Start()
        {
            checkpoint = checkpointTransform != null ? checkpointTransform.position : transform.position;
            checkpointRotation = checkpointTransform != null ? checkpointTransform.rotation : transform.rotation;
            if (UIManager.Instance != null) { UIManager.Instance.SetPlayerHealth(CurrentHealth, maximumHealth); UIManager.Instance.SetPlayerLives(remainingLives); }
        }

        private void Update()
        {
            if (!recovering && transform.position.y < -2f)
                TakeDamage(maximumHealth, "the fall");
        }

        public void GetCaught()
        {
            TakeDamage(10, "a Titan");
        }

        public void TakeDamage(int amount, string source, Transform attacker = null)
        {
            if (recovering || IsInvulnerable || amount <= 0) return;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            if (playerController != null) playerController.PlayHitReaction();
            if (attacker != null)
            {
                var titan = attacker.GetComponentInParent<TempleOfBoredTitans.Titans.TitanAI>();
                if (titan != null)
                {
                    titan.PlayAttackImpact();
                    var boss = attacker.GetComponentInParent<TempleOfBoredTitans.Titans.TitanBossHealth>();
                    if (TempleAudioDirector.Instance != null)
                        TempleAudioDirector.Instance.PlayTitanPlayerHit(boss != null ? boss.StageIndex : 0);
                }
            }
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetPlayerHealth(CurrentHealth, maximumHealth);
                UIManager.Instance.ShowDamage(source);
            }
            if (CurrentHealth <= 0)
                StartCoroutine(DefeatedBy(source));
        }

        private IEnumerator DefeatedBy(string source)
        {
            recovering = true;
            remainingLives = Mathf.Max(0, remainingLives - 1);
            if (playerController != null) playerController.PlayDeathFall();
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameManager.GameState.GameOver);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetPlayerLives(remainingLives);
                UIManager.Instance.ShowDefeat("YOU WERE SLAIN", source + " overwhelmed you. The hunt is over.");
            }
            if (remainingLives <= 0) yield break;
            yield return new WaitForSecondsRealtime(respawnDelay);
            if (controller != null) controller.enabled = false;
            transform.position = checkpoint;
            transform.rotation = checkpointRotation;
            if (controller != null) controller.enabled = true;
            if (playerController != null) playerController.ResetMotion();
            CurrentHealth = maximumHealth;
            invulnerableUntil = Time.time + respawnInvulnerability;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetPlayerHealth(CurrentHealth, maximumHealth);
                UIManager.Instance.SetPlayerLives(remainingLives);
                UIManager.Instance.HideEnding();
                UIManager.Instance.ShowNotification("You are safe for a few seconds. Return to the temple when ready.");
            }
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameManager.GameState.Playing);
            recovering = false;
        }
    }
}
