using HarmonyLib;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

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


            Harmony.DEBUG = false;



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
}
