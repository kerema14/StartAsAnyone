using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace StartAsAnyone
{
    /// <summary>
    /// The right-hand panel on the character screen: 3D tableau, traits, skills, numbers
    /// and the related-character grids. Built only for the selected hero.
    /// </summary>
    public class HeroDetailVM : SAAViewModel
    {
        private const int RelationFriendThreshold = 40;
        private const int RelationRivalThreshold = -30;
        private const int MaxRelatedShown = 10;

        private readonly Action<Hero>? _onNavigate;
        private Hero? _hero;

        private HeroViewModel? _heroCharacter;
        private string _nameText = string.Empty;
        private string _rankText = string.Empty;
        private string _informationText = string.Empty;
        private string _skillsText = string.Empty;
        private string _familyText = string.Empty;
        private string _fiefsText = string.Empty;
        private string _friendsText = string.Empty;
        private string _rivalsText = string.Empty;
        private string _emptyText = string.Empty;
        private string _levelText = string.Empty;
        private string _goldText = string.Empty;
        private string _renownText = string.Empty;
        private string _influenceText = string.Empty;
        private string _partyText = string.Empty;
        private bool _isEmpty;
        private bool _hasTableau;
        private bool _hasClanStats;
        private bool _hasParty;

        public HeroDetailVM(Action<Hero>? onNavigate = null)
        {
            _onNavigate = onNavigate;

            Traits = new MBBindingList<EncyclopediaTraitItemVM>();
            Skills = new MBBindingList<EncyclopediaSkillVM>();
            Stats = new MBBindingList<StringPairItemVM>();
            Family = new MBBindingList<PortraitTileVM>();
            Fiefs = new MBBindingList<EncyclopediaSettlementVM>();
            Friends = new MBBindingList<PortraitTileVM>();
            Rivals = new MBBindingList<PortraitTileVM>();

            // One HeroViewModel for the life of the screen, refreshed in place. A new one
            // per selection also works, but rebuilding the tableau's texture provider on
            // every click is wasted work when only the properties need to change.
            HeroCharacter = new HeroViewModel(CharacterViewModel.StanceTypes.None);

            EmptyText = new TextObject("{=SAA_pick_hero}Select a character to see their details.").ToString();
            IsEmpty = true;
        }

        /// <summary>Repoints the panel at another hero, refreshing in place.</summary>
        public void SetHero(Hero? hero)
        {
            _hero = hero;
            IsEmpty = hero == null;
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            if (_hero == null)
            {
                HasTableau = false;
                Traits.Clear();
                Skills.Clear();
                Stats.Clear();
                Family.Clear();
                Fiefs.Clear();
                Friends.Clear();
                Rivals.Clear();
                return;
            }

            SkillsText = GameTexts.FindText("str_skills", null).ToString();
            FamilyText = GameTexts.FindText("str_family_group", null).ToString();
            FiefsText = new TextObject("{=LBNzsqyb}Fiefs").ToString();
            FriendsText = GameTexts.FindText("str_friends", null).ToString();
            RivalsText = GameTexts.FindText("str_enemies", null).ToString();

            NameText = _hero.Name?.ToString() ?? string.Empty;
            RankText = SafeRankText(_hero);
            InformationText = SafeBio(_hero);

            FillTableau(_hero);
            FillTraitsAndSkills(_hero);
            FillChips(_hero);
            FillStats(_hero);
            FillFamily(_hero);
            FillFiefs(_hero);
            FillRelations(_hero);
        }

        /// <summary>
        /// Fills the 3D tableau by hand rather than through <c>HeroViewModel.FillFrom(Hero, ...)</c>.
        /// That overload opens with
        /// <c>IsHidden = CampaignUIHelper.IsHeroInformationHidden(hero, out _); if (child || IsHidden) return;</c>
        /// and "hidden" means only "the player has not met them yet" — which, in character
        /// creation, is everyone except the handful of public figures the information model
        /// gives away for free, i.e. kingdom rulers. It returns without assigning a single
        /// property, so the shared view model kept the last body that filled successfully and
        /// every vassal showed their ruler. The player is choosing this character; there is
        /// nothing about them to withhold.
        /// </summary>
        private void FillTableau(Hero hero)
        {
            HeroViewModel? character = HeroCharacter;
            if (character == null)
            {
                HasTableau = false;
                return;
            }

            // The body builder itself refuses anything below adult meshes, and refusing is
            // silent: it would leave the previous hero on screen. Hide the tableau instead.
            if (FaceGen.GetMaturityTypeWithAge(hero.Age) <= BodyMeshMaturityType.Child)
            {
                HasTableau = false;
                return;
            }

            try
            {
                character.IsHidden = false;
                character.FillFrom(hero.CharacterObject, -1);
                character.IsDead = hero.IsDead;
                character.IdleFaceAnim = Helpers.CharacterHelper.GetNonconversationFacialIdle(hero.CharacterObject) ?? string.Empty;

                Equipment? equipment = (hero.IsNoncombatant && !hero.IsPartyLeader)
                    ? hero.CivilianEquipment
                    : hero.BattleEquipment;
                if (equipment != null)
                {
                    character.SetEquipment(equipment);
                }

                character.BannerCodeText = hero.ClanBanner?.BannerCode;
                character.ArmorColor1 = hero.MapFaction?.Color ?? 0u;
                character.ArmorColor2 = hero.MapFaction?.Color2 ?? 0u;

                // Show the face, not a helmet, and stand them on their own feet.
                character.SetEquipment(EquipmentIndex.NumAllWeaponSlots, default(EquipmentElement));
                character.SetEquipment(EquipmentIndex.ArmorItemEndSlot, default(EquipmentElement));
                character.SetEquipment(EquipmentIndex.HorseHarness, default(EquipmentElement));

                HasTableau = true;
            }
            catch (Exception)
            {
                // A hero with unusual equipment must not take the panel down. Whatever was
                // built up to the throw is not this hero, so do not show it.
                HasTableau = false;
            }
        }

        private void FillTraitsAndSkills(Hero hero)
        {
            Traits.Clear();
            Skills.Clear();

            foreach (TraitObject trait in CampaignUIHelper.GetHeroTraits())
            {
                if (trait != null && hero.GetTraitLevel(trait) != 0)
                {
                    Traits.Add(new EncyclopediaTraitItemVM(trait, hero));
                }
            }

            var skills = TaleWorlds.CampaignSystem.Extensions.Skills.All
                .Where(s => s != null && hero.GetSkillValue(s) >= 50)
                .OrderByDescending(hero.GetSkillValue)
                .ToList();
            foreach (SkillObject skill in skills)
            {
                Skills.Add(new EncyclopediaSkillVM(skill, hero.GetSkillValue(skill)));
            }
            OnPropertyChangedWithValue(HasAnySkills, nameof(HasAnySkills));
        }

        /// <summary>
        /// The four numbers a player actually compares characters on, shown as icon+value
        /// around the render. An icon carries the meaning, so these need no label and fit in
        /// the empty margins either side of the body.
        /// </summary>
        private void FillChips(Hero hero)
        {
            Clan? clan = hero.Clan;

            LevelText = (hero.CharacterObject?.Level ?? 0).ToString();
            GoldText = hero.Gold.ToString("N0");

            HasClanStats = clan != null;
            RenownText = ((int)(clan?.Renown ?? 0f)).ToString("N0");
            InfluenceText = ((int)(clan?.Influence ?? 0f)).ToString("N0");

            int partySize = hero.PartyBelongedTo?.MemberRoster?.TotalManCount ?? 0;
            HasParty = partySize > 0;
            PartyText = partySize.ToString("N0");
        }

        private void FillStats(Hero hero)
        {
            Stats.Clear();
            Clan? clan = hero.Clan;

            AddStat(GameTexts.FindText("str_enc_sf_age", null).ToString(), ((int)hero.Age).ToString());
            if (clan != null)
            {
                AddStat(GameTexts.FindText("str_clan", null).ToString(), clan.Name?.ToString() ?? string.Empty);
                AddStat(new TextObject("{=SAA_clan_tier}Clan tier").ToString(), clan.Tier.ToString());
            }
            if (hero.Culture != null)
            {
                AddStat(GameTexts.FindText("str_enc_sf_culture", null).ToString(), hero.Culture.Name?.ToString() ?? string.Empty);
            }
            if (hero.Spouse != null)
            {
                AddStat(GameTexts.FindText("str_spouse", null).ToString(), hero.Spouse.Name?.ToString() ?? string.Empty);
            }
        }

        private void AddStat(string definition, string value)
        {
            if (string.IsNullOrEmpty(definition))
            {
                return;
            }

            // Some of these labels come from the game's tables carrying a colon ("Age:") and
            // some do not ("Spouse"), which reads as a typo once they are stacked in a column.
            Stats.Add(new StringPairItemVM(definition.TrimEnd(' ', ':'), value ?? string.Empty, null));
        }

        private void FillFamily(Hero hero)
        {
            Family.Clear();
            foreach (Hero relative in RelativesOf(hero))
            {
                string role = string.Empty;
                try
                {
                    role = TaleWorlds.CampaignSystem.Conversation.ConversationHelper
                        .GetHeroRelationToHeroTextShort(relative, hero, true) ?? string.Empty;
                }
                catch (Exception)
                {
                    // Fall back to no label rather than dropping the relative.
                }
                Family.Add(new PortraitTileVM(relative, role, OnTileSelected, new HintViewModel(new TextObject(relative.Name?.ToString() ?? string.Empty), null)));
            }
            OnPropertyChangedWithValue(HasFamily, nameof(HasFamily));
        }

        private static IEnumerable<Hero> RelativesOf(Hero hero)
        {
            var seen = new HashSet<Hero>();
            var candidates = new List<Hero?> { hero.Father, hero.Mother, hero.Spouse };
            candidates.AddRange(hero.Children ?? Enumerable.Empty<Hero>());
            candidates.AddRange(hero.Siblings ?? Enumerable.Empty<Hero>());
            foreach (Hero? relative in candidates)
            {
                if (relative != null && relative != hero && seen.Add(relative))
                {
                    yield return relative;
                }
            }
        }

        private void FillFiefs(Hero hero)
        {
            Fiefs.Clear();
            if (Settlement.All == null)
            {
                return;
            }
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement != null && (settlement.IsTown || settlement.IsCastle) && settlement.OwnerClan?.Leader == hero)
                {
                    Fiefs.Add(new EncyclopediaSettlementVM(settlement));
                }
            }
            OnPropertyChangedWithValue(HasFiefs, nameof(HasFiefs));
        }

        private void FillRelations(Hero hero)
        {
            Friends.Clear();
            Rivals.Clear();

            var relatives = new HashSet<Hero>(RelativesOf(hero));
            var friends = new List<KeyValuePair<Hero, int>>();
            var rivals = new List<KeyValuePair<Hero, int>>();

            IReadOnlyList<Hero>? all = Hero.AllAliveHeroes;
            if (all == null)
            {
                return;
            }
            foreach (Hero other in all)
            {
                if (other == null || other == hero || other.IsNotable || relatives.Contains(other) || other.Clan == null)
                {
                    continue;
                }
                int relation;
                try
                {
                    relation = CharacterRelationManager.GetHeroRelation(hero, other);
                }
                catch (Exception)
                {
                    continue;
                }
                if (relation >= RelationFriendThreshold)
                {
                    friends.Add(new KeyValuePair<Hero, int>(other, relation));
                }
                else if (relation <= RelationRivalThreshold)
                {
                    rivals.Add(new KeyValuePair<Hero, int>(other, relation));
                }
            }

            foreach (var entry in friends.OrderByDescending(e => e.Value).Take(MaxRelatedShown))
            {
                Friends.Add(new PortraitTileVM(entry.Key, "", OnTileSelected, new HintViewModel(new TextObject((entry.Key.Name?.ToString() + " (" + entry.Value.ToString() + ")") ?? string.Empty), null)));
            }
            foreach (var entry in rivals.OrderBy(e => e.Value).Take(MaxRelatedShown))
            {
                Rivals.Add(new PortraitTileVM(entry.Key, "", OnTileSelected, new HintViewModel(new TextObject((entry.Key.Name?.ToString() + " (" + entry.Value.ToString() + ")") ?? string.Empty), null)));
            }

            OnPropertyChangedWithValue(HasFriends, nameof(HasFriends));
            OnPropertyChangedWithValue(HasRivals, nameof(HasRivals));
        }

        private void OnTileSelected(PortraitTileVM tile)
        {
            if (tile.Hero != null)
            {
                _onNavigate?.Invoke(tile.Hero);
            }
        }

        private static string SafeRankText(Hero hero)
        {
            try
            {
                if (hero.Clan?.Kingdom != null && !hero.IsNotable && !hero.IsWanderer)
                {
                    return CampaignUIHelper.GetHeroKingdomRank(hero) ?? string.Empty;
                }
                return CampaignUIHelper.GetHeroOccupationName(hero) ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static string SafeBio(Hero hero)
        {
            try
            {
                if (!TextObject.IsNullOrEmpty(hero.EncyclopediaText))
                {
                    return hero.EncyclopediaText.ToString();
                }
                if (hero.CharacterObject?.Occupation == Occupation.Lord)
                {
                    return Hero.SetHeroEncyclopediaTextAndLinks(hero)?.ToString() ?? string.Empty;
                }
            }
            catch (Exception)
            {
                // Some heroes have no generated blurb; an empty bio is fine.
            }
            return string.Empty;
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            HeroCharacter?.OnFinalize();
        }

        [DataSourceProperty] public MBBindingList<EncyclopediaTraitItemVM> Traits { get; }
        [DataSourceProperty] public MBBindingList<EncyclopediaSkillVM> Skills { get; }
        [DataSourceProperty] public MBBindingList<StringPairItemVM> Stats { get; }
        [DataSourceProperty] public MBBindingList<PortraitTileVM> Family { get; }
        [DataSourceProperty] public MBBindingList<EncyclopediaSettlementVM> Fiefs { get; }
        [DataSourceProperty] public MBBindingList<PortraitTileVM> Friends { get; }
        [DataSourceProperty] public MBBindingList<PortraitTileVM> Rivals { get; }

        [DataSourceProperty] public bool HasAnySkills => Skills.Count > 0;
        [DataSourceProperty] public bool HasFamily => Family.Count > 0;
        [DataSourceProperty] public bool HasFiefs => Fiefs.Count > 0;
        [DataSourceProperty] public bool HasFriends => Friends.Count > 0;
        [DataSourceProperty] public bool HasRivals => Rivals.Count > 0;

        [DataSourceProperty]
        public HeroViewModel? HeroCharacter
        {
            get => _heroCharacter;
            set => SetRef(ref _heroCharacter, value);
        }

        [DataSourceProperty]
        public string NameText
        {
            get => _nameText;
            set => SetString(ref _nameText, value);
        }

        [DataSourceProperty]
        public string RankText
        {
            get => _rankText;
            set => SetString(ref _rankText, value);
        }

        [DataSourceProperty]
        public string InformationText
        {
            get => _informationText;
            set => SetString(ref _informationText, value);
        }

        [DataSourceProperty]
        public string SkillsText
        {
            get => _skillsText;
            set => SetString(ref _skillsText, value);
        }

        [DataSourceProperty]
        public string FamilyText
        {
            get => _familyText;
            set => SetString(ref _familyText, value);
        }

        [DataSourceProperty]
        public string FiefsText
        {
            get => _fiefsText;
            set => SetString(ref _fiefsText, value);
        }

        [DataSourceProperty]
        public string FriendsText
        {
            get => _friendsText;
            set => SetString(ref _friendsText, value);
        }

        [DataSourceProperty]
        public string RivalsText
        {
            get => _rivalsText;
            set => SetString(ref _rivalsText, value);
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

        /// <summary>False when no body could be built for the selected hero, so the tableau
        /// is hidden rather than left showing whoever it rendered last.</summary>
        [DataSourceProperty]
        public bool HasTableau
        {
            get => _hasTableau;
            set => SetBool(ref _hasTableau, value);
        }

        [DataSourceProperty]
        public string LevelText
        {
            get => _levelText;
            set => SetString(ref _levelText, value);
        }

        [DataSourceProperty]
        public string GoldText
        {
            get => _goldText;
            set => SetString(ref _goldText, value);
        }

        [DataSourceProperty]
        public string RenownText
        {
            get => _renownText;
            set => SetString(ref _renownText, value);
        }

        [DataSourceProperty]
        public string InfluenceText
        {
            get => _influenceText;
            set => SetString(ref _influenceText, value);
        }

        [DataSourceProperty]
        public string PartyText
        {
            get => _partyText;
            set => SetString(ref _partyText, value);
        }

        /// <summary>Renown and influence belong to the clan, so a clanless hero has neither.</summary>
        [DataSourceProperty]
        public bool HasClanStats
        {
            get => _hasClanStats;
            set => SetBool(ref _hasClanStats, value);
        }

        [DataSourceProperty]
        public bool HasParty
        {
            get => _hasParty;
            set => SetBool(ref _hasParty, value);
        }
    }
}
