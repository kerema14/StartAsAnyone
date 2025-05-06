using HarmonyLib;
using Helpers;
using StoryMode.CharacterCreationContent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;

namespace StartAsAnyone
{
    public class SubModule : MBSubModuleBase
    {
        private static readonly Harmony _harmony = new Harmony("com.kerema14.startasanyone");
        private static bool _isInitialized = false;
        public static Hero heroToBeSet;
        
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            
            if (!_isInitialized)
            {
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                _isInitialized = true;
            }
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
        }
    }

    [HarmonyPatch(typeof(StoryModeCharacterCreationContent))]
    [HarmonyPatch("CharacterCreationStages", MethodType.Getter)]
    public class StoryModeCharacterCreationContentPatches
    {
        public static IEnumerable<Type> Postfix(IEnumerable<Type> __result)
        {
            // Create a new list with our custom stage first
            var stages = new List<Type> { typeof(CharacterCreationStartAsAnyoneOrNewStage) };

            // Add all the original stages
            foreach (var stage in __result)
            {
                stages.Add(stage);
            }

            return stages;
        }
    }

    [HarmonyPatch(typeof(SandboxCharacterCreationContent))]
    [HarmonyPatch("CharacterCreationStages", MethodType.Getter)]
    public class SandboxCharacterCreationContentPatches
    {
        public static IEnumerable<Type> Postfix(IEnumerable<Type> __result)
        {
            // Create a new list with our custom stage first
            var stages = new List<Type> { typeof(CharacterCreationStartAsAnyoneOrNewStage) };

            // Add all the original stages
            foreach (var stage in __result)
            {
                stages.Add(stage);
            }

            return stages;
        }
    }

    [HarmonyPatch(typeof(CharacterCreation), nameof(CharacterCreation.ApplyFinalEffects))]
    public static class CharacterCreation_ApplyFinalEffects_Patch
    {
        // Postfix runs after the original ApplyFinalEffects finishes
        static void Postfix(CharacterCreation __instance)
        {
            try
            {
                
                setPlayerToLord(__instance);
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        private static void setPlayerToLord(CharacterCreation cc)
        {
            Hero hero = SubModule.heroToBeSet;
            
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
                else if(hero1.Clan != null)
                {
                    nonLeaders.Add(hero1);
                }
            }
            List<Hero> all = kingdomLeaders.Concat(factionLeaders).Concat(clanLeaders).Concat(nonLeaders).ToList();
            if (hero == null)
            {
                hero = all.GetRandomElement();
            }
            
            if(hero.PartyBelongedTo == null) {
                //HeroSpawnCampaignBehavior.SpawnLordParty
                if (hero.GovernorOf != null)
                {
                    ChangeGovernorAction.RemoveGovernorOf(hero);
                }
                Settlement settlement = hero.Clan.HomeSettlement;
                MobileParty result;
                if (settlement != null && settlement.MapFaction == hero.MapFaction)
                {
                    result = MobilePartyHelper.SpawnLordParty(hero, settlement);
                }
                else if (hero.MapFaction.InitialPosition.IsValid)
                {
                    result = MobilePartyHelper.SpawnLordParty(hero, hero.MapFaction.InitialPosition, 30f);
                }
                else
                {
                    foreach (Settlement settlement2 in Settlement.All)
                    {
                        if (settlement2.Culture == hero.Culture)
                        {
                            settlement = settlement2;
                            break;
                        }
                    }
                    if (settlement != null)
                    {
                        result = MobilePartyHelper.SpawnLordParty(hero, settlement);
                    }
                    else
                    {
                        result = MobilePartyHelper.SpawnLordParty(hero, Settlement.All.GetRandomElement<Settlement>());
                    }
                } 
            }


            Clan originalClan = Hero.MainHero.Clan;
            ChangePlayerCharacterAction.Apply(hero);
            Campaign.Current.SetPropertyValue("PlayerDefaultFaction", hero.Clan);
            MobileParty.MainParty.ItemRoster.Clear();
            DestroyClanAction.Apply(originalClan);
            

            //hero, hero.CO and clan all have HiddinInEncyclopedia properties, but I'd rather get rid of them than hide them

            //unregisters the hero and characterObject objects from the managers
            //until the clan is removed it will still show the leader hero on its page
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
            float num = (254f + Campaign.AverageDistanceBetweenTwoFortifications * 4.54f) / 2f;
            foreach (Settlement settlement1 in Campaign.Current.Settlements)
            {
                if (settlement1.IsVillage)
                {
                    float num2 = heroParty.Position2D.Distance(settlement1.Position2D);
                    if (num2 < num)
                    {
                        foreach (ValueTuple<ItemObject, float> valueTuple in settlement1.Village.VillageType.Productions)
                        {
                            ItemObject item = valueTuple.Item1;
                            float item2 = valueTuple.Item2;
                            float num3 = (item.ItemType == ItemObject.ItemTypeEnum.Horse && item.HorseComponent.IsRideable && !item.HorseComponent.IsPackAnimal) ? 7f : (item.IsFood ? 0.1f : 0f);
                            float num4 = ((float)heroParty.MemberRoster.TotalManCount + 2f) / 200f;
                            float num5 = 1f - num2 / num;
                            int num6 = MBRandom.RoundRandomized(num3 * item2 * num5 * num4);
                            if (num6 > 0)
                            {
                                heroParty.ItemRoster.AddToCounts(item, num6);
                            }
                        }
                    }
                }
            }
            
            ItemObject grain = DefaultItems.Grain;
            ItemObject meat = DefaultItems.Meat;
            foreach (TroopRosterElement troopRosterElement in heroParty.MemberRoster.GetTroopRoster())
            {
                int troopcount = troopRosterElement.Number;
                heroParty.ItemRoster.AddToCounts(grain, ((int)Math.Sqrt(troopRosterElement.Character.Tier)) * troopcount);
                heroParty.ItemRoster.AddToCounts(meat, troopcount);
            }

            
            


        }


        
    }




}