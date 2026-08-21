using System;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace NocturneModernController
{
    [HarmonyPatch(typeof(cmpMisc), nameof(cmpMisc.cmpChgFlagDevilToParty))]
    internal static class FormationFlagProbe
    {
        private static uint _flagBefore;

        private static void Prefix(Il2Cppnewdata_H.datUnitWork_t pStock)
        {
            _flagBefore = pStock == null || pStock.Pointer == IntPtr.Zero ? 0 : pStock.flag;
        }

        private static void Postfix(Il2Cppnewdata_H.datUnitWork_t pStock)
        {
            if (pStock == null || pStock.Pointer == IntPtr.Zero)
            {
                return;
            }

            MelonLogger.Msg(
                $"[NocturneModernController] FORMATION-FLAG id={pStock.id} " +
                $"unique={pStock.uniqueid} flag=0x{_flagBefore:X8}->0x{pStock.flag:X8}.");
        }

        internal static void DumpRoster()
        {
            try
            {
                Il2Cppdds3GlobalWork_H.dds3GlobalWork_t global = dds3GlobalWork.DDS3_GBWK;
                var stocklist = global.stocklist;
                var units = global.unitwork;
                for (int i = 0; i < global.stockcnt; i++)
                {
                    int unitIndex = stocklist[i];
                    if (unitIndex < 0 || unitIndex >= units.Length)
                    {
                        continue;
                    }

                    Il2Cppnewdata_H.datUnitWork_t? unit = units[unitIndex];
                    if (unit == null || unit.Pointer == IntPtr.Zero)
                    {
                        continue;
                    }

                    MelonLogger.Msg(
                        $"[NocturneModernController] ROSTER-FLAG order={i} unit={unitIndex} " +
                        $"id={unit.id} unique={unit.uniqueid} flag=0x{unit.flag:X8} " +
                        $"hp={unit.hp}/{unit.maxhp} mp={unit.mp}/{unit.maxmp}.");
                }
            }
            catch (Exception exception)
            {
                MelonLogger.Warning("[NocturneModernController] Roster flag probe failed: " + exception.Message);
            }
        }
    }
}
