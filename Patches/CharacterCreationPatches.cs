using HarmonyLib;
using SandBox.GauntletUI.CharacterCreation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace StartAsAnyone
{
    [HarmonyPatch(typeof(CharacterCreationManager), nameof(CharacterCreationManager.GoToStage))]
    public static class GoToStage_SimplePatch
    {
        [HarmonyPrefix]
        public static void Prefix(CharacterCreationManager __instance, int stageIndex)
        {
            // Temporarily set _furthestStageIndex to allow any stage
            if(SubModule.startAsAnyone)
            {
                var furthestField = Traverse.Create(__instance).Field<int>("_furthestStageIndex");
                if (stageIndex > furthestField.Value)
                {
                    furthestField.Value = stageIndex;
                }
            }

        }
    }

    [HarmonyPatch(typeof(CharacterCreationManager), nameof(CharacterCreationManager.ApplyFinalEffects))]
    public static class CharacterCreation_ApplyFinalEffects_Patch
    {
        // Postfix runs after the original ApplyFinalEffects finishes
        static bool Prefix(CharacterCreationManager __instance)
        {
            try
            {
                if (SubModule.startAsAnyone)
                {
                    setPlayerToLord(__instance);
                    MapState mapState;
                    if ((mapState = (GameStateManager.Current.ActiveState as MapState)) != null)
                    {
                        mapState.Handler.ResetCamera(true, true);
                        mapState.Handler.TeleportCameraToMainParty();
                    }
                    return false;
                } else
                {
                    return true;
                }


            }
            catch (Exception e)
            {
                throw e;
            }
        }
        private static void setPlayerToLord(CharacterCreationManager cc)
        {
            Hero hero = SubModule.heroToBeSet;




            if (hero == null)
            {
                List<Hero> kingdomLeaders = new List<Hero>();
                List<Hero> factionLeaders = new List<Hero>();
                List<Hero> clanLeaders = new List<Hero>();
                List<Hero> nonLeaders = new List<Hero>();

                // Loop through all heroes and add them to the selection list
                foreach (Hero hero1 in Hero.AllAliveHeroes)
                {
                    if (hero1.IsFactionLeader)
                    {
                        factionLeaders.Add(hero1);
                    }
                    else if (hero1.IsClanLeader)
                    {
                        clanLeaders.Add(hero1);
                    }
                    else if (hero1.Clan != null)
                    {
                        nonLeaders.Add(hero1);
                    }
                }
                List<Hero> all = kingdomLeaders.Concat(factionLeaders).Concat(clanLeaders).Concat(nonLeaders).ToList();
                hero = all.GetRandomElement();
            }
            List<Hero> friendsOfHero = new List<Hero>();
            foreach (Hero fhero in Hero.AllAliveHeroes)
            {
                if (Math.Abs(hero.GetRelation(fhero)) > 0) {  friendsOfHero.Add(fhero);}
            }

            SubModule.heroBirthday = hero.BirthDay;
            SubModule.heroWeight = hero.Weight;
            SubModule.heroBuild = hero.Build;
            SubModule.heroStaticBodyProperties = hero.StaticBodyProperties;
            SubModule.heroInit = true;

            Hero.MainHero.Culture = hero.Culture;
            Clan originalClan = Hero.MainHero.Clan;
            ChangePlayerCharacterAction.Apply(hero);
            Campaign.Current.SetPropertyValue("PlayerDefaultFaction", hero.Clan);
            MobileParty.MainParty.ItemRoster.Clear();
            DestroyClanAction2.Apply(originalClan);

            try
            {
                setClanColors(Hero.MainHero.Clan);
            } catch (Exception e) { }



            //IsUnderMercenaryService





            foreach (Hero hero1 in originalClan.Heroes)
            {
                //get rid of the hero
                Campaign.Current.CampaignObjectManager.CallMethod("UnregisterDeadHero", new object[] { hero1 });
                //get rid of associated CO : stops them from showing in the encyclopedia
                Campaign.Current.ObjectManager.UnregisterObject(hero1.CharacterObject);
            }

            //finally remove the clan
            //there's a banner notification that shows up to notify of the clan being destroyed
            Campaign.Current.CampaignObjectManager.CallMethod("RemoveClan", new object[] { originalClan });
            MapState mapState;
            if ((mapState = GameStateManager.Current.ActiveState as MapState) != null)
            {
                //this eventually calles MapCameraView.ResetCamera which in turn calls MCV.TeleportCameraToMainParty
                mapState.Handler.ResetCamera(true, true);
                //is this necessary? it also calls MapCameraView.TCTMP once it goes through intermediary methods
                mapState.Handler.TeleportCameraToMainParty();
            }
            MobileParty heroParty = MobileParty.MainParty;

            foreach (Settlement settlement1 in Campaign.Current.Settlements)
            {
                if (settlement1.IsVillage)
                {
                    float num2 = heroParty.Position.Distance(settlement1.Position);

                    foreach (ValueTuple<ItemObject, float> valueTuple in settlement1.Village.VillageType.Productions)
                    {
                        ItemObject item = valueTuple.Item1;
                        float item2 = valueTuple.Item2;
                        float num3 = (item.ItemType == ItemObject.ItemTypeEnum.Horse && item.HorseComponent.IsRideable && !item.HorseComponent.IsPackAnimal) ? 7f : (item.IsFood ? 0.1f : 0f);
                        float num4 = ((float)heroParty.MemberRoster.TotalManCount + 2f) / 200f;
                        float num5 = 0.5f;
                        int num6 = MBRandom.RoundRandomized(num3 * item2 * num5 * num4);
                        if (num6 > 0)
                        {
                            heroParty.ItemRoster.AddToCounts(item, num6);
                        }
                    }

                }
            }

            ItemObject grain = DefaultItems.Grain;
            ItemObject meat = DefaultItems.Meat;
            foreach (TroopRosterElement troopRosterElement in heroParty.MemberRoster.GetTroopRoster())
            {
                int troopcount = troopRosterElement.Number;
                heroParty.ItemRoster.AddToCounts(grain, ((int)Math.Sqrt(troopRosterElement.Character.Tier)) * ((int)(troopcount/2)));
                heroParty.ItemRoster.AddToCounts(meat, (int)(troopcount/3));
            }
            foreach(Hero hero3 in friendsOfHero)
            {
                hero3.SetHasMet();
            }


        }
        public static void setClanColors(Clan clan)
        {


            clan.Color = clan.Banner.GetSecondaryColor();
            clan.Color2 = clan.Banner.GetFirstIconColor();

            //clan.color = clan.Banner.GetFirstIconColor();
            //clan.AlternativeColor2 = clan.Banner.GetPrimaryColor();



        }

    }


    [HarmonyPatch(typeof(CharacterCreationOptionsStageView), nameof(CharacterCreationOptionsStageView.PreviousStage))]
    public class CharacterCreationOptionsStageViewPatch
    {
        // This is a prefix patch that completely replaces the original method
        static bool Prefix(CharacterCreationOptionsStageView __instance)
        {
            // Call the RemoveMount method from the original class
            MethodInfo removeMount = AccessTools.Method(typeof(CharacterCreationOptionsStageView), "RemoveMount");
            removeMount.Invoke(__instance, null);

            // Add your custom condition
            if (SubModule.startAsAnyone)
            {
                CharacterCreationState ccs = getCharacterCreationState();
                ccs.CharacterCreationManager.GoToStage(0);


            }
            else
            {
                return true;
            }

            // Return false to skip the original method
            return false;
        }
        private static CharacterCreationState getCharacterCreationState()
        {
            GameState gm = GameStateManager.Current.ActiveState;
            CharacterCreationState characterCreationState = (gm.GetType().Equals(typeof(CharacterCreationState))) ? (CharacterCreationState)gm : null;
            return characterCreationState;
        }
    }
}
