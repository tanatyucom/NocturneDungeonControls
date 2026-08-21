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
            HarmonyInstance.CreateClassProcessor(typeof(FieldDashPatch)).Patch();
            HarmonyInstance.CreateClassProcessor(typeof(PuzzleLogicalTurnPatch)).Patch();
            LoggerInstance.Msg("[NocturneModernController] Dash loaded: hold LT/P; press LT+RT to toggle dash keep.");
            LoggerInstance.Msg("[NocturneModernController] Q3 vanilla-turn PoC loaded: hold F6 left / F7 right.");
        }

        public override void OnUpdate()
        {
            VanillaTurnInvocationPoc.SampleTemporaryInput();
        }
    }
}
