using HarmonyLib;
using StoryMode.CharacterCreationContent;
using System;
using System.Collections.Generic;
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
}