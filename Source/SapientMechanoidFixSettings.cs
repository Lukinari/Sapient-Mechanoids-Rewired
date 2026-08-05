using Verse;

namespace SapientMechanoidFix
{
    public class SapientMechanoidFixSettings : ModSettings
    {
        public bool painImmunity = false;

        public bool allowMechResurrection = true;

        public bool fixMechSizeAndWeightClass = true;

        public bool freezeAscensionMegacorpReadiness = false;

        public int isMechanicalCacheRefreshTicks = 250;

        public bool freezeNonMechanicalCache = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref painImmunity, "painImmunity", false);
            Scribe_Values.Look(ref allowMechResurrection, "allowMechResurrection", true);
            Scribe_Values.Look(ref fixMechSizeAndWeightClass, "fixMechSizeAndWeightClass", true);
            Scribe_Values.Look(ref freezeAscensionMegacorpReadiness, "freezeAscensionMegacorpReadiness", false);
            Scribe_Values.Look(ref isMechanicalCacheRefreshTicks, "isMechanicalCacheRefreshTicks", 250);
            Scribe_Values.Look(ref freezeNonMechanicalCache, "freezeNonMechanicalCache", false);
        }
    }
}
