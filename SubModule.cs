using HarmonyLib;
using Helpers;
using SandBox.CampaignBehaviors;
using SandBox.GauntletUI.CharacterCreation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation.OptionsStage;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Clans;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Policies;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine.Screens;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.View;

namespace StartAsAnyone
{
    public class SubModule : MBSubModuleBase
    {
        private static readonly Harmony _harmony = new Harmony("com.kerema14.startasanyone");
        private static bool _isInitialized = false;
        public static Hero heroToBeSet;
        public static bool heroInit;
        public static CampaignTime heroBirthday;
        public static bool startAsAnyone;
        internal static float heroWeight;
        internal static float heroBuild;
        internal static StaticBodyProperties heroStaticBodyProperties;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            //InformationManager.DisplayMessage(new InformationMessage("Start As Anyone has been loaded"));
            
            
            Harmony.DEBUG = true;



            heroInit = false;
            if (!_isInitialized)
            {

                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                _isInitialized = true;
            }
        }
        public override void OnNewGameCreated(Game game, object initializerObject)
        {
            base.OnNewGameCreated(game, initializerObject);
            SubModule.heroToBeSet = Hero.MainHero;
            CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, setHeroAge);
            CampaignEvents.OnCharacterCreationInitializedEvent.AddNonSerializedListener(this, addSAAStageAction);
        }

        private void addSAAStageAction(CharacterCreationManager manager)
        {
            manager.AddStage(new CharacterCreationStartAsAnyoneOrNewStage());
        }

        public override void OnGameLoaded(Game game, object initializerObject)
        {
            base.OnGameLoaded(game, initializerObject);
            
        }

       

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            
        }
        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            base.InitializeGameStarter(game, starterObject);
            
        }

        public void setHeroAge()
        {
            if (heroInit)
            {
                
                Hero.MainHero.SetBirthDay(heroBirthday);
                Hero.MainHero.Weight = heroWeight;
                Hero.MainHero.Build = heroBuild;
                Hero.MainHero.StaticBodyProperties = heroStaticBodyProperties;
            }
        }
        
    }

    
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



    // Step 2: Patch the GetHeirApparents method using Harmony
    [HarmonyPatch(typeof(Clan), "GetHeirApparents")]
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


    [HarmonyPatch(typeof(CharacterCreationOptionsStageView), "PreviousStage")]
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





    [HarmonyPatch(typeof(LordConversationsCampaignBehavior),
                  nameof(LordConversationsCampaignBehavior.conversation_player_is_leaving_faction_on_condition))]
    public static class ConversationLeavingFactionPatch
    {
        // The prefix runs *before* the original.  If it returns false, the original method is skipped.
        static bool Prefix(ref bool __result)
        {
            // extra check: only proceed if MainHero is also clan leader
            if (Hero.MainHero.Clan.Leader != Hero.MainHero)
            {
                __result = false;
                return false; // skip original method entirely
            }
            return true; // continue into the original method
        }
    }
    [HarmonyPatch(
        typeof(LordConversationsCampaignBehavior),
        "conversation_clan_member_manage_troops_on_condition"
    )]
    public static class ConversationManageTroopsPatch
    {
        // Runs before the original. If we return false, the original method is skipped.
        static bool Prefix(ref bool __result)
        {
            // extra check: only allow if MainHero is clan leader
            if (Hero.MainHero.Clan.Leader != Hero.MainHero)
            {
                __result = false;
                return false; // skip original, force false
            }
            return true; // proceed to original method
        }
    }
    [HarmonyPatch(
        typeof(CompanionRolesCampaignBehavior),
        "companion_role_discuss_on_condition"
    )]
    public static class CompanionRoleDiscussPatch
    {
        static bool Prefix(ref bool __result)
        {
            var convoHero = Hero.OneToOneConversationHero;
            
            // extra check: if they're the clan leader, disallow
            if (convoHero == Clan.PlayerClan.Leader)
            {

                __result = false;
                return false;  // skip original
            }

            return true;  // proceed into original method
        }
    }
    [HarmonyPatch(
        typeof(RomanceCampaignBehavior),
        "FindPlayerRelativesEligibleForMarriage",
        new[] { typeof(Clan) }
    )]
    public static class FindPlayerRelativesMarriagePatch
    {
        // Prefix runs before the original. If it returns false, the original method is skipped.
        static bool Prefix(Clan withClan, ref List<CharacterObject> __result)
        {
            // if MainHero is not clan leader, return empty list
            if (Hero.MainHero.Clan.Leader != Hero.MainHero) 
            {
                __result = new List<CharacterObject>();
                return false; // skip original method
            }
            return true; // proceed into original method
        }
    }





    [HarmonyPatch(typeof(KingdomDecision), "IsPlayerParticipant", MethodType.Getter)]
    public static class KingdomDecisions_IsPlayerParticipant_Patch
    {
        static void Postfix(ref bool __result)
        {
            
            
            
             __result = __result && Hero.MainHero.IsClanLeader;
            

        }
    }

    [HarmonyPatch(typeof(KingdomManagementVM), "IsKingdomActionEnabled",MethodType.Getter)]
    public static class KingdomManagementVm_IsKingdomActionEnabled_Patch
    {
        static void Postfix(ref bool __result)
        {
            __result = __result && Hero.MainHero.IsClanLeader;
        }
    }

    [HarmonyPatch(typeof(DecisionOptionVM), "CanBeChosen", MethodType.Getter)]
    public static class DecisionOptionVM_CanBeChosen_Getter_Patch
    {
        static void Postfix(ref bool __result)
        {
            __result = __result && Hero.MainHero.IsClanLeader;
        }
    }
    [HarmonyPatch(typeof(KingdomDecision),nameof(KingdomDecision.NeedsPlayerResolution), MethodType.Getter)]
    public static class KingdomDecision_NeedsPlayerResolution_Getter_Patch
    {
        static void Postfix(ref bool __result)
        {
            __result = __result && Hero.MainHero.IsClanLeader;
        }
    }

    [HarmonyPatch(typeof(DecisionOptionVM),"RefreshCanChooseOption")]
    public static class DecisionOptionVM_RefreshCanChooseOption_Patch
    {
        static void Postfix(DecisionOptionVM __instance)
        {
            if(!Hero.MainHero.IsClanLeader)
            {
                __instance.OptionHint.HintText = new TextObject("You are not the clan leader, so you can't vote");
            }
        }
    }

}