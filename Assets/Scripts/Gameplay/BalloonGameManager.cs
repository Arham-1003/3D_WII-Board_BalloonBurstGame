using System.Collections.Generic;
using BalloonBalance.Logging;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BalloonBalance.Gameplay
{
    public sealed class BalloonGameManager : MonoBehaviour
    {
        private enum GameState { Ready, Countdown, Playing, Finished }
        public enum SequenceMode { FixedSeeded, Random }

        [Header("Scene references")]
        [SerializeField] private BalloonTarget balloonPrefab;
        [SerializeField] private BurstEffect burstPrefab;
        [SerializeField] private Transform balloonPoolRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text timerText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private TrialEventLogger eventLogger;
        [SerializeField] private TrialSampleLogger sampleLogger;

        [Header("Round")]
        [SerializeField, Min(5f)] private float roundDurationSeconds = 90f;
        [SerializeField, Min(1f)] private float countdownSeconds = 3f;
        [SerializeField] private Vector3 phaseSpawnIntervals = new Vector3(2.4f, 2.1f, 1.8f);
        [SerializeField] private Vector3 phaseSpeedMultipliers = new Vector3(0.85f, 1f, 1.1f);
        [SerializeField] private Vector2 horizontalRange = new Vector2(-1.55f, 1.55f);
        [SerializeField] private Vector2 verticalRange = new Vector2(0.75f, 2.55f);
        [SerializeField] private Vector2 depthRange = new Vector2(7f, 12f);
        [SerializeField] private Vector2 speedRange = new Vector2(0.7f, 1.15f);
        [SerializeField] private float missBoundaryZ = 2.75f;
        [SerializeField, Min(0.25f)] private float minimumActiveSpacing = 0.8f;
        [SerializeField, Min(0.25f)] private float maximumConsecutiveJump = 0.95f;
        [SerializeField, Range(1, 30)] private int spawnCandidateAttempts = 16;
        [SerializeField] private int poolSize = 18;
        [SerializeField] private bool useBoardFriendlyPacing = true;
        [SerializeField] private SequenceMode sequenceMode = SequenceMode.FixedSeeded;
        [SerializeField] private int[] fixedRoundSeeds = { 104729, 130363, 155921, 196613 };

        private readonly List<BalloonTarget> balloons = new List<BalloonTarget>();
        private readonly List<BurstEffect> bursts = new List<BurstEffect>();
        private readonly Color[] colors =
        {
            new Color(1f, 0.25f, 0.35f), new Color(1f, 0.72f, 0.12f),
            new Color(0.25f, 0.85f, 1f), new Color(0.65f, 0.35f, 1f),
            new Color(0.25f, 1f, 0.55f), new Color(1f, 0.35f, 0.8f)
        };

        private GameState state;
        private float remainingTime;
        private float spawnCountdown;
        private int score;
        private int misses;
        private int nextBurst;
        private System.Random random;
        private int practiceSeed;
        private int nextBalloonId;
        private float countdownRemaining;
        private Vector2 previousTargetPosition;
        private bool hasPreviousTarget;
        private int nextFixedSeed;

        public bool IsPlaying => state == GameState.Playing;
        public int Score => score;
        public int Misses => misses;
        public SequenceMode ActiveSequenceMode => sequenceMode;
        public int ActiveSeed => practiceSeed;
        public int CurrentPhase => state == GameState.Playing
            ? Mathf.Clamp(Mathf.FloorToInt((roundDurationSeconds - remainingTime) / 30f) + 1, 1, 3)
            : 0;

        private void Awake()
        {
            if (useBoardFriendlyPacing) ApplyBoardFriendlyPacing();
            startButton.onClick.AddListener(StartRound);
            restartButton.onClick.AddListener(StartRound);
            BuildPools();
            ShowReady();
        }

        private void ApplyBoardFriendlyPacing()
        {
            phaseSpawnIntervals = new Vector3(2.4f, 2.1f, 1.8f);
            phaseSpeedMultipliers = new Vector3(0.85f, 1f, 1.1f);
            speedRange = new Vector2(0.7f, 1.15f);
            maximumConsecutiveJump = 0.95f;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && state != GameState.Playing)
            {
                StartRound();
            }

            if (state == GameState.Countdown)
            {
                countdownRemaining = Mathf.Max(0f, countdownRemaining - Time.deltaTime);
                int count = Mathf.CeilToInt(countdownRemaining);
                statusText.text = count > 0 ? $"GET READY\n{count}" : "GO!";
                if (countdownRemaining <= 0f) BeginPlaying();
                return;
            }

            if (state != GameState.Playing) return;
            remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
            spawnCountdown -= Time.deltaTime;
            if (spawnCountdown <= 0f)
            {
                SpawnBalloon();
                spawnCountdown = GetPhaseValue(phaseSpawnIntervals);
            }

            UpdateHud();
            if (remainingTime <= 0f) FinishRound();
        }

        public void StartRound()
        {
            foreach (BalloonTarget balloon in balloons) balloon.ReturnToPool();
            practiceSeed = ChooseRoundSeed();
            random = new System.Random(practiceSeed);
            nextBalloonId = 0;
            hasPreviousTarget = false;
            state = GameState.Countdown;
            countdownRemaining = countdownSeconds;
            remainingTime = roundDurationSeconds;
            spawnCountdown = 0.35f;
            score = 0;
            misses = 0;
            titleText.text = "BALLOON BALANCE";
            statusText.text = $"GET READY\n{Mathf.CeilToInt(countdownRemaining)}";
            startButton.gameObject.SetActive(false);
            restartButton.gameObject.SetActive(false);
            UpdateHud();
        }

        public void SelectSequenceMode(int mode) =>
            SelectSequenceMode((SequenceMode)Mathf.Clamp(mode, 0, 1));

        public void SelectSequenceMode(SequenceMode mode)
        {
            if (state == GameState.Countdown || state == GameState.Playing) return;
            sequenceMode = mode;
        }

        public void RegisterHit(BalloonTarget balloon, Vector3 position)
        {
            score++;
            eventLogger.LogHit(balloon.BalloonId, position, balloon.ApproachSpeed, score, misses, practiceSeed);
            PlayBurst(position, balloon.BalloonColor);
            balloon.ReturnToPool();
            statusText.text = "POP!  +1";
            UpdateHud();
        }

        public void RegisterMiss(BalloonTarget balloon)
        {
            misses++;
            eventLogger.LogMiss(balloon.BalloonId, balloon.transform.position, balloon.ApproachSpeed, score, misses, practiceSeed);
            balloon.ReturnToPool();
            statusText.text = "MISSED - KEEP BALANCING";
            UpdateHud();
        }

        private void ShowReady()
        {
            state = GameState.Ready;
            titleText.text = "BALLOON BALANCE VR";
            statusText.text = "MOVE WITH WASD / ARROWS\nPRESS START OR SPACE";
            scoreText.text = "SCORE  0   |   MISSES  0";
            timerText.text = "01:30";
            startButton.gameObject.SetActive(true);
            restartButton.gameObject.SetActive(false);
        }

        private void FinishRound()
        {
            state = GameState.Finished;
            eventLogger.CompleteTrial(score, misses, practiceSeed);
            sampleLogger?.CompleteTrial();
            foreach (BalloonTarget balloon in balloons) balloon.ReturnToPool();
            titleText.text = "ROUND COMPLETE";
            statusText.text = $"FINAL SCORE  {score}\nMISSES  {misses}";
            restartButton.gameObject.SetActive(true);
        }

        private void BeginPlaying()
        {
            string mode = sequenceMode.ToString();
            eventLogger.BeginPracticeTrial(practiceSeed, mode);
            sampleLogger?.BeginPracticeTrial(practiceSeed, mode);
            state = GameState.Playing;
            statusText.text = "MOVE THE POINTER INTO THE BALLOONS";
            UpdateHud();
        }

        private int ChooseRoundSeed()
        {
            if (sequenceMode == SequenceMode.Random || fixedRoundSeeds == null || fixedRoundSeeds.Length == 0)
                return System.Environment.TickCount;
            int seed = fixedRoundSeeds[nextFixedSeed % fixedRoundSeeds.Length];
            nextFixedSeed++;
            return seed;
        }

        private void SpawnBalloon()
        {
            BalloonTarget balloon = null;
            for (int index = 0; index < balloons.Count; index++)
            {
                if (!balloons[index].gameObject.activeSelf)
                {
                    balloon = balloons[index];
                    break;
                }
            }
            if (balloon == null) return;
            Vector3 position = FindFairSpawnPosition();
            float speed = RandomRange(speedRange) * GetPhaseValue(phaseSpeedMultipliers);
            Color color = colors[random.Next(colors.Length)];
            int balloonId = ++nextBalloonId;
            balloon.Launch(this, balloonId, position, speed, missBoundaryZ, color);
            previousTargetPosition = new Vector2(position.x, position.y);
            hasPreviousTarget = true;
            eventLogger.LogSpawn(balloonId, position, speed, score, misses, practiceSeed);
        }

        private Vector3 FindFairSpawnPosition()
        {
            Vector3 fallback = new Vector3(0f, Mathf.Clamp(1.65f, verticalRange.x, verticalRange.y),
                Mathf.Max(depthRange.x, missBoundaryZ + 0.5f));
            float bestClearance = -1f;
            for (int attempt = 0; attempt < spawnCandidateAttempts; attempt++)
            {
                Vector2 target = new Vector2(RandomRange(horizontalRange), RandomRange(verticalRange));
                if (hasPreviousTarget)
                {
                    Vector2 delta = target - previousTargetPosition;
                    if (delta.magnitude > maximumConsecutiveJump)
                        target = previousTargetPosition + delta.normalized * maximumConsecutiveJump;
                    target.x = Mathf.Clamp(target.x, horizontalRange.x, horizontalRange.y);
                    target.y = Mathf.Clamp(target.y, verticalRange.x, verticalRange.y);
                }

                float clearance = MinimumClearance(target);
                Vector3 candidate = new Vector3(target.x, target.y,
                    Mathf.Max(RandomRange(depthRange), missBoundaryZ + 0.5f));
                if (clearance >= minimumActiveSpacing) return candidate;
                if (clearance > bestClearance)
                {
                    bestClearance = clearance;
                    fallback = candidate;
                }
            }
            return fallback;
        }

        private float MinimumClearance(Vector2 candidate)
        {
            float minimum = float.PositiveInfinity;
            for (int index = 0; index < balloons.Count; index++)
            {
                BalloonTarget active = balloons[index];
                if (!active.gameObject.activeSelf) continue;
                Vector2 position = new Vector2(active.transform.position.x, active.transform.position.y);
                minimum = Mathf.Min(minimum, Vector2.Distance(candidate, position));
            }
            return minimum;
        }

        private float GetPhaseValue(Vector3 values)
        {
            int phaseIndex = Mathf.Clamp(CurrentPhase - 1, 0, 2);
            return values[phaseIndex];
        }

        private float RandomRange(Vector2 range)
        {
            return Mathf.Lerp(range.x, range.y, (float)random.NextDouble());
        }

        private void PlayBurst(Vector3 position, Color balloonColor)
        {
            if (bursts.Count == 0) return;
            BurstEffect effect = bursts[nextBurst++ % bursts.Count];
            effect.Play(position, balloonColor);
        }

        private void BuildPools()
        {
            for (int index = 0; index < poolSize; index++)
            {
                BalloonTarget balloon = Instantiate(balloonPrefab, balloonPoolRoot);
                balloon.name = $"Balloon {index + 1:00}";
                balloon.ReturnToPool();
                balloons.Add(balloon);
            }

            for (int index = 0; index < 8; index++)
            {
                BurstEffect burst = Instantiate(burstPrefab, balloonPoolRoot);
                burst.name = $"Burst {index + 1:00}";
                burst.gameObject.SetActive(false);
                bursts.Add(burst);
            }
        }

        private void UpdateHud()
        {
            scoreText.text = $"SCORE  {score}   |   MISSES  {misses}";
            int seconds = Mathf.CeilToInt(remainingTime);
            timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }
    }
}
