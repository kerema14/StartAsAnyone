using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace StartAsAnyone
{
    /// <summary>
    /// One row in the character list. Deliberately cheap: there can be several hundred of
    /// these alive at once, so nothing here walks the campaign.
    /// </summary>
    public class HeroRowVM : SAAViewModel
    {
        private readonly Action<HeroRowVM> _onSelect;

        private CharacterImageIdentifierVM? _portrait;
        private BannerImageIdentifierVM? _clanBanner;
        private string _nameText = string.Empty;
        private string _roleText = string.Empty;
        private string _sortValueText = string.Empty;
        private bool _isSelected;
        private bool _isFiltered;
        private bool _isFactionLeader;
        private bool _isClanLeaderOnly;

        public HeroFacts Facts { get; }
        public Hero Hero => Facts.Hero;

        public HeroRowVM(HeroFacts facts, Action<HeroRowVM> onSelect)
        {
            Facts = facts;
            _onSelect = onSelect;

            Portrait = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(facts.Hero.CharacterObject));
            if (facts.Clan?.Banner != null)
            {
                ClanBanner = new BannerImageIdentifierVM(facts.Clan.Banner);
            }
            NameText = facts.Name;
            RoleText = facts.RoleText;
            IsFactionLeader = facts.IsFactionLeader;
            IsClanLeaderOnly = facts.IsClanLeader && !facts.IsFactionLeader;
        }

        public void ExecuteSelect() => _onSelect(this);

        public void ExecuteBeginHint()
        {
            if (Campaign.Current != null)
            {
                InformationManager.ShowTooltip(typeof(Hero), new object[] { Hero, false });
            }
        }

        public void ExecuteEndHint() => MBInformationManager.HideInformations();

        [DataSourceProperty]
        public CharacterImageIdentifierVM? Portrait
        {
            get => _portrait;
            set => SetRef(ref _portrait, value);
        }

        [DataSourceProperty]
        public BannerImageIdentifierVM? ClanBanner
        {
            get => _clanBanner;
            set => SetRef(ref _clanBanner, value);
        }

        [DataSourceProperty]
        public string NameText
        {
            get => _nameText;
            set => SetString(ref _nameText, value);
        }

        [DataSourceProperty]
        public string RoleText
        {
            get => _roleText;
            set => SetString(ref _roleText, value);
        }

        [DataSourceProperty]
        public string SortValueText
        {
            get => _sortValueText;
            set => SetString(ref _sortValueText, value);
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetBool(ref _isSelected, value);
        }

        /// <summary>Hidden by the search box or the filter rail; the row object survives.</summary>
        [DataSourceProperty]
        public bool IsFiltered
        {
            get => _isFiltered;
            set => SetBool(ref _isFiltered, value);
        }

        [DataSourceProperty]
        public bool IsFactionLeader
        {
            get => _isFactionLeader;
            set => SetBool(ref _isFactionLeader, value);
        }

        [DataSourceProperty]
        public bool IsClanLeaderOnly
        {
            get => _isClanLeaderOnly;
            set => SetBool(ref _isClanLeaderOnly, value);
        }
    }
}
