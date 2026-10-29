using System.Collections;
using System.Linq;
using System.Reflection;
using BalloonBalance.XR;
using NUnit.Framework;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace BalloonBalance.Tests.PlayMode
{
    public sealed class XrInteractionSimulatorPlayModeTests
    {
        [UnityTest]
        public IEnumerator FixedDesktopViewRejectsSimulatedPoseAndOptionalMouseLookDoesNotMoveOrigin()
        {
            SceneManager.LoadScene("Gameplay", LoadSceneMode.Single);
            yield return null;

            GameObject simulatorPrefab = LoadConfiguredSimulatorPrefab();
            GameObject simulatorInstance = Object.Instantiate(simulatorPrefab);
            yield return null;

            XROrigin origin = Object.FindAnyObjectByType<XROrigin>();
            Assert.That(origin, Is.Not.Null);
            Assert.That(XRInteractionSimulator.instance, Is.Not.Null);
            XRHMD simulatedHeadset = InputSystem.GetDevice<XRHMD>();
            Assert.That(simulatedHeadset, Is.Not.Null);
            Assert.That(origin.Camera.transform.localPosition.y, Is.EqualTo(1.65f).Within(0.05f),
                "The Editor simulator must begin at standing eye height so the interaction frame is centred.");

            Vector3 originPosition = origin.transform.position;
            Quaternion originRotation = origin.transform.rotation;
            Vector3 cameraStart = origin.Camera.transform.localPosition;
            Quaternion cameraRotation = origin.Camera.transform.localRotation;

            XRInteractionSimulator.instance.enabled = false;
            InputSystem.QueueDeltaStateEvent(simulatedHeadset.isTracked, true);
            InputSystem.QueueDeltaStateEvent(simulatedHeadset.trackingState, 3);
            InputSystem.QueueDeltaStateEvent(simulatedHeadset.centerEyePosition, cameraStart + new Vector3(0.25f, 0.1f, 0.2f));
            yield return null;

            Assert.That(origin.transform.position, Is.EqualTo(originPosition));
            Assert.That(origin.transform.rotation, Is.EqualTo(originRotation));
            Assert.That(origin.Camera.transform.localPosition, Is.EqualTo(cameraStart).Using(Vector3EqualityComparer.Instance));
            Assert.That(origin.Camera.transform.localRotation, Is.EqualTo(cameraRotation).Using(QuaternionEqualityComparer.Instance));

            EditorKeyboardCameraIsolation isolation = origin.Camera.GetComponent<EditorKeyboardCameraIsolation>();
            DesktopSimulatorHeadLookToggle toggleController = Object.FindAnyObjectByType<DesktopSimulatorHeadLookToggle>();
            Assert.That(isolation, Is.Not.Null);
            Assert.That(toggleController, Is.Not.Null);
            Toggle toggle = Object.FindObjectsByType<Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(candidate => candidate.name == "Simulator Head Look Toggle");
            Assert.That(toggle, Is.Not.Null);
            Assert.That(toggle.isOn, Is.False);

            Mouse mouse = InputSystem.AddDevice<Mouse>("BalloonBalanceHeadLookTestMouse");
            mouse.MakeCurrent();
            toggle.isOn = true;
            Assert.That(isolation.SimulatorHeadLookEnabled, Is.True);
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(20f, -10f) }.WithButton(MouseButton.Right));
            InputSystem.Update();
            isolation.SendMessage("LateUpdate");
            Assert.That(Quaternion.Angle(origin.Camera.transform.localRotation, Quaternion.identity), Is.GreaterThan(0.1f));
            Assert.That(origin.Camera.transform.localPosition, Is.EqualTo(cameraStart).Using(Vector3EqualityComparer.Instance));
            Assert.That(origin.transform.position, Is.EqualTo(originPosition));
            Assert.That(origin.transform.rotation, Is.EqualTo(originRotation));

            toggle.isOn = false;
            isolation.SendMessage("LateUpdate");
            Assert.That(origin.Camera.transform.localRotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));

            InputSystem.RemoveDevice(mouse);
            Object.Destroy(simulatorInstance);
        }

        [UnityTest]
        public IEnumerator InputSettingsMenuPausesAndRestoresGameplayTime()
        {
            SceneManager.LoadScene("Gameplay", LoadSceneMode.Single);
            yield return null;

            DesktopInputSettingsMenu menu = Object.FindAnyObjectByType<DesktopInputSettingsMenu>();
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            menu.SetOpen(true);
            Assert.That(menu.IsOpen, Is.True);
            Assert.That(DesktopInputSettingsMenu.IsAnyMenuOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);

            menu.SetOpen(false);
            Assert.That(menu.IsOpen, Is.False);
            Assert.That(DesktopInputSettingsMenu.IsAnyMenuOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        private static GameObject LoadConfiguredSimulatorPrefab()
        {
            ScriptableObject settings = Resources.Load<ScriptableObject>("XRDeviceSimulatorSettings");
            Assert.That(settings, Is.Not.Null);

            FieldInfo prefabField = settings.GetType().GetField("m_SimulatorPrefab", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(prefabField, Is.Not.Null);

            GameObject prefab = prefabField.GetValue(settings) as GameObject;
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.TryGetComponent<XRInteractionSimulator>(out _), Is.True);
            return prefab;
        }
    }
}
