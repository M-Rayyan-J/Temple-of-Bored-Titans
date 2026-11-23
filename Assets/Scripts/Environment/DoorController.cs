using System.Collections;
using UnityEngine;
using TempleOfBoredTitans.Core;
using TempleOfBoredTitans.Interaction;
using TempleOfBoredTitans.UI;

namespace TempleOfBoredTitans.Environment
{
    public sealed class DoorController : Interactable
    {
        [SerializeField] private Vector3 closedPosition;
        [SerializeField] private float openHeight = 2.5f;
        [SerializeField] private float openDuration = 1.5f;
        private bool opened;
        private Collider blockingCollider;
        public bool IsOpened { get { return opened; } }

        private void Awake()
        {
            prompt = "E  Open the great escape gate";
            blockingCollider = GetComponent<Collider>();
            // The escape gate must always begin shut; only Interact may raise it.
            opened = false;
            transform.position = closedPosition;
            if (blockingCollider != null)
                blockingCollider.enabled = true;
        }

        public override void Interact(PlayerInteraction player)
        {
            if (opened)
                return;
            if (TreasureChestInteractable.Instance == null || !TreasureChestInteractable.Instance.IsOpened)
            {
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowNotification("The escape gate is sealed. Open the treasure chest first.");
                return;
            }
            opened = true;
            StartCoroutine(RaiseDoor());
        }

        private IEnumerator RaiseDoor()
        {
            Vector3 start = transform.position;
            Vector3 end = start + Vector3.up * openHeight;
            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / openDuration));
                yield return null;
            }
            transform.position = end;
            if (blockingCollider != null)
                blockingCollider.enabled = false;
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddScore(100);
            if (UIManager.Instance != null)
                    UIManager.Instance.SetObjective("The great gate is open. Run through it and into the forest!");
        }
    }
}


