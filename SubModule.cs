using HarmonyLib;
using Helpers;
using SandBox.GauntletUI.CharacterCreation;
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
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation.OptionsStage;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Clans;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Policies;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;

namespace StartAsAnyone
{
    public class SAASubModule : MBSubModuleBase
    {
        private static readonly Harmony _harmony = new Harmony("com.kerema14.startasanyone");
        private static bool _isInitialized = false;
        public static Hero heroToBeSet;
        public static bool heroInit;
        public static CampaignTime heroBirthday;
        public static bool startAsAnyone;
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            
            
            heroInit = false;
            if (!_isInitialized)
            {
                CharacterCreationContentPatcher.ApplyPatches(_harmony);
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                _isInitialized = true;
            }
        }
        public override void OnNewGameCreated(Game game, object initializerObject)
        {
            base.OnNewGameCreated(game, initializerObject);
            CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, setHeroAge);
            
        }
        

        

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            
        }
        public void setHeroAge()
        {
            if (heroInit)
            {
                
                Hero.MainHero.SetBirthDay(heroBirthday);
            }
        }
        
    }

    public class CharacterCreationContentPatcher
    {
        // This method will run when your mod initializes
        public static void ApplyPatches(Harmony harmony)
        {
            // Find all types that inherit from CharacterCreationContentBase
            var baseType = typeof(CharacterCreationContentBase);
            var inheritingTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        return e.Types.Where(t => t != null);
                    }
                    catch
                    {
                        return Type.EmptyTypes;
                    }
                })
                .Where(type => type != null && baseType.IsAssignableFrom(type) && type != baseType && !type.IsAbstract);

            // Log the types we found (for debugging purposes)
            foreach (var type in inheritingTypes)
            {
                Console.WriteLine($"Found CharacterCreationContentBase child: {type.FullName}");
            }

            // Create a method info that points to your patch method
            var characterCreationStagesPostfix = typeof(CharacterCreationContentPatcher).GetMethod(nameof(CharacterCreationStagesPostfix));


            // Apply the patch to each type
            foreach (var type in inheritingTypes)
            {
                try
                {
                    var originalMethod = AccessTools.PropertyGetter(type, "CharacterCreationStages");
                    if (originalMethod != null)
                    {
                        harmony.Patch(originalMethod, postfix: new HarmonyMethod(characterCreationStagesPostfix));
                        Console.WriteLine($"Successfully patched {type.FullName}.CharacterCreationStages");
                    }
                    else
                    {
                        Console.WriteLine($"Could not find CharacterCreationStages property on {type.FullName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error patching {type.FullName}: {ex.Message}");
                }
            }

            var onCharacterCreationFinalizedPrefix = typeof(CharacterCreationContentPatcher).GetMethod(nameof(OnCharacterCreationFinalizedPrefix));
            foreach (var type in inheritingTypes)
            {
                try
                {
                    var originalMethod = AccessTools.Method(type, "OnCharacterCreationFinalized");
                    if (originalMethod != null)
                    {
                        harmony.Patch(originalMethod, prefix: new HarmonyMethod(onCharacterCreationFinalizedPrefix));
                        Console.WriteLine($"Successfully patched {type.FullName}.OnCharacterCreationFinalized");
                    }
                    else
                    {
                        Console.WriteLine($"Could not find OnCharacterCreationFinalized property on {type.FullName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error patching {type.FullName}: {ex.Message}");
                }
            }
        }

        public static bool OnCharacterCreationFinalizedPrefix()
        {

            if (SAASubModule.startAsAnyone)
            {

                MapState mapState;
                bool flag2 = (mapState = (GameStateManager.Current.ActiveState as MapState)) != null;
                if (flag2)
                {
                    mapState.Handler.ResetCamera(true, true);
                    mapState.Handler.TeleportCameraToMainParty();
                }

                return false;
            }

            return true;
        }

        
        public static IEnumerable<Type> CharacterCreationStagesPostfix(IEnumerable<Type> __result)
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
                if (SAASubModule.startAsAnyone)
                {
                    setPlayerToLord(__instance);
                }
                
                
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        private static void setPlayerToLord(CharacterCreation cc)
        {
            Hero hero = SAASubModule.heroToBeSet;
            
            
            
            
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
            
            SAASubModule.heroBirthday = hero.BirthDay;
            SAASubModule.heroInit = true;
           


            Clan originalClan = Hero.MainHero.Clan;
            ChangePlayerCharacterAction.Apply(hero);
            Campaign.Current.SetPropertyValue("PlayerDefaultFaction", hero.Clan);
            MobileParty.MainParty.ItemRoster.Clear();
            DestroyClanAction2.Apply(originalClan);
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
                heroParty.ItemRoster.AddToCounts(grain, ((int)Math.Sqrt(troopRosterElement.Character.Tier)) * ((int)(troopcount/2)));
                heroParty.ItemRoster.AddToCounts(meat, (int)(troopcount/3));
            }
            foreach(Hero hero3 in friendsOfHero)
            {
                hero3.SetHasMet();
            }

            





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
            if (SAASubModule.startAsAnyone)
            {
                CharacterCreationState ccs = getCharacterCreationState();
                Type characterStateType = ccs.GetType();

                // Get the private field _stageIndex
                FieldInfo stageIndexField = characterStateType.GetField("_stageIndex",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (stageIndexField != null)
                {
                    // Set the new value
                    stageIndexField.SetValue(ccs, 1);
                }


                ccs.PreviousStage();
            }
            else
            {
                // Access the private _negativeAction field using reflection
                FieldInfo negativeActionField = AccessTools.Field(typeof(CharacterCreationOptionsStageView), "_negativeAction");
                ControlCharacterCreationStage negativeAction = (ControlCharacterCreationStage)negativeActionField.GetValue(__instance);
                negativeAction?.Invoke();
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











    [HarmonyPatch(typeof(KingdomDecision), "IsPlayerParticipant", MethodType.Getter)]
    public static class KingdomDecisions_IsPlayerParticipant_Patch
    {
        static void Postfix(ref bool __result)
        {
            // Modify the return value based on your condition
            
            
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