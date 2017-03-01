using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using System.Reflection;
using UnityEngine;

namespace CompDeflector
{
    [StaticConstructorOnStartup]
    static class HarmonyCompDeflector
    {
        static HarmonyCompDeflector()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.jecrell.comps.deflector");

            harmony.Patch(typeof(Thing).GetMethod("TakeDamage"), new HarmonyMethod(typeof(HarmonyCompDeflector).GetMethod("PreApplyDamagePreFix")), null);
            harmony.Patch(typeof(PawnRenderer).GetMethod("DrawEquipmentAiming"), new HarmonyMethod(typeof(HarmonyCompDeflector).GetMethod("DrawEquipmentAimingPreFix")), null);
            //harmony.Patch(typeof(Thing).GetMethod("get_SpecialDisplayStats"), null, new HarmonyMethod(typeof(HarmonyCompDeflector).GetMethod("SpecialDisplayStatsPostFix")), null);
        }



        //=================================== COMPDEFLECTOR

        //public static void SpecialDisplayStatsPostFix(Thing __instance, ref IEnumerable<StatDrawEntry> __result)
        //{
        //    //Log.Message("3");
        //    ThingWithComps thingWithComps = __instance as ThingWithComps;
        //    if (thingWithComps != null)
        //    {
        //        CompDeflector compDeflector = thingWithComps.GetComp<CompDeflector>();
        //        if (compDeflector != null)
        //        {
        //            List<StatDrawEntry> origin = new List<StatDrawEntry>();
        //            foreach (StatDrawEntry entry in __result)
        //            {
        //                Log.Message("Entry");
        //                origin.Add(entry);
        //            }

        //            List<StatDrawEntry> entries = new List<StatDrawEntry>();
        //            foreach (StatDrawEntry entry in compDeflector.PostSpecialDisplayStats())
        //            {
        //                Log.Message("Hey!");
        //                entries.Add(entry);
        //            }

        //            origin.Concat(entries);

        //            __result = origin;
        //        }
        //    }
        //}

        public static bool DrawEquipmentAimingPreFix(PawnRenderer __instance, Thing eq, Vector3 drawLoc, float aimAngle)
        {
            Pawn pawn = (Pawn)AccessTools.Field(typeof(PawnRenderer), "pawn").GetValue(__instance);
            if (pawn != null)
            {
                Pawn_EquipmentTracker pawn_EquipmentTracker = pawn.equipment;
                if (pawn_EquipmentTracker != null)
                {
                    foreach (ThingWithComps thingWithComps in pawn_EquipmentTracker.AllEquipment)
                    {
                        if (thingWithComps != null)
                        {
                            //Log.Message("3");
                            CompDeflector compDeflector = thingWithComps.GetComp<CompDeflector>();
                            if (compDeflector != null)
                            {
                                if (compDeflector.IsAnimatingNow)
                                {
                                    compDeflector.AnimationDeflectionTicks -= 20;
                                    float num = aimAngle - 90f;
                                    Mesh mesh;
                                    if (aimAngle > 20f && aimAngle < 160f)
                                    {
                                        mesh = MeshPool.plane10;
                                        num += eq.def.equippedAngleOffset + ((compDeflector.AnimationDeflectionTicks + 1) / 2);
                                    }
                                    else if (aimAngle > 200f && aimAngle < 340f)
                                    {
                                        mesh = MeshPool.plane10Flip;
                                        num -= 180f;
                                        num -= eq.def.equippedAngleOffset - ((compDeflector.AnimationDeflectionTicks + 1) / 2);
                                    }
                                    else
                                    {
                                        mesh = MeshPool.plane10;
                                        num += eq.def.equippedAngleOffset + ((compDeflector.AnimationDeflectionTicks + 1) / 2);
                                    }
                                    num %= 360f;
                                    Graphic_StackCount graphic_StackCount = eq.Graphic as Graphic_StackCount;
                                    Material matSingle;
                                    if (graphic_StackCount != null)
                                    {
                                        matSingle = graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingle;
                                    }
                                    else
                                    {
                                        matSingle = eq.Graphic.MatSingle;
                                    }
                                    Graphics.DrawMesh(mesh, drawLoc, Quaternion.AngleAxis(num, Vector3.up), matSingle, 0);
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        public static bool PreApplyDamagePreFix(Thing __instance, ref DamageInfo dinfo)
        {
            //Pawn pawn = (Pawn)AccessTools.Field(typeof(Pawn_HealthTracker), "pawn").GetValue(__instance);
            Pawn pawn = __instance as Pawn;
            if (pawn != null)
            {
                Pawn_EquipmentTracker pawn_EquipmentTracker = pawn.equipment;
                if (pawn_EquipmentTracker != null)
                {
                    foreach (ThingWithComps thingWithComps in pawn_EquipmentTracker.AllEquipment)
                    {
                        if (thingWithComps != null)
                        {
                            //Log.Message("3");
                            CompDeflector compDeflector = thingWithComps.GetComp<CompDeflector>();
                            if (compDeflector != null)
                            {
                                bool newAbsorbed = false;
                                compDeflector.PostPreApplyDamage(dinfo, out newAbsorbed);
                                if (newAbsorbed)
                                {
                                    compDeflector.AnimationDeflectionTicks = 400;
                                    dinfo.SetAmount(0);
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }
        
    }
}
