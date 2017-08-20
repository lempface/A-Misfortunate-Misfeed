using System.Linq;
using Harmony;
using HugsLib.Settings;
using HugsLib.Utils;
using RimWorld;
using Verse;

namespace Lempface.Mods.Rimworld
{
    public class AnUnfortunateMisfeed : HugsLib.ModBase
    {
        public override void Initialize()
        {
            base.Initialize();
            Verb_LaunchProjectile_Patch.logger = Logger;
            Verb_LaunchProjectile_Patch.settings = Settings;

            Settings.GetHandle<float>("awfulJamPercentage", "Awful Quality Jam Percentage",
                "Awful Quality Jam Percentage", 0.5f, Validators.FloatRangeValidator(0f, 1f));
            Settings.GetHandle<float>("shoddyJamPercentage", "Shoddy Quality Jam Percentage",
                "Shoddy Quality Jam Percentage", 0.45f, Validators.FloatRangeValidator(0f, 1f));
            Settings.GetHandle<float>("poorJamPercentage", "Poor Quality Jam Percentage",
                "Poor Quality Jam Percentage", 0.4f, Validators.FloatRangeValidator(0f, 1f));
            Settings.GetHandle<float>("normalJamPercentage", "Normal Quality Jam Percentage",
                "Normal Quality Jam Percentage", 0.25f, Validators.FloatRangeValidator(0f, 1f));
            Settings.GetHandle<float>("goodJamPercentage", "Good Quality Jam Percentage",
                "Good Quality Jam Percentage", 0.1f, Validators.FloatRangeValidator(0f, 1f));
            Settings.GetHandle<float>("superiorJamPercentage", "Superior Quality Jam Percentage",
                "Superior Quality Jam Percentage", 0.03f, Validators.FloatRangeValidator(0f, 1f));
            Settings.GetHandle<float>("excellentJamPercentage", "Excellent Quality Jam Percentage",
                "Excellent Quality Jam Percentage", 0.02f, Validators.FloatRangeValidator(0f, 1f));
            Settings.GetHandle<float>("masterworkJamPercentage", "Masterwork Quality Jam Percentage",
                "Masterwork Quality Jam Percentage", 0.01f, Validators.FloatRangeValidator(0f, 1f));
            Settings.GetHandle<float>("legendaryJamPercentage", "Legendary Quality Jam Percentage",
                "Legendary Quality Jam Percentage", 0.005f, Validators.FloatRangeValidator(0f, 1f));

            Settings.GetHandle<float>("damageOnJamPercentage", "Damage on Jam Percentage",
                "When a gun jams, the chance that the gun's condition (HP) is damaged.", 0.2f, Validators.FloatRangeValidator(0f, 1f));
            Settings.GetHandle<int>("damageOnJamAmount", "Damage on Jam Amount",
                "When a gun jams, the amount of damage caused to the gun's condition (HP).", 1, Validators.IntRangeValidator(0, 100));
        }

        public override string ModIdentifier => "AnUnfortunateMisfeed";

        public class CompJammable : ThingComp
        {
            public bool jammed;
            public override void PostExposeData()
            {
                base.PostExposeData();
                Scribe_Values.Look<bool>(ref jammed, "IsJammed", false);
            }
        }

        [HarmonyPatch(typeof(Verb_LaunchProjectile))]
        [HarmonyPatch("TryCastShot")]
        public class Verb_LaunchProjectile_Patch
        {
            public static ModLogger logger;
            public static ModSettingsPack settings;
            public static bool Prefix(Verb_LaunchProjectile __instance)
            {
                logger.Trace("Verb_LaunchProjectile_Patch Prefix Executed.");

                var jamPercentage = 0.50f;
                var compJammable = __instance.ownerEquipment.GetComp<CompJammable>();
                var shootingSkillDef = DefDatabase<SkillDef>.AllDefs.Single(x => x.defName == "Shooting");
                var shootingSkillRecord = __instance.CasterPawn.skills.GetSkill(shootingSkillDef);

                logger.Trace("Hitpoint: " + __instance.ownerEquipment.HitPoints);
                logger.Trace("Max Hitpoint: " + __instance.ownerEquipment.MaxHitPoints);

                if (__instance.ownerEquipment.HitPoints <= 0)
                {
                    __instance.ownerEquipment.Destroy();

                    return false;
                }

                var weaponCondition = (float)__instance.ownerEquipment.HitPoints / __instance.ownerEquipment.MaxHitPoints;
                logger.Trace("weaponCondition: " + weaponCondition);

                var weaponConditionModifier = (1f - weaponCondition) * 0.2f;
                logger.Trace("weaponConditionModifier: " + weaponConditionModifier);

                var compQuality = __instance.ownerEquipment.GetComp<CompQuality>();
                logger.Trace("compQuality: " + compQuality.Quality);

                SettingHandle<float> handle = null;
       
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
                    jamPercentage = handle.Value;

                jamPercentage += weaponConditionModifier;

                if (compJammable == null)
                {
                    compJammable = new CompJammable();
                    __instance.ownerEquipment.AllComps.Add(compJammable);
                }
                
                var jamRoll = Rand.Value;
                logger.Trace("jamRoll: " + jamRoll + " jamPercentage: " + jamPercentage);

                if (jamRoll <= jamPercentage)
                {
                    logger.Trace("Weapon Jammed!");
                    compJammable.jammed = true;
                }

                if (!compJammable.jammed)
                    return true;

                logger.Trace("Shooting Skill: " + shootingSkillRecord.levelInt);

                var damageOnJamRoll = Rand.Value;
                logger.Trace("damageOnJamRoll: " + damageOnJamRoll + " damageOnJamPercentage: " + settings.GetHandle<float>("damageOnJamPercentage").Value);

                if (damageOnJamRoll <= settings.GetHandle<float>("damageOnJamPercentage").Value)
                {
                    logger.Trace("Weapon Damaged! " + settings.GetHandle<int>("damageOnJamAmount").Value + " HP");
                    __instance.ownerEquipment.HitPoints -= settings.GetHandle<int>("damageOnJamAmount").Value;
                }

                const int maxSkillLevel = 20;
                var unjamPercentage = 0.2f + (float)shootingSkillRecord.levelInt / maxSkillLevel;
                var unjamRoll = Rand.Value;

                logger.Trace("unjamRoll: " + jamRoll + " unjamPercentage: " + unjamPercentage);

                if (unjamRoll <= unjamPercentage)
                {
                    logger.Trace("Weapon Unjammed!");
                    compJammable.jammed = false;
                }

                MoteMaker.ThrowText(__instance.caster.DrawPos, __instance.caster.Map, "JAMMED", -1f);
                return false;
            }
        }
    }
}
