using UnityEngine;
using UnityEngine.UI;

namespace F3.Interrogation
{
    // =====================================================================
    // InterrogationUI.cs
    // ---------------------------------------------------------------------
    // Builds the whole interrogation screen from code and exposes simple
    // methods for the manager to call ("show this line", "open the wheel",
    // "show the retry screen"). It contains NO interrogation rules.
    //
    // Screen layout (1920x1080 reference):
    //   top-left  : COOPERATION meter
    //   centre    : the wheel (only while a question is being answered)
    //   bottom    : dialogue box (speaker name + typed text + hint)
    //   full-screen overlay: TESTIMONY UNLOCKED / INTERROGATION FAILED + RETRY
    // =====================================================================
    public class InterrogationUI : MonoBehaviour
    {
        [Tooltip("Higher draws on top of your other UI.")]
        public int sortingOrder = 100;

        static readonly Color FrankcescaColor = new Color(0.55f, 0.80f, 1.00f);
        static readonly Color VivekColor = new Color(1.00f, 0.75f, 0.45f);
        static readonly Color PanelColor = new Color(0.04f, 0.05f, 0.08f, 0.92f);
        static readonly Color GoodColor = new Color(0.55f, 1f, 0.65f);
        static readonly Color BadColor = new Color(1f, 0.45f, 0.45f);

        public WheelMenu Wheel { get; private set; }
        public CooperationMeter Meter { get; private set; }
        public bool IsTyping { get { return typewriter != null && typewriter.IsTyping; } }

        Canvas canvas;
        Font font;
        InterrogationAudio audioPlayer;

        // dialogue box
        Text speakerText;
        Text bodyText;
        Text hintText;
        Typewriter typewriter;

        // full-screen verdict overlay
        GameObject verdictRoot;
        Text verdictHeadline;
        Text verdictBody;
        Text verdictPrompt;
        RectTransform retryButton;

        InterrogationQuestion currentQuestion;

        // ------------------------------------------------------------
        // Build everything once.
        // ------------------------------------------------------------
        public void Build(Font fontOverride, InterrogationAudio audioPlayer, int maxCooperation)
        {
            font = fontOverride != null ? fontOverride : UIFactory.DefaultFont();
            this.audioPlayer = audioPlayer;

            // --- Canvas ---
            var canvasObject = new GameObject("InterrogationCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            // CanvasScaler makes the UI scale with the screen so it looks the same on any resolution.
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var root = (RectTransform)canvasObject.transform;

            // --- Dim backdrop (so it reads over whatever scene is behind it) ---
            Image dim = UIFactory.MakeImage("Dim", root, new Color(0f, 0f, 0f, 0.72f));
            UIFactory.Stretch(dim.rectTransform, 0, 0, 0, 0);

            // --- Cooperation meter ---
            var meterObject = new GameObject("CooperationMeter", typeof(RectTransform));
            meterObject.transform.SetParent(root, false);
            Meter = meterObject.AddComponent<CooperationMeter>();
            Meter.Build(font, maxCooperation, audioPlayer);

            // --- Wheel ---
            var wheelObject = new GameObject("Wheel", typeof(RectTransform));
            wheelObject.transform.SetParent(root, false);
            Wheel = wheelObject.AddComponent<WheelMenu>();
            Wheel.Build(font, audioPlayer);
            Wheel.SelectionChanged += OnWheelSelectionChanged;

            BuildDialogueBox(root);
            BuildVerdictOverlay(root);

            canvasObject.SetActive(false);
        }

        void BuildDialogueBox(RectTransform root)
        {
            // Stretches across the bottom with 80px margins either side.
            Image panel = UIFactory.MakeImage("DialoguePanel", root, PanelColor);
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 40f);
            panelRect.sizeDelta = new Vector2(-160f, 340f);

            speakerText = UIFactory.MakeText("Speaker", panelRect, font, 34, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.PlaceTopLeft(speakerText.rectTransform, 40f, -22f, 700f, 44f);

            bodyText = UIFactory.MakeText("Body", panelRect, font, 30, Color.white, TextAnchor.UpperLeft);
            UIFactory.Stretch(bodyText.rectTransform, 40f, 52f, 40f, 76f);

            hintText = UIFactory.MakeText("Hint", panelRect, font, 22, new Color(1f, 1f, 1f, 0.7f), TextAnchor.LowerRight);
            hintText.rectTransform.anchorMin = new Vector2(1f, 0f);
            hintText.rectTransform.anchorMax = new Vector2(1f, 0f);
            hintText.rectTransform.pivot = new Vector2(1f, 0f);
            hintText.rectTransform.anchoredPosition = new Vector2(-40f, 16f);
            hintText.rectTransform.sizeDelta = new Vector2(1500f, 32f);
            hintText.text = "";

            // The typewriter lives on the body text object.
            typewriter = bodyText.gameObject.AddComponent<Typewriter>();
            typewriter.target = bodyText;
            typewriter.onBlip = OnTypewriterBlip;
        }

        void BuildVerdictOverlay(RectTransform root)
        {
            Image overlay = UIFactory.MakeImage("Verdict", root, new Color(0f, 0f, 0f, 0.88f));
            UIFactory.Stretch(overlay.rectTransform, 0, 0, 0, 0);
            verdictRoot = overlay.gameObject;

            verdictHeadline = UIFactory.MakeText("Headline", overlay.transform, font, 72, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.PlaceCentered(verdictHeadline.rectTransform, 0f, 250f, 1500f, 110f);

            verdictBody = UIFactory.MakeText("Body", overlay.transform, font, 34, Color.white, TextAnchor.MiddleCenter);
            UIFactory.PlaceCentered(verdictBody.rectTransform, 0f, 60f, 1200f, 280f);

            Image button = UIFactory.MakeImage("RetryButton", overlay.transform, new Color(0.85f, 0.25f, 0.25f, 1f));
            retryButton = button.rectTransform;
            UIFactory.PlaceCentered(retryButton, 0f, -180f, 360f, 96f);
            Text buttonText = UIFactory.MakeText("Text", retryButton, font, 44, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(buttonText.rectTransform, 0, 0, 0, 0);
            buttonText.text = "RETRY";

            verdictPrompt = UIFactory.MakeText("Prompt", overlay.transform, font, 26, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter);
            UIFactory.PlaceCentered(verdictPrompt.rectTransform, 0f, -330f, 1200f, 40f);

            verdictRoot.SetActive(false);
        }

        // ------------------------------------------------------------
        // Visibility
        // ------------------------------------------------------------
        public void Show()
        {
            canvas.gameObject.SetActive(true);
            HideVerdict();
            speakerText.text = "";
            bodyText.text = "";
            hintText.text = "";
        }

        public void Hide()
        {
            canvas.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------
        // Dialogue box
        // ------------------------------------------------------------
        public void ShowLine(Speaker speaker, string text)
        {
            ApplySpeaker(speaker);
            hintText.text = "";
            typewriter.Play(text);
        }

        public void SkipTyping()
        {
            typewriter.Complete();
        }

        public void ShowContinuePrompt(bool show)
        {
            hintText.text = show ? "Click / Space to continue" : "";
        }

        void ApplySpeaker(Speaker speaker)
        {
            bool isFrankcesca = speaker == Speaker.Frankcesca;
            speakerText.text = isFrankcesca ? "FRANKCESCA" : "VIVEK";
            speakerText.color = isFrankcesca ? FrankcescaColor : VivekColor;
            typewriter.blipPitch = isFrankcesca ? 1.15f : 0.85f; // two voices, two pitches
        }

        void OnTypewriterBlip(float pitch)
        {
            if (audioPlayer != null) audioPlayer.PlayBlip(pitch);
        }

        // ------------------------------------------------------------
        // Wheel
        // ------------------------------------------------------------
        public void OpenWheel(InterrogationQuestion question)
        {
            currentQuestion = question;
            typewriter.Stop();
            ApplySpeaker(Speaker.Frankcesca);
            hintText.text = "A / D, arrows or scroll to turn the wheel   |   Enter / Space / click to choose";
            Wheel.Open(question.options);
        }

        public void CloseWheel()
        {
            Wheel.Close();
            hintText.text = "";
        }

        // While the wheel is open, the dialogue box previews the highlighted option in full.
        void OnWheelSelectionChanged(int index)
        {
            if (currentQuestion == null || index < 0 || index >= currentQuestion.options.Count) return;
            typewriter.Stop();
            bodyText.text = currentQuestion.options[index].fullText;
        }

        // ------------------------------------------------------------
        // Verdict overlay (success / fail)
        // ------------------------------------------------------------
        public void ShowTestimony(string testimony)
        {
            verdictRoot.SetActive(true);
            verdictHeadline.text = "TESTIMONY UNLOCKED";
            verdictHeadline.color = GoodColor;
            verdictBody.text = testimony;
            retryButton.gameObject.SetActive(false);
            verdictPrompt.text = "Click / Space to continue";
        }

        public void ShowFailed(int cooperation, int maxCooperation)
        {
            verdictRoot.SetActive(true);
            verdictHeadline.text = "INTERROGATION FAILED";
            verdictHeadline.color = BadColor;
            verdictBody.text = "Vivek has shut down.\nCooperation reached " + cooperation + "/" + maxCooperation +
                               ". You need " + maxCooperation + "/" + maxCooperation + " to get his testimony.";
            retryButton.gameObject.SetActive(true);
            verdictPrompt.text = "Enter / Space / R, or click RETRY";
        }

        public void HideVerdict()
        {
            if (verdictRoot != null) verdictRoot.SetActive(false);
        }

        /// <summary>True the frame the player asks to retry (keys, gamepad, or clicking the button).</summary>
        public bool RetryPressed()
        {
            if (InterrogationInput.SubmitPressed() || InterrogationInput.RetryKeyPressed()) return true;

            Vector2 pointer;
            if (InterrogationInput.PointerPressed(out pointer) &&
                RectTransformUtility.RectangleContainsScreenPoint(retryButton, pointer, null))
                return true;

            return false;
        }

        // Gentle pulsing on the prompts and the retry button so they draw the eye.
        void Update()
        {
            if (canvas == null || !canvas.gameObject.activeSelf) return;

            float pulse = 0.55f + 0.35f * Mathf.Sin(Time.unscaledTime * 4f);
            hintText.color = new Color(1f, 1f, 1f, pulse);
            verdictPrompt.color = new Color(1f, 1f, 1f, pulse);

            if (retryButton != null && retryButton.gameObject.activeInHierarchy)
                retryButton.localScale = Vector3.one * (1f + 0.03f * Mathf.Sin(Time.unscaledTime * 5f));
        }
    }
}
