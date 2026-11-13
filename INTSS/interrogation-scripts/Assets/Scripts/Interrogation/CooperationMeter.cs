using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace F3.Interrogation
{
    // =====================================================================
    // CooperationMeter.cs
    // ---------------------------------------------------------------------
    // The COOPERATION 0-5 meter (top-left). Shows pips plus a number, so it is
    // readable without relying on colour alone. When the value changes it
    // pulses, floats a "+1" / "-2" next to the number and plays a sound.
    //
    // Reminder from the design doc: this is NOT a morality score. It measures
    // how safe Vivek feels saying something useful.
    // =====================================================================
    public class CooperationMeter : MonoBehaviour
    {
        static readonly Color PanelColor = new Color(0.04f, 0.05f, 0.08f, 0.90f);
        static readonly Color EmptyPip = new Color(1f, 1f, 1f, 0.14f);
        static readonly Color FilledPip = new Color(1f, 0.78f, 0.30f, 1f);
        static readonly Color GainColor = new Color(0.55f, 1f, 0.65f, 1f);
        static readonly Color LossColor = new Color(1f, 0.45f, 0.45f, 1f);

        const float PipWidth = 64f;
        const float PipHeight = 30f;
        const float PipGap = 10f;

        RectTransform rect;
        Image[] pips;
        Text valueText;
        Text deltaText;
        Vector2 deltaBasePosition;
        InterrogationAudio audioPlayer;
        int max;
        int current;
        Coroutine pulse;

        public void Build(Font font, int maxValue, InterrogationAudio audioPlayer)
        {
            max = maxValue;
            this.audioPlayer = audioPlayer;

            rect = (RectTransform)transform;
            UIFactory.PlaceTopLeft(rect, 48f, -40f, 680f, 120f);

            Image panel = UIFactory.MakeImage("Panel", rect, PanelColor);
            UIFactory.Stretch(panel.rectTransform, 0, 0, 0, 0);

            Text title = UIFactory.MakeText("Title", rect, font, 24, new Color(1f, 1f, 1f, 0.7f),
                                            TextAnchor.UpperLeft, FontStyle.Bold);
            title.text = "COOPERATION";
            UIFactory.PlaceTopLeft(title.rectTransform, 24f, -12f, 400f, 32f);

            pips = new Image[max];
            for (int i = 0; i < max; i++)
            {
                pips[i] = UIFactory.MakeImage("Pip" + i, rect, EmptyPip);
                UIFactory.PlaceTopLeft(pips[i].rectTransform, 24f + i * (PipWidth + PipGap), -60f, PipWidth, PipHeight);
            }

            float afterPips = 24f + max * (PipWidth + PipGap) + 10f;

            valueText = UIFactory.MakeText("Value", rect, font, 38, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.PlaceTopLeft(valueText.rectTransform, afterPips, -50f, 130f, 50f);

            deltaText = UIFactory.MakeText("Delta", rect, font, 32, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            deltaBasePosition = new Vector2(afterPips + 125f, -50f);
            UIFactory.PlaceTopLeft(deltaText.rectTransform, deltaBasePosition.x, deltaBasePosition.y, 100f, 50f);
            deltaText.text = "";

            Refresh();
        }

        /// <summary>Set the meter. animate=false for silent resets (e.g. on retry).</summary>
        public void SetValue(int value, bool animate)
        {
            value = Mathf.Clamp(value, 0, max);
            int change = value - current;
            current = value;
            Refresh();

            if (!animate || change == 0 || !isActiveAndEnabled) return;

            if (audioPlayer != null) audioPlayer.Play(change > 0 ? Sfx.MeterUp : Sfx.MeterDown);
            if (pulse != null) StopCoroutine(pulse);
            pulse = StartCoroutine(Pulse(change));
        }

        void Refresh()
        {
            for (int i = 0; i < pips.Length; i++)
                pips[i].color = i < current ? FilledPip : EmptyPip;
            valueText.text = current + " / " + max;
        }

        IEnumerator Pulse(int change)
        {
            Color flash = change > 0 ? GainColor : LossColor;
            deltaText.text = change > 0 ? "+" + change : change.ToString(); // negatives already include "-"

            const float duration = 1.1f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);

                // Quick "bump" in size during the first 30% of the animation.
                float bump = Mathf.Sin(Mathf.Clamp01(k / 0.3f) * Mathf.PI);
                rect.localScale = Vector3.one * (1f + 0.07f * bump);

                // The +1 / -2 drifts up and fades out.
                deltaText.color = new Color(flash.r, flash.g, flash.b, 1f - k);
                deltaText.rectTransform.anchoredPosition = deltaBasePosition + new Vector2(0f, 24f * k);

                yield return null;
            }

            deltaText.text = "";
            rect.localScale = Vector3.one;
            pulse = null;
        }
    }
}
