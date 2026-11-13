using System;
using System.Collections.Generic;

namespace F3.Interrogation
{
    // =====================================================================
    // InterrogationData.cs
    // ---------------------------------------------------------------------
    // Plain data classes that describe an interrogation. They hold NO game
    // logic - they are just the "shape" of the dialogue so the dialogue text
    // can live in one place (Dialogue4Script.cs) and be edited without
    // touching the systems that play it.
    //
    // [Serializable] means Unity could show these in the Inspector later if
    // you ever move this data into a ScriptableObject.
    // =====================================================================

    /// <summary>Who is talking. Used to pick the name label, colour and typing-blip pitch.</summary>
    public enum Speaker
    {
        Frankcesca,
        Vivek
    }

    /// <summary>One line of spoken dialogue.</summary>
    [Serializable]
    public class DialogueLine
    {
        public Speaker speaker;
        public string text;

        public DialogueLine(Speaker speaker, string text)
        {
            this.speaker = speaker;
            this.text = text;
        }
    }

    /// <summary>
    /// One leg of the wheel = one thing the player can choose to say or do.
    /// </summary>
    [Serializable]
    public class DialogueOption
    {
        /// <summary>Short text shown on the wheel leg.</summary>
        public string wheelLabel;

        /// <summary>
        /// Full text shown in the dialogue box while this leg is highlighted.
        /// If playerSpeaksText is true, Frankcesca also says this out loud after you pick it.
        /// </summary>
        public string fullText;

        /// <summary>How much this choice changes the COOPERATION meter (can be negative or 0).</summary>
        public int cooperationChange;

        /// <summary>
        /// true  = the option is a spoken sentence (Frankcesca says fullText first).
        /// false = the option is an action/approach ("Present the timeline") and
        ///         responseLines already begins with Frankcesca's actual words.
        /// </summary>
        public bool playerSpeaksText;

        /// <summary>The conversation that plays after the option is chosen.</summary>
        public List<DialogueLine> responseLines = new List<DialogueLine>();
    }

    /// <summary>One question (A, B or C): a prompt plus the wheel options for it.</summary>
    [Serializable]
    public class InterrogationQuestion
    {
        public string id;
        public string prompt; // Frankcesca says this, then the wheel opens
        public List<DialogueOption> options = new List<DialogueOption>();
    }

    /// <summary>The whole interrogation.</summary>
    [Serializable]
    public class InterrogationScript
    {
        public int startCooperation = 2;
        public int maxCooperation = 5;

        public List<DialogueLine> opening = new List<DialogueLine>();
        public List<InterrogationQuestion> questions = new List<InterrogationQuestion>();

        /// <summary>Plays when the meter is full (5/5) after the last question.</summary>
        public List<DialogueLine> successLines = new List<DialogueLine>();

        /// <summary>Plays when the meter is below 5/5 after the last question. Then the player must RETRY.</summary>
        public List<DialogueLine> failLines = new List<DialogueLine>();

        /// <summary>Text shown on the "TESTIMONY UNLOCKED" screen.</summary>
        public string testimonyText;
    }
}
