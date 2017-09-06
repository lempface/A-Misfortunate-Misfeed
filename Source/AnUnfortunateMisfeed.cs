using System.Collections.Generic;
using Harmony;
using HugsLib;
using HugsLib.Settings;
using HugsLib.Utils;
using RimWorld;
using Verse;
using Verse.Sound;

namespace Lempface.Mods.Rimworld
{
    public class AnUnfortunateMisfeed : ModBase
    {
        public override string ModIdentifier => "AnUnfortunateMisfeed";

        public override void Initialize()
        {
            base.Initialize();
            Verb_LaunchProjectile_Patch.logger = Logger;
            Verb_LaunchProjectile_Patch.settings = Settings;

            Settings.GetHandle("jinxedJamPercentageModifier", "Jinxed Jam Percentage Modifier",
                "When a pawn with the jinxed trait fires a gun, this percentage will be added to the calculated chance to jam.",
                0.2f, Validators.FloatRangeValidator(0f, 1f));

            Settings.GetHandle("awfulJamPercentage", "Awful Quality Jam Percentage",
                "Awful Quality Jam Percentage", 0.5f, Validators.FloatRangeValidator(0f, 1f));
            Settings.GetHandle("shoddyJamPercentage", "Shoddy Quality Jam Percentage",
                "Shoddy Quality Jam Percentage", 0.45f, Validators.FloatRangeValidator(0f, 1f));
            Settings.GetHandle("poorJamPercentage", "Poor Quality Jam Percentage",
                "Poor Quality Jam Percentage", 0.4f, Validators.FloatRangeValidator(0f, 1f));
            Settings.GetHandle("normalJamPercentage", "Normal Quality Jam Percentage",
                "Normal Quality Jam Percentage", 0.25f, Validators.FloatRangeValidator(0f, 1f));
            Settings.GetHandle("goodJamPercentage", "Good Quality Jam Percentage",
                "Good Quality Jam Percentage", 0.1f, Validators.FloatRangeValidator(0f, 1f));
            Settings.GetHandle("superiorJamPercentage", "Superior Quality Jam Percentage",
                "Superior Quality Jam Percentage", 0.03f, Validators.FloatRangeValidator(0f, 1f));
            Settings.GetHandle("excellentJamPercentage", "Excellent Quality Jam Percentage",
                "Excellent Quality Jam Percentage", 0.02f, Validators.FloatRangeValidator(0f, 1f));
            Settings.GetHandle("masterworkJamPercentage", "Masterwork Quality Jam Percentage",
                "Masterwork Quality Jam Percentage", 0.01f, Validators.FloatRangeValidator(0f, 1f));
            Settings.GetHandle("legendaryJamPercentage", "Legendary Quality Jam Percentage",
                "Legendary Quality Jam Percentage", 0.005f, Validators.FloatRangeValidator(0f, 1f));

            Settings.GetHandle("damageOnJamPercentage", "Damage on Jam Percentage",
                "When a gun jams, the chance that the gun's condition (HP) is damaged.", 0.2f,
                Validators.FloatRangeValidator(0f, 1f));
            Settings.GetHandle("damageOnJamAmount", "Damage on Jam Amount",
                "When a gun jams, the amount of damage caused to the gun's condition (HP).", 1,
                Validators.IntRangeValidator(0, 100));
            Settings.GetHandle("criticalFailurePercentage", "Critical Failure Percentage",
                "When a gun jam causes damage, the chance that the gun will explode.", 0.05f, Validators.FloatRangeValidator(0f, 1f));

            Settings.GetHandle("weaponConditionWeight",
                "Weapon Condition Weight",
                "Adjusts the amount weapon condition affects jam chance.", 0.2f,
                Validators.FloatRangeValidator(0f, 1f));

            Settings.GetHandle("minimumUnjamPercentage", "Minimum Jam Clear Percentage",
                "Minimum Jam Clear Percentage", 0.2f, Validators.FloatRangeValidator(0f, 1f));
        }

        #region Nested type: CompJammable

        public class CompJammable : ThingComp
        {
            public bool jammed;

            public override void PostExposeData()
            {
                base.PostExposeData();
                Scribe_Values.Look(ref jammed, "IsJammed", false);
            }
        }

        #endregion

        #region Nested type: ModDefOf

        [DefOf]
        public static class ModDefOf
        {
            // ReSharper disable InconsistentNaming
            public static TraitDef Jinxed;
            public static TraitDef ProficientArmsman;
            public static SoundDef Jammed;
            // ReSharper restore InconsistentNaming
        }

        #endregion

        #region Nested type: Verb_LaunchProjectile_Patch

        [HarmonyPatch(typeof(Verb_LaunchProjectile))]
        [HarmonyPatch("TryCastShot")]
        public class Verb_LaunchProjectile_Patch
        {
            public static ModLogger logger;
            public static ModSettingsPack settings;

            // ReSharper disable once InconsistentNaming
            public static bool Prefix(Verb_LaunchProjectile __instance)
            {
                logger.Trace("Verb_LaunchProjectile_Patch Prefix Executed.");

                const int maxSkillLevel = 20;

                var pawn = __instance.CasterPawn;

                // if this is not a Pawn, like a turret, continue shot
                if (!__instance.CasterIsPawn)
                {
                    logger.Trace("Caster is not a pawn.");
                    return true;
                }

                var weaponConditionWeight = settings.GetHandle<float>("weaponConditionWeight").Value;
                var minimumUnjamPercentage = settings.GetHandle<float>("minimumUnjamPercentage").Value;
                var damageOnJamPercentage = settings.GetHandle<float>("damageOnJamPercentage").Value;
                var damageOnJamAmount = settings.GetHandle<int>("damageOnJamAmount").Value;
                var criticalFailurePercentage = settings.GetHandle<float>("criticalFailurePercentage").Value;

                var weapon = __instance.ownerEquipment;
                var compQuality = weapon.GetComp<CompQuality>();
                var compJammable = GetCompJammable(weapon);

                var shootingSkill = GetShootingSkillLevel(pawn);

                var weaponConditionModifier =
                    CalculateWeaponConditionModifier(weapon, weaponConditionWeight);
                var jamPercentage = CalculateJamPercentage(compQuality, weaponConditionModifier);
                var unjamPercentage = CalculateUnjamPercentage(minimumUnjamPercentage, shootingSkill, maxSkillLevel);

                var weaponIsJammed = compJammable.jammed;

                // if weapon is already jammed, try to unjam, skip shot
                if (weaponIsJammed)
                {
                    var unjammed = CheckForUnjam(unjamPercentage, pawn);

                    if (unjammed)
                    {
                        compJammable.jammed = false;
                        return false;
                    }
                }
                else
                {
                    // check for a jam
                    weaponIsJammed = CheckForJam(jamPercentage, pawn);
                }

                // if no jam, continue shot
                if (!weaponIsJammed)
                    return true;

                // if jam, set jammed, check if damaged, skip shot
                compJammable.jammed = true;
                ModDefOf.Jammed.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
                var damaged = CheckForJamDamage(damageOnJamPercentage);

                if (damaged)
                {
                    var criticalFailure = CheckForCriticalFailure(criticalFailurePercentage);

                    if (criticalFailure)
                    {
                        var bulletDef = __instance.verbProps.projectileDef.projectile;
                        GenExplosion.DoExplosion(pawn.Position, pawn.Map, 3.9f, bulletDef.damageDef, weapon,
                            DamageDefOf.Bomb.soundExplosion);
                        weapon.Destroy();
                    }
                    else
                    {
                        ApplyJamDamage(weapon, damageOnJamAmount);
                    }
                }

                return false;
            }

            private static CompJammable GetCompJammable(ThingWithComps weapon)
            {
                var compJammable = weapon.GetComp<CompJammable>();

                if (compJammable != null)
                    return compJammable;

                compJammable = new CompJammable();
                weapon.AllComps.Add(compJammable);

                return compJammable;
            }

            private static int GetShootingSkillLevel(Pawn pawn)
            {
                var shootingSkillRecord = pawn.skills.GetSkill(SkillDefOf.Shooting);

                if (shootingSkillRecord == null)
                    throw new KeyNotFoundException("Could not locate shooting skill.");

                logger.Trace("Shooting Skill: " + shootingSkillRecord.levelInt);

                return shootingSkillRecord.levelInt;
            }

            private static bool CheckForJam(float jamPercentage, Pawn pawn)
            {
                if (pawn.story.traits.HasTrait(ModDefOf.Jinxed))
                {
                    var modPercentage = settings.GetHandle<float>("jinxedJamPercentageModifier");

                    logger.Trace("jinxed pawn adds " + modPercentage + " to the jamPercentage.");

                    jamPercentage += modPercentage;
                }

                var jamRoll = Rand.Value;
                logger.Trace("jamRoll: " + jamRoll + " jamPercentage: " + jamPercentage);

                if (jamRoll <= jamPercentage)
                {
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "JAMMED", -1f);
                    logger.Trace("Weapon Jammed!");
                    return true;
                }

                return false;
            }

            private static bool CheckForUnjam(float unjamPercentage, Pawn pawn)
            {
                if (pawn.story.traits.HasTrait(ModDefOf.ProficientArmsman))
                {
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "UNJAMMED", -1f);
                    logger.Trace("Weapon Unjammed (Proficient Armsman)!");
                    return true;
                }

                var unjamRoll = Rand.Value;

                if (unjamRoll <= unjamPercentage)
                {
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "UNJAMMED", -1f);
                    logger.Trace("Weapon Unjammed!");
                    return true;
                }

                return false;
            }

            private static bool CheckForJamDamage(float damageOnJamPercentage)
            {
                var damageOnJamRoll = Rand.Value;

                logger.Trace("damageOnJamRoll: " + damageOnJamRoll +
                             " damageOnJamPercentage: " + damageOnJamPercentage);

                return damageOnJamRoll <= damageOnJamPercentage;
            }

            private static bool CheckForCriticalFailure(float criticalFailurePercentage)
            {
                var criticalFailureRoll = Rand.Value;

                logger.Trace("criticalFailureRoll: " + criticalFailureRoll + " criticalFailurePercentage: " + criticalFailurePercentage);

                return criticalFailureRoll <= criticalFailurePercentage;
            }

            private static void ApplyJamDamage(Thing weapon, int amount)
            {
                logger.Trace("Weapon Damaged! " + amount + " HP");
                weapon.HitPoints -= amount;

                if (weapon.HitPoints <= 0)
                    weapon.Destroy();
            }

            private static float CalculateJamPercentage(CompQuality compQuality, float weaponConditionModifier)
            {
                SettingHandle<float> handle = null;
                logger.Trace("compQuality: " + compQuality.Quality);

                switch (compQuality.Quality)
                {
                    case QualityCategory.Awful:
                        handle = settings.GetHandle<float>("awfulJamPercentage");
                        break;
                    case QualityCategory.Shoddy:
                        handle = settings.GetHandle<float>("shoddyJamPercentage");
                        break;
                    case QualityCategory.Poor:
                        handle = settings.GetHandle<float>("poorJamPercentage");
                        break;
                    case QualityCategory.Normal:
                        handle = settings.GetHandle<float>("normalJamPercentage");
                        break;
                    case QualityCategory.Good:
                        handle = settings.GetHandle<float>("goodJamPercentage");
                        break;
                    case QualityCategory.Superior:
                        handle = settings.GetHandle<float>("superiorJamPercentage");
                        break;
                    case QualityCategory.Excellent:
                        handle = settings.GetHandle<float>("excellentJamPercentage");
                        break;
                    case QualityCategory.Masterwork:
                        handle = settings.GetHandle<float>("masterworkJamPercentage");
                        break;
                    case QualityCategory.Legendary:
                        handle = settings.GetHandle<float>("legendaryJamPercentage");
                        break;
                }

                if (handle != null)
                    return handle.Value + weaponConditionModifier;

                return weaponConditionModifier;
            }

            private static float CalculateUnjamPercentage(float minimumUnjamPercentage, int shootingSkillLevel,
                int maxSkillLevel)
            {
                return minimumUnjamPercentage + (float) shootingSkillLevel / maxSkillLevel;
            }

            private static float CalculateWeaponCondition(ThingWithComps weapon)
            {
                var weaponCondition = (float) weapon.HitPoints / weapon.MaxHitPoints;

                logger.Trace("Hitpoint: " + weapon.HitPoints);
                logger.Trace("Max Hitpoint: " + weapon.MaxHitPoints);
                logger.Trace("Weapon Condition: " + weaponCondition);
                return weaponCondition;
            }

            private static float CalculateWeaponConditionModifier(ThingWithComps weapon, float weaponConditionWeight)
            {
                var weaponCondition = CalculateWeaponCondition(weapon);
                var weaponConditionModifier = InvertFloat(weaponCondition) * weaponConditionWeight;

                logger.Trace("weaponConditionModifier: " + weaponConditionModifier);
                return weaponConditionModifier;
            }

            private static float InvertFloat(float value)
            {
                return 1f - value;
            }
        }

        #endregion
    }
}