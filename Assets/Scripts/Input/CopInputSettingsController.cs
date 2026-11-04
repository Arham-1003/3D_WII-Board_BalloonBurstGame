using UnityEngine;
using UnityEngine.UI;
using BalloonBalance.Gameplay;

namespace BalloonBalance.Input
{
    public sealed class CopInputSettingsController : MonoBehaviour
    {
        [SerializeField] private CopInputManager inputManager;
        [SerializeField] private Dropdown sourceDropdown;
        [SerializeField] private Dropdown filterDropdown;
        [SerializeField] private Dropdown sequenceDropdown;
        [SerializeField] private Text connectionText;
        [SerializeField] private BalloonGameManager gameManager;
        private Button calibrationButton;
        private Text calibrationStatus;

        private void Awake()
        {
            EnsureFilterControls();
            EnsureSequenceControls();
            EnsureCalibrationControls();
            ApplyElegantLayout();
            if (gameManager == null) gameManager = FindAnyObjectByType<BalloonGameManager>();
            sourceDropdown.ClearOptions();
            sourceDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Keyboard", "Synthetic Board", "CSV Playback", "Live Wii Board (UDP)"
            });
            sourceDropdown.SetValueWithoutNotify((int)inputManager.InputMode);
            sourceDropdown.onValueChanged.AddListener(inputManager.SelectInput);

            filterDropdown.ClearOptions();
            filterDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Raw / No Filter", "Moving Average", "EMA", "Butterworth Low-pass", "Median Spike Filter",
                "Adaptive Low-pass", "Alpha-Beta Predictive"
            });
            filterDropdown.SetValueWithoutNotify((int)inputManager.ActiveFilter);
            filterDropdown.onValueChanged.AddListener(inputManager.SelectFilter);

            sequenceDropdown.ClearOptions();
            sequenceDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Fixed Matched Sequences", "Random Sequence"
            });
            sequenceDropdown.SetValueWithoutNotify((int)gameManager.ActiveSequenceMode);
            sequenceDropdown.onValueChanged.AddListener(gameManager.SelectSequenceMode);
            SetDropdownListHeight(sourceDropdown);
            SetDropdownListHeight(filterDropdown);
            SetDropdownListHeight(sequenceDropdown);
        }

        private void Update()
        {
            filterDropdown.interactable = !gameManager.IsPlaying;
            sequenceDropdown.interactable = !gameManager.IsPlaying;
            calibrationButton.interactable = !gameManager.IsPlaying && inputManager.InputMode != CopInputMode.Keyboard;
            calibrationStatus.text = string.IsNullOrEmpty(inputManager.CalibrationFeedback)
                ? inputManager.CalibrationInstruction
                : $"{inputManager.CalibrationFeedback}  {inputManager.CalibrationInstruction}";
            Text calibrationLabel = calibrationButton.GetComponentInChildren<Text>();
            if (calibrationLabel != null) calibrationLabel.text = CalibrationButtonText();
            connectionText.text = $"INPUT: {inputManager.InputMode}   |   FILTER: {inputManager.ActiveFilter}   |   ROUND: {gameManager.ActiveSequenceMode}   |   {inputManager.ConnectionStatus}";
            connectionText.color = inputManager.ConnectionStatus == "CONNECTED"
                ? new Color(0.25f, 1f, 0.55f) : new Color(1f, 0.65f, 0.2f);
        }

        private void OnDestroy()
        {
            if (sourceDropdown != null && inputManager != null)
                sourceDropdown.onValueChanged.RemoveListener(inputManager.SelectInput);
            if (filterDropdown != null && inputManager != null)
                filterDropdown.onValueChanged.RemoveListener(inputManager.SelectFilter);
            if (sequenceDropdown != null && gameManager != null)
                sequenceDropdown.onValueChanged.RemoveListener(gameManager.SelectSequenceMode);
            if (calibrationButton != null) calibrationButton.onClick.RemoveListener(CaptureCalibration);
        }

        private void EnsureCalibrationControls()
        {
            Transform existingButton = transform.Find("Calibration Button");
            if (existingButton != null) calibrationButton = existingButton.GetComponent<Button>();
            else
            {
                GameObject buttonObject = new GameObject("Calibration Button", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(transform, false);
                calibrationButton = buttonObject.GetComponent<Button>();
                GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(buttonObject.transform, false);
                Text label = labelObject.GetComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
            }
            calibrationButton.onClick.AddListener(CaptureCalibration);

            Transform existingStatus = transform.Find("Calibration Status");
            if (existingStatus != null) calibrationStatus = existingStatus.GetComponent<Text>();
            else
            {
                GameObject statusObject = new GameObject("Calibration Status", typeof(RectTransform), typeof(Text));
                statusObject.transform.SetParent(transform, false);
                calibrationStatus = statusObject.GetComponent<Text>();
                calibrationStatus.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        private void CaptureCalibration() => inputManager.CaptureCalibrationPoint();

        private string CalibrationButtonText() => inputManager.CurrentCalibrationStep switch
        {
            CopInputManager.CalibrationStep.Neutral => "CAPTURE CENTRE",
            CopInputManager.CalibrationStep.Left => "CAPTURE LEFT",
            CopInputManager.CalibrationStep.Right => "CAPTURE RIGHT",
            CopInputManager.CalibrationStep.Forward => "CAPTURE FORWARD",
            CopInputManager.CalibrationStep.Backward => "CAPTURE BACKWARD",
            CopInputManager.CalibrationStep.Complete => "RECALIBRATE",
            _ => "START CALIBRATION"
        };

        private void EnsureFilterControls()
        {
            if (filterDropdown != null) return;

            RectTransform panel = transform as RectTransform;
            if (panel != null) panel.sizeDelta = new Vector2(860f, 700f);
            sourceDropdown.GetComponent<RectTransform>().anchoredPosition = new Vector2(100f, -235f);
            filterDropdown = Instantiate(sourceDropdown, transform);
            filterDropdown.name = "Filter Selection Dropdown";
            filterDropdown.GetComponent<RectTransform>().anchoredPosition = new Vector2(100f, -295f);

            Transform sourceLabel = transform.Find("Input Source Label");
            if (sourceLabel != null)
            {
                Text filterLabel = Instantiate(sourceLabel, transform).GetComponent<Text>();
                filterLabel.name = "Filter Selection Label";
                filterLabel.text = "CONTROL FILTER";
                filterLabel.rectTransform.anchoredPosition = new Vector2(-360f, -295f);
            }

            Move("Input Source Label", new Vector2(-360f, -235f));
        }

        private void EnsureSequenceControls()
        {
            if (sequenceDropdown != null) return;
            RectTransform panel = transform as RectTransform;
            if (panel != null) panel.sizeDelta = new Vector2(860f, 700f);
            sourceDropdown.GetComponent<RectTransform>().anchoredPosition = new Vector2(100f, -235f);
            filterDropdown.GetComponent<RectTransform>().anchoredPosition = new Vector2(100f, -295f);
            Move("Input Source Label", new Vector2(-360f, -235f));
            Move("Filter Selection Label", new Vector2(-360f, -295f));
            sequenceDropdown = Instantiate(sourceDropdown, transform);
            sequenceDropdown.name = "Round Sequence Dropdown";
            sequenceDropdown.GetComponent<RectTransform>().anchoredPosition = new Vector2(100f, -355f);

            Transform sourceLabel = transform.Find("Input Source Label");
            if (sourceLabel != null)
            {
                Text sequenceLabel = Instantiate(sourceLabel, transform).GetComponent<Text>();
                sequenceLabel.name = "Round Sequence Label";
                sequenceLabel.text = "ROUND SEQUENCE";
                sequenceLabel.rectTransform.anchoredPosition = new Vector2(-360f, -355f);
            }

            Move("Connection Status", new Vector2(0f, -405f));
            Move("Simulator Head Look Toggle", new Vector2(-70f, -455f));
            Move("Head Look Explanation", new Vector2(0f, -505f));
            Move("Close Settings Button", new Vector2(0f, -305f));
        }

        private void Move(string childName, Vector2 position)
        {
            Transform child = transform.Find(childName);
            if (child != null) child.GetComponent<RectTransform>().anchoredPosition = position;
        }

        private void ApplyElegantLayout()
        {
            RectTransform panel = transform as RectTransform;
            panel.sizeDelta = new Vector2(920f, 760f);
            Image panelImage = GetComponent<Image>();
            panelImage.color = new Color(0.018f, 0.065f, 0.11f, 0.985f);

            StyleText("Settings Title", new Vector2(0f, -40f), new Vector2(840f, 52f), 34,
                new Color(0.2f, 0.9f, 1f), TextAnchor.MiddleCenter);
            StyleText("Settings Help", new Vector2(0f, -102f), new Vector2(800f, 68f), 18,
                new Color(0.78f, 0.88f, 0.94f), TextAnchor.UpperLeft);

            CreateRowBackground("Input Row Background", -188f);
            CreateRowBackground("Filter Row Background", -248f);
            CreateRowBackground("Sequence Row Background", -308f);
            StyleLabel("Input Source Label", "INPUT SOURCE", -188f);
            StyleLabel("Filter Selection Label", "MOVEMENT FILTER", -248f);
            StyleLabel("Round Sequence Label", "ROUND SEQUENCE", -308f);
            StyleDropdown(sourceDropdown, new Vector2(135f, -188f));
            StyleDropdown(filterDropdown, new Vector2(135f, -248f));
            StyleDropdown(sequenceDropdown, new Vector2(135f, -308f));

            StyleText("Connection Status", new Vector2(0f, -358f), new Vector2(720f, 34f), 16,
                Color.white, TextAnchor.MiddleCenter);
            RectTransform calibrationRect = calibrationButton.GetComponent<RectTransform>();
            calibrationRect.anchorMin = calibrationRect.anchorMax = new Vector2(0.5f, 1f);
            calibrationRect.pivot = new Vector2(0.5f, 0.5f);
            calibrationRect.anchoredPosition = new Vector2(0f, -407f);
            calibrationRect.sizeDelta = new Vector2(330f, 46f);
            calibrationButton.GetComponent<Image>().color = new Color(0.04f, 0.48f, 0.82f, 1f);
            RectTransform calibrationLabelRect = calibrationButton.GetComponentInChildren<Text>().rectTransform;
            calibrationLabelRect.anchorMin = Vector2.zero;
            calibrationLabelRect.anchorMax = Vector2.one;
            calibrationLabelRect.offsetMin = calibrationLabelRect.offsetMax = Vector2.zero;
            calibrationButton.GetComponentInChildren<Text>().fontSize = 19;
            calibrationStatus.rectTransform.anchorMin = calibrationStatus.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            calibrationStatus.rectTransform.anchoredPosition = new Vector2(0f, -452f);
            calibrationStatus.rectTransform.sizeDelta = new Vector2(700f, 38f);
            calibrationStatus.fontSize = 15;
            calibrationStatus.alignment = TextAnchor.MiddleCenter;
            calibrationStatus.color = new Color(0.68f, 0.9f, 1f);

            Move("Simulator Head Look Toggle", new Vector2(-65f, -498f));
            RectTransform toggle = transform.Find("Simulator Head Look Toggle") as RectTransform;
            if (toggle != null) toggle.sizeDelta = new Vector2(700f, 44f);
            StyleText("Head Look Explanation", new Vector2(0f, -543f), new Vector2(700f, 48f), 16,
                new Color(0.68f, 0.79f, 0.86f), TextAnchor.UpperLeft);

            Transform close = transform.Find("Close Settings Button");
            if (close != null)
            {
                RectTransform closeRect = close.GetComponent<RectTransform>();
                closeRect.anchoredPosition = new Vector2(0f, -325f);
                closeRect.sizeDelta = new Vector2(250f, 58f);
                close.GetComponent<Image>().color = new Color(0.04f, 0.48f, 0.82f, 1f);
                Text label = close.GetComponentInChildren<Text>();
                if (label != null) label.fontSize = 24;
            }
        }

        private void CreateRowBackground(string objectName, float y)
        {
            Transform existing = transform.Find(objectName);
            GameObject row = existing != null ? existing.gameObject :
                new GameObject(objectName, typeof(RectTransform), typeof(Image));
            if (existing == null) row.transform.SetParent(transform, false);
            RectTransform rect = row.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(840f, 52f);
            row.GetComponent<Image>().color = new Color(0.035f, 0.13f, 0.21f, 0.96f);
            row.transform.SetAsFirstSibling();
        }

        private void StyleLabel(string childName, string label, float y)
        {
            Transform child = transform.Find(childName);
            if (child == null) return;
            Text text = child.GetComponent<Text>();
            text.text = label;
            text.fontSize = 17;
            text.color = new Color(0.55f, 0.9f, 1f);
            text.alignment = TextAnchor.MiddleLeft;
            text.rectTransform.anchoredPosition = new Vector2(-315f, y);
            text.rectTransform.sizeDelta = new Vector2(210f, 42f);
        }

        private static void StyleDropdown(Dropdown dropdown, Vector2 position)
        {
            RebuildDropdownTemplate(dropdown);
            RectTransform rect = dropdown.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(530f, 42f);
            dropdown.GetComponent<Image>().color = new Color(0.025f, 0.22f, 0.34f, 1f);
            if (dropdown.captionText != null)
            {
                dropdown.captionText.fontSize = 18;
                dropdown.captionText.color = Color.white;
            }
            if (dropdown.itemText != null)
            {
                dropdown.itemText.fontSize = 17;
                dropdown.itemText.color = Color.white;
            }
            ColorBlock colors = dropdown.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.65f, 0.92f, 1f);
            colors.pressedColor = new Color(0.35f, 0.75f, 0.95f);
            dropdown.colors = colors;
        }

        private static void RebuildDropdownTemplate(Dropdown dropdown)
        {
            RectTransform oldTemplate = dropdown.template;
            if (oldTemplate != null)
            {
                oldTemplate.gameObject.SetActive(false);
                oldTemplate.name = "Legacy Template (Replaced)";
            }

            GameObject templateObject = new GameObject("Template", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            templateObject.transform.SetParent(dropdown.transform, false);
            RectTransform template = templateObject.GetComponent<RectTransform>();
            template.anchorMin = new Vector2(0f, 0f);
            template.anchorMax = new Vector2(1f, 0f);
            template.pivot = new Vector2(0.5f, 1f);
            template.anchoredPosition = new Vector2(0f, -4f);
            template.sizeDelta = new Vector2(0f, 190f);
            templateObject.GetComponent<Image>().color = new Color(0.018f, 0.1f, 0.17f, 1f);

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(template, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(2f, 2f);
            viewport.offsetMax = new Vector2(-2f, -2f);
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = Color.white;
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewport, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 38f);

            GameObject itemObject = new GameObject("Item", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
            itemObject.transform.SetParent(content, false);
            RectTransform item = itemObject.GetComponent<RectTransform>();
            item.anchorMin = new Vector2(0f, 0.5f);
            item.anchorMax = new Vector2(1f, 0.5f);
            item.pivot = new Vector2(0.5f, 0.5f);
            item.anchoredPosition = Vector2.zero;
            item.sizeDelta = new Vector2(0f, 38f);
            Image itemImage = itemObject.GetComponent<Image>();
            itemImage.color = new Color(0.03f, 0.16f, 0.25f, 1f);
            Toggle itemToggle = itemObject.GetComponent<Toggle>();
            itemToggle.targetGraphic = itemImage;
            ColorBlock itemColors = itemToggle.colors;
            itemColors.normalColor = Color.white;
            itemColors.highlightedColor = new Color(0.35f, 0.75f, 0.95f);
            itemColors.selectedColor = new Color(0.12f, 0.55f, 0.78f);
            itemToggle.colors = itemColors;

            GameObject labelObject = new GameObject("Item Label", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(item, false);
            Text itemLabel = labelObject.GetComponent<Text>();
            itemLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            itemLabel.fontSize = 17;
            itemLabel.color = Color.white;
            itemLabel.alignment = TextAnchor.MiddleLeft;
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 0f);
            labelRect.offsetMax = new Vector2(-16f, 0f);

            ScrollRect scroll = templateObject.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            dropdown.template = template;
            dropdown.itemText = itemLabel;
            templateObject.SetActive(false);
        }

        private static void SetDropdownListHeight(Dropdown dropdown)
        {
            if (dropdown == null || dropdown.template == null) return;
            const float itemHeight = 40f;
            const float padding = 8f;
            float visibleItems = Mathf.Min(dropdown.options.Count, 7);
            dropdown.template.sizeDelta = new Vector2(0f, visibleItems * itemHeight + padding);
        }

        private void StyleText(string childName, Vector2 position, Vector2 size, int fontSize,
            Color color, TextAnchor alignment)
        {
            Transform child = transform.Find(childName);
            if (child == null) return;
            Text text = child.GetComponent<Text>();
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = size;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
        }
    }
}
