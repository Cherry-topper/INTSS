using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace F3.Interrogation
{
    // =====================================================================
    // InterrogationManager.cs
    // ---------------------------------------------------------------------
    // THE ONLY SCRIPT YOU ADD TO A GAMEOBJECT. It runs the whole interrogation:
    //
    //   opening dialogue
    //     -> for each question (A, B, C):
    //          Frankcesca asks -> wheel opens -> player picks a leg
    //          -> cooperation meter changes -> Vivek responds
    //     -> cooperation == 5/5 ?  YES: crucial testimony, TESTIMONY UNLOCKED
    //                              NO : Vivek shuts down, INTERROGATION FAILED
    //                                   -> the player MUST press RETRY
    //                                   -> the interrogation restarts from the top
    //
    // It is written as one coroutine so the flow reads top-to-bottom like the
    // script. (A coroutine is a method that can pause with "yield return" and
    // continue later - perfect for "wait until the player presses a key".)
    // Intro to coroutines: https://docs.unity3d.com/Manual/Coroutines.html
    // =====================================================================
    public class InterrogationManager : MonoBehaviour
    {
        [Header("Behaviour")]
        [Tooltip("Start automatically when you press Play. Untick to start it yourself with BeginInterrogation().")]
        public bool startOnPlay = true;

        [Tooltip("After a failed attempt, skip Frankcesca's opening lines when retrying.")]
        public bool skipOpeningOnRetry = false;

        [Tooltip("Print step-by-step messages to the Console so you can follow what is happening.")]
        public bool logSteps = true;

        [Header("Optional")]
        [Tooltip("Your own font. Leave empty to use Unity's built-in font.")]
        public Font uiFont;

        [Header("Hook up the rest of your game here")]
        [Tooltip("Fires once when the player reaches 5/5 and Vivek gives his testimony.")]
        public UnityEvent onTestimonyUnlocked;
        [Tooltip("Fires every time an attempt ends below 5/5.")]
        public UnityEvent onInterrogationFailed;
        [Tooltip("Fires when the player presses RETRY.")]
        public UnityEvent onRetry;

        /// <summary>Current cooperation value (0-5).</summary>
        public int Cooperation { get; private set; }

        /// <summary>true once the testimony has been unlocked. Other scripts can read this.</summary>
        public bool TestimonyUnlocked { get; private set; }

        /// <summary>How many times the player has failed so far.</summary>
        public int FailedAttempts { get; private set; }

        InterrogationScript script;
        InterrogationUI ui;
        InterrogationAudio audioPlayer;
        Coroutine running;
        int chosenIndex = -1; // -1 = nothing chosen yet

        // ------------------------------------------------------------
        // SETUP
        // ------------------------------------------------------------
        void Awake()
        {
            Step("Setup: loading the Dialogue 4 script.");
            script = Dialogue4Script.Create();

            Step("Setup: creating the audio player (placeholder beeps unless you assign clips).");
            audioPlayer = GetComponent<InterrogationAudio>();
            if (audioPlayer == null) audioPlayer = gameObject.AddComponent<InterrogationAudio>();

            Step("Setup: building the UI from code (canvas, wheel, meter, dialogue box).");
            var uiObject = new GameObject("InterrogationUI");
            uiObject.transform.SetParent(transform, false);
            ui = uiObject.AddComponent<InterrogationUI>();
            ui.Build(uiFont, audioPlayer, script.maxCooperation);

            ui.Wheel.OptionChosen += OnOptionChosen;
        }

        void Start()
        {
            if (startOnPlay) BeginInterrogation();
        }

        void OnDestroy()
        {
            if (ui != null && ui.Wheel != null) ui.Wheel.OptionChosen -= OnOptionChosen;
        }

        void OnOptionChosen(int index)
        {
            chosenIndex = index; // the coroutine below is waiting for this to become >= 0
        }

        // ------------------------------------------------------------
        // PUBLIC API
        // ------------------------------------------------------------
        /// <summary>Start (or restart) the interrogation from the beginning.</summary>
        public void BeginInterrogation()
        {
            if (running != null) StopCoroutine(running);
            TestimonyUnlocked = false;
            FailedAttempts = 0;
            running = StartCoroutine(RunInterrogation());
        }

        /// <summary>Stop and hide the interrogation.</summary>
        public void StopInterrogation()
        {
            if (running != null) StopCoroutine(running);
            running = null;
            ui.Wheel.Close();
            ui.Hide();
        }

        // ------------------------------------------------------------
        // THE MAIN FLOW
        // ------------------------------------------------------------
        IEnumerator RunInterrogation()
        {
            ui.Show();
            bool firstAttempt = true;

            // "while (true)" = keep going until we reach a "yield break" (only on success).
            // That is exactly the "you have to retry" rule: failure loops back to the top.
            while (true)
            {
                // ---- STEP 1: reset ----
                Cooperation = script.startCooperation;
                ui.Meter.SetValue(Cooperation, false);
                Step("Attempt " + (FailedAttempts + 1) + " begins. Cooperation = " + Cooperation + "/" + script.maxCooperation);

                // ---- STEP 2: opening dialogue ----
                if (firstAttempt || !skipOpeningOnRetry)
                {
                    Step("Opening dialogue.");
                    yield return PlayLines(script.opening);
                }
                firstAttempt = false;

                // ---- STEP 3: the three questions ----
                for (int q = 0; q < script.questions.Count; q++)
                {
                    InterrogationQuestion question = script.questions[q];
                    Step("Question " + question.id + ": Frankcesca asks, then the wheel opens.");

                    yield return PlayLine(new DialogueLine(Speaker.Frankcesca, question.prompt));

                    // Open the wheel and wait until the player confirms a leg.
                    chosenIndex = -1;
                    ui.OpenWheel(question);
                    while (chosenIndex < 0) yield return null;

                    ui.CloseWheel();
                    DialogueOption option = question.options[chosenIndex];
                    Step("Player chose '" + option.wheelLabel + "' (cooperation change " + option.cooperationChange + ").");

                    // If the option is a spoken sentence, Frankcesca says it first.
                    if (option.playerSpeaksText)
                        yield return PlayLine(new DialogueLine(Speaker.Frankcesca, option.fullText));

                    // The meter moves BEFORE Vivek replies, so you see the consequence of your words.
                    ChangeCooperation(option.cooperationChange);

                    yield return PlayLines(option.responseLines);
                }

                // ---- STEP 4: verdict ----
                bool success = Cooperation >= script.maxCooperation;
                Step("Questions finished. Cooperation = " + Cooperation + "/" + script.maxCooperation +
                     (success ? " -> SUCCESS" : " -> FAIL"));

                if (success)
                {
                    PlaySfx(Sfx.Success);
                    yield return PlayLines(script.successLines);

                    TestimonyUnlocked = true;
                    ui.ShowTestimony(script.testimonyText);
                    onTestimonyUnlocked.Invoke();

                    yield return WaitForAdvance();
                    Step("Testimony acknowledged. Interrogation finished.");
                    ui.Hide();
                    running = null;
                    yield break; // leaves the loop: the only way out
                }

                // ---- FAIL: must retry ----
                FailedAttempts++;
                PlaySfx(Sfx.Fail);
                yield return PlayLines(script.failLines);

                ui.ShowFailed(Cooperation, script.maxCooperation);
                onInterrogationFailed.Invoke();

                // Short lock-out so a double-click on the last line cannot skip the screen.
                yield return new WaitForSecondsRealtime(0.5f);
                yield return null;

                // There is no "quit" here on purpose. The only way forward is RETRY.
                while (!ui.RetryPressed()) yield return null;

                Step("RETRY pressed. Restarting the interrogation.");
                ui.HideVerdict();
                onRetry.Invoke();
                // ...and the while(true) loop goes round again.
            }
        }

        // ------------------------------------------------------------
        // HELPERS
        // ------------------------------------------------------------
        void ChangeCooperation(int change)
        {
            Cooperation = Mathf.Clamp(Cooperation + change, 0, script.maxCooperation);
            ui.Meter.SetValue(Cooperation, true); // meter works out the real +/- itself, even when clamped
            Step("Cooperation is now " + Cooperation + "/" + script.maxCooperation);
        }

        IEnumerator PlayLines(IList<DialogueLine> lines)
        {
            for (int i = 0; i < lines.Count; i++)
                yield return PlayLine(lines[i]);
        }

        // Types one line. First press skips to the end of the line, next press moves on.
        IEnumerator PlayLine(DialogueLine line)
        {
            ui.ShowLine(line.speaker, line.text);

            // Wait one frame so the key press that brought us here is not counted as "skip".
            yield return null;

            while (ui.IsTyping)
            {
                if (InterrogationInput.AdvancePressed()) ui.SkipTyping();
                yield return null;
            }

            ui.ShowContinuePrompt(true);
            yield return WaitForAdvance();
            ui.ShowContinuePrompt(false);
        }

        // Waits until the player presses continue. Starts with a frame's pause for the same reason as above.
        IEnumerator WaitForAdvance()
        {
            yield return null;
            while (!InterrogationInput.AdvancePressed()) yield return null;
        }

        void PlaySfx(Sfx sfx)
        {
            if (audioPlayer != null) audioPlayer.Play(sfx);
        }

        void Step(string message)
        {
            if (logSteps) Debug.Log("[Interrogation] " + message, this);
        }
    }
}
