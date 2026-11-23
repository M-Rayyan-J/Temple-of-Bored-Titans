using System.Collections;
using UnityEngine;
using TempleOfBoredTitans.Core;
using TempleOfBoredTitans.Player;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TempleOfBoredTitans.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }
        private UIDocument document;
        private Label symbolLabel;
        private Label scoreLabel;
        private Label objectiveLabel;
        private Label healthLabel;
        private int currentHealth = 100;
        private int maximumHealth = 100;
        private Label weaponLabel;
        private Label bossLabel;
        private Label promptLabel;
        private Label notificationLabel;
        private VisualElement endingOverlay;
        private Label endingTitle;
        private Label endingBody;
        private Coroutine hideNotification;
        private VisualElement menuOverlay;
        private VisualElement menuPanel;
        private Button pauseButton;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            document = GetComponent<UIDocument>();
        }

        private void Start()
        {
            if (document == null) return;
            VisualElement root = document.rootVisualElement;
            symbolLabel = root.Q<Label>("symbol-counter");
            scoreLabel = root.Q<Label>("score-label");
            objectiveLabel = root.Q<Label>("objective-label");
            healthLabel = root.Q<Label>("health-label");
            weaponLabel = root.Q<Label>("weapon-label");
            bossLabel = root.Q<Label>("boss-label");
            promptLabel = root.Q<Label>("prompt-label");
            notificationLabel = root.Q<Label>("notification-label");
            endingOverlay = root.Q<VisualElement>("ending-overlay");
            endingTitle = root.Q<Label>("ending-title");
            endingBody = root.Q<Label>("ending-body");
            SetSymbolCount(0, 5);
            SetScore(0);
            SetPlayerHealth(110, 110);
            SetWeaponStatus("Rusted Sidearm", 8, 24);
            HideBossStatus();
            HideEnding();
            SetObjective("Find the ancient temple. Enter it, then defeat five Titans to claim the vault key.");
            SetPrompt(string.Empty);
            ShowNotification("WASD move · Mouse look · Left-click aim and fire · E interact · Esc pause.");
            BuildPauseButton(root);
            BuildEndingButtons();
            BuildMenu();
            ShowMainMenu();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Keyboard.current == null || GameManager.Instance == null)
                return;

            if ((GameManager.Instance.CurrentState == GameManager.GameState.GameOver ||
                 GameManager.Instance.CurrentState == GameManager.GameState.Victory) &&
                Keyboard.current.rKey.wasPressedThisFrame)
            {
                RestartGame();
                return;
            }

            if (!Keyboard.current.escapeKey.wasPressedThisFrame)
                return;

            if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
                GameManager.Instance.TogglePause();
            else if (GameManager.Instance.CurrentState == GameManager.GameState.Paused)
                GameManager.Instance.TogglePause();
        }

        public void SetSymbolCount(int count, int total)
        {
            if (symbolLabel != null) symbolLabel.text = "TITANS DEFEATED  " + count + " / " + total + (count >= total ? "  ·  KEY ACQUIRED" : "");
        }

        public void SetPlayerHealth(int current, int maximum)
        {
            maximum = Mathf.Max(1, maximum);
            currentHealth = Mathf.Clamp(Mathf.RoundToInt(current * 100f / maximum), 0, 100);
            maximumHealth = 100;
            RefreshHealthLabel();
        }

        public void SetPlayerLives(int lives)
        {
            // Lives are intentionally hidden; the HUD communicates health only.
        }

        private void RefreshHealthLabel()
        {
            if (healthLabel != null) healthLabel.text = "HEALTH  " + currentHealth + " / " + maximumHealth;
        }

        public void SetWeaponStatus(string weapon, int ammunition, int reserve)
        {
            if (weaponLabel != null) weaponLabel.text = "WEAPON  " + weapon.ToUpperInvariant();
        }

        private void BuildMenu()
        {
            VisualElement root = document.rootVisualElement;
            menuOverlay = new VisualElement { name = "front-end-overlay" };
            menuOverlay.style.position = Position.Absolute;
            menuOverlay.style.left = 0;
            menuOverlay.style.right = 0;
            menuOverlay.style.top = 0;
            menuOverlay.style.bottom = 0;
            menuOverlay.style.backgroundColor = new Color(.025f, .018f, .025f, .94f);
            menuOverlay.style.justifyContent = Justify.Center;
            menuOverlay.style.alignItems = Align.Center;
            menuOverlay.style.display = DisplayStyle.None;
            root.Add(menuOverlay);
        }

        private void BuildPauseButton(VisualElement root)
        {
            pauseButton = new Button(PauseFromButton) { text = "PAUSE  Esc" };
            pauseButton.style.position = Position.Absolute;
            pauseButton.style.right = 28;
            pauseButton.style.top = 68;
            pauseButton.style.paddingLeft = 14;
            pauseButton.style.paddingRight = 14;
            pauseButton.style.paddingTop = 8;
            pauseButton.style.paddingBottom = 8;
            pauseButton.style.color = new Color(.95f, .89f, .76f);
            pauseButton.style.backgroundColor = new Color(.055f, .045f, .043f, .88f);
            pauseButton.style.borderLeftColor = new Color(.57f, .35f, .13f);
            pauseButton.style.borderRightColor = new Color(.57f, .35f, .13f);
            pauseButton.style.borderTopColor = new Color(.57f, .35f, .13f);
            pauseButton.style.borderBottomColor = new Color(.57f, .35f, .13f);
            root.Add(pauseButton);
            pauseButton.style.display = DisplayStyle.None;
        }

        private void BuildEndingButtons()
        {
            if (endingOverlay == null) return;
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.Center;
            row.style.marginTop = 20;
            endingOverlay.Add(row);
            AddEndingButton(row, "RESTART", RestartGame);
            AddEndingButton(row, "QUIT GAME", QuitGame);
        }

        private static void AddEndingButton(VisualElement parent, string text, System.Action callback)
        {
            Button button = new Button(callback) { text = text };
            button.style.minWidth = 150;
            button.style.height = 46;
            button.style.marginLeft = 8;
            button.style.marginRight = 8;
            button.style.color = new Color(.95f, .89f, .76f);
            button.style.backgroundColor = new Color(.16f, .12f, .1f);
            button.style.borderTopColor = new Color(.57f, .35f, .13f);
            button.style.borderBottomColor = new Color(.57f, .35f, .13f);
            button.style.borderLeftColor = new Color(.57f, .35f, .13f);
            button.style.borderRightColor = new Color(.57f, .35f, .13f);
            button.style.fontSize = 16;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            parent.Add(button);
        }

        private void ShowMainMenu()
        {
            if (pauseButton != null) pauseButton.style.display = DisplayStyle.None;
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameManager.GameState.MainMenu);
            PlayerController.SetCursorLocked(false);
            ShowMenu("TEMPLE OF BORED TITANS", "An ancient ruin. Five Titans. One chance to claim the lost treasure.",
                new[] { "PLAY", "CONTROLS", "QUIT GAME" }, new System.Action[] { StartGame, ShowControls, QuitGame });
        }

        private void ShowControls()
        {
            ShowMenu("CONTROLS", "W A S D   Move\nMouse   Look\nLeft Click   Aim and fire\nE   Interact\nShift   Run\nEsc   Pause\n\nYour weapon never runs out of ammunition.",
                new[] { "BACK", "PLAY", "QUIT GAME" }, new System.Action[] { ShowMainMenu, StartGame, QuitGame });
        }

        public void ShowPauseMenu()
        {
            if (pauseButton != null) pauseButton.style.display = DisplayStyle.None;
            PlayerController.SetCursorLocked(false);
            ShowMenu("PAUSED", "The hunt is waiting.", new[] { "RESUME", "RESTART", "QUIT GAME" },
                new System.Action[] { ResumeGame, RestartGame, QuitGame });
        }

        private void PauseFromButton()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
                GameManager.Instance.TogglePause();
        }

        public void HidePauseMenu()
        {
            if (menuOverlay != null) menuOverlay.style.display = DisplayStyle.None;
            if (pauseButton != null) pauseButton.style.display = DisplayStyle.Flex;
            PlayerController.SetCursorLocked(true);
        }

        private void StartGame()
        {
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameManager.GameState.Playing);
            if (menuOverlay != null) menuOverlay.style.display = DisplayStyle.None;
            if (pauseButton != null) pauseButton.style.display = DisplayStyle.Flex;
            HideEnding();
            PlayerController.SetCursorLocked(true);
            ShowNotification("Find the temple entrance. Defeat the five Titans, claim the treasure, and escape.");
        }

        private void ResumeGame()
        {
            if (GameManager.Instance != null) GameManager.Instance.TogglePause();
        }

        private void RestartGame()
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartCurrentScene();
        }

        private void QuitGame()
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
            else Application.Quit();
        }

        private void ShowMenu(string title, string body, string[] buttonTexts, System.Action[] callbacks)
        {
            if (menuOverlay == null) return;
            menuOverlay.Clear();
            menuOverlay.style.display = DisplayStyle.Flex;
            menuPanel = new VisualElement();
            menuPanel.style.width = 540;
            menuPanel.style.maxWidth = new Length(90, LengthUnit.Percent);
            menuPanel.style.paddingLeft = 34;
            menuPanel.style.paddingRight = 34;
            menuPanel.style.paddingTop = 30;
            menuPanel.style.paddingBottom = 30;
            menuPanel.style.backgroundColor = new Color(.055f, .045f, .043f, .98f);
            menuPanel.style.borderLeftWidth = 3;
            menuPanel.style.borderLeftColor = new Color(.57f, .35f, .13f);
            menuOverlay.Add(menuPanel);

            Label heading = new Label(title);
            heading.style.color = new Color(.82f, .64f, .34f);
            heading.style.fontSize = 32;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.unityTextAlign = TextAnchor.MiddleCenter;
            heading.style.marginBottom = 14;
            heading.style.whiteSpace = WhiteSpace.Normal;
            menuPanel.Add(heading);

            Label description = new Label(body);
            description.style.color = new Color(.82f, .8f, .75f);
            description.style.fontSize = 17;
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.marginBottom = 22;
            menuPanel.Add(description);

            for (int i = 0; i < buttonTexts.Length; i++)
            {
                Button button = new Button(callbacks[i]) { text = buttonTexts[i] };
                button.style.height = 44;
                button.style.marginTop = 5;
                button.style.marginBottom = 5;
                button.style.color = new Color(.95f, .89f, .76f);
                button.style.backgroundColor = new Color(.16f, .12f, .1f);
                button.style.borderTopColor = new Color(.4f, .29f, .18f);
                button.style.borderBottomColor = new Color(.4f, .29f, .18f);
                button.style.borderLeftColor = new Color(.4f, .29f, .18f);
                button.style.borderRightColor = new Color(.4f, .29f, .18f);
                button.style.fontSize = 16;
                button.style.unityFontStyleAndWeight = FontStyle.Bold;
                menuPanel.Add(button);
            }
        }

        public void SetBossStatus(string bossName, int current, int maximum)
        {
            if (bossLabel == null) return;
            int segments = maximum > 0 ? Mathf.Clamp(Mathf.CeilToInt(current * 12f / maximum), 0, 12) : 0;
            bossLabel.text = bossName.ToUpperInvariant() + "   " + new string('█', segments) + new string('░', 12 - segments);
            bossLabel.style.display = DisplayStyle.Flex;
        }

        public void HideBossStatus()
        {
            if (bossLabel != null) bossLabel.style.display = DisplayStyle.None;
        }

        public void SetScore(int score)
        {
            if (scoreLabel != null) scoreLabel.text = "SCORE  " + score;
        }

        public void SetObjective(string objective)
        {
            if (objectiveLabel != null) objectiveLabel.text = "OBJECTIVE  " + objective;
        }

        public void SetPrompt(string prompt)
        {
            if (promptLabel == null) return;
            promptLabel.text = prompt;
            promptLabel.style.display = string.IsNullOrEmpty(prompt) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void ShowNotification(string message)
        {
            if (TempleAudioDirector.Instance != null) TempleAudioDirector.Instance.PlayNotification();
            if (notificationLabel == null) return;
            notificationLabel.text = message;
            notificationLabel.style.display = DisplayStyle.Flex;
            if (hideNotification != null) StopCoroutine(hideNotification);
            hideNotification = StartCoroutine(HideNotificationAfter(5f));
        }

        public void ShowDamage(string source)
        {
            ShowNotification("Hit by " + source + ". Find cover or keep moving.");
        }

        public void ShowDefeat(string title, string message)
        {
            if (pauseButton != null) pauseButton.style.display = DisplayStyle.None;
            PlayerController.SetCursorLocked(false);
            if (endingTitle != null) endingTitle.text = title;
            if (endingBody != null) endingBody.text = message + "\nClick RESTART or press R to try again.";
            if (endingOverlay != null) endingOverlay.style.display = DisplayStyle.Flex;
        }

        public void ShowEscapeEnding()
        {
            ShowDefeat("YOU ESCAPED WITH THE GOLD", "You unlocked the ancient vault, claimed its gold, and escaped into the forest. The temple torches fade behind you.");
            if (endingTitle != null) endingTitle.style.color = new StyleColor(new Color(.72f, .72f, .58f));
        }

        public void HideEnding()
        {
            if (endingOverlay != null) endingOverlay.style.display = DisplayStyle.None;
        }

        private IEnumerator HideNotificationAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (notificationLabel != null) notificationLabel.style.display = DisplayStyle.None;
            hideNotification = null;
        }
    }
}

