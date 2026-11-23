using UnityEngine;
using TempleOfBoredTitans.Interaction;

namespace TempleOfBoredTitans.Collectibles
{
    public sealed class CollectibleSymbol : Interactable
    {
        [SerializeField] private int symbolIndex;
        [SerializeField] private string symbolName = "Forgotten Titan";
        public void Configure(int index, string title)
        {
            symbolIndex = index;
            symbolName = title;
            prompt = string.Empty;
        }

        public override void Interact(PlayerInteraction player)
        {
            // Legacy symbol pickups are intentionally inert. Progress now comes from defeating Titans.
        }
    }
}
