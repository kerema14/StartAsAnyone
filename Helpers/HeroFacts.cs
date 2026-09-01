using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace StartAsAnyone
{
    /// <summary>
    /// Everything the character screen needs about one candidate, computed once when a
    /// faction is chosen. Filtering, sorting and searching then run over these instead of
    /// walking the campaign again on every keystroke.
    /// </summary>
    public sealed class HeroFacts
    {
        public Hero Hero { get; }
        public Clan? Clan { get; }
        public CultureObject? Culture { get; }

        public string Name { get; }
        public string SearchName { get; }
        public string ClanName { get; }
        public string RoleText { get; }

        public float Age { get; }
        public int Gold { get; }
        public int Level { get; }
        public int Renown { get; }
        public int ClanTier { get; }
        public int Influence { get; }
        public int FiefCount { get; }
        public int TopSkill { get; }
        public int PartySize { get; }

        public bool IsFactionLeader { get; }
        public bool IsClanLeader { get; }
        public bool IsFemale { get; }
        public bool IsMarried { get; }
        public bool HasParty { get; }

        private HeroFacts(Hero hero, int fiefCount)
        {
            Hero = hero;
            Clan = hero.Clan;
            Culture = hero.Culture;

            Name = hero.Name?.ToString() ?? string.Empty;
            SearchName = Name.ToLowerInvariant();
            ClanName = Clan?.Name?.ToString() ?? string.Empty;

            Age = hero.Age;
            Gold = hero.Gold;
            Level = hero.CharacterObject?.Level ?? 0;
            Renown = (int)(Clan?.Renown ?? 0f);
            ClanTier = Clan?.Tier ?? 0;
            Influence = (int)(Clan?.Influence ?? 0f);
            FiefCount = fiefCount;
            IsFactionLeader = hero.IsFactionLeader;
            IsClanLeader = hero.IsClanLeader;
            IsFemale = hero.IsFemale;
            IsMarried = hero.Spouse != null;
            HasParty = hero.PartyBelongedTo != null;
            PartySize = hero.PartyBelongedTo?.MemberRoster?.TotalManCount ?? 0;
            TopSkill = BestSkillValue(hero);

            // One meaning per column: this is always the clan. Ruler and clan-leader
            // rank is shown by the crowns on the row portrait instead.
            RoleText = ClanName;
        }

        private static int BestSkillValue(Hero hero)
        {
            int best = 0;
            foreach (SkillObject skill in TaleWorlds.CampaignSystem.Extensions.Skills.All)
            {
                int value = hero.GetSkillValue(skill);
                if (value > best)
                {
                    best = value;
                }
            }
            return best;
        }

        /// <summary>
        /// Candidates playable from a faction. Skips anything malformed rather than
        /// throwing, so a mod-added kingdom with odd data costs us entries, not the screen.
        /// </summary>
        public static List<HeroFacts> Collect(IEnumerable<Hero>? heroes)
        {
            var result = new List<HeroFacts>();
            if (heroes == null)
            {
                return result;
            }

            Dictionary<Hero, int> fiefs = FiefCountsByOwner();
            var seen = new HashSet<string>();

            foreach (Hero hero in heroes)
            {
                if (!IsPlayable(hero))
                {
                    continue;
                }
                string id = hero.StringId ?? string.Empty;
                if (id.Length > 0 && !seen.Add(id))
                {
                    continue;
                }
                try
                {
                    result.Add(new HeroFacts(hero, fiefs.TryGetValue(hero, out int count) ? count : 0));
                }
                catch (Exception)
                {
                    // Malformed hero; leave it out of the list instead of failing the screen.
                }
            }
            return result;
        }

        private static bool IsPlayable(Hero? hero)
        {
            return hero != null
                && hero.IsAlive
                && hero.HeroState == Hero.CharacterStates.Active
                && hero.IsLord
                // Children carry Occupation.Lord from birth, so they reach here. They cannot
                // lead anything, and the character tableau has no adult body to build for them.
                && !hero.IsChild
                && hero.Clan != null
                && hero.CharacterObject != null
                && hero != Hero.MainHero;
        }

        private static Dictionary<Hero, int> FiefCountsByOwner()
        {
            var counts = new Dictionary<Hero, int>();
            if (Settlement.All == null)
            {
                return counts;
            }
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement == null || !(settlement.IsTown || settlement.IsCastle))
                {
                    continue;
                }
                Hero? owner = settlement.OwnerClan?.Leader;
                if (owner == null)
                {
                    continue;
                }
                counts[owner] = counts.TryGetValue(owner, out int current) ? current + 1 : 1;
            }
            return counts;
        }
    }
}
