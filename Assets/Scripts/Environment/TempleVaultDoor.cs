using System.Collections;
using UnityEngine;
using TempleOfBoredTitans.Collectibles;
using TempleOfBoredTitans.Interaction;
using TempleOfBoredTitans.UI;

namespace TempleOfBoredTitans.Environment
{
    /// <summary>One of three ancient doors opens with the vault key.</summary>
    public sealed class TempleVaultDoor : Interactable
    {
        [SerializeField, Range(0, 2)] private int doorIndex = 1;
        private Collider doorCollider;
        private bool opened;

        private void Awake()
        {
            doorCollider = GetComponent<Collider>();
            prompt = "E / Click  Test this ancient door";
        }

        public void Configure(int index) { doorIndex = Mathf.Clamp(index, 0, 2); }

        public override void Interact(PlayerInteraction player)
        {
            if (opened) return;
            if (doorIndex != 1)
            {
                if (UIManager.Instance != null) UIManager.Instance.ShowNotification("The ancient door does not move. There may be another way through.");
                return;
            }
            if (SymbolManager.Instance == null || !SymbolManager.Instance.HasTreasureKey)
            {
                if (UIManager.Instance != null) UIManager.Instance.ShowNotification("The stone mechanism is locked. Find a way to release it.");
                return;
            }
            opened = true;
            StartCoroutine(RaiseDoor());
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification("The key turns. An ancient door grinds open, revealing a chamber beyond.");
                UIManager.Instance.SetObjective("Explore the newly opened chamber and search for the treasure.");
            }
        }

        private IEnumerator RaiseDoor()
        {
            Vector3 start = transform.position;
            Vector3 end = start + Vector3.up * 4.8f;
            float elapsed = 0f;
            while (elapsed < 1.6f)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, elapsed / 1.6f));
                yield return null;
            }
            transform.position = end;
            if (doorCollider != null) doorCollider.enabled = false;
        }
    }
}
