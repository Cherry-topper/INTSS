using UnityEngine;

namespace F3.Interrogation
{
    /// <summary>The UI sound effects the interrogation can play.</summary>
    public enum Sfx
    {
        WheelTick,
        Confirm,
        MeterUp,
        MeterDown,
        Success,
        Fail
    }

    // =====================================================================
    // InterrogationAudio.cs
    // ---------------------------------------------------------------------
    // Plays every interrogation sound. Leave the clip slots empty and you get
    // generated placeholder beeps. Drag real clips in later and they replace
    // the beeps automatically.
    //
    // Where to get real UI sounds: Unity Asset Store (search "UI SFX"),
    // kenney.nl/assets (free, CC0 "UI Audio" and "Interface Sounds" packs),
    // or freesound.org (check each licence). Import .wav or .ogg files into
    // Assets, then drag them onto the slots below.
    // =====================================================================
    public class InterrogationAudio : MonoBehaviour
    {
        [Header("Optional: assign your own clips (empty = generated beep)")]
        public AudioClip wheelTick;
        public AudioClip confirm;
        public AudioClip textBlip;
        public AudioClip meterUp;
        public AudioClip meterDown;
        public AudioClip success;
        public AudioClip fail;

        [Range(0f, 1f)] public float volume = 0.7f;

        AudioSource sfxSource;
        AudioSource blipSource; // separate source so the typing blip can change pitch per speaker
        bool ready;

        void Awake()
        {
            Setup();
        }

        // Done lazily too, in case something plays a sound before Awake has run.
        void Setup()
        {
            if (ready) return;
            ready = true;

            sfxSource = CreateSource();
            blipSource = CreateSource();

            if (wheelTick == null) wheelTick = ProceduralSfx.Blip("wheelTick", 900f, 900f, 0.04f, 0.5f);
            if (confirm == null) confirm = ProceduralSfx.Blip("confirm", 600f, 1000f, 0.12f, 0.5f);
            if (textBlip == null) textBlip = ProceduralSfx.Blip("textBlip", 520f, 480f, 0.03f, 0.35f);
            if (meterUp == null) meterUp = ProceduralSfx.Blip("meterUp", 500f, 900f, 0.22f, 0.5f);
            if (meterDown == null) meterDown = ProceduralSfx.Blip("meterDown", 450f, 200f, 0.28f, 0.5f);
            if (success == null) success = ProceduralSfx.Blip("success", 500f, 1300f, 0.6f, 0.5f);
            if (fail == null) fail = ProceduralSfx.Blip("fail", 320f, 110f, 0.7f, 0.5f);
        }

        AudioSource CreateSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 0 = plain 2D UI sound, not positional
            return source;
        }

        public void Play(Sfx sfx)
        {
            Setup();
            AudioClip clip = null;
            switch (sfx)
            {
                case Sfx.WheelTick: clip = wheelTick; break;
                case Sfx.Confirm: clip = confirm; break;
                case Sfx.MeterUp: clip = meterUp; break;
                case Sfx.MeterDown: clip = meterDown; break;
                case Sfx.Success: clip = success; break;
                case Sfx.Fail: clip = fail; break;
            }
            if (clip != null) sfxSource.PlayOneShot(clip, volume);
        }

        /// <summary>Quiet blip played while text is typing. Pitch differs per speaker.</summary>
        public void PlayBlip(float pitch)
        {
            Setup();
            blipSource.pitch = pitch * Random.Range(0.95f, 1.05f); // tiny random wobble sounds less robotic
            blipSource.PlayOneShot(textBlip, volume * 0.5f);
        }
    }
}
