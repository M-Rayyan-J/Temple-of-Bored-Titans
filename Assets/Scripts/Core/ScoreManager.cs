using UnityEngine;
using TempleOfBoredTitans.UI;

namespace TempleOfBoredTitans.Core
{
    public sealed class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }
        public int Score { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void AddScore(int points)
        {
            Score = Mathf.Max(0, Score + points);
            if (UIManager.Instance != null)
                UIManager.Instance.SetScore(Score);
        }
    }
}

