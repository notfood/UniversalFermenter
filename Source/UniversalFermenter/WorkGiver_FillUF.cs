﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace UniversalFermenter
{
    public class WorkGiver_FillUF : WorkGiver_Scanner
    {
        private static string TemperatureTrans;
        private static string NoIngredientTrans;

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.GetComponent<MapComponent_UF>().thingsWithUFComp;
        }

        public override bool Prioritized => true;
        public override float GetPriority(Pawn pawn, TargetInfo t)
        {
            CompUniversalFermenter comp = t.Thing.TryGetComp<CompUniversalFermenter>();
            if (comp != null)
            {
                return 1 / comp.SpaceLeftForIngredient;
            }
            return 0f;
        }

        public static void Reset()
        {
            TemperatureTrans = "BadTemperature".Translate().ToLower();
            NoIngredientTrans = "UF_NoIngredient".Translate();
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            CompUniversalFermenter comp = t.TryGetComp<CompUniversalFermenter>();
            if (comp == null || comp.Finished || comp.SpaceLeftForIngredient <= 0)
            {
                return false;
            }

            float ambientTemperature = comp.parent.AmbientTemperature;
            if (ambientTemperature < comp.CurrentProcess.temperatureSafe.min + 2f ||
                ambientTemperature > comp.CurrentProcess.temperatureSafe.max - 2f)
            {
                JobFailReason.Is(TemperatureTrans);
                return false;
            }

            if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
            {
                return false;
            }

            if (t.IsForbidden(pawn) || !pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger(), 1, -1, null, forced))
            {
                return false;
            }

            if (FindIngredient(pawn, t) == null)
            {
                JobFailReason.Is(NoIngredientTrans);
                return false;
            }

            return !t.IsBurning();
        }


        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Thing t2 = FindIngredient(pawn, t);
            return new Job(UF_DefOf.FillUniversalFermenter, t, t2);
        }

        private Thing FindIngredient(Pawn pawn, Thing fermenter)
        {
            ThingFilter filter = fermenter.TryGetComp<CompUniversalFermenter>().CurrentProcess.ingredientFilter;
            Predicate<Thing> validator = x => !x.IsForbidden(pawn) && pawn.CanReserve(x) && filter.Allows(x);
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, filter.BestThingRequest, PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, validator);
        }
    }
}