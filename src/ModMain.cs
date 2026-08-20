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
            LoggerInstance.Msg("[NocturneModernController] Dash loaded: hold LT/P; press LT+RT to toggle dash keep.");
        }
    }
}
