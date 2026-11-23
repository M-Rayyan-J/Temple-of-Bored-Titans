using System.Collections;
using UnityEngine;
using TempleOfBoredTitans.Core;
using TempleOfBoredTitans.Titans;
using TempleOfBoredTitans.UI;

namespace TempleOfBoredTitans.Story
{
    /// <summary>Introduces the wardens and seals the entrance once the hunter enters.</summary>
    public sealed class TempleStoryboardIntro : MonoBehaviour
    {
        [SerializeField] private Transform entranceDoor;
        [SerializeField] private float doorClosedY = .25f;
        [SerializeField] private Light[] statueEyes;
        private bool started;

        private void OnTriggerEnter(Collider other)
        {
            if (started || other.GetComponentInParent<TempleOfBoredTitans.Player.PlayerController>() == null) return;
            started = true;
            StartCoroutine(PlayIntro());
        }

        private IEnumerator PlayIntro()
        {
            var hunter = GameObject.Find("Treasure Hunter Player");
            if (hunter != null)
            {
                var health = hunter.GetComponent<TempleOfBoredTitans.Player.PlayerHealth>();
                if (health != null) health.SetCheckpoint(new Vector3(0f, 0f, -9.2f), Quaternion.identity);
            }
            if (UIManager.Instance != null)
                UIManager.Instance.ShowNotification("You entered the temple. The entrance is sealing behind you. Defeat the five Titans for the vault key.");

            if (entranceDoor != null)
            {
                Vector3 start = entranceDoor.position;
                Vector3 end = new Vector3(start.x, doorClosedY, start.z);
                float elapsed = 0f;
                while (elapsed < .9f)
                {
                    elapsed += Time.deltaTime;
                    entranceDoor.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, elapsed / .9f));
                    yield return null;
                }
                entranceDoor.position = end;
            }

            // The entrance seals as soon as the hunter crosses it, but the
            // statues do not awaken until the player reaches their hall.
            while (hunter != null && hunter.transform.position.z < 3f)
                yield return null;

            for (int i = 0; i < statueEyes.Length; i++)
            {
                yield return new WaitForSeconds(0.35f);
                Light eye = statueEyes[i];
                if (eye != null)
                {
                    eye.enabled = true;
                    float elapsed = 0f;
                    while (elapsed < .45f)
                    {
                        elapsed += Time.deltaTime;
                        eye.intensity = Mathf.Lerp(0f, 4f, Mathf.Clamp01(elapsed / .45f));
                        yield return null;
                    }
                    eye.intensity = 4f;
                }
                if (TempleAudioDirector.Instance != null) TempleAudioDirector.Instance.PlayThreat();
            }

            yield return new WaitForSeconds(.6f);
            if (TitanEncounterDirector.Instance != null) TitanEncounterDirector.Instance.RevealFirstTitan();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification("The statues flare with cold light. Something stirs beyond them.");
                UIManager.Instance.SetObjective("Defeat the five Titans and claim the key. Search the three ancient doors to discover the way to the treasure.");
            }

        }
    }
}
