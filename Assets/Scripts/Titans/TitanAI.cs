using UnityEngine;
using System.Collections.Generic;
using TempleOfBoredTitans.Core;
using TempleOfBoredTitans.UI;
using TempleOfBoredTitans.Player;

namespace TempleOfBoredTitans.Titans
{
    public sealed class TitanAI : MonoBehaviour
    {
        public enum State { Idle, Patrol, Chase, Attack, Search, Return }

        [Header("Sensing")]
        [SerializeField] private Transform player;
        [SerializeField] private float detectionDistance = 8f;
        [SerializeField, Range(10f, 180f)] private float fieldOfView = 100f;
        [SerializeField] private float attackDistance = 1.2f;
        [SerializeField] private float searchDuration = 3f;
        [Header("Movement")]
        [SerializeField] private float patrolSpeed = 1f;
        [SerializeField] private float chaseSpeed = 1.9f;
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float wanderRadius = 4f;
        [Header("Attack")]
        [SerializeField] private float attackCooldown = 5f;
        [SerializeField] private Animator animator;
        [SerializeField] private string idleAnimation = "Idle";
        [SerializeField] private string moveAnimation = "Run";
        [SerializeField] private string attackAnimation = "Attack";
        [SerializeField, TextArea(1, 2)] private string noticedMessage = "A Titan has noticed you.";

        public State CurrentState { get; private set; }
        private CharacterController controller;
        private PlayerHealth playerHealth;
        private TitanSpecialAbility specialAbility;
        private int patrolIndex;
        private float searchTimer;
        private float attackTimer;
        private Vector3 lastKnownPosition;
        private Vector3 homePosition;
        private bool warnedPlayer;
        private float nextWanderTime;
        private float nextPathRefresh;
        private Vector3 lastRequestedDestination;
        private Vector3 wanderCenter;
        private int routeIndex;
        private readonly List<Vector3> route = new List<Vector3>();

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            specialAbility = GetComponent<TitanSpecialAbility>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            if (player == null)
            {
                GameObject found = GameObject.Find("Treasure Hunter Player");
                if (found != null) player = found.transform;
            }
            if (player != null) playerHealth = player.GetComponent<PlayerHealth>();
            homePosition = transform.position;
            wanderCenter = homePosition;
            nextWanderTime = Time.time + Random.Range(0.5f, 1.5f);
            SetState(patrolPoints != null && patrolPoints.Length > 0 ? State.Patrol : State.Idle);
        }

        public void ConfigureEncounter(Transform targetPlayer, float detection = 40f, float chase = 2.8f)
        {
            player = targetPlayer;
            detectionDistance = detection;
            fieldOfView = 180f;
            chaseSpeed = chase;
            attackCooldown = 3.2f;
            enabled = true;
            if (specialAbility == null) specialAbility = GetComponent<TitanSpecialAbility>();
            if (controller == null) controller = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        public void PlayAttackImpact()
        {
            if (animator != null && animator.runtimeAnimatorController != null && !string.IsNullOrEmpty(attackAnimation))
                animator.CrossFade(attackAnimation, .05f);
        }

        private void Update()
        {
            if (player == null || (playerHealth != null && playerHealth.IsInvulnerable) ||
                (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing))
                return;

            float distance = FlatDistance(transform.position, player.position);
            float attackRange = specialAbility != null ? Mathf.Max(attackDistance, specialAbility.AttackRange) : attackDistance;
            if (distance <= detectionDistance && CanSeePlayer())
            {
                lastKnownPosition = player.position;
                searchTimer = searchDuration;
                if (CurrentState != State.Chase && CurrentState != State.Attack && !warnedPlayer)
                {
                    warnedPlayer = true;
                    if (UIManager.Instance != null) UIManager.Instance.ShowNotification(noticedMessage);
                }
                if (distance <= attackRange) SetState(State.Attack);
                else SetState(State.Chase);
            }

            switch (CurrentState)
            {
                case State.Idle:
                    if (TempleNavigation.IsReady && Time.time >= nextWanderTime) TryWander();
                    break;
                case State.Patrol:
                    Patrol();
                    break;
                case State.Chase:
                    if (CanSeePlayer()) MoveToward(player.position, chaseSpeed);
                    else SetState(State.Search);
                    break;
                case State.Attack:
                    Face(player.position);
                    if (distance > attackRange * 1.15f)
                        SetState(CanSeePlayer() ? State.Chase : State.Search);
                    else if (Time.time >= attackTimer)
                    {
                        attackTimer = Time.time + attackCooldown;
                    if (specialAbility != null) specialAbility.TryAttack(player, playerHealth);
                        else if (playerHealth != null) playerHealth.TakeDamage(8, gameObject.name, transform);
                    }
                    break;
                case State.Search:
                    searchTimer -= Time.deltaTime;
                    Face(lastKnownPosition);
                    if (CanSeePlayer()) SetState(State.Chase);
                    else if (searchTimer <= 0f) SetState(patrolPoints != null && patrolPoints.Length > 0 ? State.Return : State.Idle);
                    else MoveToward(lastKnownPosition, patrolSpeed);
                    break;
                case State.Return:
                    Vector3 destination = patrolPoints != null && patrolPoints.Length > 0 ? patrolPoints[Mathf.Clamp(patrolIndex, 0, patrolPoints.Length - 1)].position : homePosition;
                    if (FlatDistance(transform.position, destination) < 0.25f) SetState(State.Patrol);
                    else MoveToward(destination, patrolSpeed);
                    break;
            }
        }

        private bool CanSeePlayer()
        {
            if (player == null) return false;
            Vector3 origin = transform.position + Vector3.up * 1.2f;
            Vector3 target = player.position + Vector3.up * 1.0f;
            Vector3 direction = target - origin;
            float distance = direction.magnitude;
            if (distance > detectionDistance) return false;
            if (fieldOfView < 179f && Vector3.Angle(transform.forward, new Vector3(direction.x, 0f, direction.z)) > fieldOfView * 0.5f && distance > 2f) return false;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, delegate(RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); });
            for (int i = 0; i < hits.Length; i++)
            {
                Transform hit = hits[i].transform;
                if (hit.IsChildOf(transform)) continue;
                if (hit == player || hit.IsChildOf(player)) return true;
                return false;
            }
            return true;
        }

        private void Patrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                if (routeIndex >= route.Count)
                {
                    SetState(State.Idle);
                    nextWanderTime = Time.time + Random.Range(1f, 2.5f);
                }
                return;
            }
            Transform point = patrolPoints[patrolIndex];
            if (FlatDistance(transform.position, point.position) < 0.25f)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                point = patrolPoints[patrolIndex];
            }
            MoveToward(point.position, patrolSpeed);
        }

        private void MoveToward(Vector3 destination, float speed)
        {
            Vector3 direction = destination - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) return;
            if (TempleNavigation.IsReady)
            {
                if (Time.time >= nextPathRefresh)
                {
                    lastRequestedDestination = destination;
                    nextPathRefresh = Time.time + 0.7f;
                    routeIndex = 0;
                    TempleNavigation.FindPath(transform.position, destination, route);
                }
                while (routeIndex < route.Count && FlatDistance(transform.position, route[routeIndex]) < 0.42f)
                    routeIndex++;
                if (routeIndex < route.Count)
                {
                    direction = route[routeIndex] - transform.position;
                    direction.y = 0f;
                    if (direction.sqrMagnitude > 0.01f) direction.Normalize();
                }
                else direction.Normalize();
            }
            else direction.Normalize();

            // Fallback for missing nav data: turn along an open side when a wall blocks the direct path.
            Vector3 moveDirection = direction;
            Vector3 origin = transform.position + Vector3.up * 0.8f;
            if (Physics.SphereCast(origin, 0.28f, direction, out RaycastHit hit, 1.35f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(transform))
            {
                Vector3 left = Quaternion.AngleAxis(-55f, Vector3.up) * direction;
                Vector3 right = Quaternion.AngleAxis(55f, Vector3.up) * direction;
                float leftOpen = OpenDistance(origin, left);
                float rightOpen = OpenDistance(origin, right);
                moveDirection = leftOpen >= rightOpen ? left : right;
            }
            Face(transform.position + moveDirection);
            float abilityMultiplier = CurrentState == State.Chase && specialAbility != null ? specialAbility.ChaseMultiplier : 1f;
            if (controller != null && controller.enabled) controller.SimpleMove(moveDirection * speed * abilityMultiplier);
            else transform.position += moveDirection * speed * abilityMultiplier * Time.deltaTime;
        }

        private float OpenDistance(Vector3 origin, Vector3 direction)
        {
            if (Physics.SphereCast(origin, 0.28f, direction, out RaycastHit hit, 2.5f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(transform))
                return hit.distance;
            return 2.5f;
        }

        private void TryWander()
        {
            if (TempleNavigation.IsReady)
            {
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    Vector2 offset = Random.insideUnitCircle * wanderRadius;
                    Vector3 candidate = wanderCenter + new Vector3(offset.x, 0f, offset.y);
                    route.Clear();
                    if (!TempleNavigation.FindPath(transform.position, candidate, route) || route.Count == 0) continue;
                    lastRequestedDestination = candidate;
                    nextPathRefresh = Time.time + 0.7f;
                    routeIndex = 0;
                    MoveToward(candidate, patrolSpeed);
                    SetState(State.Patrol);
                    return;
                }
            }
            nextWanderTime = Time.time + 2f;
        }

        private void Face(Vector3 target)
        {
            Vector3 direction = target - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 7f * Time.deltaTime);
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void SetState(State state)
        {
            if (CurrentState == state) return;
            CurrentState = state;
            if (state == State.Attack) PlayAttackImpact();
            if (animator == null || animator.runtimeAnimatorController == null) return;
            string clip = state == State.Attack ? attackAnimation :
                (state == State.Chase || state == State.Patrol || state == State.Return) ? moveAnimation : idleAnimation;
            if (!string.IsNullOrEmpty(clip)) animator.CrossFade(clip, 0.15f);
        }
    }
}


