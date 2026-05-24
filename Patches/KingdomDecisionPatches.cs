using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions;
using TaleWorlds.Localization;

namespace StartAsAnyone
{
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
