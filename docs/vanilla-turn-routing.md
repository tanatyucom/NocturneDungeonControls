# Vanilla turn routing

## Q3 scope

Q3 proves that SMT3's native left/right turn behavior can be invoked from an
arbitrary readable source before the SDL right-stick reader is connected. The
temporary inputs are F6 for left and F7 for right.

This PoC does not suppress LB/RB, connect SDL input, alter BATTLE controls, or
install a native detour. The former `DDS3_PADCHECK_PRESS` native-hook experiment
remains excluded from compilation because it caused startup crashes.

## Routing

Live testing confirmed that FIELD/DUNGEON turning is successfully augmented at
the logical `SteamInputAssign.padcheck` route. The PoC augments
`FD_Turn_Left`, `FD_Turn_Right`,
`PZL_MapRot_Left`, and `PZL_MapRot_Right` only with `SIPressType.DOWN`.
Existing true results, including normal LB/RB input, remain untouched.

Diagnostic patches around `fldCamera` and `DDS3_PADCHECK_PRESS` did not observe
the successful injected path and were removed after the logical route passed.
No native detour is used.

Both temporary keys held simultaneously resolve to neutral. Release removes the
augmented held state on the next update.

## Validation

1. Start SMT3 and enter a normal FIELD/DUNGEON area.
2. Hold F6, release it, then hold F7 and release it.
3. Confirm native-equivalent continuous turning and immediate release behavior.
4. Confirm LB/RB still work normally.
5. Enter BATTLE and confirm F6/F7 do not alter battle commands.
6. When PUZZLE is available, repeat the held/release test for map rotation.

PASS requires native-equivalent continuous turning without physical LB/RB in
the relevant exploration context, with release behavior and no BATTLE effect.

## Live result

FIELD/DUNGEON testing confirmed continuous native-equivalent left and right
turning from F6/F7, including release behavior. Logs recorded
`FD_Turn_Left` and `FD_Turn_Right` logical injections. Q3 therefore passes for
the tested exploration route. BATTLE and PUZZLE remain explicit regression
checks before this PoC is promoted into the right-stick integration.

Internal interfaces and implementation details are documented for technical
reference only. No stable public API is provided, and internal interfaces may
change without notice.
