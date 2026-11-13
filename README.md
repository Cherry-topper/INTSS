# F^3 – Interrogation (Dialogue 4: Frankcesca vs Vivek)

Cooperation meter (0–5, starts at 2), three questions with a rotating option wheel, and a
**fail → RETRY** loop. Built for Unity 6 (6000.3), URP, new Input System, legacy UGUI `Text`.

## Install (step by step)

1. Copy the `Assets/Scripts/Interrogation` folder from this repo into your Unity project's `Assets/Scripts/` folder.
2. Wait for Unity to compile. The Console (Window → General → Console) should show no red errors.
3. Open your scene (e.g. `SampleScene`). **GameObject → Create Empty**, name it `Interrogation`.
4. With it selected, click **Add Component** and add **Interrogation Manager**.
5. Press **Play**. The UI builds itself – no Canvas, prefabs or EventSystem needed.

## Controls

| Action | Keyboard / mouse | Gamepad | Touch |
|---|---|---|---|
| Turn wheel | A/D, ←/→, W/S, ↑/↓, mouse scroll | D-pad, left stick | Tap a label |
| Choose option | Enter / Space / click highlighted label | A / Cross | Tap highlighted label |
| Continue / skip typing | Enter / Space / click | A / Cross | Tap |
| Retry | Enter / Space / R / click RETRY | A / Cross | Tap RETRY |

## How the rules work

- Start at **2/5**. Choices change the meter (clamped 0–5).
- After question C: **5/5** → crucial testimony + "TESTIMONY UNLOCKED". **Below 5/5** → Vivek shuts down, "INTERROGATION FAILED",
  and the only way forward is **RETRY**, which restarts the interrogation.
- Reaching 5/5 needs at least +3 total across A, B and C.

## Hooking it into your game

On the **Interrogation Manager** component:
- `On Testimony Unlocked` – fires once on success (unlock a clue, change scene, etc.)
- `On Interrogation Failed` / `On Retry` – fire on each failure / retry

From code: `manager.TestimonyUnlocked`, `manager.Cooperation`, `manager.FailedAttempts`, `manager.BeginInterrogation()`.

Inspector options: `Start On Play`, `Skip Opening On Retry`, `Log Steps` (step-by-step Console messages), `Ui Font`.

## Editing dialogue

Everything is in `Dialogue4Script.cs`. Scores are the `cooperationChange` numbers.
**Note:** option C3 had no spoken line for Frankcesca in the doc; one placeholder line was added – replace it with your own.

## Audio

`InterrogationAudio` generates placeholder beeps (wheel tick, confirm, typing blip, meter up/down, success, fail).
Drag real clips into its slots to replace them. Free UI sound sources: kenney.nl/assets, freesound.org (check licences).

## Files

| File | Job |
|---|---|
| `InterrogationManager.cs` | Flow/rules (add this one to a GameObject) |
| `Dialogue4Script.cs` / `InterrogationData.cs` | Dialogue text + data classes |
| `InterrogationUI.cs` | Builds the screen, dialogue box, retry overlay |
| `WheelMenu.cs` | Rotating wheel |
| `CooperationMeter.cs` | 0–5 meter |
| `Typewriter.cs` | Text animation |
| `InterrogationInput.cs` | New Input System polling |
| `InterrogationAudio.cs`, `ProceduralSfx.cs` | Sound |
| `UIFactory.cs` | UI-building helpers |
