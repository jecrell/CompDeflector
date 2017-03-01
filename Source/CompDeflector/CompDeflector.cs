using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CompDeflector
{
    public class CompDeflector : ThingComp
    {
        private int animationDeflectionTicks = 0;
        public int AnimationDeflectionTicks
        {
            set
            {
                animationDeflectionTicks = value;
            }
            get
            {
                return animationDeflectionTicks;
            }
        }
        public bool IsAnimatingNow
        {
            get
            {
                if (animationDeflectionTicks >= 0) return true;
                return false;
            }
        }

        public CompEquippable GetEquippable
        {
            get
            {
                return this.parent.GetComp<CompEquippable>();
            }
        }

        public Pawn GetPawn
        {
            get
            {
                return GetEquippable.verbTracker.PrimaryVerb.CasterPawn;
            }
        }

        public ThingComp GetActivatableEffect
        {
            get
            {
                return this.parent.AllComps.FirstOrDefault<ThingComp>((ThingComp y) => y.GetType().ToString() == "CompActivatableEffect.CompActivatableEffect");
            }
        }

        public bool HasCompActivatableEffect
        {
            get
            {
                ThingWithComps x = this.parent as ThingWithComps;
                if (x != null)
                {
                    if (GetActivatableEffect != null)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
            

        public Verb_Shoot deflectVerb;

        public float DeflectionChance
        {
            get
            {
                float calc = Props.baseDeflectChance;

                if (GetEquippable != null)
                {
                    if (GetPawn != null)
                    {
                        Pawn pawn = GetPawn;

                        //This handles if a deflection skill is defined.
                        //Example, melee skill of 20.
                        if (Props.useSkillInCalc)
                        {
                            SkillDef skillToCheck = this.Props.deflectSkill;
                            if (skillToCheck != null)
                            {
                                if (pawn.skills != null)
                                {
                                    SkillRecord skillRecord = pawn.skills.GetSkill(skillToCheck);
                                    if (skillRecord != null)
                                    {
                                        float param = this.Props.deflectRatePerSkillPoint;
                                        if (param != 0)
                                        {
                                            calc += ((float)skillRecord.Level) * param; //Makes the skill into float percent
                                                                                        //Ex: Melee skill of 20. Multiplied by 0.015f. Equals 0.3f, or 30%
                                        }
                                        else
                                        {
                                            Log.Error("CompDeflector :: deflectRatePerSkillPoint is set to 0, but useSkillInCalc is set to true.");
                                        }

                                    }
                                }

                            }
                        }

                        //This handles if manipulation needs to be checked.
                        if (Props.useManipulationInCalc)
                        {
                            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                            {
                                calc *= pawn.health.capacities.GetEfficiency(PawnCapacityDefOf.Manipulation);
                            }
                            else
                            {
                                calc = 0f;
                            }
                        }
                    }
                }
                return Mathf.Clamp(calc, 0, 1.0f);
            }
        }

        public string ChanceToString
        {
            get
            {
                return DeflectionChance.ToStringPercent();
            }
        }

        public IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            yield return new StatDrawEntry(StatCategoryDefOf.Basics, "DeflectChance".Translate(), ChanceToString, 0);
            
            yield break;
        }

        //        	if (this.ingestible != null)
        //	{
        //		IEnumerator<StatDrawEntry> enumerator2 = this.ingestible.SpecialDisplayStats(this).GetEnumerator();
        //		while (enumerator2.MoveNext())
        //		{
        //			StatDrawEntry current2 = enumerator2.Current;
        //        yield return current2;
        //		}
        //}

        public Verb CopyAndReturnNewVerb(Verb newVerb = null)
        {
            if (newVerb != null)
            {
                deflectVerb = null;
                deflectVerb = (Verb_Shoot)Activator.CreateInstance(newVerb.verbProps.verbClass);
                deflectVerb.caster = GetPawn;

                //Initialize VerbProperties
                VerbProperties newVerbProps = new VerbProperties();

                //Copy values over to a new verb props
                newVerbProps.hasStandardCommand = newVerb.verbProps.hasStandardCommand;
                newVerbProps.projectileDef = newVerb.verbProps.projectileDef;
                newVerbProps.range = newVerb.verbProps.range;
                newVerbProps.muzzleFlashScale = newVerb.verbProps.muzzleFlashScale;
                newVerbProps.warmupTime = 0;
                newVerbProps.defaultCooldownTime = 0;
                newVerbProps.soundCast = this.Props.deflectSound;

                //Apply values
                deflectVerb.verbProps = newVerbProps;
            }
            else
            {
                if (deflectVerb == null)
                {
                    deflectVerb = (Verb_Shoot)Activator.CreateInstance(this.Props.DeflectVerb.verbClass);
                    deflectVerb.caster = GetPawn;
                    deflectVerb.verbProps = this.Props.DeflectVerb;
                }
            }
            return deflectVerb;
        }

        public void ResolveDeflectVerb()
        {
            CopyAndReturnNewVerb(null);
        }

        public void GiveDeflectJob(DamageInfo dinfo)
        {
            Job job = new Job(CompDeflectorDefOf.CastDeflectVerb, dinfo.Instigator);
            job.playerForced = true;

            Pawn pawn2 = dinfo.Instigator as Pawn;
            job.verbToUse = deflectVerb;

            if (pawn2 != null)
            {
                if (pawn2.equipment != null)
                {
                    CompEquippable compEquip = pawn2.equipment.PrimaryEq;
                    if (compEquip != null)
                    {
                        if (compEquip.PrimaryVerb != null)
                            job.verbToUse = CopyAndReturnNewVerb(compEquip.PrimaryVerb);
                    }
                }
                job.killIncappedTarget = pawn2.Downed;
            }
            GetPawn.jobs.TryTakeOrderedJob(job);
            Log.Message("TryToTakeOrderedJob Called");
        }
        
        /// <summary>
        /// This does the math for determining if shots are deflected.
        /// </summary>
        /// <param name="dinfo"></param>
        /// <param name="absorbed"></param>
        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            if (dinfo.WeaponGear != null)
            {
                if (!dinfo.WeaponGear.IsMeleeWeapon && dinfo.WeaponBodyPartGroup == null)
                {

                    if (HasCompActivatableEffect)
                    {
                        bool? isActive = (bool)AccessTools.Method(GetActivatableEffect.GetType(), "IsActive").Invoke(GetActivatableEffect, null);
                        if (isActive == false)
                        {
                            Log.Message("Inactivate Weapon");
                            absorbed = false;
                            return;
                        }
                    }
                    float calc = DeflectionChance;
                    int deflectThreshold = (int)(calc * 100); // 0.3f => 30
                    if (Rand.Range(1, 100) > deflectThreshold)
                    {
                        absorbed = false;
                        return;
                    }

                    ResolveDeflectVerb();
                    GiveDeflectJob(dinfo);
                    absorbed = true;
                    return;
                }
            }
            absorbed = false;
            return;
        }

        public override void PostExposeData()
        {
            Scribe_Values.LookValue<int>(ref this.animationDeflectionTicks, "animationDeflectionTicks", 0);
            base.PostExposeData();
        }


        public CompProperties_Deflector Props
        {
            get
            {
                return (CompProperties_Deflector)this.props;
            }
        }
    }
}
