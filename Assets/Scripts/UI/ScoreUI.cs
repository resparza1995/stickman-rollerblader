using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// UI Controller for top-left total score HUD, top-center match timer (00:30 -> 00:00),
    /// dynamic trick score popups, and end-of-match summary panel with trick breakdown and Retry button.
    /// Auto-initializes Canvas and UI elements if missing.
    /// </summary>
    public class ScoreUI : MonoBehaviour
    {
        public static ScoreUI Instance { get; private set; }

        [Header("Match Timer Settings")]
        public float matchDuration = 30.0f; // 30-second match timer

        [Header("Top Left Score HUD")]
        public TextMeshProUGUI totalScoreTMP;
        public Text totalScoreLegacy;

        [Header("Trick Popup HUD")]
        public RectTransform trickCardRect;
        public Image trickCardBackground;
        public TextMeshProUGUI trickPointsTMP;
        public Text trickPointsLegacy;
        public TextMeshProUGUI trickNameTMP;
        public Text trickNameLegacy;

        [Header("Font Settings")]
        public TMP_FontAsset customFont;

        [Header("Popup Settings")]
        public float popupDuration = 2.0f;
        public float popScale = 1.25f;

        private Canvas mainCanvas;
        private Coroutine popupCoroutine;
        private CanvasGroup cardCanvasGroup;

        // HUD Containers
        private GameObject scoreHUDContainer;
        private GameObject timerHUDContainer;
        private GameObject muteHUDContainer;
        private TextMeshProUGUI matchTimerTMP;
        private TextMeshProUGUI muteTextTMP;
        private Coroutine matchTimerCoroutine;
        private bool isMuted = false;

        // End Game Summary Panel
        private GameObject summaryPanelObj;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null && FindAnyObjectByType<ScoreUI>() == null)
            {
                GameObject go = new GameObject("ScoreUI");
                go.AddComponent<ScoreUI>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildUIIfNeeded();
        }

        private void OnEnable()
        {
            ScoreManager.OnTotalScoreChanged += UpdateTotalScoreDisplay;
            ScoreManager.OnTrickScored += DisplayTrickPopup;
            CountdownManager.OnCountdownFinished += HandleCountdownFinished;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            ScoreManager.OnTotalScoreChanged -= UpdateTotalScoreDisplay;
            ScoreManager.OnTrickScored -= DisplayTrickPopup;
            CountdownManager.OnCountdownFinished -= HandleCountdownFinished;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResetUI();
        }

        /// <summary>
        /// Resets the UI state, destroys summary panel, and hides HUD containers until countdown finished.
        /// </summary>
        public void ResetUI()
        {
            if (popupCoroutine != null) StopCoroutine(popupCoroutine);
            if (matchTimerCoroutine != null) StopCoroutine(matchTimerCoroutine);

            if (summaryPanelObj != null)
            {
                Destroy(summaryPanelObj);
                summaryPanelObj = null;
            }

            if (scoreHUDContainer != null) scoreHUDContainer.SetActive(false);
            if (timerHUDContainer != null) timerHUDContainer.SetActive(false);
            if (muteHUDContainer != null) muteHUDContainer.SetActive(false);
            if (trickCardRect != null) trickCardRect.gameObject.SetActive(false);
            if (matchTimerTMP != null) matchTimerTMP.text = "00:30";

            EnsureEventSystem();
            UpdateTotalScoreDisplay(0);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            EnsureEventSystem();

            if (ScoreManager.Instance != null)
            {
                UpdateTotalScoreDisplay(ScoreManager.Instance.totalScore);
            }
            else
            {
                UpdateTotalScoreDisplay(0);
            }

            if (trickCardRect != null)
            {
                trickCardRect.gameObject.SetActive(false);
            }

            // Initially hide Score, Timer, and Mute HUD until countdown finishes
            if (CountdownManager.Instance != null && CountdownManager.Instance.IsCountingDown)
            {
                if (scoreHUDContainer != null) scoreHUDContainer.SetActive(false);
                if (timerHUDContainer != null) timerHUDContainer.SetActive(false);
                if (muteHUDContainer != null) muteHUDContainer.SetActive(false);
            }
            else
            {
                // Countdown already done or not present
                HandleCountdownFinished();
            }
        }

        /// <summary>
        /// Ensures an EventSystem exists in the scene using the new InputSystemUIInputModule.
        /// </summary>
        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();

                try
                {
                    eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
                catch
                {
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }
        }

        /// <summary>
        /// Toggles master audio volume mute state and updates MUTE button text.
        /// </summary>
        public void ToggleMute()
        {
            isMuted = !isMuted;
            AudioListener.volume = isMuted ? 0f : 1f;

            if (muteTextTMP != null)
            {
                muteTextTMP.text = isMuted ? "<s>MUTE</s>" : "MUTE";
                muteTextTMP.color = isMuted ? new Color(0.88f, 0.40f, 0.35f) : new Color(0.96f, 0.90f, 0.75f);
            }
        }

        /// <summary>
        /// Starts the HUD display and 30s match timer when countdown reaches GO!
        /// </summary>
        private void HandleCountdownFinished()
        {
            if (scoreHUDContainer != null) scoreHUDContainer.SetActive(true);
            if (timerHUDContainer != null) timerHUDContainer.SetActive(true);
            if (muteHUDContainer != null) muteHUDContainer.SetActive(true);

            if (matchTimerCoroutine != null) StopCoroutine(matchTimerCoroutine);
            matchTimerCoroutine = StartCoroutine(RunMatchTimerRoutine());
        }

        /// <summary>
        /// Decrements the 30-second match timer (00:30 -> 00:00) and triggers summary panel.
        /// </summary>
        private IEnumerator RunMatchTimerRoutine()
        {
            float timeLeft = matchDuration;

            while (timeLeft > 0f)
            {
                int seconds = Mathf.CeilToInt(timeLeft);
                int mins = seconds / 60;
                int secs = seconds % 60;
                string formattedTime = $"{mins:D2}:{secs:D2}";

                if (matchTimerTMP != null) matchTimerTMP.text = formattedTime;

                timeLeft -= Time.deltaTime;
                yield return null;
            }

            if (matchTimerTMP != null) matchTimerTMP.text = "00:00";

            // End of match: show translucent summary panel
            ShowSummaryPanel();
        }

        /// <summary>
        /// Dynamically builds canvas and HUD text components matching the reference style if not already linked.
        /// </summary>
        private void BuildUIIfNeeded()
        {
            GameObject canvasObj = GameObject.Find("ScoreCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("ScoreCanvas");
                mainCanvas = canvasObj.AddComponent<Canvas>();
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                mainCanvas.sortingOrder = 90;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                mainCanvas = canvasObj.GetComponent<Canvas>();
            }
            if (canvasObj != null) DontDestroyOnLoad(canvasObj);

            // Load default TMP Font if not assigned
            if (customFont == null)
            {
                customFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }

            // 1. Top Left SCORE HUD Container
            if (scoreHUDContainer == null)
            {
                scoreHUDContainer = new GameObject("TopLeftScoreHUD");
                scoreHUDContainer.transform.SetParent(mainCanvas.transform, false);

                RectTransform rect = scoreHUDContainer.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(15, -12);
                rect.sizeDelta = new Vector2(250, 40);

                totalScoreTMP = scoreHUDContainer.AddComponent<TextMeshProUGUI>();
                totalScoreTMP.alignment = TextAlignmentOptions.Left;
                totalScoreTMP.fontSize = 20;
                totalScoreTMP.fontStyle = FontStyles.Bold | FontStyles.Italic;
                totalScoreTMP.color = new Color(0.96f, 0.90f, 0.75f); // Vintage cream parchment
                totalScoreTMP.text = "SCORE: 0";

                if (customFont != null) totalScoreTMP.font = customFont;
            }

            // 2. Top Center MATCH TIMER HUD (00:30)
            if (timerHUDContainer == null)
            {
                timerHUDContainer = new GameObject("TopCenterTimerHUD");
                timerHUDContainer.transform.SetParent(mainCanvas.transform, false);

                RectTransform rect = timerHUDContainer.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0, -15);
                rect.sizeDelta = new Vector2(150, 40);

                matchTimerTMP = timerHUDContainer.AddComponent<TextMeshProUGUI>();
                matchTimerTMP.alignment = TextAlignmentOptions.Center;
                matchTimerTMP.fontSize = 22;
                matchTimerTMP.fontStyle = FontStyles.Bold | FontStyles.Italic;
                matchTimerTMP.color = new Color(0.96f, 0.90f, 0.75f); // Vintage cream
                matchTimerTMP.text = "00:30";
                matchTimerTMP.outlineWidth = 0.2f;
                matchTimerTMP.outlineColor = new Color(0.08f, 0.06f, 0.04f, 0.9f);

                if (customFont != null) matchTimerTMP.font = customFont;
            }

            // 3. Top Right MUTE HUD Button
            if (muteHUDContainer == null)
            {
                muteHUDContainer = new GameObject("TopRightMuteHUD");
                muteHUDContainer.transform.SetParent(mainCanvas.transform, false);

                RectTransform rect = muteHUDContainer.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(1, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(1, 1);
                rect.anchoredPosition = new Vector2(-15, -12);
                rect.sizeDelta = new Vector2(76, 32);

                Image btnImg = muteHUDContainer.AddComponent<Image>();
                btnImg.sprite = CreateRoundedSprite(76, 32, 10);
                btnImg.color = new Color(0.16f, 0.13f, 0.10f, 0.90f);

                Outline outline = muteHUDContainer.AddComponent<Outline>();
                outline.effectColor = new Color(0.78f, 0.60f, 0.38f, 0.85f);
                outline.effectDistance = new Vector2(1.2f, -1.2f);

                Button btn = muteHUDContainer.AddComponent<Button>();
                ColorBlock cb = btn.colors;
                cb.normalColor = new Color(0.16f, 0.13f, 0.10f, 0.90f);
                cb.highlightedColor = new Color(0.28f, 0.22f, 0.16f, 0.95f);
                cb.pressedColor = new Color(0.10f, 0.08f, 0.06f, 0.95f);
                btn.colors = cb;
                btn.onClick.AddListener(ToggleMute);

                GameObject txtObj = new GameObject("MuteText");
                txtObj.transform.SetParent(muteHUDContainer.transform, false);
                RectTransform txtRect = txtObj.AddComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.offsetMin = Vector2.zero;
                txtRect.offsetMax = Vector2.zero;

                muteTextTMP = txtObj.AddComponent<TextMeshProUGUI>();
                muteTextTMP.alignment = TextAlignmentOptions.Center;
                muteTextTMP.fontSize = 13;
                muteTextTMP.fontStyle = FontStyles.Bold;
                muteTextTMP.color = isMuted ? new Color(0.88f, 0.40f, 0.35f) : new Color(0.96f, 0.90f, 0.75f);
                muteTextTMP.text = isMuted ? "<s>MUTE</s>" : "MUTE";
                if (customFont != null) muteTextTMP.font = customFont;
            }

            // 3. Dynamic Trick Popup Container (Behind Skater)
            if (trickCardRect == null)
            {
                GameObject popupObj = new GameObject("TrickScoreContainer");
                popupObj.transform.SetParent(mainCanvas.transform, false);

                trickCardRect = popupObj.AddComponent<RectTransform>();
                trickCardRect.anchorMin = Vector2.zero;
                trickCardRect.anchorMax = Vector2.zero;
                trickCardRect.pivot = new Vector2(0.5f, 0.5f);
                trickCardRect.anchoredPosition = Vector2.zero;
                trickCardRect.sizeDelta = new Vector2(300, 70);

                cardCanvasGroup = popupObj.AddComponent<CanvasGroup>();

                // Points Text (Gold/Yellow, Bold Italic, smaller size with drop shadow)
                GameObject pointsObj = new GameObject("PointsText");
                pointsObj.transform.SetParent(popupObj.transform, false);
                RectTransform pRect = pointsObj.AddComponent<RectTransform>();
                pRect.anchorMin = new Vector2(0, 0.45f);
                pRect.anchorMax = new Vector2(1, 1f);
                pRect.anchoredPosition = Vector2.zero;
                pRect.sizeDelta = Vector2.zero;

                trickPointsTMP = pointsObj.AddComponent<TextMeshProUGUI>();
                trickPointsTMP.alignment = TextAlignmentOptions.Center;
                trickPointsTMP.fontSize = 28;
                trickPointsTMP.fontStyle = FontStyles.Bold | FontStyles.Italic;
                trickPointsTMP.color = new Color(1.0f, 0.76f, 0.0f); // Bright Gold/Yellow
                trickPointsTMP.text = "+450";
                trickPointsTMP.outlineWidth = 0.25f;
                trickPointsTMP.outlineColor = new Color(0.08f, 0.06f, 0.04f, 0.95f);
                if (customFont != null) trickPointsTMP.font = customFont;

                // Trick Name Text (White, Italic, smaller size with drop shadow)
                GameObject nameObj = new GameObject("NameText");
                nameObj.transform.SetParent(popupObj.transform, false);
                RectTransform nRect = nameObj.AddComponent<RectTransform>();
                nRect.anchorMin = new Vector2(0, 0f);
                nRect.anchorMax = new Vector2(1, 0.5f);
                nRect.anchoredPosition = Vector2.zero;
                nRect.sizeDelta = Vector2.zero;

                trickNameTMP = nameObj.AddComponent<TextMeshProUGUI>();
                trickNameTMP.alignment = TextAlignmentOptions.Center;
                trickNameTMP.fontSize = 20;
                trickNameTMP.fontStyle = FontStyles.Italic;
                trickNameTMP.color = new Color(0f, 0f, 0f);
                trickNameTMP.outlineWidth = 0.25f;
                trickNameTMP.outlineColor = new Color(0.08f, 0.06f, 0.04f, 0.95f);
                if (customFont != null) trickNameTMP.font = customFont;
            }
            else
            {
                cardCanvasGroup = trickCardRect.GetComponent<CanvasGroup>();
                if (cardCanvasGroup == null) cardCanvasGroup = trickCardRect.gameObject.AddComponent<CanvasGroup>();
            }
        }

        /// <summary>
        /// Updates the top-left score HUD text.
        /// </summary>
        public void UpdateTotalScoreDisplay(int newScore)
        {
            string formatted = $"SCORE: {newScore:N0}";
            if (totalScoreTMP != null) totalScoreTMP.text = formatted;
            if (totalScoreLegacy != null) totalScoreLegacy.text = formatted;
        }

        /// <summary>
        /// Displays the trick points popup matching the requested aesthetic.
        /// </summary>
        public void DisplayTrickPopup(string trickName, int points, int multiplier)
        {
            string pointsText = $"+{points}";

            if (trickPointsTMP != null) trickPointsTMP.text = pointsText;
            if (trickPointsLegacy != null) trickPointsLegacy.text = pointsText;

            if (trickNameTMP != null) trickNameTMP.text = trickName;
            if (trickNameLegacy != null) trickNameLegacy.text = trickName;

            if (trickCardRect != null)
            {
                if (popupCoroutine != null) StopCoroutine(popupCoroutine);
                popupCoroutine = StartCoroutine(AnimateTrickCard());
            }
        }

        // Cache references
        private PlayerMovement cachedPlayer;
        private Camera cachedMainCam;
        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Animates the pop-in emergence from bottom to top behind the skater, then floating away in opposite movement direction.
        /// </summary>
        private IEnumerator AnimateTrickCard()
        {
            trickCardRect.gameObject.SetActive(true);
            if (cardCanvasGroup == null) cardCanvasGroup = trickCardRect.GetComponent<CanvasGroup>();

            if (cachedPlayer == null)
            {
                cachedPlayer = FindAnyObjectByType<PlayerMovement>();
            }

            if (cachedMainCam == null || !cachedMainCam.isActiveAndEnabled)
            {
                cachedMainCam = Camera.main;
            }

            Vector2 startScreenPos;
            float moveDir = 1f;

            if (cachedPlayer != null && cachedMainCam != null)
            {
                Vector3 playerWorldPos = cachedPlayer.transform.position;
                startScreenPos = cachedMainCam.WorldToScreenPoint(playerWorldPos);
                moveDir = (cachedPlayer.horizontalMovement != 0f) ? cachedPlayer.horizontalMovement : (cachedPlayer.transform.localScale.x > 0 ? 1f : -1f);
            }
            else
            {
                startScreenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.4f);
            }

            // Offset behind skater (Opposite of movement/facing direction)
            float behindOffsetX = (moveDir >= 0f) ? -80f : 80f;
            float driftDirectionX = (moveDir >= 0f) ? -1f : 1f;

            Vector2 targetBasePos = startScreenPos + new Vector2(behindOffsetX, 50f);
            Vector2 initialRisingPos = targetBasePos + new Vector2(0f, -40f);

            trickCardRect.position = initialRisingPos;
            trickCardRect.localScale = Vector3.one * 0.4f;
            cardCanvasGroup.alpha = 0f;

            // 1. Emergence: Bottom-to-top rising pop-in
            float elapsed = 0f;
            float emergeTime = 0.22f;
            while (elapsed < emergeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / emergeTime;
                trickCardRect.position = Vector2.Lerp(initialRisingPos, targetBasePos, t);
                trickCardRect.localScale = Vector3.Lerp(Vector3.one * 0.4f, Vector3.one, t);
                cardCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            trickCardRect.position = targetBasePos;
            trickCardRect.localScale = Vector3.one;
            cardCanvasGroup.alpha = 1f;

            // 2. Main display & gradual drift
            float holdTimer = 0.5f;
            Vector2 currentPos = targetBasePos;
            float floatUpSpeed = 50f;
            float driftAwaySpeed = 75f;

            while (holdTimer > 0f)
            {
                holdTimer -= Time.deltaTime;
                currentPos += new Vector2(driftDirectionX * driftAwaySpeed * 0.4f, floatUpSpeed * 0.5f) * Time.deltaTime;
                trickCardRect.position = currentPos;
                yield return null;
            }

            // 3. Fade out & Float away in opposite direction
            elapsed = 0f;
            float fadeTime = 0.45f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeTime;
                currentPos += new Vector2(driftDirectionX * driftAwaySpeed, floatUpSpeed) * Time.deltaTime;
                trickCardRect.position = currentPos;
                cardCanvasGroup.alpha = 1f - t;
                yield return null;
            }

            trickCardRect.gameObject.SetActive(false);
            popupCoroutine = null;
        }

        /// <summary>
        /// Generates a procedural rounded rectangle sprite at runtime with caching to prevent texture memory leaks.
        /// </summary>
        private Sprite CreateRoundedSprite(int width, int height, int radius)
        {
            string key = $"{width}_{height}_{radius}";
            if (spriteCache.TryGetValue(key, out Sprite cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] colors = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = 0;
                    float dy = 0;

                    if (x < radius) dx = radius - x;
                    else if (x > width - radius) dx = x - (width - radius);

                    if (y < radius) dy = radius - y;
                    else if (y > height - radius) dy = y - (height - radius);

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > radius)
                    {
                        colors[y * width + x] = Color.clear;
                    }
                    else if (dist > radius - 1.2f)
                    {
                        float alpha = Mathf.Clamp01(radius - dist);
                        colors[y * width + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        colors[y * width + x] = Color.white;
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            spriteCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Displays translucent Game Over summary panel with total points, trick counts breakdown, and Retry button in English.
        /// </summary>
        public void ShowSummaryPanel()
        {
            EnsureEventSystem();

            if (summaryPanelObj != null)
            {
                summaryPanelObj.SetActive(true);
                return;
            }

            summaryPanelObj = new GameObject("GameOverSummaryPanel");
            summaryPanelObj.transform.SetParent(mainCanvas.transform, false);

            RectTransform panelRect = summaryPanelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Dark translucent overlay
            Image panelBG = summaryPanelObj.AddComponent<Image>();
            panelBG.color = new Color(0.06f, 0.05f, 0.04f, 0.88f); // Vintage sepia dark overlay
            panelBG.raycastTarget = true;

            // Outer Gold Border Frame with rounded corners
            GameObject borderObj = new GameObject("SummaryCardBorder");
            borderObj.transform.SetParent(summaryPanelObj.transform, false);
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0.5f, 0.5f);
            borderRect.anchorMax = new Vector2(0.5f, 0.5f);
            borderRect.pivot = new Vector2(0.5f, 0.5f);
            borderRect.anchoredPosition = Vector2.zero;
            borderRect.sizeDelta = new Vector2(456, 506);

            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.sprite = CreateRoundedSprite(456, 506, 24);
            borderImg.color = new Color(0.78f, 0.60f, 0.38f, 0.95f); // Antique Gold rounded border

            // Main Card Container (Nested inside borderObj for perfectly rounded outline)
            GameObject cardObj = new GameObject("SummaryCard");
            cardObj.transform.SetParent(borderObj.transform, false);
            RectTransform cardRect = cardObj.AddComponent<RectTransform>();
            cardRect.anchorMin = Vector2.zero;
            cardRect.anchorMax = Vector2.one;
            cardRect.offsetMin = new Vector2(3, 3);
            cardRect.offsetMax = new Vector2(-3, -3);

            Image cardBG = cardObj.AddComponent<Image>();
            cardBG.sprite = CreateRoundedSprite(450, 500, 22);
            cardBG.color = new Color(0.14f, 0.11f, 0.09f, 0.98f);

            // Rounded Header Box for "TIME'S UP!" Title (Strictly contained inside cardObj)
            GameObject titleBoxObj = new GameObject("TitleHeaderBox");
            titleBoxObj.transform.SetParent(cardObj.transform, false);
            RectTransform tbRect = titleBoxObj.AddComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0.10f, 0.86f);
            tbRect.anchorMax = new Vector2(0.90f, 0.96f);
            tbRect.offsetMin = Vector2.zero;
            tbRect.offsetMax = Vector2.zero;

            Image tbImg = titleBoxObj.AddComponent<Image>();
            tbImg.sprite = CreateRoundedSprite(360, 50, 14); // Rounded corners banner
            tbImg.color = new Color(0.38f, 0.28f, 0.18f, 0.90f); // Antique brown vintage header banner

            // Title Text inside the rounded header banner
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(titleBoxObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.fontSize = 26;
            titleTMP.fontStyle = FontStyles.Bold | FontStyles.Italic;
            titleTMP.color = new Color(0.96f, 0.90f, 0.78f); // Vintage Parchment Cream
            titleTMP.text = "TIME'S UP!";
            if (customFont != null) titleTMP.font = customFont;

            // Total Score Display (Positioned cleanly below the header banner)
            int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.totalScore : 0;
            GameObject scoreObj = new GameObject("FinalScoreText");
            scoreObj.transform.SetParent(cardObj.transform, false);
            RectTransform scoreRect = scoreObj.AddComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0, 0.74f);
            scoreRect.anchorMax = new Vector2(1, 0.84f);
            scoreRect.offsetMin = Vector2.zero;
            scoreRect.offsetMax = Vector2.zero;

            TextMeshProUGUI scoreTMP = scoreObj.AddComponent<TextMeshProUGUI>();
            scoreTMP.alignment = TextAlignmentOptions.Center;
            scoreTMP.fontSize = 23;
            scoreTMP.fontStyle = FontStyles.Bold | FontStyles.Italic;
            scoreTMP.color = new Color(1.0f, 0.76f, 0.0f); // Gold
            scoreTMP.text = $"TOTAL SCORE: {finalScore:N0}";
            if (customFont != null) scoreTMP.font = customFont;

            // Trick Breakdown List Container (Positioned comfortably in middle section)
            GameObject breakdownObj = new GameObject("TrickBreakdownText");
            breakdownObj.transform.SetParent(cardObj.transform, false);
            RectTransform bdRect = breakdownObj.AddComponent<RectTransform>();
            bdRect.anchorMin = new Vector2(0.08f, 0.18f);
            bdRect.anchorMax = new Vector2(0.92f, 0.68f);
            bdRect.offsetMin = Vector2.zero;
            bdRect.offsetMax = Vector2.zero;

            TextMeshProUGUI bdTMP = breakdownObj.AddComponent<TextMeshProUGUI>();
            bdTMP.alignment = TextAlignmentOptions.Top;
            bdTMP.fontSize = 18;
            bdTMP.fontStyle = FontStyles.Italic;
            bdTMP.color = new Color(0.94f, 0.90f, 0.84f);
            bdTMP.lineSpacing = 10f;

            string breakdownText = "<b>TRICKS PERFORMED</b>\n\n";
            if (ScoreManager.Instance != null && ScoreManager.Instance.trickCounts.Count > 0)
            {
                foreach (var kvp in ScoreManager.Instance.trickCounts)
                {
                    breakdownText += $"{kvp.Key}   —   x{kvp.Value}\n";
                }
            }
            else
            {
                breakdownText += "(No tricks performed)";
            }
            bdTMP.text = breakdownText;
            if (customFont != null) bdTMP.font = customFont;

            // Vintage Styled Rounded Retry Button
            GameObject btnObj = new GameObject("RetryButton");
            btnObj.transform.SetParent(cardObj.transform, false);
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.08f);
            btnRect.anchorMax = new Vector2(0.5f, 0.08f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = Vector2.zero;
            btnRect.sizeDelta = new Vector2(210, 50);

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = CreateRoundedSprite(210, 50, 16);
            btnImg.color = new Color(0.78f, 0.60f, 0.38f); // Antique Muted Gold (#C79960)
            btnImg.raycastTarget = true;

            Outline btnOutline = btnObj.AddComponent<Outline>();
            btnOutline.effectColor = new Color(0.12f, 0.09f, 0.07f, 1f); // Charcoal border
            btnOutline.effectDistance = new Vector2(1.5f, -1.5f);

            Button retryBtn = btnObj.AddComponent<Button>();
            ColorBlock colors = retryBtn.colors;
            colors.normalColor = new Color(0.78f, 0.60f, 0.38f);
            colors.highlightedColor = new Color(0.88f, 0.70f, 0.45f);
            colors.pressedColor = new Color(0.62f, 0.46f, 0.28f);
            retryBtn.colors = colors;

            retryBtn.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            });

            GameObject btnTextObj = new GameObject("RetryText");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform btnTextRect = btnTextObj.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI btnTMP = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnTMP.alignment = TextAlignmentOptions.Center;
            btnTMP.fontSize = 22;
            btnTMP.fontStyle = FontStyles.Bold | FontStyles.Italic;
            btnTMP.color = new Color(0.12f, 0.09f, 0.07f); // Deep Sepia Charcoal text (#1F1712)
            btnTMP.raycastTarget = false;
            btnTMP.text = "Retry!";
            if (customFont != null) btnTMP.font = customFont;
        }
    }
}
