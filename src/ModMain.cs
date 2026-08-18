using MelonLoader;

[assembly: MelonInfo(
    typeof(NocturneDungeonControls.ModMain),
    "Nocturne Dungeon Controls",
    "0.9.4-native-calc-dash-160",
    "Gray Ghost")]
[assembly: MelonGame(null, "smt3hd")]

namespace NocturneDungeonControls
{
    public sealed class ModMain : MelonMod
    {
        public override void OnInitializeMelon()
        {
            HarmonyInstance.CreateClassProcessor(typeof(FieldDashPatch)).Patch();
            LoggerInstance.Msg("[NocturneDungeonControls] Contextual dungeon dash PoC loaded. Hold reWASD P binding while moving.");
        }
    }
}
