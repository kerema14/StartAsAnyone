using HarmonyLib;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace StartAsAnyone
{
    // Step 2: Patch the GetHeirApparents method using Harmony
    [HarmonyPatch(typeof(Clan), nameof(Clan.GetHeirApparents))]
    public static class GetHeirApparentsPatch
    {
        public static bool Prefix(ref Dictionary<Hero, int> __result, Clan __instance)
        {
            // Check if Hero.MainHero.Clan is this and Hero.MainHero != this.Leader
            if (Hero.MainHero.Clan == __instance && Hero.MainHero != __instance.Leader)
            {
                // Call your custom method
                __result = CustomGetHeirApparents();
                return false; // Skip the original method
            }

            // Proceed with the original method if the condition isn't met
            return true;
        }
        public static Dictionary<Hero, int> CustomGetHeirApparents()
        {
            Clan mainClan = Hero.MainHero.Clan;
            Dictionary<Hero, int> dictionary = new Dictionary<Hero, int>();
            int heroComesOfAge = Campaign.Current.Models.AgeModel.HeroComesOfAge;
            Hero leader = mainClan.Leader;
            foreach (Hero hero in mainClan.Heroes)
            {
                if (hero == mainClan.Leader && hero.IsAlive && hero.DeathMark == KillCharacterAction.KillCharacterActionDetail.None && !hero.IsNotSpawned && !hero.IsDisabled && !hero.IsWanderer && !hero.IsNotable && hero.Age >= (float)heroComesOfAge)
                {
                    int value = Campaign.Current.Models.HeirSelectionCalculationModel.CalculateHeirSelectionPoint(hero, mainClan.Leader, ref leader);
                    dictionary.Add(hero, value);
                }
            }
            if (leader != mainClan.Leader)
            {
                Dictionary<Hero, int> dictionary2 = dictionary;
                Hero key = leader;
                dictionary2[key] += Campaign.Current.Models.HeirSelectionCalculationModel.HighestSkillPoint;
            }
            return dictionary;
        }
    }
}
