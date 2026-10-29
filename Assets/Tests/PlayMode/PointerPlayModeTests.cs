using System.Collections;
using System.Reflection;
using BalloonBalance.Gameplay;
using BalloonBalance.Input;
using BalloonBalance.XR;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace BalloonBalance.Tests.PlayMode
{
    public sealed class PointerPlayModeTests
    {
        [UnityTest]
        public IEnumerator KeyboardInputMovesPointerOnFrontPlane()
        {
            SceneManager.LoadScene("Gameplay", LoadSceneMode.Single);
            yield return null;

            GameObject simulatorInstance = Object.Instantiate(LoadConfiguredSimulatorPrefab());
            yield return null;

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>("BalloonBalanceTestKeyboard");
            keyboard.MakeCurrent();
            PointerController pointer = Object.FindAnyObjectByType<PointerController>();
            CopInputManager inputManager = Object.FindAnyObjectByType<CopInputManager>();
            KeyboardCopInputSource keyboardSource = Object.FindAnyObjectByType<KeyboardCopInputSource>();
            Assert.That(pointer, Is.Not.Null);
            Assert.That(inputManager, Is.Not.Null);
            Assert.That(keyboardSource, Is.Not.Null);

            yield return null;
            Assert.That(XRInteractionSimulator.instance, Is.Not.Null);
            Assert.That(XRInteractionSimulator.instance.enabled, Is.False,
                "Keyboard gameplay must disable XR simulator camera controls that share WASD/arrows.");

            Vector3 start = pointer.transform.position;
            Camera camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            EditorKeyboardCameraIsolation isolation = camera.GetComponent<EditorKeyboardCameraIsolation>();
            TrackedPoseDriver poseDriver = camera.GetComponent<TrackedPoseDriver>();
            Assert.That(isolation, Is.Not.Null);
            Assert.That(poseDriver, Is.Not.Null);

            camera.transform.SetLocalPositionAndRotation(new Vector3(0.2f, 0.4f, -5f), Quaternion.Euler(-15f, 12f, 0f));
            isolation.SendMessage("LateUpdate");
            Assert.That(camera.transform.localPosition, Is.EqualTo(new Vector3(0f, 1.65f, 0f)).Using(Vector3EqualityComparer.Instance),
                "Fixed desktop view must share the interaction frame's centre height and must clear stale simulator translation.");
            Assert.That(camera.transform.localRotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance),
                "Fixed desktop view must look directly through the interaction frame centre.");
            Assert.That(poseDriver.enabled, Is.False,
                "Tracked pose updates must not reapply a stale simulator pose while fixed desktop view is active.");
            Vector3 cameraPosition = camera.transform.localPosition;
            Quaternion cameraRotation = camera.transform.localRotation;
            yield return new WaitForSecondsRealtime(0.05f);
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();
            Assert.That(keyboard.dKey.isPressed, Is.True);
            keyboardSource.SendMessage("Update");
            inputManager.SendMessage("Update");
            pointer.SendMessage("LateUpdate");

            Assert.That(inputManager.HasValidSample, Is.True);
            Assert.That(inputManager.ControlPosition.x, Is.GreaterThan(0.0001f));
            Assert.That(pointer.transform.position.x, Is.GreaterThan(start.x + 0.0001f));
            Assert.That(pointer.transform.position.z, Is.EqualTo(start.z).Within(0.001f));

            Vector3 releasedPosition = pointer.transform.position;

            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            yield return new WaitForSecondsRealtime(0.08f);

            Assert.That(pointer.transform.position.x, Is.EqualTo(releasedPosition.x).Within(0.01f),
                "The keyboard pointer must hold its last commanded position after key release.");
            Assert.That(pointer.transform.position.y, Is.EqualTo(releasedPosition.y).Within(0.01f));
            Assert.That(camera.transform.localPosition, Is.EqualTo(cameraPosition).Using(Vector3EqualityComparer.Instance));
            Assert.That(camera.transform.localRotation, Is.EqualTo(cameraRotation).Using(QuaternionEqualityComparer.Instance));

            InputSystem.RemoveDevice(keyboard);
            Object.Destroy(simulatorInstance);
        }

        private static GameObject LoadConfiguredSimulatorPrefab()
        {
            ScriptableObject settings = Resources.Load<ScriptableObject>("XRDeviceSimulatorSettings");
            Assert.That(settings, Is.Not.Null);
            FieldInfo prefabField = settings.GetType().GetField("m_SimulatorPrefab", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(prefabField, Is.Not.Null);
            GameObject prefab = prefabField.GetValue(settings) as GameObject;
            Assert.That(prefab, Is.Not.Null);
            return prefab;
        }
    }
}
