using System.Collections;
using UnityEngine;
using TempleOfBoredTitans.Player;
using TempleOfBoredTitans.UI;

namespace TempleOfBoredTitans.Titans
{
    /// <summary>One distinct attack pattern for each Titan in the campaign.</summary>
    public sealed class TitanSpecialAbility : MonoBehaviour
    {
        public enum AbilityKind { FlameBurst, SoulScream, AcidSpit, Pounce, GroundSlam }

        [SerializeField] private AbilityKind ability;
        [SerializeField] private float cooldown = 4f;
        [SerializeField] private int damage = 22;
        private float nextUseTime;
        private TitanBossHealth boss;
        private TitanAI ai;
        private CharacterController controller;

        public void Configure(AbilityKind kind, int attackDamage, float attackCooldown)
        {
            ability = kind; damage = attackDamage; cooldown = Mathf.Max(0.5f, attackCooldown);
            if (boss == null) boss = GetComponent<TitanBossHealth>();
            if (ai == null) ai = GetComponent<TitanAI>();
            if (controller == null) controller = GetComponent<CharacterController>();
        }

        public float AttackRange
        {
            get
            {
                switch (ability)
                {
                    case AbilityKind.FlameBurst: return 8f;
                    case AbilityKind.SoulScream: return 12f;
                    case AbilityKind.AcidSpit: return 14f;
                    case AbilityKind.Pounce: return 14f;
                    case AbilityKind.GroundSlam: return 16f;
                    default: return 4.6f;
                }
            }
        }

        public float ChaseMultiplier { get { return ability == AbilityKind.Pounce ? 1.2f : 1f; } }

        private void Awake()
        {
            boss = GetComponent<TitanBossHealth>();
            ai = GetComponent<TitanAI>();
            controller = GetComponent<CharacterController>();
        }

        public bool TryAttack(Transform target, PlayerHealth health)
        {
            if (target == null || health == null || Time.time < nextUseTime) return false;
            nextUseTime = Time.time + cooldown;
            string name = boss != null ? boss.DisplayName : gameObject.name;
            switch (ability)
            {
                case AbilityKind.FlameBurst:
                    Warn(name + " spits a blazing fireball!");
                    LaunchAt(target, TitanProjectile.ShotStyle.Fireball, 12f, .6f, 1.1f);
                    break;
                case AbilityKind.SoulScream:
                    Warn(name + " hurls a volley of cursed bones!");
                    StartCoroutine(LaunchVolley(target, TitanProjectile.ShotStyle.Bone, 10f, .5f, 1.2f, 3, .14f));
                    break;
                case AbilityKind.AcidSpit:
                    Warn(name + " spits a glowing plague bubble!");
                    LaunchAt(target, TitanProjectile.ShotStyle.PlagueBubble, 11f, .65f, 1.3f);
                    break;
                case AbilityKind.Pounce:
                    Warn(name + " calls down purple soul lightning!");
                    LaunchAt(target, TitanProjectile.ShotStyle.SoulLightning, 15f, .75f, 1.25f);
                    break;
                case AbilityKind.GroundSlam:
                    Warn(name + " fires a volley of light-blue stone bolts!");
                    StartCoroutine(LaunchVolley(target, TitanProjectile.ShotStyle.IceBolt, 17f, .7f, 1.45f, 3, .1f));
                    break;
            }
            return true;
        }

        private void LaunchAt(Transform target, TitanProjectile.ShotStyle style, float speed, float height, float forwardOffset)
        {
            Vector3 aim = target.position - transform.position;
            aim.y = 0f;
            if (aim.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(aim.normalized);
            Vector3 origin = transform.position + Vector3.up * height + transform.forward * forwardOffset;
            TitanProjectile.Launch(origin, target, transform, damage, style, speed);
        }

        private IEnumerator LaunchVolley(Transform target, TitanProjectile.ShotStyle style, float speed, float height, float forwardOffset, int count, float interval)
        {
            for (int i = 0; i < count && target != null; i++)
            {
                LaunchAt(target, style, speed, height, forwardOffset);
                if (i + 1 < count) yield return new WaitForSeconds(interval);
            }
        }

        private IEnumerator ResolveBurst(Transform target, PlayerHealth health, float windup, float radius, string name)
        {
            yield return new WaitForSeconds(windup);
            if (target != null && health != null && FlatDistance(transform.position, target.position) <= radius)
                health.TakeDamage(damage, name + "'s fire burst", transform);
        }

        private IEnumerator ResolveScream(Transform target, PlayerHealth health, float windup, string name)
        {
            yield return new WaitForSeconds(windup);
            if (target == null || health == null || FlatDistance(transform.position, target.position) > 9.5f) yield break;
            Vector3 origin = transform.position + Vector3.up * 1.2f;
            Vector3 toPlayer = target.position + Vector3.up - origin;
            if (!Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, toPlayer.magnitude,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || hit.transform.IsChildOf(target))
                health.TakeDamage(damage, name + "'s death scream", transform);
        }

        private IEnumerator Pounce(Transform target, PlayerHealth health)
        {
            if (target == null) yield break;
            bool aiWasEnabled = ai != null && ai.enabled;
            if (ai != null) ai.enabled = false;
            float timer = .42f;
            while (timer > 0f && target != null)
            {
                timer -= Time.deltaTime;
                Vector3 direction = target.position - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > .01f)
                {
                    direction.Normalize();
                    transform.rotation = Quaternion.LookRotation(direction);
                    if (controller != null && controller.enabled) controller.SimpleMove(direction * 8f);
                    else transform.position += direction * 8f * Time.deltaTime;
                }
                yield return null;
            }
            if (target != null && health != null && FlatDistance(transform.position, target.position) <= 2.2f)
                health.TakeDamage(damage + 5, "the pouncing " + (boss != null ? boss.DisplayName : "Titan"), transform);
            if (ai != null && aiWasEnabled) ai.enabled = true;
        }

        private IEnumerator ResolveSlam(Transform target, PlayerHealth health, float windup, string name)
        {
            yield return new WaitForSeconds(windup);
            if (target != null && health != null && FlatDistance(transform.position, target.position) <= 4.6f)
                health.TakeDamage(damage + 8, name + "'s ground slam", transform);
        }

        private void Warn(string message)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowNotification(message);
            if (TempleOfBoredTitans.Core.TempleAudioDirector.Instance != null)
                TempleOfBoredTitans.Core.TempleAudioDirector.Instance.PlayTitanAttack(boss != null ? boss.StageIndex : 0);
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}

