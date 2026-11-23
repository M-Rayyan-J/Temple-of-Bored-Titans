using UnityEngine;

namespace TempleOfBoredTitans.Interaction
{
    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField] protected string prompt = "E / Click  Interact";
        public string Prompt { get { return prompt; } }
        public abstract void Interact(PlayerInteraction player);
    }
}
