using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// Manages the initial 3, 2, 1... GO! countdown sequence with a slow expanding iris circle dark overlay transition and vintage typography.
    /// Locks player controls during countdown and broadcasts events when finished.
    /// </summary>
    public class CountdownManager : MonoBehaviour
    {
        public static CountdownManager Instance { get; private set; }

        [Header("Countdown Settings")]
        [Tooltip("Number of seconds to count down from (e.g. 3)")]
        public int countdownFrom = 3;

        [Tooltip("Delay before starting countdown sequence")]
        public float startDelay = 0.3f;

        [Tooltip("Text displayed when countdown reaches zero")]
        public string goText = "¡GO!";

        [Tooltip("How long the GO text stays on screen before hiding")]
        public float goDisplayDuration = 0.9f;

        [Header("Font & Sizing Settings")]
        [Tooltip("Custom TextMeshPro Font Asset for vintage look")]
        public TMP_FontAsset vintageFontAsset;

        [Tooltip("Custom Legacy Font for fallback text")]
        public Font vintageLegacyFont;

        [Tooltip("Font size for 3, 2, 1 numbers")]
        public float numberFontSize = 130f;

        [Tooltip("Font size for GO! text (smaller than numbers)")]
        public float goFontSize = 85f;

        [Header("UI References")]
        [Tooltip("Optional TextMeshPro text component. Created automatically if null.")]
        public TextMeshProUGUI tmpText;

        [Tooltip("Optional legacy UI Text fallback if TextMeshPro is not used.")]
        public Text legacyText;

        [Tooltip("Dark overlay image for expanding circle transition.")]
        public Image overlayImage;

        [Header("Animation & Iris Transition Settings")]
        public float popScaleMultiplier = 1.4f;
        public float popDuration = 0.4f;

        [Tooltip("Slower speed multiplier for the expanding circle transition (higher = slower opening)")]
        public float irisSlowFactor = 1.35f;

        [Tooltip("Dark overlay color and initial opacity")]
        public Color overlayColor = new Color(0.06f, 0.05f, 0.04f, 0.88f);

        [Header("Muted Vintage Aesthetic Palette")]
        [Tooltip("Muted vintage parchment cream for 3, 2, 1 numbers")]
        public Color numberTextColor = new Color(0.92f, 0.85f, 0.70f); // Vintage Parchment Cream (#EBE0B3)

        [Tooltip("Muted vintage antique gold/ochre for ¡GO! (desaturated, warm)")]
        public Color goTextColor = new Color(0.78f, 0.60f, 0.38f); // Antique Muted Gold (#C79960)

        [Tooltip("Deep sepia charcoal outline color")]
        public Color outlineColor = new Color(0.12f, 0.09f, 0.07f, 1f); // Deep Sepia Charcoal (#1F1712)

        // Events
        public static event Action OnCountdownStarted;
        public static event Action<int> OnCountdownTick;
        public static event Action OnCountdownFinished;

        public bool IsCountingDown { get; private set; } = true;
        public bool HasFinished { get; private set; } = false;

        private RectTransform textRectTransform;
        private Material irisMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null && FindAnyObjectByType<CountdownManager>() == null)
            {
                GameObject go = new GameObject("CountdownManager");
                go.AddComponent<CountdownManager>();
            }
        }

        private AudioSource countdownAudioSource;
        private AudioClip tickClip;
        private AudioClip goClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            countdownAudioSource = gameObject.AddComponent<AudioSource>();
            countdownAudioSource.playOnAwake = false;
            countdownAudioSource.spatialBlend = 0f;

            tickClip = GenerateTickClip();
            goClip = GenerateGoClip();
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            IsCountingDown = true;
            HasFinished = false;
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
            EnsureUIReferences();
            IsCountingDown = true;
            HasFinished = false;
        }

        /// <summary>
        /// Starts the 3.. 2.. 1.. GO! countdown sequence when the player clicks Ready.
        /// </summary>
        public void StartCountdown()
        {
            StopAllCoroutines();
            EnsureUIReferences();
            StartCoroutine(RunCountdownRoutine());
        }

        /// <summary>
        /// Ensures Canvas, Overlay Image with Iris Material, and Text component exist with vintage font styling.
        /// </summary>
        private void EnsureUIReferences()
        {
            GameObject canvasObj = GameObject.Find("CountdownCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("CountdownCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            DontDestroyOnLoad(canvasObj);

            // Create Dark Overlay Image for expanding circle transition if not assigned
            if (overlayImage == null)
            {
                Transform foundOverlay = canvasObj.transform.Find("CountdownOverlay");
                if (foundOverlay != null)
                {
                    overlayImage = foundOverlay.GetComponent<Image>();
                }
                else
                {
                    GameObject overlayObj = new GameObject("CountdownOverlay");
                    overlayObj.transform.SetParent(canvasObj.transform, false);
                    overlayObj.transform.SetAsFirstSibling(); // Behind text

                    overlayImage = overlayObj.AddComponent<Image>();
                    RectTransform overlayRect = overlayImage.rectTransform;
                    overlayRect.anchorMin = Vector2.zero;
                    overlayRect.anchorMax = Vector2.one;
                    overlayRect.offsetMin = Vector2.zero;
                    overlayRect.offsetMax = Vector2.one;
                }
            }

            // Setup Iris Transition Material
            Shader irisShader = Shader.Find("UI/IrisTransition");
            if (irisShader != null)
            {
                irisMaterial = new Material(irisShader);
                irisMaterial.SetColor("_OverlayColor", overlayColor);
                irisMaterial.SetFloat("_Progress", 0f);
                float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1.777f;
                irisMaterial.SetFloat("_AspectRatio", aspect);
                overlayImage.material = irisMaterial;
                overlayImage.color = Color.white;
            }
            if (overlayImage != null)
            {
                overlayImage.gameObject.SetActive(false);
            }

            // Setup Text Component
            if (tmpText != null)
            {
                textRectTransform = tmpText.rectTransform;
                ApplyVintageFontStyle(tmpText);
                return;
            }

            if (legacyText != null)
            {
                textRectTransform = legacyText.rectTransform;
                ApplyVintageLegacyFontStyle(legacyText);
                return;
            }

            tmpText = canvasObj.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
            {
                textRectTransform = tmpText.rectTransform;
                ApplyVintageFontStyle(tmpText);
                return;
            }

            legacyText = canvasObj.GetComponentInChildren<Text>();
            if (legacyText != null)
            {
                textRectTransform = legacyText.rectTransform;
                ApplyVintageLegacyFontStyle(legacyText);
                return;
            }

            // Create new Text object
            GameObject textObj = new GameObject("CountdownText");
            textObj.transform.SetParent(canvasObj.transform, false);

            bool tmpSuccess = false;
            try
            {
                tmpText = textObj.AddComponent<TextMeshProUGUI>();
                if (tmpText != null)
                {
                    ApplyVintageFontStyle(tmpText);
                    textRectTransform = tmpText.rectTransform;
                    tmpSuccess = true;
                }
            }
            catch (Exception)
            {
                tmpSuccess = false;
            }

            if (!tmpSuccess || tmpText == null || tmpText.font == null)
            {
                if (tmpText != null)
                {
                    DestroyImmediate(tmpText);
                    tmpText = null;
                }
                legacyText = textObj.AddComponent<Text>();
                ApplyVintageLegacyFontStyle(legacyText);
                textRectTransform = legacyText.rectTransform;
            }

            textRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            textRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            textRectTransform.pivot = new Vector2(0.5f, 0.5f);
            textRectTransform.anchoredPosition = Vector2.zero;
            textRectTransform.sizeDelta = new Vector2(600f, 300f);
        }

        private void ApplyVintageFontStyle(TextMeshProUGUI tmp)
        {
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = numberFontSize;
            tmp.fontStyle = FontStyles.Bold | FontStyles.Italic;
            tmp.color = numberTextColor;
            tmp.outlineWidth = 0.28f;
            tmp.outlineColor = outlineColor;

            if (vintageFontAsset != null)
            {
                tmp.font = vintageFontAsset;
            }
        }

        private void ApplyVintageLegacyFontStyle(Text txt)
        {
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = Mathf.RoundToInt(numberFontSize);
            txt.fontStyle = FontStyle.BoldAndItalic;
            txt.color = numberTextColor;

            if (vintageLegacyFont == null)
            {
                vintageLegacyFont = Font.CreateDynamicFontFromOSFont("Georgia", Mathf.RoundToInt(numberFontSize));
                if (vintageLegacyFont == null)
                {
                    vintageLegacyFont = Font.CreateDynamicFontFromOSFont("Times New Roman", Mathf.RoundToInt(numberFontSize));
                }
                if (vintageLegacyFont == null)
                {
                    vintageLegacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
            }
            if (vintageLegacyFont != null)
            {
                txt.font = vintageLegacyFont;
            }

            Outline outline = txt.GetComponent<Outline>();
            if (outline == null)
            {
                outline = txt.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(3f, -3f);
        }

        /// <summary>
        /// Coroutine driving the 3.. 2.. 1.. GO! visual sequence and slow iris transition.
        /// </summary>
        private IEnumerator RunCountdownRoutine()
        {
            IsCountingDown = true;
            HasFinished = false;
            OnCountdownStarted?.Invoke();

            SetTextVisible(false);

            float totalCountdownTime = (countdownFrom * 1.0f) + startDelay;
            Coroutine irisCoroutine = StartCoroutine(AnimateIrisTransitionRoutine(totalCountdownTime));

            if (startDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(startDelay);
            }

            SetTextVisible(true);

            // Count down loop
            for (int current = countdownFrom; current > 0; current--)
            {
                UpdateText(current.ToString(), isGo: false);
                if (countdownAudioSource != null && tickClip != null) countdownAudioSource.PlayOneShot(tickClip, 0.65f);
                OnCountdownTick?.Invoke(current);
                yield return StartCoroutine(AnimatePopScale(isGo: false));
                yield return new WaitForSecondsRealtime(Mathf.Max(0f, 1.0f - popDuration));
            }

            // GO! state
            UpdateText(goText, isGo: true);
            if (countdownAudioSource != null && goClip != null) countdownAudioSource.PlayOneShot(goClip, 0.85f);
            IsCountingDown = false;
            HasFinished = true;
            OnCountdownFinished?.Invoke();

            yield return StartCoroutine(AnimatePopScale(isGo: true));

            if (irisCoroutine != null)
            {
                StopCoroutine(irisCoroutine);
            }
            StartCoroutine(FadeOutOverlayRoutine());

            yield return new WaitForSecondsRealtime(goDisplayDuration);

            SetTextVisible(false);
        }

        /// <summary>
        /// Animates the circular iris mask expansion gradually and smoothly during the countdown.
        /// </summary>
        private IEnumerator AnimateIrisTransitionRoutine(float duration)
        {
            if (overlayImage == null) yield break;

            overlayImage.gameObject.SetActive(true);
            float elapsed = 0f;
            // Slower transition speed so the opening feels deliberate and atmospheric
            float transitionDuration = duration * Mathf.Max(1f, irisSlowFactor);

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);

                // Gradual ease-in-out curve for a slow initial opening that expands gracefully
                float smoothT = Mathf.Pow(t, 1.7f);
                float progress = Mathf.Lerp(0f, 1.35f, smoothT);

                if (irisMaterial != null)
                {
                    irisMaterial.SetFloat("_Progress", progress);
                    float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1.777f;
                    irisMaterial.SetFloat("_AspectRatio", aspect);
                }
                else
                {
                    Color c = overlayColor;
                    c.a = Mathf.Lerp(overlayColor.a, 0f, smoothT);
                    overlayImage.color = c;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Final smooth fade out of overlay when GO! completes.
        /// </summary>
        private IEnumerator FadeOutOverlayRoutine()
        {
            if (overlayImage == null) yield break;

            float elapsed = 0f;
            float fadeDuration = 0.4f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

                if (irisMaterial != null)
                {
                    Color c = overlayColor;
                    c.a *= alpha;
                    irisMaterial.SetColor("_OverlayColor", c);
                }
                else
                {
                    Color c = overlayImage.color;
                    c.a = alpha * overlayColor.a;
                    overlayImage.color = c;
                }

                yield return null;
            }

            overlayImage.gameObject.SetActive(false);
        }

        private void UpdateText(string value, bool isGo = false)
        {
            Color targetColor = isGo ? goTextColor : numberTextColor;
            float fontSize = isGo ? goFontSize : numberFontSize;

            if (tmpText != null)
            {
                tmpText.text = value;
                tmpText.fontSize = fontSize;
                tmpText.color = targetColor;
                tmpText.outlineColor = outlineColor;
                tmpText.outlineWidth = 0.28f;
                if (vintageFontAsset != null)
                {
                    tmpText.font = vintageFontAsset;
                }
            }
            else if (legacyText != null)
            {
                legacyText.text = value;
                legacyText.fontSize = Mathf.RoundToInt(fontSize);
                legacyText.color = targetColor;
                if (vintageLegacyFont != null)
                {
                    legacyText.font = vintageLegacyFont;
                }

                Outline outline = legacyText.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = legacyText.gameObject.AddComponent<Outline>();
                }
                outline.effectColor = outlineColor;
                outline.effectDistance = new Vector2(3f, -3f);
            }
        }

        private void SetTextVisible(bool visible)
        {
            if (tmpText != null) tmpText.gameObject.SetActive(visible);
            if (legacyText != null) legacyText.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Animates scale pop effect when text changes.
        /// </summary>
        private IEnumerator AnimatePopScale(bool isGo = false)
        {
            if (textRectTransform == null) yield break;

            float elapsed = 0f;
            float activePopMultiplier = isGo ? (1f + (popScaleMultiplier - 1f) * 0.65f) : popScaleMultiplier;

            Vector3 targetScale = Vector3.one;
            Vector3 startScale = Vector3.one * activePopMultiplier;

            while (elapsed < popDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popDuration);
                t = Mathf.Sin(t * Mathf.PI * 0.5f);
                textRectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            textRectTransform.localScale = Vector3.one;
        }

        /// <summary>
        /// Generates a retro warm woodblock tick sound clip for 3.. 2.. 1..
        /// </summary>
        private static AudioClip GenerateTickClip()
        {
            int sampleRate = 44100;
            float duration = 0.08f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // 480Hz woodblock tone with quick decay
                float wave = Mathf.Sin(2f * Mathf.PI * 480f * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * 960f * t) * 0.3f;
                float envelope = Mathf.Pow(1f - progress, 2.5f);

                samples[i] = wave * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create("CountdownTickSynth", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generates a triumphant 2-tone chord chime sound clip for GO!
        /// </summary>
        private static AudioClip GenerateGoClip()
        {
            int sampleRate = 44100;
            float duration = 0.22f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // 880Hz + 1320Hz chord chime (A5 + E6)
                float wave = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.5f;
                float envelope = (progress < 0.1f) ? (progress / 0.1f) : Mathf.Pow(1f - progress, 1.6f);

                samples[i] = wave * envelope * 0.6f;
            }

            AudioClip clip = AudioClip.Create("CountdownGoSynth", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
