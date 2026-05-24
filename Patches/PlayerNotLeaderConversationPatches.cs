using HarmonyLib;
using SandBox.CampaignBehaviors;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Core;

namespace StartAsAnyone
{
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
}
