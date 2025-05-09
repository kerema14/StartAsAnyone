﻿using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using System.Reflection;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace StartAsAnyone;

public static class DestroyClanAction2
{
    private enum DestroyClanActionDetails
    {
        Default,
        RebellionFailure,
        ClanLeaderDeath
    }

    private static void ApplyInternal(Clan destroyedClan, DestroyClanActionDetails details)
    {
        Type targetType = typeof(Clan);
        FieldInfo fieldInfo = AccessTools.Field(targetType, "_isEliminated");
        fieldInfo.SetValue(destroyedClan, true);
        foreach (WarPartyComponent item in destroyedClan.WarPartyComponents.ToList())
        {
            PartyBase destroyerParty = null;
            if (item.MobileParty.MapEvent != null)
            {
                destroyerParty = item.MobileParty.MapEventSide.OtherSide.LeaderParty;
                if (item.MobileParty.MapEvent != MobileParty.MainParty.MapEvent)
                {
                    item.MobileParty.MapEventSide = null;
                }
            }

            DestroyPartyAction.Apply(destroyerParty, item.MobileParty);
        }

        List<Hero> list = destroyedClan.Heroes.Where((Hero x) => x.IsAlive).ToList();
        for (int i = 0; i < list.Count; i++)
        {
            Hero hero = list[i];
            if (details != DestroyClanActionDetails.ClanLeaderDeath || hero != destroyedClan.Leader)
            {
                KillCharacterAction.ApplyByRemove(hero);
            }
        }

        if (details != DestroyClanActionDetails.ClanLeaderDeath && destroyedClan.Leader != null && destroyedClan.Leader.IsAlive && destroyedClan.Leader.DeathMark == KillCharacterAction.KillCharacterActionDetail.None)
        {
            KillCharacterAction.ApplyByRemove(destroyedClan.Leader);
        }

        if (!destroyedClan.Settlements.IsEmpty())
        {
            Clan clan = FactionHelper.ChooseHeirClanForFiefs(destroyedClan);
            foreach (Settlement item2 in destroyedClan.Settlements.ToList())
            {
                if (item2.IsTown || item2.IsCastle)
                {
                    Hero randomElementWithPredicate = clan.Lords.GetRandomElementWithPredicate((Hero x) => !x.IsChild && x.IsAlive);
                    ChangeOwnerOfSettlementAction.ApplyByDestroyClan(item2, randomElementWithPredicate);
                }
            }
        }

        
        if (destroyedClan.Kingdom != null)
        {
            ChangeKingdomAction.ApplyByLeaveKingdomByClanDestruction(destroyedClan);
        }

        if (destroyedClan.IsRebelClan)
        {
            ReflectionHelper.CallMethod(Campaign.Current.CampaignObjectManager, "RemoveClan", destroyedClan);
       
        }

    }

    public static void Apply(Clan destroyedClan)
    {
        ApplyInternal(destroyedClan, DestroyClanActionDetails.Default);
    }

    public static void ApplyByFailedRebellion(Clan failedRebellionClan)
    {
        ApplyInternal(failedRebellionClan, DestroyClanActionDetails.RebellionFailure);
    }

    public static void ApplyByClanLeaderDeath(Clan destroyedClan)
    {
        ApplyInternal(destroyedClan, DestroyClanActionDetails.ClanLeaderDeath);
    }
}
