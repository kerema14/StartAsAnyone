using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace StartAsAnyone
{
    /// <summary>One banner tile on the faction screen.</summary>
    public class FactionTileVM : SAAViewModel
    {
        private readonly Action<FactionTileVM> _onSelect;

        private BannerImageIdentifierVM? _banner;
        private HintViewModel? _hint;
        private string _nameText = string.Empty;
        private bool _isSelected;

        public IFaction Faction { get; }

        public FactionTileVM(IFaction faction, Action<FactionTileVM> onSelect)
        {
            Faction = faction;
            _onSelect = onSelect;

            NameText = faction.Name?.ToString() ?? string.Empty;
            if (faction.Banner != null)
            {
                Banner = new BannerImageIdentifierVM(faction.Banner, true);
            }

            string ruler = faction.Leader?.Name?.ToString() ?? string.Empty;
            Hint = new HintViewModel(ruler.Length > 0
                ? new TextObject("{=SAA_ruled_by}Ruled by {RULER}").SetTextVariable("RULER", ruler)
                : new TextObject(NameText), null);
        }

        public void ExecuteSelect() => _onSelect(this);

        [DataSourceProperty]
        public BannerImageIdentifierVM? Banner
        {
            get => _banner;
            set => SetRef(ref _banner, value);
        }

        [DataSourceProperty]
        public HintViewModel? Hint
        {
            get => _hint;
            set => SetRef(ref _hint, value);
        }

        [DataSourceProperty]
        public string NameText
        {
            get => _nameText;
            set => SetString(ref _nameText, value);
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetBool(ref _isSelected, value);
        }
    }
}
