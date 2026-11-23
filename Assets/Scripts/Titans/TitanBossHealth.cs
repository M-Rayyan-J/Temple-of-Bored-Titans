using System.Collections;
using UnityEngine;

namespace TempleOfBoredTitans.Titans
{
    /// <summary>Health, damage feedback, and a procedural fall animation for one campaign Titan.</summary>
    public sealed class TitanBossHealth : MonoBehaviour
    {
        [SerializeField] private int stageIndex;
        [SerializeField] private string displayName = "Titan";
        [SerializeField, Min(1)] private int maxHealth = 60;
        private int currentHealth;
        private bool defeated;
        private TitanAI ai;
        private TitanSpecialAbility ability;
        private CharacterController controller;
        private Animator[] animators;

        public int StageIndex { get { return stageIndex; } }
        public string DisplayName { get { return displayName; } }
        public int CurrentHealth { get { return currentHealth; } }
        public int MaxHealth { get { return maxHealth; } }
        public bool IsDefeated { get { return defeated; } }

        private void Awake()
        {
            currentHealth = maxHealth;
            ai = GetComponent<TitanAI>();
            ability = GetComponent<TitanSpecialAbility>();
            controller = GetComponent<CharacterController>();
            animators = GetComponentsInChildren<Animator>(true);
        }

        public void Configure(int stage, string title, int health)
        {
            stageIndex = stage; displayName = title; maxHealth = Mathf.Max(1, health);
            currentHealth = maxHealth; defeated = false;
        }

        public void TakeDamage(int amount)
        {
            TakeDamage(amount, transform.position + Vector3.up, -transform.forward);
        }

        public void TakeDamage(int amount, Vector3 impactPoint, Vector3 impactNormal)
        {
            if (defeated || amount <= 0) return;
            currentHealth = Mathf.Max(0, currentHealth - amount);
            TitanHitEffects.Spawn(impactPoint, impactNormal);
            if (TitanEncounterDirector.Instance != null)
                TitanEncounterDirector.Instance.UpdateBossHealth(this);
            if (TempleOfBoredTitans.Core.TempleAudioDirector.Instance != null)
                TempleOfBoredTitans.Core.TempleAudioDirector.Instance.PlayTitanHit();
            if (currentHealth <= 0)
            {
                defeated = true;
                StartCoroutine(DeathFall());
            }
        }

        private IEnumerator DeathFall()
        {
            if (ai != null) ai.enabled = false;
            if (ability != null) ability.StopAllCoroutines();
            if (controller != null) controller.enabled = false;
            for (int i = 0; i < animators.Length; i++) if (animators[i] != null) animators[i].enabled = false;
            Quaternion startRotation = transform.rotation;
            Vector3 startPosition = transform.position;
            float timer = 0f;
            while (timer < 1.1f)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / 1.1f);
                transform.rotation = startRotation * Quaternion.Euler(78f * t, 0f, 0f);
                transform.position = startPosition + Vector3.down * (.38f * t);
                yield return null;
            }
            if (TitanEncounterDirector.Instance != null)
                TitanEncounterDirector.Instance.BossDefeated(this);
            gameObject.SetActive(false);
        }
    }
}

