# Controller input layer

## Scope

Q2 is a read-only controller side-observation investigation. It does not patch
SMT3, create or replace a virtual controller, hide a device, suppress input, or
change the normal `Controller -> reWASD -> Steam Input -> SMT3` path.

## Provider decision

GameInput was checked first. `C:\Windows\System32\GameInput.dll` is installed,
but the development machine does not currently have `GameInput.h`,
`GameInput.lib`, or a configured C++ compiler. Microsoft documents the current
PC development path through the `Microsoft.GameInput` NuGet package. Per the Q2
setup rule, the investigation did not pause to install another SDK and moved to
Windows.Gaming.Input (WGI), which is already buildable in this repository.

References:

- <https://learn.microsoft.com/en-us/gaming/gdk/docs/features/common/input/overviews/input-faq>
- <https://learn.microsoft.com/en-us/gaming/gdk/docs/reference/input/gameinput/functions/gameinputcreate>

SDL remains the next fallback if WGI cannot expose the live post-reWASD device.
Raw Input/HID remains last-resort diagnostics only.

The D-condition WGI run enumerated one `Xbox One Game Controller`
(`VID 045E`, `PID 0B00`) but returned zero for every stick, trigger, button,
raw axis, and timestamp across 276 snapshots. WGI therefore failed the D target
and the probe was extended with an SDL3 provider.

The first SDL side-read run enumerated `Xbox One Controller` on `XInput#0`
(`VID 045E`, `PID 02FF`) for 298 snapshots while SMT3 retained focus. Both
right-stick axes and both left-stick axes reached their full negative and
positive ranges (`-32768..32767`), and face buttons 0-3 were observed. The
standardized trigger values remained zero, while raw axes 2 and 5 traversed the
full range. This is a PASS for the primary Q2 right-stick side-read criterion.

## A-D comparison result

All four labelled captures completed with 298 snapshots each and exposed the
same SDL candidate and input ranges. Because closing the reWASD UI alone does
not establish its mapping-power state, clean focused retests were performed for
A, C, and the real target D with the mapping state explicitly confirmed.

| Condition | Candidate | Right stick X/Y | Left stick X/Y | Face buttons | Trigger observation |
|---|---|---|---|---|---|
| A: reWASD OFF / Steam OFF | `Xbox One Controller`, `XInput#0` | `-32768..32767` | `-32768..32767` | 0, 1, 2, 3 | raw axes 2/5 changed; clean retest confirmed |
| B: reWASD OFF / Steam ON | `Xbox One Controller`, `XInput#0` | `-32768..32767` | `-32768..32767` | 0, 1, 2, 3 | raw axes 2/5 changed |
| C: reWASD ON / Steam OFF | `Xbox One Controller`, `XInput#0` | `-32768..32767` | `-32768..32767` | 0, 1, 2, 3 | focused retest confirmed; standardized triggers stayed zero |
| D: reWASD ON / Steam ON | `Xbox One Controller`, `XInput#0` | `-32768..32767` | `-32768..32767` | 0, 1, 2, 3 | focused real-target retest confirmed; raw axes 2/5 changed |

The SDL standardized trigger fields were zero in all runs, but raw axes 2 and 5
traversed their complete signed ranges when LT and RT were operated. Trigger
normalization is therefore a later mapping detail, not an input visibility
failure.

The clean A retest was performed only after explicitly switching the reWASD
mapping power OFF and leaving Steam Input OFF. Across 298 snapshots, both
sticks again covered `-32768..32767` on both axes and face buttons 0-3 were
observed. Standardized LT/RT remained zero in that retest. Although the probe
file was initially tagged `D` during the interactive session, it represents
condition A by the matrix above; the documentation uses the actual environment
rather than the mistaken runtime label.

A clean condition C retest was then performed with reWASD mapping explicitly
ON, Steam Input OFF, and SMT3 focused. Across 298 snapshots, both sticks covered
`-32768..32767` on both axes and face buttons 0-3 were observed. This differs
from an earlier C rerun in which all controls stayed near neutral while the game
was not focused. The focused result shows that the earlier no-input capture was
a focus-condition artifact, not loss of SDL controller visibility. Standardized
LT/RT remained zero and still require separate normalization work.

The final condition D retest was performed with reWASD mapping ON, Steam Input
ON, and SMT3 focused. It enumerated one `Xbox One Controller` on `XInput#0`
(`VID 045E`, `PID 02FF`) for 298 snapshots. Both sticks covered
`-32768..32767` on both axes, face buttons 0-3 were observed, and all six raw
axes traversed the full signed range. SDL's standardized LT/RT fields still
reported zero, but raw axes 2 and 5 changed across their full ranges. This is
the definitive real-target evidence for the Q2 PASS.

**Q2 result: PASS.** SDL can observe stable right-stick input from the side in
the real D environment while SMT3 remains focused and operational. No device
hiding, virtual-controller replacement, suppression, or game modification was
used. The identified candidate is `XInput#0`; no automatic selection policy is
implemented in this PoC.

## WGI probe

`tools/ControllerSideRead` enumerates every `RawGameController` visible to WGI.
For each candidate it records:

- provider and candidate index;
- display name, VID, PID, axis/button/switch counts;
- raw axes, pressed button indices, and switch states;
- standardized right/left sticks, LT, RT, and buttons when WGI can map the raw
  candidate to a `Gamepad`;
- unmatched standardized `Gamepad` candidates, so a live virtual device is not
  silently omitted.

Output is JSON Lines to support side-by-side comparison without auto-selecting
a device.

## Required comparison matrix

Run the probe while moving both sticks in all four directions, pressing LT/RT,
and pressing several face buttons:

| Condition | reWASD | Steam Input |
|---|---:|---:|
| A | OFF | OFF |
| B | OFF | ON |
| C | ON | OFF |
| D | ON | ON |

Condition D is the real target. A-C are diagnostics only.

```powershell
dotnet run --project .\tools\ControllerSideRead\NocturneModernController.ControllerProbe.csproj `
  -c Release -- --provider wgi --condition D --seconds 30 --output .\controller-D.jsonl
```

The SDL provider uses the official SDL3 Windows x64 runtime and logs every SDL
joystick candidate plus standardized gamepad values where available:

```powershell
.\tools\ControllerSideRead\Fetch-Sdl.ps1

dotnet run --project .\tools\ControllerSideRead\NocturneModernController.ControllerProbe.csproj `
  -c Release -- --provider sdl --condition D --seconds 30 --output .\controller-D-sdl.jsonl
```

Do not design auto-selection until the four logs identify which candidate, if
any, carries live right-stick and trigger values in condition D.

## PASS criterion

Q2 passes only when a candidate in condition D reports stable live
`rightStickX` / `rightStickY` values while SMT3 continues to operate normally.
Merely enumerating a controller or observing only the left stick is not a pass.
