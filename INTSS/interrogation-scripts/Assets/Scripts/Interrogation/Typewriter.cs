using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace F3.Interrogation
{
    // =====================================================================
    // Typewriter.cs
    // ---------------------------------------------------------------------
    // Reveals text one character at a time (the classic dialogue effect).
    //
    // How it works: a coroutine adds (deltaTime * charsPerSecond) to a counter
    // every frame and shows that many characters. Counting with time instead
    // of "wait 0.02s per letter" keeps the speed identical at 30, 60 or 144 FPS.
    //
    // Tweak in the Inspector (or from code): charsPerSecond for speed,
    // punctuationPause for the little beat after . , ? ! ...
    // =====================================================================
    public class Typewriter : MonoBehaviour
    {
        public Text target;
        public float charsPerSecond = 55f;
        public float punctuationPause = 0.12f;
        [Tooltip("Play a blip every N visible characters.")]
        public int blipEvery = 2;

        /// <summary>Pitch multiplier for the blip (set per speaker).</summary>
        [NonSerialized] public float blipPitch = 1f;

        /// <summary>Raised each time new text appears. UI uses it to play the blip sound.</summary>
        public Action<float> onBlip;

        public bool IsTyping { get; private set; }

        Coroutine routine;
        string fullText = "";

        public void Play(string text)
        {
            Stop();
            fullText = text ?? "";
            target.text = "";
            IsTyping = fullText.Length > 0;
            if (IsTyping) routine = StartCoroutine(Run());
        }

        /// <summary>Jump straight to the full text (used when the player presses a key while typing).</summary>
        public void Complete()
        {
            if (!IsTyping) return;
            Stop();
            target.text = fullText;
        }

        public void Stop()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            IsTyping = false;
        }

        IEnumerator Run()
        {
            float shown = 0f;
            int count = 0;

            while (count < fullText.Length)
            {
                // Time.unscaledDeltaTime keeps working even if you set Time.timeScale = 0 (pause menus).
                shown += Time.unscaledDeltaTime * charsPerSecond;
                int wanted = Mathf.Min(fullText.Length, Mathf.FloorToInt(shown));

                if (wanted > count)
                {
                    count = wanted;
                    target.text = fullText.Substring(0, count);

                    char last = fullText[count - 1];
                    if (!char.IsWhiteSpace(last) && blipEvery > 0 && count % blipEvery == 0 && onBlip != null)
                        onBlip(blipPitch);

                    if (IsPausePunctuation(last) && count < fullText.Length)
                        yield return new WaitForSecondsRealtime(punctuationPause);
                }

                yield return null;
            }

            routine = null;
            IsTyping = false;
        }

        static bool IsPausePunctuation(char c)
        {
            return c == '.' || c == ',' || c == '?' || c == '!' || c == ';' || c == ':';
        }
    }
}
