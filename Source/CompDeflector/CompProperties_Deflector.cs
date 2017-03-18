using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace CompDeflector
{
    public class CompProperties_Deflector : CompProperties
    {
        public float baseDeflectChance = 0.3f;
        public SoundDef deflectSound;

        public bool useManipulationInCalc = false;

        public bool useSkillInCalc = false;
        public SkillDef deflectSkill;
        public float deflectRatePerSkillPoint = 0.015f;
        public float deflectSkillLearnRate = 250f;

        public bool canReflect = false;
        public SkillDef reflectSkill;
        public float reflectRatePerSkillPoint = 3f;
        public float reflectSkillLearnRate = 500f;

        public VerbProperties DeflectVerb;

        public CompProperties_Deflector()
        {
            this.compClass = typeof(CompDeflector);
        }
        
        public virtual IEnumerable<StatDrawEntry> PostSpecialDisplayStats()
        {
            yield break;
        }


        [DebuggerHidden]
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            if (!useSkillInCalc)
            {
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon, "DeflectChance".Translate(), baseDeflectChance.ToStringPercent(), 0)
                {
                    overrideReportText = "DeflectChanceEx".Translate(),
                };
            }
            else
            {
                float calc = Mathf.Clamp(baseDeflectChance + (deflectRatePerSkillPoint * 20), 0f, 1.0f);
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon, "MaxDeflectChance".Translate(), calc.ToStringPercent(), 0)
                {
                    overrideReportText = "MaxDeflectChanceEx".Translate(new object[]
                    {
                        deflectSkill.label,
                        deflectRatePerSkillPoint.ToStringPercent(),
                        calc.ToStringPercent()
                    }),
                };
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon, "DeflectChancePerLevel".Translate(new object[] { deflectSkill.label }), deflectRatePerSkillPoint.ToStringPercent(), 0)
                {
                    overrideReportText = "DeflectChancePerLevelEx".Translate(new object[] { deflectSkill.label })
                };
            }

            IEnumerator<StatDrawEntry> enumerator2 = this.PostSpecialDisplayStats().GetEnumerator();
            while (enumerator2.MoveNext())
            {
                StatDrawEntry current2 = enumerator2.Current;
                yield return current2;
            }

            yield break;
        }
    }
}
