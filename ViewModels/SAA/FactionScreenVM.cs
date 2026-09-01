using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace StartAsAnyone
{
    /// <summary>Kingdoms / minor factions tabs plus the banner grid and detail panel.</summary>
    public class FactionScreenVM : SAAViewModel
    {
        private readonly Action _onSelectionChanged;
        private readonly List<FactionTileVM> _kingdoms = new List<FactionTileVM>();
        private readonly List<FactionTileVM> _minorFactions = new List<FactionTileVM>();

        private readonly FactionDetailVM _detail;
        private FactionTileVM? _selected;
        private string _kingdomsTabText = string.Empty;
        private string _minorFactionsTabText = string.Empty;
        private string _emptyText = string.Empty;
        private bool _isKingdomsTab = true;
        private bool _isMinorFactionsTab;
        private bool _isEmpty;

        public IFaction? SelectedFaction => _selected?.Faction;

        public FactionScreenVM(Action onSelectionChanged)
        {
            _onSelectionChanged = onSelectionChanged;
            VisibleFactions = new MBBindingList<FactionTileVM>();
            _detail = new FactionDetailVM();

            KingdomsTabText = new TextObject("{=SAA_tab_kingdoms}Kingdoms").ToString();
            MinorFactionsTabText = new TextObject("{=SAA_tab_minor}Minor factions").ToString();
            EmptyText = new TextObject("{=SAA_no_factions}No factions are available here.").ToString();

            Build();
            ShowTab(kingdoms: _kingdoms.Count > 0);
        }

        private void Build()
        {
            foreach (Kingdom kingdom in Kingdom.All ?? Enumerable.Empty<Kingdom>())
            {
                if (kingdom == null || kingdom.IsEliminated || !HeroesOf(kingdom).Any())
                {
                    continue;
                }
                _kingdoms.Add(new FactionTileVM(kingdom, OnTileSelected));
            }

            foreach (Clan clan in Clan.All ?? Enumerable.Empty<Clan>())
            {
                if (clan == null || clan.IsEliminated || clan.Kingdom != null || clan == Clan.PlayerClan)
                {
                    continue;
                }
                if (!HeroesOf(clan).Any())
                {
                    continue;
                }
                _minorFactions.Add(new FactionTileVM(clan, OnTileSelected));
            }
        }

        /// <summary>Heroes this mod is willing to hand the player, for a given faction.</summary>
        public static IEnumerable<Hero> HeroesOf(IFaction? faction)
        {
            IEnumerable<Hero>? heroes = faction switch
            {
                Kingdom kingdom => kingdom.Heroes,
                Clan clan => clan.Heroes,
                _ => null
            };
            if (heroes == null)
            {
                return Enumerable.Empty<Hero>();
            }
            return heroes.Where(h => h != null
                                     && h.IsAlive
                                     && h.HeroState == Hero.CharacterStates.Active
                                     && h.IsLord
                                     && h.Clan != null
                                     && h.CharacterObject != null
                                     && h != Hero.MainHero);
        }

        public void ExecuteSelectKingdomsTab() => ShowTab(kingdoms: true);

        public void ExecuteSelectMinorFactionsTab() => ShowTab(kingdoms: false);

        private void ShowTab(bool kingdoms)
        {
            IsKingdomsTab = kingdoms;
            IsMinorFactionsTab = !kingdoms;

            VisibleFactions.Clear();
            foreach (FactionTileVM tile in kingdoms ? _kingdoms : _minorFactions)
            {
                VisibleFactions.Add(tile);
            }
            IsEmpty = VisibleFactions.Count == 0;

            FactionTileVM? first = VisibleFactions.FirstOrDefault();
            if (first != null)
            {
                OnTileSelected(first);
            }
            else
            {
                SetSelection(null);
            }
        }

        private void OnTileSelected(FactionTileVM tile) => SetSelection(tile);

        private void SetSelection(FactionTileVM? tile)
        {
            foreach (FactionTileVM other in VisibleFactions)
            {
                other.IsSelected = ReferenceEquals(other, tile);
            }
            _selected = tile;
            Detail.SetFaction(tile?.Faction, tile == null ? 0 : HeroesOf(tile.Faction).Count());
            _onSelectionChanged();
        }

        [DataSourceProperty] public MBBindingList<FactionTileVM> VisibleFactions { get; }

        [DataSourceProperty] public FactionDetailVM Detail => _detail;

        [DataSourceProperty]
        public string KingdomsTabText
        {
            get => _kingdomsTabText;
            set => SetString(ref _kingdomsTabText, value);
        }

        [DataSourceProperty]
        public string MinorFactionsTabText
        {
            get => _minorFactionsTabText;
            set => SetString(ref _minorFactionsTabText, value);
        }

        [DataSourceProperty]
        public string EmptyText
        {
            get => _emptyText;
            set => SetString(ref _emptyText, value);
        }

        [DataSourceProperty]
        public bool IsKingdomsTab
        {
            get => _isKingdomsTab;
            set => SetBool(ref _isKingdomsTab, value);
        }

        [DataSourceProperty]
        public bool IsMinorFactionsTab
        {
            get => _isMinorFactionsTab;
            set => SetBool(ref _isMinorFactionsTab, value);
        }

        [DataSourceProperty]
        public bool IsEmpty
        {
            get => _isEmpty;
            set => SetBool(ref _isEmpty, value);
        }
    }
}
