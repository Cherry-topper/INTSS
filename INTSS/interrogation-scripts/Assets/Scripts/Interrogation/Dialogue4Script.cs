using System.Collections.Generic;

namespace F3.Interrogation
{
    // =====================================================================
    // Dialogue4Script.cs
    // ---------------------------------------------------------------------
    // All the words of "DIALOGUE 4" (Frankcesca interrogates Vivek),
    // transcribed from the "Interrogation .pdf" design doc.
    //
    // To change any line, edit it here. Nothing else needs to change.
    //
    // Scoring recap (from the doc):
    //   Start 2/5.  A: +1 / -1 / +1    B: 0 / -1 / +1    C: +2 / -2 / +1
    //   You need exactly 5/5 after question C to unlock the testimony.
    //   Anything lower = Vivek shuts down and you must RETRY.
    // =====================================================================
    public static class Dialogue4Script
    {
        // Tiny helpers so the dialogue below reads like a script.
        static DialogueLine F(string text) { return new DialogueLine(Speaker.Frankcesca, text); }
        static DialogueLine V(string text) { return new DialogueLine(Speaker.Vivek, text); }

        public static InterrogationScript Create()
        {
            var s = new InterrogationScript
            {
                startCooperation = 2,
                maxCooperation = 5
            };

            // ------------------------------------------------------------
            // OPENING
            // ------------------------------------------------------------
            s.opening.Add(F("You signed the forensic reviews on all four cases. I want to understand the common workflow. I want to state that this is not an accusation or attempt to implicate you."));
            s.opening.Add(V("That's a careful way to begin an uncomfortable conversation."));
            s.opening.Add(F("I prefer uncomfortable conversations to inaccurate reports."));

            // ------------------------------------------------------------
            // QUESTION A
            // ------------------------------------------------------------
            var qA = new InterrogationQuestion
            {
                id = "A",
                prompt = "The records show the same correction. Is that standard procedure?"
            };

            // A1 (+1) - action: Frankcesca's words are the first response line.
            qA.options.Add(new DialogueOption
            {
                wheelLabel = "Present the CI-40-3019 timeline",
                fullText = "Present the CI-40-3019 timeline.",
                cooperationChange = +1,
                playerSpeaksText = false,
                responseLines = new List<DialogueLine>
                {
                    F("Tebo Dlamini's file records a correction at 14:08 and a normal-range reading five minutes later, marked successful. The originating operator field isn't in my view. Is that normal?"),
                    V("It's not unusual for a case view to omit fields. The source service and the case service don't show every user the same thing."),
                    F("But someone can see it."),
                    V("Someone with the right access.")
                }
            });

            // A2 (-1) - spoken accusation.
            qA.options.Add(new DialogueOption
            {
                wheelLabel = "\"Was that your doing?\"",
                fullText = "These files read as though they were written to avoid explaining anything. Was that your doing?",
                cooperationChange = -1,
                playerSpeaksText = true,
                responseLines = new List<DialogueLine>
                {
                    V("If you've already decided I'm bad at my job, I don't see what you need from me.")
                }
            });

            // A3 (+1) - spoken.
            qA.options.Add(new DialogueOption
            {
                wheelLabel = "Incomplete vs. incorrect",
                fullText = "I'm trying to tell an incomplete file from an incorrect one.",
                cooperationChange = +1,
                playerSpeaksText = true,
                responseLines = new List<DialogueLine>
                {
                    V("That's a distinction people forget. An incomplete file can pass validation if the missing fields aren't mandatory for its classification."),
                    F("So it can be valid as a record and still fail to explain the event."),
                    V("Yes. Those are two different tests.")
                }
            });
            s.questions.Add(qA);

            // ------------------------------------------------------------
            // QUESTION B
            // ------------------------------------------------------------
            var qB = new InterrogationQuestion
            {
                id = "B",
                prompt = "Who authorised these events?"
            };

            // B1 (+0) - kind, but naming his family sounds like leverage.
            qB.options.Add(new DialogueOption
            {
                wheelLabel = "Acknowledge the risk to his position",
                fullText = "Acknowledge the risk to his position.",
                cooperationChange = 0,
                playerSpeaksText = false,
                responseLines = new List<DialogueLine>
                {
                    F("I understand why this is hard to talk about. Your job, your family, your access to care. I'm not asking you to pretend those things don't matter. I'm asking what the record says."),
                    V("That's the problem, isn't it? The record says enough to pass review. I don't need to tell anyone anything beyond that unless someone with higher authorisation asks me. And don't bring my family into this."),
                    F("Understood.")
                }
            });

            // B2 (-1) - threat.
            qB.options.Add(new DialogueOption
            {
                wheelLabel = "Threaten to name him in the report",
                fullText = "If you don't cooperate, I'll put your name in the report.",
                cooperationChange = -1,
                playerSpeaksText = true,
                responseLines = new List<DialogueLine>
                {
                    V("My name is already on the reports. That's what you're looking at.")
                }
            });

            // B3 (+1)
            qB.options.Add(new DialogueOption
            {
                wheelLabel = "Ask about the shared review chain",
                fullText = "Ask about the shared review chain.",
                cooperationChange = +1,
                playerSpeaksText = false,
                responseLines = new List<DialogueLine>
                {
                    F("Why were you on all four?"),
                    V("Not because they were connected. I sit on the neural-event death desk. Every closed death with an implant event in the preceding months is routed through it. Four of them were mine."),
                    F("That explains why your name repeats. Does it explain the missing source fields?"),
                    V("No. It explains the assignment, not the data.")
                }
            });
            s.questions.Add(qB);

            // ------------------------------------------------------------
            // QUESTION C
            // ------------------------------------------------------------
            var qC = new InterrogationQuestion
            {
                id = "C",
                prompt = "What happened to the missing entry in CI-41-8821?"
            };

            // C1 (+2)
            qC.options.Add(new DialogueOption
            {
                wheelLabel = "Show the three same-day Index entries",
                fullText = "Show the three same-day Index entries.",
                cooperationChange = +2,
                playerSpeaksText = false,
                responseLines = new List<DialogueLine>
                {
                    F("Three entries on one morning. One has no associated event, yet the neural trace changed. What would normally connect them?"),
                    V("An event token. It tells the case service what authorised the change and where to pull the source trace."),
                    F("There's no token here."),
                    V("Not in the view you're allowed."),
                    F("Is that different from the token being absent at the source?"),
                    V("Very. If the source token were absent, the audit service would report an incomplete source record. If the case view just doesn't show it, the source may still exist.")
                }
            });

            // C2 (-2) - accusation.
            qC.options.Add(new DialogueOption
            {
                wheelLabel = "Ask why he left out the reasons",
                fullText = "Why did you leave out the reason for the interventions?",
                cooperationChange = -2,
                playerSpeaksText = true,
                responseLines = new List<DialogueLine>
                {
                    V("No. I won't sit here and accept an accusation built on a screen you can't fully read.")
                }
            });

            // C3 (+1)
            // NOTE: the doc has no spoken line for Frankcesca here (it only says
            // "Ask him to define 'source record'"), so the first F(...) line below
            // is a placeholder I added so Vivek's answer has something to answer.
            // Edit or replace it with your own wording.
            qC.options.Add(new DialogueOption
            {
                wheelLabel = "Ask him to define \"source record\"",
                fullText = "Ask him to define \"source record.\"",
                cooperationChange = +1,
                playerSpeaksText = false,
                responseLines = new List<DialogueLine>
                {
                    F("When you say \"source record\", what exactly do you mean?"), // <- placeholder, not in the doc
                    V("The case file is a summary. The source record is the underlying clinical or maintenance event and its audit trail. A summary can close while the trail lives somewhere else."),
                    F("Another service?"),
                    V("Usually.")
                }
            });
            s.questions.Add(qC);

            // ------------------------------------------------------------
            // SUCCESS: cooperation reaches 5/5 -> crucial testimony
            // ------------------------------------------------------------
            s.successLines.Add(V("Stop a moment. I can tell you what I saw, but understand what I'm not saying. I didn't authorise a programme. I never met these people. I signed reviews on the records that were delivered to my desk."));
            s.successLines.Add(F("Then tell me what you saw."));
            s.successLines.Add(V("A maintenance event normally carries a source token, a clinician or service identifier, and a reason code. In these files the summary says the event completed, but the token is hidden from the case view or missing from my copy. I assumed a migration fault. Then I saw the same routing label in more than one review."));
            s.successLines.Add(F("What label?"));
            s.successLines.Add(V("\"Continuity override.\" It exists so an authorised service can keep a medical process from being interrupted. I was told not to interpret it. Only to verify that the outcome field matched the submitted report."));
            s.successLines.Add(F("Who told you?"));
            s.successLines.Add(V("I don't have a name I can prove. It came through a supervisory queue. If you need proof, you need the audit trail, not my memory."));
            s.successLines.Add(F("Did you tell Frank?"));
            s.successLines.Add(V("I told him the files were incomplete. He said incomplete was becoming the explanation for everything. He wanted the source tokens. I told him to file a formal request."));
            s.successLines.Add(F("He did."));
            s.successLines.Add(V("I know. I watched it come back out of scope. A day later my supervisor reminded me not to access source logs outside my assigned review. I have a wife and two children, Investigator Clark. I am not proud of how quickly I understood what that meant."));

            s.testimonyText =
                "\"Continuity override\" was attached to the routing of several neural-health events. " +
                "Vivek cannot name who authorised the commands, and his statement needs corroboration from an audit trail.";

            // ------------------------------------------------------------
            // FAIL: below 5/5 -> must retry
            // ------------------------------------------------------------
            s.failLines.Add(V("I'm sorry. I don't have anything more I can responsibly say without the source logs. I'd suggest requesting them through Records Integrity."));
            s.failLines.Add(F("I will."));
            s.failLines.Add(V("Then that's all I can do for you."));

            return s;
        }
    }
}
