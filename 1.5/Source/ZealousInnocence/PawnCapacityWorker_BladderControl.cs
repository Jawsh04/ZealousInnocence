﻿using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ZealousInnocence
{
    public class PawnCapacityWorker_BladderControl : PawnCapacityWorker
    {
        public bool simulateSleep = false;
        public override float CalculateCapacityLevel(HediffSet diffSet,
                                                      List<PawnCapacityUtility.CapacityImpactor> impactors = null)
        {
            float num2 = PawnCapacityUtility.CalculateTagEfficiency(
                diffSet,
                BodyPartTagDefOf.BladderControlSource,
                float.MaxValue,
                default(FloatRange),
                impactors) * Mathf.Min(CalculateCapacityAndRecord(diffSet, RimWorld.PawnCapacityDefOf.Consciousness, impactors), 1f);
            Pawn pawn = diffSet.pawn;
            if (pawn != null)
            {
                float ageFactor = GetAgeFactor(pawn);
                if (ageFactor != 1f && impactors != null)
                {
                    impactors.Add(new CapacityImpactorCustom { customLabel = "Age", customValue = ageFactor});
                }
                num2 *= ageFactor;

                bool needsDiaper = num2 <= 0.5;
                bool awake = pawn.Awake() && !simulateSleep;

                float sleepFactor = GetSleepingFactor(pawn, awake);
                if (sleepFactor != 1f && impactors != null)
                {
                    impactors.Add(new CapacityImpactorCustom { customLabel = "Sleeping", customValue = sleepFactor });
                }
                num2 *= sleepFactor;

                if (impactors != null)
                {
                    if (needsDiaper)
                    {
                        impactors.Add(new CapacityImpactorCustom { customString = "Needs Diapers" });
                    }
                    else
                    {
                        float whileAsleep = awake ? GetSleepingFactor(pawn, false) * num2 : num2;
                        if (whileAsleep <= 0.5f)
                        {
                            impactors.Add(new CapacityImpactorCustom { customString = "Bedwetting Risk" });
                        }
                    }
                }
            }
            else
            {
                Log.Message($"there was no pawn defined?");
            }

            return num2;
        }

        public override bool CanHaveCapacity(BodyDef body)
        {
            return body.HasPartWithTag(BodyPartTagDefOf.BladderControlSource);
        }

        private float GetAgeFactor(Pawn pawn)
        {
            if (!pawn.RaceProps.Humanlike) return 1.0f;
            int age = pawn.ageTracker.AgeBiologicalYears;
            float factor;

            // Young age factor calculation
            if (age <= 3)
            {
                factor = 0f; // Toddlers have no bladder control
            }
            else if (age < 6)
            {
                factor = (age - 3) / 3f * 0.5f; // Linear increase from 0 at age 3 to 0.5 at age 6
            }
            else if(age == 6)
            {
                factor = 0.51f; // tweak it to slightly be out of day diapers
            }
            else if (age <= 15)
            {
                factor = 0.5f + (age - 6) / 9f * 0.5f; // Linear increase from 0.5 at age 6 to 1.0 at age 14
            }
            // Senior age factor calculation
            else if (age >= 50)
            {
                if (age <= 70)
                {
                    factor = 1.0f - (age - 50) / 20f * 0.25f; // Linear decrease from 1.0 at age 50 to 0.75 at age 70
                }
                else
                {
                    factor = 0.75f; // Maximum reduction at age 70 and beyond
                }
            }
            else
            {
                factor = 1.0f; // Full control for ages 14 to 50
            }

            // Round to 2 decimal places
            factor = Mathf.Round(factor * 100f) / 100f;

            return factor;
        }
        private float GetSleepingFactor(Pawn pawn, bool isAwake)
        {
            if (!isAwake)
            {
                List<Hediff> health = pawn.health.hediffSet.hediffs;
                for (int i = 0; i < health.Count; i++)
                {

                    switch (health[i].def.defName)
                    {
                        case "BedWetting":
                            return 0f;
                        default:
                            continue;
                    }

                }
                return 0.75f;
            }
            return 1.0f;
        }

        public bool CapableOf(Pawn pawn)
        {
            float bladderControlLevel = pawn.health.capacities.GetLevel(PawnCapacityDefOf.BladderControl);
            return bladderControlLevel >= PawnCapacityDefOf.BladderControl.minForCapable;
        }
        public float SimulateBladderControlDuringSleep(Pawn pawn)
        {
            simulateSleep = true;
            float simulatedLevel = CalculateCapacityLevel(pawn.health.hediffSet, null);
            simulateSleep = false;
            return simulatedLevel;
        }
    }
}
