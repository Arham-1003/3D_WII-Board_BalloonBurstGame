using System.Collections;
using System.Linq;
using BalloonBalance.Gameplay;
using BalloonBalance.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BalloonBalance.Tests.PlayMode
{
    public sealed class PlayableRoundPlayModeTests
    {
        [UnityTest]
        public IEnumerator StartButtonSpawnsApproachingBalloonAndPointerBurstsIt()
        {
            SceneManager.LoadScene("Gameplay", LoadSceneMode.Single);
            yield return null;

            BalloonGameManager manager = Object.FindAnyObjectByType<BalloonGameManager>();
            PointerController pointer = Object.FindAnyObjectByType<PointerController>();
            Button start = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(button => button.name == "Start Button");
            Assert.That(manager, Is.Not.Null);
            Assert.That(pointer, Is.Not.Null);
            Assert.That(start.gameObject.activeSelf, Is.True);

            start.onClick.Invoke();
            manager.SendMessage("BeginPlaying");
            Assert.That(manager.IsPlaying, Is.True);
            yield return new WaitForSeconds(0.35f);

            BalloonTarget balloon = Object.FindObjectsByType<BalloonTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault();
            Assert.That(balloon, Is.Not.Null);
            Color balloonColor = balloon.BalloonColor;
            float startingZ = balloon.transform.position.z;
            yield return new WaitForSeconds(0.12f);
            Assert.That(balloon.transform.position.z, Is.LessThan(startingZ));

            balloon.transform.position = pointer.transform.position;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(manager.Score, Is.EqualTo(1));
            Assert.That(balloon.gameObject.activeSelf, Is.False);
            Assert.That(Object.FindObjectsByType<BurstEffect>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length, Is.GreaterThan(0));
            BurstEffect activeBurst = Object.FindObjectsByType<BurstEffect>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).First();
            Color burstColor = activeBurst.GetComponent<Renderer>().material.color;
            Assert.That(Vector4.Distance(balloonColor, burstColor), Is.LessThan(0.02f),
                "The pooled burst must inherit the colour of the balloon that was hit.");

            TrialEventLogger logger = Object.FindAnyObjectByType<TrialEventLogger>();
            Assert.That(logger, Is.Not.Null);
            Assert.That(logger.EventCount, Is.GreaterThanOrEqualTo(3), "round_start, spawn, and hit must be logged");

            manager.SendMessage("FinishRound");
            Assert.That(System.IO.File.Exists(logger.SavedPath), Is.True);
            string eventCsv = System.IO.File.ReadAllText(logger.SavedPath);
            StringAssert.Contains("round_start", eventCsv);
            StringAssert.Contains("spawn", eventCsv);
            StringAssert.Contains("hit", eventCsv);
            StringAssert.Contains("round_complete", eventCsv);
            Button restart = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(button => button.name == "Restart Button");
            Assert.That(restart.gameObject.activeSelf, Is.True);
            restart.onClick.Invoke();
            Assert.That(manager.IsPlaying, Is.False, "Restart begins with the safety countdown.");
        }

        [UnityTest]
        public IEnumerator SpawnCandidatesStayForwardAndLimitConsecutiveMovement()
        {
            SceneManager.LoadScene("Gameplay", LoadSceneMode.Single);
            yield return null;

            BalloonGameManager manager = Object.FindAnyObjectByType<BalloonGameManager>();
            manager.StartRound();
            manager.SendMessage("BeginPlaying");

            Vector2? previous = null;
            for (int index = 0; index < 6; index++)
            {
                manager.SendMessage("SpawnBalloon");
                BalloonTarget newest = Object.FindObjectsByType<BalloonTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .OrderByDescending(candidate => candidate.BalloonId).First();
                Assert.That(newest.transform.position.z, Is.GreaterThan(2.75f));
                Vector2 current = new Vector2(newest.transform.position.x, newest.transform.position.y);
                if (previous.HasValue)
                    Assert.That(Vector2.Distance(previous.Value, current), Is.LessThanOrEqualTo(1.351f));
                previous = current;
            }

            Assert.That(manager.CurrentPhase, Is.EqualTo(1));
        }
    }
}
