using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SapientMechanoidFix
{
    /// <summary>
    /// [AV] Mechanoid Skins is an optional dependency, so none of its types are referenced
    /// at compile time - everything here resolves by name at runtime, same pattern as the
    /// rest of this mod's optional-mod integrations. AV_MechanoidSkins.SkinDef itself is
    /// stored and passed around as a plain Verse.Def (its actual base class), which is
    /// enough to Scribe it and show its label without needing SkinDef's own type at
    /// compile time at all - only calling AV Mechanoid Skins' own static helper methods
    /// (AllSkinDefsPawnkind) and setting fields on its Comp_MechanoidSkin needs reflection.
    /// </summary>
    public static class SummonedMechSkinChoiceSupport
    {
        private static readonly Type CompMechanoidSkinType = AccessTools.TypeByName("AV_MechanoidSkins.Comp_MechanoidSkin");
        private static readonly Type MechSkinManagerType = AccessTools.TypeByName("AV_MechanoidSkins.MechSkinManager");

        private static readonly MethodInfo AllSkinDefsPawnkindMethod = MechSkinManagerType == null
            ? null
            : AccessTools.Method(MechSkinManagerType, "AllSkinDefsPawnkind", new[] { typeof(PawnKindDef) });

        // AllUseableSkinDefs is a property, not a method - just filters DefDatabase<SkinDef>
        // down to ones with a real texPath/maskPath (see MechSkinManager.IsValidSkin), no
        // pawn-kind filtering at all. matchingMechs/useableForAll (what AllSkinDefsPawnkind
        // filters by) is purely curation on the skin author's part - every skin is one
        // universal texture set (texPath_south/west/east/north), not authored per body
        // shape, so nothing about actually rendering a skin requires it be "intended" for
        // the pawn kind wearing it.
        private static readonly PropertyInfo AllUseableSkinDefsProperty = MechSkinManagerType == null
            ? null
            : AccessTools.Property(MechSkinManagerType, "AllUseableSkinDefs");

        private static readonly FieldInfo CurrentSkinField = CompMechanoidSkinType == null
            ? null
            : AccessTools.Field(CompMechanoidSkinType, "currentSkin");

        private static readonly FieldInfo SpawnedBeforeField = CompMechanoidSkinType == null
            ? null
            : AccessTools.Field(CompMechanoidSkinType, "SpawnedBefore");

        private static readonly Type SkinDefType = AccessTools.TypeByName("AV_MechanoidSkins.SkinDef");

        // texPath_south/west/east/north are computed properties (texPath + "_<direction>",
        // each with its own override hook), not plain fields - see AV_MechanoidSkins.SkinDef.
        // Used here purely for preview icons in the picker; the real per-direction/masked
        // rendering happens entirely inside AV Mechanoid Skins' own render node once a skin
        // is actually applied to a pawn, this doesn't need to replicate any of that.
        private static readonly PropertyInfo[] TexPathDirectionProperties = SkinDefType == null
            ? new PropertyInfo[4]
            : new[]
            {
                AccessTools.Property(SkinDefType, "texPath_south"),
                AccessTools.Property(SkinDefType, "texPath_west"),
                AccessTools.Property(SkinDefType, "texPath_east"),
                AccessTools.Property(SkinDefType, "texPath_north"),
            };

        private static readonly Dictionary<Def, Texture2D[]> IconCache = new Dictionary<Def, Texture2D[]>();

        public static bool IsAvailable => CompMechanoidSkinType != null && AllSkinDefsPawnkindMethod != null
            && CurrentSkinField != null && SpawnedBeforeField != null;

        /// <summary>
        /// Every skin AV Mechanoid Skins considers valid for the given pawn kind (the
        /// summoned urchin's own kind, not the summoner's) - wraps its own public
        /// MechSkinManager.AllSkinDefsPawnkind(PawnKindDef), the same lookup its own
        /// PostSpawnSetup uses for automatic skin assignment.
        /// </summary>
        public static List<Def> GetSkinsForKind(PawnKindDef kind)
        {
            var result = new List<Def>();
            if (!IsAvailable || kind == null)
                return result;

            try
            {
                if (AllSkinDefsPawnkindMethod.Invoke(null, new object[] { kind }) is IEnumerable list)
                {
                    foreach (object item in list)
                    {
                        if (item is Def def)
                            result.Add(def);
                    }
                }
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Failed to look up skins for " + kind?.defName + ": " + e, 91274550);
            }
            return result;
        }

        /// <summary>
        /// Every skin AV Mechanoid Skins considers technically valid, regardless of which
        /// pawn kind(s) it was curated for - lets the player pick a design that "belongs"
        /// to some other mech entirely.
        /// </summary>
        public static List<Def> GetAllSkins()
        {
            var result = new List<Def>();
            if (!IsAvailable || AllUseableSkinDefsProperty == null)
                return result;

            try
            {
                if (AllUseableSkinDefsProperty.GetValue(null) is IEnumerable list)
                {
                    foreach (object item in list)
                    {
                        if (item is Def def)
                            result.Add(def);
                    }
                }
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Failed to look up all skins: " + e, 91274553);
            }
            return result;
        }

        /// <summary>
        /// All four directional preview textures for the picker - [South, West, East,
        /// North], matching AV Mechanoid Skins' own texPath_south/west/east/north order.
        /// Any entry can be null individually (missing texture for that direction, or the
        /// property couldn't be resolved) - caller just skips drawing that one slot. Never
        /// throws into the UI.
        /// </summary>
        public static Texture2D[] GetSkinIcons(Def skin)
        {
            if (skin == null)
                return new Texture2D[4];

            if (IconCache.TryGetValue(skin, out Texture2D[] cached))
                return cached;

            var result = new Texture2D[4];
            for (int i = 0; i < 4; i++)
            {
                PropertyInfo prop = TexPathDirectionProperties[i];
                if (prop == null)
                    continue;

                try
                {
                    if (prop.GetValue(skin) is string texPath && !texPath.NullOrEmpty())
                        result[i] = ContentFinder<Texture2D>.Get(texPath, reportFailure: false);
                }
                catch (Exception e)
                {
                    Log.ErrorOnce("[SapientMechanoidFix] Failed to load preview icon for " + skin.defName + ": " + e, 91274554);
                }
            }

            IconCache[skin] = result;
            return result;
        }

        /// <summary>
        /// Sets the pawn's Comp_MechanoidSkin.currentSkin directly and marks SpawnedBefore
        /// true, so AV Mechanoid Skins' own PostSpawnSetup (which only auto-assigns a skin
        /// when currentSkin is still null and SpawnedBefore is still false) doesn't
        /// immediately overwrite our choice on its own next check.
        /// </summary>
        public static void ApplySkin(Pawn pawn, Def skin)
        {
            if (pawn == null || skin == null || !IsAvailable)
                return;

            try
            {
                object comp = null;
                foreach (ThingComp c in pawn.AllComps)
                {
                    if (CompMechanoidSkinType.IsInstanceOfType(c))
                    {
                        comp = c;
                        break;
                    }
                }
                if (comp == null)
                    return;

                CurrentSkinField.SetValue(comp, skin);
                SpawnedBeforeField.SetValue(comp, true);
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Failed to apply chosen skin to " + pawn?.LabelShort + ": " + e, 91274551);
            }
        }
    }
}
