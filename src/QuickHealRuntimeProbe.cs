using System;
using System.Text;
using Il2Cpp;
using MelonLoader;

namespace NocturneModernController
{
    internal static class QuickHealRuntimeProbe
    {
        private static bool _wasHeld;
        private static bool _healSequenceActive;
        private static int _nextHealTick;
        private static int _sequenceActionCount;

        private const int HealIntervalMilliseconds = 250;
        private const int MaximumActionsPerSequence = 64;

        internal static void Sample()
        {
            if (!FieldDashPatch.IsExplorationActive)
            {
                _wasHeld = false;
                _healSequenceActive = false;
                return;
            }

            bool held;
            try
            {
                held = dds3PadManager.DDS3_PADCHECK_PRESS(
                    Il2Cpplibsdf_H.SDF_PADMAP.SDF_PADMAP_R1,
                    0);
            }
            catch (Exception)
            {
                return;
            }

            if (held && !_wasHeld)
            {
                StartHealSequence();
            }
            _wasHeld = held;

            if (_healSequenceActive && unchecked(Environment.TickCount - _nextHealTick) >= 0)
            {
                RunNextHeal();
            }
        }

        private static void StartHealSequence()
        {
            _healSequenceActive = true;
            _sequenceActionCount = 0;
            _nextHealTick = Environment.TickCount;
            FormationFlagProbe.DumpRoster();
            MelonLogger.Msg("[NocturneModernController] Q7 AUTO-HEAL started.");
        }

        private static void RunNextHeal()
        {
            try
            {
                Il2Cppdds3GlobalWork_H.dds3GlobalWork_t global = dds3GlobalWork.DDS3_GBWK;
                var stocklist = global.stocklist;
                var units = global.unitwork;

                // Revive first. A successful revival is determined only by the
                // target actually leaving HP=0; effect inspection alone is not
                // trusted because ordinary recovery skills may report a value.
                for (int targetStockIndex = 0; targetStockIndex < global.stockcnt; targetStockIndex++)
                {
                    int targetIndex = stocklist[targetStockIndex];
                    if (targetIndex < 0 || targetIndex >= units.Length)
                    {
                        continue;
                    }

                    Il2Cppnewdata_H.datUnitWork_t? target = units[targetIndex];
                    if (target == null || target.Pointer == IntPtr.Zero || target.hp != 0)
                    {
                        continue;
                    }

                    for (int sourcePriority = 0; sourcePriority < 3; sourcePriority++)
                    {
                        for (int sourceStockIndex = 0; sourceStockIndex < global.stockcnt; sourceStockIndex++)
                        {
                            int sourceIndex = stocklist[sourceStockIndex];
                            if (sourceIndex < 0 || sourceIndex >= units.Length)
                            {
                                continue;
                            }

                            Il2Cppnewdata_H.datUnitWork_t? source = units[sourceIndex];
                            if (source == null || source.Pointer == IntPtr.Zero || source.hp == 0 ||
                                GetSourcePriority(sourceIndex, source) != sourcePriority)
                            {
                                continue;
                            }

                            int skillCount = Math.Min(source.skillcnt, source.skill.Length);
                            for (int skillIndex = 0; skillIndex < skillCount; skillIndex++)
                            {
                                ushort skillId = unchecked((ushort)source.skill[skillIndex]);
                                try
                                {
                                    int effect = datCalc.datGetSkillKouka(skillId, 0, source, target);
                                    if (effect <= 0 || cmpMisc.cmpChkSkillCost(skillId, source) == 0)
                                    {
                                        continue;
                                    }

                                    ushort mpBefore = source.mp;
                                    ushort statusBefore = target.badstatus;
                                    int cost = cmpDrawSkill.cmpGetSkillCost(skillId, source);
                                    cmpMisc.cmpRecover(skillId, source, target);
                                    if (target.hp == 0)
                                    {
                                        continue;
                                    }

                                    if (cost > 0 && source.mp >= cost)
                                    {
                                        source.mp = unchecked((ushort)(source.mp - cost));
                                    }

                                    MelonLogger.Msg(
                                        $"[NocturneModernController] Q7 AUTO-REVIVE " +
                                        $"sourceKind={GetSourceKind(sourcePriority)} " +
                                        $"skill={skillId} src={sourceIndex} dst={targetIndex} " +
                                        $"hp=0->{target.hp}/{target.maxhp} " +
                                        $"bad=0x{statusBefore:X4}->0x{target.badstatus:X4} " +
                                        $"cost={cost} mp={mpBefore}->{source.mp}");

                                    ScheduleNextAction();
                                    return;
                                }
                                catch (Exception)
                                {
                                    // Passive and non-camp skills can reject inspection.
                                }
                            }
                        }
                    }
                }

                for (int targetStockIndex = 0; targetStockIndex < global.stockcnt; targetStockIndex++)
                {
                    int targetIndex = stocklist[targetStockIndex];
                    if (targetIndex < 0 || targetIndex >= units.Length)
                    {
                        continue;
                    }
                    Il2Cppnewdata_H.datUnitWork_t? target = units[targetIndex];
                    if (target == null || target.Pointer == IntPtr.Zero ||
                        target.hp == 0 || target.hp >= target.maxhp)
                    {
                        continue;
                    }

                    // Prefer reserve demons, then active demons, and use the
                    // protagonist only as the final fallback. Bit 0x2 is set
                    // by cmpChgFlagDevilToParty when a demon joins the active party.
                    for (int sourcePriority = 0; sourcePriority < 3; sourcePriority++)
                    {
                        for (int sourceStockIndex = 0; sourceStockIndex < global.stockcnt; sourceStockIndex++)
                        {
                            int sourceIndex = stocklist[sourceStockIndex];
                            if (sourceIndex < 0 || sourceIndex >= units.Length)
                            {
                                continue;
                            }
                            Il2Cppnewdata_H.datUnitWork_t? source = units[sourceIndex];
                            if (source == null || source.Pointer == IntPtr.Zero || source.hp == 0 ||
                                GetSourcePriority(sourceIndex, source) != sourcePriority)
                            {
                                continue;
                            }

                            int skillCount = Math.Min(source.skillcnt, source.skill.Length);
                            for (int skillIndex = 0; skillIndex < skillCount; skillIndex++)
                            {
                                ushort skillId = unchecked((ushort)source.skill[skillIndex]);
                                try
                                {
                                    int effect = datCalc.datGetSkillKouka(skillId, 0, source, target);
                                    if (effect <= 0 || cmpMisc.cmpChkSkillCost(skillId, source) == 0)
                                    {
                                        continue;
                                    }

                                    ushort hpBefore = target.hp;
                                    ushort mpBefore = source.mp;
                                    int cost = cmpDrawSkill.cmpGetSkillCost(skillId, source);
                                    cmpMisc.cmpRecover(skillId, source, target);
                                    if (target.hp != hpBefore && cost > 0 && source.mp >= cost)
                                    {
                                        source.mp = unchecked((ushort)(source.mp - cost));
                                    }
                                    MelonLogger.Msg(
                                        $"[NocturneModernController] Q7 AUTO-RECOVER " +
                                        $"sourceKind={GetSourceKind(sourcePriority)} " +
                                        $"skill={skillId} src={sourceIndex} dst={targetIndex} " +
                                        $"hp={hpBefore}->{target.hp}/{target.maxhp} " +
                                        $"cost={cost} mp={mpBefore}->{source.mp}");

                                    _sequenceActionCount++;
                                    if (_sequenceActionCount >= MaximumActionsPerSequence)
                                    {
                                        StopHealSequence("safety action limit reached");
                                    }
                                    else
                                    {
                                        _nextHealTick = unchecked(Environment.TickCount + HealIntervalMilliseconds);
                                    }
                                    return;
                                }
                                catch (Exception)
                                {
                                    // Passive and non-camp skills can reject effect inspection.
                                }
                            }
                        }
                    }
                }

                // HP recovery is complete (or currently impossible). Continue
                // the same sequence with curable ailments. cmpChkSkillBad returns
                // the ailment mask handled by the skill.
                for (int targetStockIndex = 0; targetStockIndex < global.stockcnt; targetStockIndex++)
                {
                    int targetIndex = stocklist[targetStockIndex];
                    if (targetIndex < 0 || targetIndex >= units.Length)
                    {
                        continue;
                    }

                    Il2Cppnewdata_H.datUnitWork_t? target = units[targetIndex];
                    if (target == null || target.Pointer == IntPtr.Zero ||
                        target.hp == 0 || target.badstatus == 0)
                    {
                        continue;
                    }

                    for (int sourcePriority = 0; sourcePriority < 3; sourcePriority++)
                    {
                        for (int sourceStockIndex = 0; sourceStockIndex < global.stockcnt; sourceStockIndex++)
                        {
                            int sourceIndex = stocklist[sourceStockIndex];
                            if (sourceIndex < 0 || sourceIndex >= units.Length)
                            {
                                continue;
                            }

                            Il2Cppnewdata_H.datUnitWork_t? source = units[sourceIndex];
                            if (source == null || source.Pointer == IntPtr.Zero || source.hp == 0 ||
                                GetSourcePriority(sourceIndex, source) != sourcePriority)
                            {
                                continue;
                            }

                            int skillCount = Math.Min(source.skillcnt, source.skill.Length);
                            for (int skillIndex = 0; skillIndex < skillCount; skillIndex++)
                            {
                                ushort skillId = unchecked((ushort)source.skill[skillIndex]);
                                try
                                {
                                    uint cureMask = cmpMisc.cmpChkSkillBad(skillId);
                                    if (cureMask == 0 ||
                                        (cureMask & target.badstatus) == 0 ||
                                        cmpMisc.cmpChkSkillCost(skillId, source) == 0)
                                    {
                                        continue;
                                    }

                                    ushort statusBefore = target.badstatus;
                                    ushort mpBefore = source.mp;
                                    int cost = cmpDrawSkill.cmpGetSkillCost(skillId, source);
                                    cmpMisc.cmpRecover(skillId, source, target);
                                    if (target.badstatus == statusBefore)
                                    {
                                        continue;
                                    }

                                    if (cost > 0 && source.mp >= cost)
                                    {
                                        source.mp = unchecked((ushort)(source.mp - cost));
                                    }

                                    MelonLogger.Msg(
                                        $"[NocturneModernController] Q7 AUTO-CURE " +
                                        $"sourceKind={GetSourceKind(sourcePriority)} " +
                                        $"skill={skillId} src={sourceIndex} dst={targetIndex} " +
                                        $"bad=0x{statusBefore:X4}->0x{target.badstatus:X4} " +
                                        $"mask=0x{cureMask:X8} cost={cost} mp={mpBefore}->{source.mp}");

                                    ScheduleNextAction();
                                    return;
                                }
                                catch (Exception)
                                {
                                    // Passive and non-camp skills can reject inspection.
                                }
                            }
                        }
                    }
                }

                StopHealSequence(
                    _sequenceActionCount == 0
                        ? "no wounded/ailing target or usable recovery skill"
                        : "all reachable HP/status recovery completed");
            }
            catch (Exception exception)
            {
                _healSequenceActive = false;
                MelonLogger.Warning("[NocturneModernController] Q7 auto-heal failed: " + exception);
            }
        }

        private static int GetSourcePriority(
            int sourceIndex,
            Il2Cppnewdata_H.datUnitWork_t source)
        {
            if (sourceIndex == 0)
            {
                return 2;
            }

            return (source.flag & 0x2u) == 0 ? 0 : 1;
        }

        private static string GetSourceKind(int sourcePriority)
        {
            return sourcePriority == 0
                ? "reserve"
                : sourcePriority == 1 ? "active" : "protagonist";
        }

        private static void ScheduleNextAction()
        {
            _sequenceActionCount++;
            if (_sequenceActionCount >= MaximumActionsPerSequence)
            {
                StopHealSequence("safety action limit reached");
            }
            else
            {
                _nextHealTick = unchecked(Environment.TickCount + HealIntervalMilliseconds);
            }
        }

        private static void StopHealSequence(string reason)
        {
            _healSequenceActive = false;
            MelonLogger.Msg(
                $"[NocturneModernController] Q7 AUTO-HEAL stopped: {reason}; " +
                $"actions={_sequenceActionCount}.");
        }
    }
}
