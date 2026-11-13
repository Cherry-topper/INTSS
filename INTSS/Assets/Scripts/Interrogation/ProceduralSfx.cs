using UnityEngine;

namespace F3.Interrogation
{
    // =====================================================================
    // ProceduralSfx.cs
    // ---------------------------------------------------------------------
    // Generates tiny placeholder beeps in code so the UI has sound straight
    // away, before you have any real audio files. Replace them later by
    // dragging real AudioClips into the InterrogationAudio component.
    //
    // The idea (a sine wave with a fade-out, written into an AudioClip) is
    // the standard Unity pattern from the AudioClip.Create / SetData docs:
    // https://docs.unity3d.com/ScriptReference/AudioClip.Create.html
    // =====================================================================
    public static class ProceduralSfx
    {
        const int SampleRate = 44100;

        /// <summary>
        /// A short beep. startHz -> endHz lets the pitch slide up or down
        /// (up = positive feeling, down = negative feeling).
        /// </summary>
        public static AudioClip Blip(string name, float startHz, float endHz, float seconds, float volume)
        {
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * seconds));
            var data = new float[sampleCount];
            float phase = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleCount;                       // 0..1 through the clip
                float hz = Mathf.Lerp(startHz, endHz, t);               // sliding pitch
                phase += 2f * Mathf.PI * hz / SampleRate;               // advance the sine wave

                float fadeIn = Mathf.Clamp01(i / (SampleRate * 0.004f)); // 4 ms ramp-up removes the "click"
                float fadeOut = (1f - t) * (1f - t);                     // smooth fade-out
                data[i] = Mathf.Sin(phase) * fadeIn * fadeOut * volume;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
