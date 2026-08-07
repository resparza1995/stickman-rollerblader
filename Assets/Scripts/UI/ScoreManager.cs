using System;
using UnityEngine;
using TricksSystem;

namespace UISystem
{
    /// <summary>
    /// Singleton manager that tracks total score, trick combo multiplier, and score events.
    /// Auto-initializes on scene load if not present.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Score Settings")]
        public int totalScore = 0;
        public int currentMultiplier = 1;
        public float comboTimeout = 2.5f;

        // Events
        public static event Action<int> OnTotalScoreChanged;
        public static event Action<string, int, int> OnTrickScored; // trickName, points, multiplier

        private float comboTimer = 0f;
        private bool isComboActive = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null && FindAnyObjectByType<ScoreManager>() == null)
            {
                GameObject go = new GameObject("ScoreManager");
                go.AddComponent<ScoreManager>();
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
            ResetScore();
            TrickController controller = FindAnyObjectByType<TrickController>();
            if (controller != null)
            {
                controller.OnTrickExecuted -= HandleTrickExecuted;
                controller.OnTrickExecuted += HandleTrickExecuted;
            }
        }

        private void Start()
        {
            // Subscribe to TrickController if active in scene
            TrickController controller = FindAnyObjectByType<TrickController>();
            if (controller != null)
            {
                controller.OnTrickExecuted -= HandleTrickExecuted;
                controller.OnTrickExecuted += HandleTrickExecuted;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            TrickController controller = FindAnyObjectByType<TrickController>();
            if (controller != null)
            {
                controller.OnTrickExecuted -= HandleTrickExecuted;
            }
        }

        private void Update()
        {
            if (isComboActive)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0f)
                {
                    ResetCombo();
                }
            }
        }

        private void HandleTrickExecuted(TrickData trickData)
        {
            if (trickData == null) return;
            AddScore(trickData.trickName, trickData.scorePoints);
        }

        public System.Collections.Generic.Dictionary<string, int> trickCounts = new System.Collections.Generic.Dictionary<string, int>();

        /// <summary>
        /// Adds a trick score, updates total score and multiplier, and triggers UI updates.
        /// </summary>
        public void AddScore(string trickName, int basePoints)
        {
            if (basePoints <= 0) basePoints = 100;

            if (!trickCounts.ContainsKey(trickName))
            {
                trickCounts[trickName] = 0;
            }
            trickCounts[trickName]++;

            if (isComboActive)
            {
                currentMultiplier++;
            }
            else
            {
                isComboActive = true;
                currentMultiplier = 1;
            }

            comboTimer = comboTimeout;
            int earnedPoints = basePoints * currentMultiplier;
            totalScore += earnedPoints;

            OnTotalScoreChanged?.Invoke(totalScore);
            OnTrickScored?.Invoke(trickName, basePoints, currentMultiplier);
        }

        /// <summary>
        /// Resets the active trick combo multiplier back to 1.
        /// </summary>
        public void ResetCombo()
        {
            isComboActive = false;
            currentMultiplier = 1;
        }

        /// <summary>
        /// Resets total score, trick counts, and combo.
        /// </summary>
        public void ResetScore()
        {
            totalScore = 0;
            trickCounts.Clear();
            ResetCombo();
            OnTotalScoreChanged?.Invoke(totalScore);
        }
    }
}
