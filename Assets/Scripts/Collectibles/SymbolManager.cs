using UnityEngine;
using TempleOfBoredTitans.Core;
using TempleOfBoredTitans.UI;
using TempleOfBoredTitans.Interaction;

namespace TempleOfBoredTitans.Collectibles
{
    public sealed class SymbolManager : MonoBehaviour
    {
        public const int RequiredSymbols = 5;
        public static SymbolManager Instance { get; private set; }
        private readonly bool[] collected = new bool[RequiredSymbols];
        public int Count { get; private set; }
        public bool AllCollected { get { return Count >= RequiredSymbols; } }
        public bool HasTreasureKey { get { return AllCollected; } }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (UIManager.Instance != null)
                UIManager.Instance.SetSymbolCount(Count, RequiredSymbols);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool Collect(int index, string symbolName)
        {
            if (index < 0 || index >= collected.Length || collected[index])
                return false;

            collected[index] = true;
            Count++;
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddScore(100);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetSymbolCount(Count, RequiredSymbols);
                UIManager.Instance.ShowNotification(symbolName + " defeated. The treasure key grows warmer.");
                if (AllCollected)
                {
                    UIManager.Instance.SetObjective("The key is yours. Search all three ancient doors to discover the route to the treasure.");
                    UIManager.Instance.ShowNotification("The vault key is yours. Inspect each ancient door and find where it leads.");
                    if (TreasureChestInteractable.Instance != null) TreasureChestInteractable.Instance.ShowKey();
                }
            }
            return true;
        }
    }
}



