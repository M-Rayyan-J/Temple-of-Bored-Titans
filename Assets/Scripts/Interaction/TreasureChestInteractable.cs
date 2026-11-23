using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TempleOfBoredTitans.Collectibles;
using TempleOfBoredTitans.Core;
using TempleOfBoredTitans.Environment;
using TempleOfBoredTitans.UI;

namespace TempleOfBoredTitans.Interaction
{
    /// <summary>Unlocks the buried hoard after the five temple wardens fall.</summary>
    public sealed class TreasureChestInteractable : Interactable
    {
        public static TreasureChestInteractable Instance { get; private set; }
        [SerializeField] private GameObject keyVisual;
        [SerializeField] private GameObject goldVisual;
        [SerializeField] private GameObject goldBeacon;
        private bool opened;
        private bool opening;
        private Collider chestCollider;
        private readonly List<Transform> lidParts = new List<Transform>();
        private readonly List<Quaternion> closedLidRotations = new List<Quaternion>();
        private Vector3 goldScale;
        public bool IsOpened { get { return opened; } }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            prompt = "E / Click  Open the gold vault chest";
            chestCollider = GetComponent<Collider>();
            if (goldVisual != null) goldVisual.SetActive(false);
            if (goldBeacon == null)
            {
                Transform beacon = transform.Find("Treasure Vault Gold Beacon");
                if (beacon != null) goldBeacon = beacon.gameObject;
            }
            if (goldBeacon != null) goldBeacon.SetActive(false);
            if (goldVisual != null) goldScale = goldVisual.transform.localScale;
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name.IndexOf("Chest_Top", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                lidParts.Add(child);
                closedLidRotations.Add(child.localRotation);
            }
        }

        public void Configure(GameObject key, GameObject gold)
        {
            keyVisual = key;
            goldVisual = gold;
            if (keyVisual != null) keyVisual.SetActive(false);
            if (goldVisual != null) { goldVisual.SetActive(false); goldScale = goldVisual.transform.localScale; }
        }

        public void ShowKey()
        {
            if (keyVisual != null) keyVisual.SetActive(true);

            if (goldBeacon != null) goldBeacon.SetActive(true);
        }

        public override void Interact(PlayerInteraction player)
        {
            if (opened || opening) return;
            if (SymbolManager.Instance == null || !SymbolManager.Instance.HasTreasureKey)
            {
                if (UIManager.Instance != null) UIManager.Instance.ShowNotification("The chest is sealed. Defeat all five Titans to claim its key.");
                return;
            }
            opening = true;
            if (keyVisual != null) keyVisual.SetActive(false);
            if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(2500);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification("The ancient chest opens. Gold glitters inside. Take it, then reach the great outer gate.");
                UIManager.Instance.SetObjective("Treasure and key secured. Go to the great outer gate and press E to open it.");
            }
            StartCoroutine(OpenChestAnimation());
        }

        private IEnumerator OpenChestAnimation()
        {
            float duration = .9f;
            float elapsed = 0f;
            if (goldVisual != null)
            {
                goldVisual.SetActive(true);
                goldVisual.transform.localScale = Vector3.zero;
            }
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                for (int i = 0; i < lidParts.Count; i++)
                    if (lidParts[i] != null) lidParts[i].localRotation = closedLidRotations[i] * Quaternion.Euler(-92f * t, 0f, 0f);
                if (goldVisual != null) goldVisual.transform.localScale = Vector3.Lerp(Vector3.zero, goldScale, Mathf.Clamp01((t - .38f) / .62f));
                yield return null;
            }
            for (int i = 0; i < lidParts.Count; i++)
                if (lidParts[i] != null) lidParts[i].localRotation = closedLidRotations[i] * Quaternion.Euler(-92f, 0f, 0f);
            if (goldVisual != null) goldVisual.transform.localScale = goldScale;
            if (chestCollider != null) chestCollider.enabled = false;
            opened = true;
            opening = false;
            if (UIManager.Instance != null) UIManager.Instance.ShowNotification("The hoard is yours. Go to the great outer gate and press E to open it.");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
