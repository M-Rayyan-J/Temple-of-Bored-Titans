using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TempleOfBoredTitans.Environment;
using TempleOfBoredTitans.Interaction;
using TempleOfBoredTitans.Player;
using TempleOfBoredTitans.UI;

namespace TempleOfBoredTitans.Core
{
    /// <summary>Shows a clear ending after the player claims the hoard and reaches the forest.</summary>
    public sealed class TempleEscapeExit : MonoBehaviour
    {
        [SerializeField] private float escapeLineZ = 37.5f;
        private Transform player;
        private bool escaped;

        private void Update()
        {
            if (escaped)
            {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && GameManager.Instance != null)
                    GameManager.Instance.RestartCurrentScene();
                return;
            }
            if (player == null)
            {
                GameObject found = GameObject.Find("Treasure Hunter Player");
                if (found != null) player = found.transform;
            }
            if (player == null || player.position.z < escapeLineZ || Mathf.Abs(player.position.x) > 6f) return;
            if (TreasureChestInteractable.Instance == null || !TreasureChestInteractable.Instance.IsOpened) return;
            DoorController exitGate = GameObject.Find("Final Chamber Gate") != null
                ? GameObject.Find("Final Chamber Gate").GetComponent<DoorController>() : null;
            if (exitGate == null || !exitGate.IsOpened) return;

            escaped = true;
            StartCoroutine(EscapeSequence());
        }

        private IEnumerator EscapeSequence()
        {
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameManager.GameState.Victory);
            PlayerController.SetCursorLocked(false);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideEnding();
                UIManager.Instance.SetObjective("ESCAPE  Run through the gate with the gold!");
                UIManager.Instance.ShowNotification("You finally escaped with the gold!");
            }

            PlayerController controller = player != null ? player.GetComponent<PlayerController>() : null;
            if (controller != null)
                yield return controller.PlayEscapeRun(8f, 2.6f);
            else
                yield return new WaitForSecondsRealtime(2.6f);

            if (UIManager.Instance != null) UIManager.Instance.ShowEscapeEnding();
        }
    }
}
