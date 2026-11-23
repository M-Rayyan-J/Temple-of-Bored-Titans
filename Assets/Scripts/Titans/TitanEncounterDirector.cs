using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TempleOfBoredTitans.Collectibles;
using TempleOfBoredTitans.Core;
using TempleOfBoredTitans.Player;
using TempleOfBoredTitans.UI;
using TempleOfBoredTitans.Interaction;

namespace TempleOfBoredTitans.Titans
{
    /// <summary>Runs the five wardens in order; defeating them earns the treasure key.</summary>
    public sealed class TitanEncounterDirector : MonoBehaviour
    {
        public static TitanEncounterDirector Instance { get; private set; }
        [SerializeField] private TitanBossHealth[] bosses;
        private int currentIndex;
        private Transform playerTransform;
        private bool rewardInProgress;
        public TitanBossHealth CurrentBoss { get; private set; }

        private static readonly string[] Titles = { "Ashen Dragon", "Wailing Skeleton", "Plague Brute", "Winged Reaper", "Stone Colossus" };
        private static readonly string[] Taunts =
        {
            "ASHEN DRAGON: The gold was buried with my oath. You leave empty-handed.",
            "WAILING SKELETON: I hear your footsteps. I will hear your last breath, too.",
            "PLAGUE BRUTE: The vault key belongs to the dead.",
            "WINGED REAPER: The treasure is close. Your hope is not.",
            "STONE COLOSSUS: Defeat me, and the ancient hoard is yours."
        };

        public void SetEncounterBosses(TitanBossHealth[] encounterBosses, Transform hunter = null)
        {
            bosses = encounterBosses;
            currentIndex = 0;
            playerTransform = hunter;
        }

        public static void EnsureRuntimeEncounter(Transform player)
        {
            string[][] aliases =
            {
                new[] { "Angry Titan - Dragon", "Fire Titan", "Ashen Dragon" },
                new[] { "Crying Monk", "Wailing Skeleton" },
                new[] { "Lazy Guardian", "Plague Brute" },
                new[] { "Dancing Titan", "Winged Reaper" },
                new[] { "Echo Archivist", "Stone Colossus" }
            };
            int[] healthValues = { 220, 280, 350, 430, 520 };
            TitanSpecialAbility.AbilityKind[] abilities =
            {
                TitanSpecialAbility.AbilityKind.FlameBurst, TitanSpecialAbility.AbilityKind.SoulScream,
                TitanSpecialAbility.AbilityKind.AcidSpit, TitanSpecialAbility.AbilityKind.Pounce,
                TitanSpecialAbility.AbilityKind.GroundSlam
            };
            Vector3[] roomPositions =
            {
                new Vector3(0f,.85f,-5.2f), new Vector3(-13.5f,.85f,4.2f), new Vector3(13.5f,.85f,4.2f),
                new Vector3(-13.5f,.85f,14.5f), new Vector3(0f,.85f,29.15f)
            };
            var found = new List<TitanBossHealth>();
            for (int stage = 0; stage < aliases.Length; stage++)
            {
                GameObject enemy = null;
                for (int alias = 0; alias < aliases[stage].Length && enemy == null; alias++) enemy = FindSceneObject(aliases[stage][alias]);
                if (enemy == null) continue;
                var ai = enemy.GetComponent<TitanAI>() ?? enemy.AddComponent<TitanAI>();
                ai.ConfigureEncounter(player, 42f, 2.65f + stage * .12f);
                var boss = enemy.GetComponent<TitanBossHealth>() ?? enemy.AddComponent<TitanBossHealth>();
                boss.Configure(stage, Titles[stage], healthValues[stage]);
                var special = enemy.GetComponent<TitanSpecialAbility>() ?? enemy.AddComponent<TitanSpecialAbility>();
                special.Configure(abilities[stage], 8 + stage, stage == 1 ? 5.2f : 4.6f);
                enemy.name = Titles[stage];
                enemy.transform.position = new Vector3(roomPositions[stage].x, 0f, roomPositions[stage].z + .7f);
                found.Add(boss);
            }
            if (found.Count != aliases.Length)
                Debug.LogError("Titan encounter setup found " + found.Count + " of 5 required Titans. Check that the Stone Colossus is present in the scene.");
            if (found.Count == 0) return;
            GameManager manager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
            GameObject host = manager != null ? manager.gameObject : player.gameObject;
            var director = host.GetComponent<TitanEncounterDirector>() ?? host.AddComponent<TitanEncounterDirector>();
            director.SetEncounterBosses(found.ToArray(), player);
            for (int i = 0; i < found.Count; i++) if (found[i] != null) found[i].gameObject.SetActive(false);
        }

        private static GameObject FindSceneObject(string objectName)
        {
            Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || !all[i].gameObject.scene.IsValid() || all[i].name != objectName) continue;
                // Legacy scene backups can contain duplicate Titan names. Activating a
                // child under an inactive backup parent still leaves the Titan invisible.
                bool hiddenByParent = false;
                for (Transform parent = all[i].parent; parent != null; parent = parent.parent)
                {
                    if (!parent.gameObject.activeInHierarchy) { hiddenByParent = true; break; }
                }
                if (!hiddenByParent) return all[i].gameObject;
            }
            return GameObject.Find(objectName);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (bosses == null || bosses.Length == 0)
            {
                bosses = FindObjectsByType<TitanBossHealth>(FindObjectsSortMode.None);
                System.Array.Sort(bosses, (a, b) => a.StageIndex.CompareTo(b.StageIndex));
            }
            if (playerTransform == null)
            {
                PlayerController hunter = FindFirstObjectByType<PlayerController>();
                if (hunter != null) playerTransform = hunter.transform;
            }
            for (int i = 0; i < bosses.Length; i++) if (bosses[i] != null && i != 0) bosses[i].gameObject.SetActive(false);
            if (bosses.Length == 0 || bosses[0] == null) { Debug.LogError("No Titan bosses are assigned to the encounter director.", this); return; }
            currentIndex = 0;
            for (int i = 0; i < bosses.Length; i++) if (bosses[i] != null) bosses[i].gameObject.SetActive(false);
            CurrentBoss = bosses[0];
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideBossStatus();
                UIManager.Instance.SetObjective("Reach the statues. Defeat the five Titans and search the ancient doors for the treasure route.");
            }
        }

        public void RevealFirstTitan()
        {
            if (bosses == null || bosses.Length == 0 || bosses[0] == null || CurrentBoss != bosses[0]) return;
            if (bosses[0].gameObject.activeSelf) return;
            ActivateBoss(bosses[0], false);
            CurrentBoss = bosses[0];
            ShowCurrentBoss();
            if (UIManager.Instance != null) UIManager.Instance.SetObjective("Defeat the Ashen Dragon. Each Titan guards the path to the ancient treasure.");
        }

        public void UpdateBossHealth(TitanBossHealth boss)
        {
            if (boss == CurrentBoss && UIManager.Instance != null) UIManager.Instance.SetBossStatus(boss.DisplayName, boss.CurrentHealth, boss.MaxHealth);
        }

        public void BossDefeated(TitanBossHealth boss)
        {
            if (boss == null || boss != CurrentBoss || boss.StageIndex != currentIndex || rewardInProgress) return;
            rewardInProgress = true;
            StartCoroutine(RewardDefeat(boss, currentIndex));
        }

        private IEnumerator RewardDefeat(TitanBossHealth boss, int index)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowNotification(boss.DisplayName + " collapses. The ancient vault trembles.");
            yield return new WaitForSeconds(.45f);
            if (SymbolManager.Instance != null) SymbolManager.Instance.Collect(index, boss.DisplayName);
            if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(500);
            currentIndex++;
            if (currentIndex < bosses.Length && bosses[currentIndex] != null)
            {
                CurrentBoss = bosses[currentIndex];
                ActivateBoss(CurrentBoss, true);
                ShowCurrentBoss();
                if (UIManager.Instance != null) UIManager.Instance.SetObjective(currentIndex == 4
                    ? "The Stone Colossus has entered the central hall ahead. Defeat it to claim the vault key."
                    : "A new Titan enters the next chamber. Defeat it and continue toward the vault.");
            }
            else
            {
                CurrentBoss = null;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.HideBossStatus();
                    UIManager.Instance.SetObjective("The key is yours. Search all three ancient doors to discover the route to the treasure.");
                    UIManager.Instance.ShowNotification("The vault key is yours. Inspect each ancient door and find where it leads.");
                }
                if (TreasureChestInteractable.Instance != null) TreasureChestInteractable.Instance.ShowKey();
            }
            rewardInProgress = false;
        }

        private void ShowCurrentBoss()
        {
            if (CurrentBoss != null && UIManager.Instance != null)
            {
                UIManager.Instance.SetBossStatus(CurrentBoss.DisplayName, CurrentBoss.CurrentHealth, CurrentBoss.MaxHealth);
                UIManager.Instance.ShowNotification(Taunts[Mathf.Clamp(CurrentBoss.StageIndex, 0, Taunts.Length - 1)]);
            }
            if (CurrentBoss != null && TempleAudioDirector.Instance != null) TempleAudioDirector.Instance.PlayTitanAttack(CurrentBoss.StageIndex);
        }

        private void ActivateBoss(TitanBossHealth boss, bool bringIntoView)
        {
            if (boss == null) return;
            boss.gameObject.SetActive(true);
            if (bringIntoView && playerTransform != null)
            {
                // Later chambers have sharp turns and no baked NavMesh. Bring each
                // successor into the current chamber so it is visibly reachable.
                Vector3 spawn;
                if (boss.StageIndex == 4)
                {
                    // The treasure sanctum is sealed until after the fifth Titan,
                    // so Stone Colossus must fight in the open hall before the
                    // vault doors, not behind them. Spawn ahead of the hunter and
                    // keep it inside the central route (z 16-22, before the wall at z=25).
                    Vector3 forward = playerTransform.forward;
                    forward.y = 0f;
                    if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
                    spawn = playerTransform.position + forward.normalized * 5.5f;
                    spawn.x = Mathf.Clamp(spawn.x, -6f, 6f);
                    spawn.z = Mathf.Clamp(spawn.z, 16f, 22f);
                    spawn.y = 0f;
                }
                else
                {
                    spawn = playerTransform.position + playerTransform.forward * 7f;
                    spawn.x = Mathf.Clamp(spawn.x, -7.5f, 7.5f);
                    spawn.z = Mathf.Min(spawn.z, 18.5f);
                    spawn.y = 0f;
                }
                boss.transform.position = spawn;
                Vector3 towardHunter = playerTransform.position - spawn;
                towardHunter.y = 0f;
                if (towardHunter.sqrMagnitude > .01f) boss.transform.rotation = Quaternion.LookRotation(towardHunter);
            }
            foreach (Renderer renderer in boss.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
            foreach (Collider collider in boss.GetComponentsInChildren<Collider>(true)) collider.enabled = true;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
