using MelonLoader;

[assembly: MelonInfo(
    typeof(NocturneDungeonControls.ModMain),
    "Nocturne Dungeon Controls",
    "1.0.0",
    "Gray Ghost")]
[assembly: MelonGame(null, "smt3hd")]

namespace NocturneDungeonControls
{
    public sealed class ModMain : MelonMod
    {
        public override void OnInitializeMelon()
        {
            HarmonyInstance.CreateClassProcessor(typeof(FieldDashPatch)).Patch();
            LoggerInstance.Msg("[NocturneDungeonControls] Dash loaded: hold LT/P; press LT+RT to toggle dash keep.");
        }
    }
}
