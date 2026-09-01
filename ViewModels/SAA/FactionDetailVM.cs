using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace StartAsAnyone
{
    /// <summary>Right-hand summary of the highlighted faction.</summary>
    public class FactionDetailVM : SAAViewModel
    {
        private BannerImageIdentifierVM? _banner;
        private PortraitTileVM? _leader;
        private string _nameText = string.Empty;
        private string _descriptorText = string.Empty;
        private string _leaderText = string.Empty;
        private string _warsText = string.Empty;
        private string _informationText = string.Empty;
        private string _emptyText = string.Empty;
        private bool _isEmpty;

        public FactionDetailVM()
        {
            Stats = new MBBindingList<StringPairItemVM>();
            Wars = new MBBindingList<EncyclopediaFactionVM>();
            EmptyText = new TextObject("{=SAA_pick_faction}Select a faction to see its details.").ToString();
            IsEmpty = true;
        }

        /// <summary>Repoints the panel at another faction, refreshing in place.</summary>
        public void SetFaction(IFaction? faction, int playableCount)
        {
            Stats.Clear();
            Wars.Clear();
            IsEmpty = faction == null;
            if (faction == null)
            {
                Banner = null;
                Leader = null;
                NameText = string.Empty;
                InformationText = string.Empty;
                OnPropertyChangedWithValue(HasLeader, nameof(HasLeader));
                OnPropertyChangedWithValue(HasWars, nameof(HasWars));
                return;
            }

            LeaderText = GameTexts.FindText("str_leader", null).ToString();
            WarsText = new TextObject("{=zZlWRZjO}Wars").ToString();
            NameText = faction.Name?.ToString() ?? string.Empty;
            DescriptorText = faction.IsKingdomFaction
                ? GameTexts.FindText("str_kingdom_faction", null).ToString()
                : new TextObject("{=SAA_minor_faction}Minor faction").ToString();

            Banner = faction.Banner != null ? new BannerImageIdentifierVM(faction.Banner, true) : null;
            Leader = faction.Leader != null ? new PortraitTileVM(faction.Leader) : null;
            OnPropertyChangedWithValue(HasLeader, nameof(HasLeader));

            InformationText = SafeText(faction);
            FillStats(faction, playableCount);
            FillWars(faction);
        }

        private void FillStats(IFaction faction, int playableCount)
        {
            CultureObject? culture = faction.Culture;
            if (culture != null)
            {
                Add(GameTexts.FindText("str_enc_sf_culture", null).ToString(), culture.Name?.ToString() ?? string.Empty);
            }

            var clans = ClansOf(faction).ToList();
            if (clans.Count > 0)
            {
                Add(new TextObject("{=bfQLwMUp}Clans").ToString(), clans.Count.ToString());
            }

            int fiefs = 0;
            if (Settlement.All != null)
            {
                fiefs = Settlement.All.Count(s => s != null && (s.IsTown || s.IsCastle) && s.MapFaction == faction);
            }
            Add(new TextObject("{=LBNzsqyb}Fiefs").ToString(), fiefs.ToString());

            Add(new TextObject("{=SAA_strength}Strength").ToString(), ((int)StrengthOf(faction)).ToString("N0"));
            Add(new TextObject("{=SAA_wealth}Wealth").ToString(), WealthOf(clans).ToString("N0"));
            Add(new TextObject("{=SAA_playable}Playable characters").ToString(), playableCount.ToString());
        }

        private void Add(string definition, string value)
        {
            if (!string.IsNullOrEmpty(definition))
            {
                Stats.Add(new StringPairItemVM(definition, value ?? string.Empty, null));
            }
        }

        private static IEnumerable<Clan> ClansOf(IFaction faction)
        {
            if (faction is Kingdom kingdom)
            {
                return kingdom.Clans?.Where(c => c != null) ?? Enumerable.Empty<Clan>();
            }
            return faction is Clan clan ? new[] { clan } : Enumerable.Empty<Clan>();
        }

        private static float StrengthOf(IFaction faction)
        {
            float total = 0f;
            if (MobileParty.AllLordParties == null)
            {
                return 0f;
            }
            foreach (MobileParty party in MobileParty.AllLordParties)
            {
                if (party?.Party == null || party.IsDisbanding || party.MapFaction != faction)
                {
                    continue;
                }
                try
                {
                    total += party.Party.GetCustomStrength(BattleSideEnum.Attacker, MapEvent.PowerCalculationContext.PlainBattle);
                }
                catch (Exception)
                {
                    // Skip parties the strength model cannot evaluate.
                }
            }
            return total;
        }

        private static int WealthOf(IEnumerable<Clan> clans)
        {
            int gold = 0;
            foreach (Clan clan in clans)
            {
                foreach (Hero lord in clan.Heroes ?? Enumerable.Empty<Hero>())
                {
                    if (lord != null && lord.IsAlive)
                    {
                        gold += lord.Gold;
                    }
                }
            }
            return gold;
        }

        private void FillWars(IFaction faction)
        {
            if (Campaign.Current?.Factions == null)
            {
                return;
            }
            foreach (IFaction other in Campaign.Current.Factions)
            {
                if (other == null || other == faction || other.IsBanditFaction || other.MapFaction == null)
                {
                    continue;
                }
                bool atWar;
                try
                {
                    atWar = FactionManager.IsAtWarAgainstFaction(faction, other.MapFaction);
                }
                catch (Exception)
                {
                    continue;
                }
                if (atWar && !Wars.Any(w => w.Faction == other.MapFaction))
                {
                    Wars.Add(new EncyclopediaFactionVM(other.MapFaction));
                }
            }
            OnPropertyChangedWithValue(HasWars, nameof(HasWars));
        }

        private static string SafeText(IFaction faction)
        {
            try
            {
                return faction.EncyclopediaText?.ToString() ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        [DataSourceProperty] public MBBindingList<StringPairItemVM> Stats { get; }
        [DataSourceProperty] public MBBindingList<EncyclopediaFactionVM> Wars { get; }
        [DataSourceProperty] public bool HasLeader => Leader != null;
        [DataSourceProperty] public bool HasWars => Wars.Count > 0;

        [DataSourceProperty]
        public BannerImageIdentifierVM? Banner
        {
            get => _banner;
            set => SetRef(ref _banner, value);
        }

        [DataSourceProperty]
        public PortraitTileVM? Leader
        {
            get => _leader;
            set => SetRef(ref _leader, value);
        }

        [DataSourceProperty]
        public string NameText
        {
            get => _nameText;
            set => SetString(ref _nameText, value);
        }

        [DataSourceProperty]
        public string DescriptorText
        {
            get => _descriptorText;
            set => SetString(ref _descriptorText, value);
        }

        [DataSourceProperty]
        public string LeaderText
        {
            get => _leaderText;
            set => SetString(ref _leaderText, value);
        }

        [DataSourceProperty]
        public string WarsText
        {
            get => _warsText;
            set => SetString(ref _warsText, value);
        }

        [DataSourceProperty]
        public string InformationText
        {
            get => _informationText;
            set => SetString(ref _informationText, value);
        }

        [DataSourceProperty]
        public string EmptyText
        {
            get => _emptyText;
            set => SetString(ref _emptyText, value);
        }

        [DataSourceProperty]
        public bool IsEmpty
        {
            get => _isEmpty;
            set => SetBool(ref _isEmpty, value);
        }
    }
}
