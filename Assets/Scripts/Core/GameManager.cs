using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TempleOfBoredTitans.Core
{
    /// <summary>Small, scene-local state service for the game foundation.</summary>
    public sealed class GameManager : MonoBehaviour
    {
        public enum GameState { MainMenu, Playing, Paused, GameOver, Victory }

        public static GameManager Instance { get; private set; }
        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        private float previousTimeScale = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            // Deliberately scene-local for now: no persistent duplicate across scenes.
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            // UIManager presents the front-end; gameplay begins when Play is selected.
            SetState(GameState.MainMenu);
        }

        public void SetState(GameState state)
        {
            CurrentState = state;
            bool shouldPause = state == GameState.Paused || state == GameState.MainMenu;
            Time.timeScale = shouldPause ? 0f : 1f;
        }

        public void TogglePause()
        {
            if (CurrentState == GameState.Paused)
            {
                SetState(GameState.Playing);
                if (TempleOfBoredTitans.UI.UIManager.Instance != null)
                    TempleOfBoredTitans.UI.UIManager.Instance.HidePauseMenu();
            }
            else if (CurrentState == GameState.Playing)
            {
                SetState(GameState.Paused);
                if (TempleOfBoredTitans.UI.UIManager.Instance != null)
                    TempleOfBoredTitans.UI.UIManager.Instance.ShowPauseMenu();
            }
        }

        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("Cannot load a scene with an empty name.", this);
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        public void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitGame()
        {
            Time.timeScale = 1f;
            Application.Quit();
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && CurrentState == GameState.Playing)
                previousTimeScale = Time.timeScale;
        }
    }
}
