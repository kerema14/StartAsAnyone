using System;
using System.Reflection;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace StartAsAnyone
{
    /// <summary>
    /// Every campaign mutation the mod performs while the character creation screen is
    /// open. Kept out of the view models on purpose: a view model that edits the campaign
    /// fires at unpredictable times and is the first thing to break next to another mod.
    /// </summary>
    public static class StartAsAnyoneApplier
    {
        /// <summary>Commits the player's choice. Safe to call only with a real hero.</summary>
        public static void Apply(Hero? hero)
        {
            if (hero == null)
            {
                return;
            }

            SubModule.heroToBeSet = hero;
            SubModule.startAsAnyone = true;

            SpawnParty(hero);
            CopyMountedEquipment(hero);
            CopyCharacterObject(hero);
        }

        private static void SpawnParty(Hero hero)
        {
            if (hero.PartyBelongedTo != null || hero.Clan == null)
            {
                return;
            }

            if (hero.GovernorOf != null)
            {
                ChangeGovernorAction.RemoveGovernorOf(hero);
            }

            // Landless clans start next to a kinsman rather than in a stranger's town.
            if (hero.Clan.Kingdom == null)
            {
                foreach (Hero kin in hero.Clan.Heroes)
                {
                    if (kin?.PartyBelongedTo != null)
                    {
                        MobilePartyHelper.SpawnLordParty(hero, kin.PartyBelongedTo.Position, 30f);
                        return;
                    }
                }
            }

            Settlement? home = hero.HomeSettlement;
            if (home != null && home.MapFaction == hero.MapFaction)
            {
                MobilePartyHelper.SpawnLordParty(hero, home);
                return;
            }
            if (hero.MapFaction?.FactionMidSettlement != null)
            {
                MobilePartyHelper.SpawnLordParty(hero, hero.MapFaction.FactionMidSettlement);
                return;
            }

            Settlement? fallback = null;
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement?.Culture == hero.Culture)
                {
                    fallback = settlement;
                    break;
                }
            }
            fallback ??= Settlement.All?.GetRandomElement<Settlement>();
            if (fallback != null)
            {
                MobilePartyHelper.SpawnLordParty(hero, fallback);
            }
        }

        private static void CopyCharacterObject(Hero hero)
        {
            if (hero.CharacterObject == null || Hero.MainHero == null)
            {
                return;
            }

            MethodInfo? setCharacterObject = AccessTools.Method(typeof(Hero), "SetCharacterObject");
            if (setCharacterObject == null)
            {
                // Game update moved the method; the rest of the flow still works.
                return;
            }
            CharacterObject character = CharacterObject.CreateFrom(hero.CharacterObject, null);
            setCharacterObject.Invoke(Hero.MainHero, new object[] { character });
            Hero.MainHero.StaticBodyProperties = hero.StaticBodyProperties;
        }

        private static void CopyMountedEquipment(Hero hero)
        {
            if (hero.CharacterObject == null || !hero.CharacterObject.HasMount() || Hero.MainHero == null)
            {
                return;
            }

            PropertyInfo? battleEquipment = typeof(Hero).GetProperty(
                "_battleEquipment", BindingFlags.NonPublic | BindingFlags.Instance);
            if (battleEquipment?.CanWrite == true)
            {
                battleEquipment.SetValue(Hero.MainHero, hero.BattleEquipment.Clone());
            }
        }
    }
}
