using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace F3.Interrogation
{
    // =====================================================================
    // WheelMenu.cs
    // ---------------------------------------------------------------------
    // The rotating wheel. Each option is a "leg" (spoke) radiating from a hub.
    // Turning the wheel rotates the legs so the highlighted option swings up
    // to the 12 o'clock position, where it grows and brightens.
    //
    // Controls: A/D, arrow keys, W/S, mouse scroll, d-pad or left stick turn it.
    //           Enter / Space / gamepad A confirms.
    //           Mouse/touch: tap a label to turn to it, tap the highlighted one to confirm.
    //
    // Angle convention used below: 0 degrees = straight up, positive = clockwise.
    // (Unity's z-rotation is counter-clockwise, so leg rotation uses -angle.)
    // =====================================================================
    public class WheelMenu : MonoBehaviour
    {
        [Header("Layout (pixels, at the 1920x1080 reference size)")]
        public float radius = 230f;
        public float wheelCentreOffsetY = 110f;
        public Vector2 labelSize = new Vector2(400f, 96f);
        public float legWidth = 6f;

        [Header("Feel")]
        [Tooltip("Smaller = snappier rotation.")]
        public float rotateSmoothTime = 0.12f;
        public float openDuration = 0.35f;

        /// <summary>Raised whenever the highlighted option changes (also once when the wheel opens).</summary>
        public event Action<int> SelectionChanged;

        /// <summary>Raised when the player confirms an option.</summary>
        public event Action<int> OptionChosen;

        static readonly Color LegDim = new Color(1f, 1f, 1f, 0.22f);
        static readonly Color LegActive = new Color(1f, 0.78f, 0.30f, 1f);
        static readonly Color LabelBgDim = new Color(0.04f, 0.05f, 0.08f, 0.55f);
        static readonly Color LabelBgActive = new Color(0.10f, 0.12f, 0.18f, 0.95f);
        static readonly Color TextDim = new Color(1f, 1f, 1f, 0.55f);

        // One entry per option.
        class Leg
        {
            public RectTransform legRect;
            public Image legImage;
            public Image nodeImage;
            public RectTransform labelRect;
            public Image labelBackground;
            public Text labelText;
        }

        readonly List<Leg> legs = new List<Leg>();

        Font font;
        InterrogationAudio audioPlayer;
        RectTransform root;
        RectTransform legsLayer;
        RectTransform labelsLayer;

        int selected;
        float stepAngle = 120f;   // degrees between legs = 360 / optionCount
        float wheelAngle;         // current (smoothed) rotation
        float targetAngle;        // where it is turning to
        float angleVelocity;      // used by SmoothDampAngle

        float openAmount;         // 0 = closed, 1 = fully open (drives the scale animation)
        bool isOpen;
        bool interactive;
        float ignoreInputUntil;   // stops the click that opened the wheel from also choosing something

        // ------------------------------------------------------------
        // STEP 1: build the static parts (hub + layers). Called once.
        // ------------------------------------------------------------
        public void Build(Font font, InterrogationAudio audioPlayer)
        {
            this.font = font;
            this.audioPlayer = audioPlayer;

            root = (RectTransform)transform;
            UIFactory.PlaceCentered(root, 0f, wheelCentreOffsetY, 0f, 0f);

            // Layer order = draw order: legs at the back, hub above them, labels on top.
            legsLayer = UIFactory.MakeRect("Legs", root);

            Image ring = UIFactory.MakeImage("HubRing", root, new Color(0.04f, 0.05f, 0.08f, 0.95f), UIFactory.Circle());
            UIFactory.PlaceCentered(ring.rectTransform, 0f, 0f, 120f, 120f);
            Image core = UIFactory.MakeImage("HubCore", ring.transform, LegActive, UIFactory.Circle());
            UIFactory.PlaceCentered(core.rectTransform, 0f, 0f, 44f, 44f);

            labelsLayer = UIFactory.MakeRect("Labels", root);

            gameObject.SetActive(false);
        }

        // ------------------------------------------------------------
        // STEP 2: open the wheel for one question.
        // ------------------------------------------------------------
        public void Open(IList<DialogueOption> options)
        {
            ClearLegs();

            int count = options.Count;
            stepAngle = 360f / Mathf.Max(1, count);

            for (int i = 0; i < count; i++)
                legs.Add(CreateLeg(i, options[i]));

            selected = 0;
            targetAngle = 0f;
            wheelAngle = -stepAngle * 1.5f; // start turned away so it spins into place when opening
            angleVelocity = 0f;

            openAmount = 0f;
            isOpen = true;
            interactive = true;
            ignoreInputUntil = Time.unscaledTime + 0.3f;
            InterrogationInput.ResetLatches();

            gameObject.SetActive(true);
            ApplyScale();
            Layout();

            if (SelectionChanged != null) SelectionChanged(selected);
        }

        /// <summary>Start the closing animation (the wheel deactivates itself when finished).</summary>
        public void Close()
        {
            isOpen = false;
            interactive = false;
        }

        Leg CreateLeg(int index, DialogueOption option)
        {
            var leg = new Leg();

            // The spoke: a thin rectangle from the hub outward. Pivot at its bottom
            // so rotating it swings the free end around the hub.
            leg.legImage = UIFactory.MakeImage("Leg" + index, legsLayer, LegDim);
            leg.legRect = leg.legImage.rectTransform;
            leg.legRect.anchorMin = new Vector2(0.5f, 0.5f);
            leg.legRect.anchorMax = new Vector2(0.5f, 0.5f);
            leg.legRect.pivot = new Vector2(0.5f, 0f);
            leg.legRect.anchoredPosition = Vector2.zero;
            leg.legRect.sizeDelta = new Vector2(legWidth, radius);

            // A round node at the tip of the spoke (child of the leg so it rotates with it).
            leg.nodeImage = UIFactory.MakeImage("Node", leg.legRect, LegDim, UIFactory.Circle());
            RectTransform nodeRect = leg.nodeImage.rectTransform;
            nodeRect.anchorMin = new Vector2(0.5f, 1f);
            nodeRect.anchorMax = new Vector2(0.5f, 1f);
            nodeRect.pivot = new Vector2(0.5f, 0.5f);
            nodeRect.anchoredPosition = Vector2.zero;
            nodeRect.sizeDelta = new Vector2(30f, 30f);

            // The label: lives on a separate layer that does NOT rotate, so the text stays upright.
            leg.labelBackground = UIFactory.MakeImage("Label" + index, labelsLayer, LabelBgDim);
            leg.labelRect = leg.labelBackground.rectTransform;
            UIFactory.PlaceCentered(leg.labelRect, 0f, 0f, labelSize.x, labelSize.y);

            leg.labelText = UIFactory.MakeText("Text", leg.labelRect, font, 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(leg.labelText.rectTransform, 14f, 8f, 14f, 8f);
            leg.labelText.text = option.wheelLabel;

            return leg;
        }

        void ClearLegs()
        {
            for (int i = 0; i < legs.Count; i++)
            {
                if (legs[i].legRect != null) Destroy(legs[i].legRect.gameObject);
                if (legs[i].labelRect != null) Destroy(legs[i].labelRect.gameObject);
            }
            legs.Clear();
        }

        // ------------------------------------------------------------
        // STEP 3: every frame - animate, read input, lay out.
        // ------------------------------------------------------------
        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            openAmount = Mathf.MoveTowards(openAmount, isOpen ? 1f : 0f, dt / Mathf.Max(0.01f, openDuration));
            ApplyScale();

            if (!isOpen && openAmount <= 0f)
            {
                gameObject.SetActive(false); // finished closing
                return;
            }

            if (isOpen && interactive && Time.unscaledTime >= ignoreInputUntil)
                HandleInput();

            // SmoothDampAngle eases toward the target and takes the shortest way round.
            // Docs: https://docs.unity3d.com/ScriptReference/Mathf.SmoothDampAngle.html
            wheelAngle = Mathf.SmoothDampAngle(wheelAngle, targetAngle, ref angleVelocity, rotateSmoothTime, Mathf.Infinity, dt);

            Layout();
        }

        void HandleInput()
        {
            if (legs.Count == 0) return;

            int step = InterrogationInput.NavigateStep();
            if (step != 0 && legs.Count > 1) MoveSelection(step);

            if (InterrogationInput.SubmitPressed())
            {
                Confirm();
                return;
            }

            Vector2 pointer;
            if (InterrogationInput.PointerPressed(out pointer))
            {
                for (int i = 0; i < legs.Count; i++)
                {
                    // null camera = Screen Space Overlay canvas
                    if (!RectTransformUtility.RectangleContainsScreenPoint(legs[i].labelRect, pointer, null)) continue;

                    if (i == selected) Confirm();
                    else MoveSelectionTo(i);
                    break;
                }
            }
        }

        void MoveSelectionTo(int index)
        {
            int delta = index - selected;
            int count = legs.Count;
            // Take the shortest way round the wheel.
            if (Mathf.Abs(delta) > count / 2) delta -= (int)Mathf.Sign(delta) * count;
            MoveSelection(delta);
        }

        void MoveSelection(int delta)
        {
            int count = legs.Count;
            selected = ((selected + delta) % count + count) % count; // positive modulo (wraps around)
            targetAngle += delta * stepAngle;

            PlaySfx(Sfx.WheelTick);
            if (SelectionChanged != null) SelectionChanged(selected);
        }

        void Confirm()
        {
            interactive = false;
            PlaySfx(Sfx.Confirm);
            if (OptionChosen != null) OptionChosen(selected);
        }

        // Position and style every leg for the current wheel rotation.
        void Layout()
        {
            for (int i = 0; i < legs.Count; i++)
            {
                Leg leg = legs[i];

                float angle = i * stepAngle - wheelAngle;      // this leg's angle on screen (0 = top)
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));

                leg.legRect.localRotation = Quaternion.Euler(0f, 0f, -angle);

                // Label sits just beyond the node: pushed out sideways (so it never covers the node)
                // and a little vertically. 230 / 50 are tuned for the default 400x96 label.
                Vector2 labelPos = dir * radius + new Vector2(dir.x * 230f, dir.y * 50f);
                leg.labelRect.anchoredPosition = labelPos;

                // focus = 1 when this leg is at the top, falling to 0 half a step away.
                float focus = Mathf.Clamp01(1f - Mathf.Abs(Mathf.DeltaAngle(angle, 0f)) / (stepAngle * 0.5f));

                leg.labelRect.localScale = Vector3.one * Mathf.Lerp(0.88f, 1.08f, focus);
                leg.labelBackground.color = Color.Lerp(LabelBgDim, LabelBgActive, focus);
                leg.labelText.color = Color.Lerp(TextDim, Color.white, focus);
                leg.legImage.color = Color.Lerp(LegDim, LegActive, focus);
                leg.nodeImage.color = Color.Lerp(LegDim, LegActive, focus);
            }
        }

        void ApplyScale()
        {
            // "Ease out back": overshoots slightly then settles, which feels springy.
            // Formula from https://easings.net/#easeOutBack
            float t = openAmount - 1f;
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float eased = 1f + c3 * t * t * t + c1 * t * t;
            root.localScale = Vector3.one * Mathf.Max(0f, eased);
        }

        void PlaySfx(Sfx sfx)
        {
            if (audioPlayer != null) audioPlayer.Play(sfx);
        }
    }
}
