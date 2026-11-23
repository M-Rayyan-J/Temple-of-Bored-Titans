using System.Collections;
using UnityEngine;
using TempleOfBoredTitans.Interaction;
using TempleOfBoredTitans.UI;

namespace TempleOfBoredTitans.Puzzles
{
    /// <summary>Lets the player test the ancient doors without revealing the route.</summary>
    public sealed class ThreeDoorChoice : Interactable
    {
        [SerializeField] private int doorNumber = 1;
        private bool opened;
        private Collider doorCollider;

        private void Awake()
        {
            doorCollider = GetComponent<Collider>();
            prompt = "E / Click  Test this ancient arch";
        }

        public override void Interact(PlayerInteraction player)
        {
            if (opened) return;
            if (doorNumber != 2)
            {
                if (UIManager.Instance != null) UIManager.Instance.ShowNotification("The ancient arch does not move. Try another one.");
                return;
            }
            opened = true;
            if (doorCollider != null) doorCollider.enabled = false;
            StartCoroutine(OpenDoor());
            if (UIManager.Instance != null) UIManager.Instance.ShowNotification("The ancient arch grinds open. Cold air rises from beyond it.");
        }

        private IEnumerator OpenDoor()
        {
            Vector3 start = transform.position;
            Vector3 end = start + Vector3.up * 4.4f;
            float elapsed = 0f;
            while (elapsed < .9f)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, elapsed / .9f));
                yield return null;
            }
            transform.position = end;
        }
    }
}
