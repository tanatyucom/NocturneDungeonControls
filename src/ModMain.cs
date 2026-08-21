using MelonLoader;

[assembly: MelonInfo(
    typeof(NocturneModernController.ModMain),
    "Nocturne Modern Controller",
    "1.0.0",
    "Gray Ghost")]
[assembly: MelonGame(null, "smt3hd")]

namespace NocturneModernController
{
    public sealed class ModMain : MelonMod
    {
        public override void OnInitializeMelon()
        {
            SdlRightStickInput.Initialize(LoggerInstance);
            HarmonyInstance.CreateClassProcessor(typeof(FieldDashPatch)).Patch();
            HarmonyInstance.CreateClassProcessor(typeof(PuzzleLogicalTurnPatch)).Patch();
            HarmonyInstance.CreateClassProcessor(typeof(FormationFlagProbe)).Patch();
            HarmonyInstance.CreateClassProcessor(typeof(NativeRightStickVerticalPatch)).Patch();
            LoggerInstance.Msg("[NocturneModernController] Dash loaded: hold LT/RT/P; press LT+RT to toggle dash keep.");
            LoggerInstance.Msg("[NocturneModernController] Native right-stick dungeon camera loaded; legacy LB/RB field turn suppressed, BATTLE untouched.");
        }

        public override void OnUpdate()
        {
            SdlRightStickInput.Sample();
            bool explorationActive = FieldDashPatch.IsExplorationActive;
            ExternalInputBridge.UpdateGameContext(explorationActive);
            ExplorationCursorController.Update(explorationActive);
            QuickHealRuntimeProbe.Sample();
        }

        public override void OnDeinitializeMelon()
        {
            ExplorationCursorController.Restore();
            SdlRightStickInput.Shutdown();
        }
    }
}
