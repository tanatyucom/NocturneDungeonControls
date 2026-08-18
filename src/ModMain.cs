using MelonLoader;

[assembly: MelonInfo(
    typeof(NocturneDungeonControls.ModMain),
    "Nocturne Dungeon Controls",
    "0.9.0-rb-dash-poc",
    "Gray Ghost")]
[assembly: MelonGame(null, "smt3hd")]

namespace NocturneDungeonControls
{
    public sealed class ModMain : MelonMod
    {
        public override void OnInitializeMelon()
        {
            HarmonyInstance.CreateClassProcessor(typeof(FieldDashPatch)).Patch();
            LoggerInstance.Msg("[NocturneDungeonControls] Dungeon dash PoC loaded. Hold reWASD F15 binding while moving.");
        }
    }
}
