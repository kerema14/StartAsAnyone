using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace StartAsAnyone
{
    /// <summary>
    /// Search, filter, sort and pick a character. Filtering hides existing rows rather
    /// than rebuilding the list, so typing in the search box costs nothing but a pass
    /// over precomputed facts.
    /// </summary>
    public class CharacterScreenVM : SAAViewModel
    {
        private sealed class SortOption
        {
            public TextObject Label { get; }
            public Comparison<HeroFacts> Comparison { get; }
            public Func<HeroFacts, string> Display { get; }

            public SortOption(TextObject label, Comparison<HeroFacts> comparison, Func<HeroFacts, string> display)
            {
                Label = label;
                Comparison = comparison;
                Display = display;
            }
        }

        private readonly Action _onSelectionChanged;
        private readonly List<HeroFacts> _facts = new List<HeroFacts>();
        private readonly List<SortOption> _sortOptions = new List<SortOption>();

        private readonly HeroDetailVM _detail;
        private SelectorVM<SelectorItemVM>? _sortSelection;
        private HeroRowVM? _selectedRow;
        private string _searchText = string.Empty;
        private string _searchPlaceholderText = string.Empty;
        private string _resultCountText = string.Empty;
        private string _emptyText = string.Empty;
        private string _resetFiltersText = string.Empty;
        private string _nameColumnText = string.Empty;
        private string _sortByLabel = string.Empty;
        private bool _isEmpty;
        private bool _isSearchEmpty = true;
        private bool _canResetFilters;
        private int _sortIndex;

        public Hero? SelectedHero => _selectedRow?.Hero;

        public CharacterScreenVM(Action onSelectionChanged)
        {
            _onSelectionChanged = onSelectionChanged;
            Rows = new MBBindingList<HeroRowVM>();
            FilterGroups = new MBBindingList<SAAFilterGroupVM>();
            _detail = new HeroDetailVM(NavigateTo);

            SearchPlaceholderText = new TextObject("{=SAA_search}Search by name...").ToString();
            ResetFiltersText = new TextObject("{=SAA_reset_filters}Reset filters").ToString();
            NameColumnText = GameTexts.FindText("str_sort_by_name_label", null).ToString();
            SortByLabel = new TextObject("{=SAA_sort_by}Sort by").ToString();
            EmptyText = new TextObject("{=SAA_no_characters}No characters match your filters.").ToString();

            BuildSortOptions();
        }

        /// <summary>Rebuilds the whole screen for a newly chosen faction.</summary>
        public void SetCandidates(IEnumerable<Hero>? heroes)
        {
            _facts.Clear();
            _facts.AddRange(HeroFacts.Collect(heroes));

            Rows.Clear();
            foreach (HeroFacts facts in _facts)
            {
                Rows.Add(new HeroRowVM(facts, OnRowSelected));
            }

            BuildFilterGroups();
            ApplySort();
            ApplyFilters();
        }

        public int CandidateCount => _facts.Count;

        // -- sorting ---------------------------------------------------------

        private void BuildSortOptions()
        {
            _sortOptions.Clear();
            _sortOptions.Add(new SortOption(new TextObject("{=SAA_sort_level}Level"),
                (a, b) => b.Level.CompareTo(a.Level), f => f.Level.ToString()));
            _sortOptions.Add(new SortOption(new TextObject("{=SAA_sort_skill}Best skill"),
                (a, b) => b.TopSkill.CompareTo(a.TopSkill), f => f.TopSkill.ToString()));
            _sortOptions.Add(new SortOption(new TextObject("{=SAA_sort_name}Name"),
                (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase), f => f.Name));
            _sortOptions.Add(new SortOption(new TextObject("{=SAA_sort_age}Age"),
                (a, b) => a.Age.CompareTo(b.Age), f => ((int)f.Age).ToString()));
            _sortOptions.Add(new SortOption(new TextObject("{=SAA_sort_gold}Gold"),
                (a, b) => b.Gold.CompareTo(a.Gold), f => f.Gold.ToString("N0")));
            _sortOptions.Add(new SortOption(new TextObject("{=SAA_sort_renown}Clan renown"),
                (a, b) => b.Renown.CompareTo(a.Renown), f => f.Renown.ToString("N0")));
            _sortOptions.Add(new SortOption(new TextObject("{=SAA_sort_fiefs}Fiefs"),
                (a, b) => b.FiefCount.CompareTo(a.FiefCount), f => f.FiefCount.ToString()));
            _sortOptions.Add(new SortOption(new TextObject("{=SAA_sort_tier}Clan tier"),
                (a, b) => b.ClanTier.CompareTo(a.ClanTier), f => f.ClanTier.ToString()));

            SortSelection = new SelectorVM<SelectorItemVM>(_sortOptions.Select(o => o.Label), 0, OnSortChanged);
        }

        private void OnSortChanged(SelectorVM<SelectorItemVM> selector)
        {
            int index = selector?.SelectedIndex ?? 0;
            if (index < 0 || index >= _sortOptions.Count || index == _sortIndex)
            {
                return;
            }
            _sortIndex = index;
            ApplySort();
        }

        private void ApplySort()
        {
            if (_sortOptions.Count == 0)
            {
                return;
            }
            SortOption option = _sortOptions[Math.Min(_sortIndex, _sortOptions.Count - 1)];
            var ordered = Rows.OrderBy(r => r, Comparer<HeroRowVM>.Create((a, b) => option.Comparison(a.Facts, b.Facts))).ToList();

            Rows.Clear();
            foreach (HeroRowVM row in ordered)
            {
                row.SortValueText = option.Display(row.Facts);
                Rows.Add(row);
            }
        }

        // -- filtering -------------------------------------------------------

        private void BuildFilterGroups()
        {
            FilterGroups.Clear();
            if (_facts.Count == 0)
            {
                return;
            }

            var role = new SAAFilterGroupVM(new TextObject("{=SAA_filter_role}Role").ToString());
            role.Add(new TextObject("{=SAA_role_ruler}Rulers").ToString(), h => h.IsFactionLeader, ApplyFilters);
            role.Add(new TextObject("{=SAA_role_clan_leader}Clan leaders").ToString(), h => h.IsClanLeader && !h.IsFactionLeader, ApplyFilters);
            role.Add(new TextObject("{=SAA_role_noble}Nobles").ToString(), h => !h.IsClanLeader && !h.IsFactionLeader, ApplyFilters);
            AddGroup(role);

            var sex = new SAAFilterGroupVM(new TextObject("{=SAA_filter_sex}Sex").ToString());
            sex.Add(new TextObject("{=SAA_male}Male").ToString(), h => !h.IsFemale, ApplyFilters);
            sex.Add(new TextObject("{=SAA_female}Female").ToString(), h => h.IsFemale, ApplyFilters);
            AddGroup(sex);

            var age = new SAAFilterGroupVM(new TextObject("{=SAA_filter_age}Age").ToString());
            age.Add(new TextObject("{=SAA_age_young}Young (under 30)").ToString(), h => h.Age < 30f, ApplyFilters);
            age.Add(new TextObject("{=SAA_age_adult}Adult (30 to 50)").ToString(), h => h.Age >= 30f && h.Age < 50f, ApplyFilters);
            age.Add(new TextObject("{=SAA_age_veteran}Veteran (50 and over)").ToString(), h => h.Age >= 50f, ApplyFilters);
            AddGroup(age);

            var status = new SAAFilterGroupVM(new TextObject("{=SAA_filter_status}Status").ToString());
            status.Add(new TextObject("{=SAA_status_fief}Holds a fief").ToString(), h => h.FiefCount > 0, ApplyFilters);
            status.Add(new TextObject("{=SAA_status_landless}Landless").ToString(), h => h.FiefCount == 0, ApplyFilters);
            status.Add(new TextObject("{=SAA_status_married}Married").ToString(), h => h.IsMarried, ApplyFilters);
            status.Add(new TextObject("{=SAA_status_single}Unmarried").ToString(), h => !h.IsMarried, ApplyFilters);
            status.Add(new TextObject("{=SAA_status_party}Leads a party").ToString(), h => h.HasParty, ApplyFilters);
            AddGroup(status);

            var renown = new SAAFilterGroupVM(new TextObject("{=SAA_filter_renown}Renown").ToString());
            renown.Add(Threshold(500), h => h.Renown >= 500, ApplyFilters);
            renown.Add(Threshold(1000), h => h.Renown >= 1000, ApplyFilters);
            renown.Add(Threshold(2000), h => h.Renown >= 2000, ApplyFilters);
            AddGroup(renown);

            var skill = new SAAFilterGroupVM(new TextObject("{=SAA_filter_skill}Best skill").ToString());
            skill.Add(Threshold(150), h => h.TopSkill >= 150, ApplyFilters);
            skill.Add(Threshold(200), h => h.TopSkill >= 200, ApplyFilters);
            skill.Add(Threshold(250), h => h.TopSkill >= 250, ApplyFilters);
            AddGroup(skill);

            var cultures = _facts.Select(f => f.Culture).Where(c => c != null).Distinct()
                .OrderBy(c => c!.Name?.ToString() ?? string.Empty).ToList();
            if (cultures.Count > 1)
            {
                var group = new SAAFilterGroupVM(GameTexts.FindText("str_enc_sf_culture", null).ToString());
                foreach (CultureObject? culture in cultures)
                {
                    CultureObject captured = culture!;
                    group.Add(captured.Name?.ToString() ?? string.Empty, h => h.Culture == captured, ApplyFilters);
                }
                AddGroup(group);
            }

            var clans = _facts.Select(f => f.Clan).Where(c => c != null).Distinct()
                .OrderBy(c => c!.Name?.ToString() ?? string.Empty).ToList();
            if (clans.Count > 1)
            {
                var group = new SAAFilterGroupVM(GameTexts.FindText("str_clan", null).ToString());
                foreach (Clan? clan in clans)
                {
                    Clan captured = clan!;
                    group.Add(captured.Name?.ToString() ?? string.Empty, h => h.Clan == captured, ApplyFilters);
                }
                AddGroup(group);
            }
        }

        private static string Threshold(int value)
        {
            return new TextObject("{=SAA_threshold}{VALUE} and above").SetTextVariable("VALUE", value).ToString();
        }

        private void AddGroup(SAAFilterGroupVM group)
        {
            if (!group.IsEmpty)
            {
                FilterGroups.Add(group);
            }
        }

        public void ApplyFilters()
        {
            string needle = SearchText.Trim().ToLowerInvariant();
            int visible = 0;

            foreach (HeroRowVM row in Rows)
            {
                bool matches = needle.Length == 0 || row.Facts.SearchName.Contains(needle);
                if (matches)
                {
                    foreach (SAAFilterGroupVM group in FilterGroups)
                    {
                        if (!group.Matches(row.Facts))
                        {
                            matches = false;
                            break;
                        }
                    }
                }
                row.IsFiltered = !matches;
                if (matches)
                {
                    visible++;
                }
            }

            IsEmpty = visible == 0;
            CanResetFilters = needle.Length > 0 || FilterGroups.Any(g => g.HasAnySelection);
            ResultCountText = new TextObject("{=SAA_result_count}{SHOWN} of {TOTAL} characters")
                .SetTextVariable("SHOWN", visible)
                .SetTextVariable("TOTAL", Rows.Count)
                .ToString();

            EnsureValidSelection();
        }

        public void ExecuteResetFilters()
        {
            foreach (SAAFilterGroupVM group in FilterGroups)
            {
                group.ClearSelection();
            }
            SearchText = string.Empty;
            ApplyFilters();
        }

        // -- selection -------------------------------------------------------

        private void EnsureValidSelection()
        {
            if (_selectedRow != null && !_selectedRow.IsFiltered)
            {
                return;
            }
            HeroRowVM? first = Rows.FirstOrDefault(r => !r.IsFiltered);
            if (first != null)
            {
                OnRowSelected(first);
            }
            else
            {
                SetSelection(null);
            }
        }

        private void OnRowSelected(HeroRowVM row) => SetSelection(row);

        private void SetSelection(HeroRowVM? row)
        {
            foreach (HeroRowVM other in Rows)
            {
                other.IsSelected = ReferenceEquals(other, row);
            }
            _selectedRow = row;
            Detail.SetHero(row?.Hero);
            _onSelectionChanged();
        }

        /// <summary>Clicking a family member or rival jumps to them when they are in the list.</summary>
        private void NavigateTo(Hero hero)
        {
            HeroRowVM? target = Rows.FirstOrDefault(r => r.Hero == hero);
            if (target == null)
            {
                return;
            }
            if (target.IsFiltered)
            {
                ExecuteResetFilters();
            }
            OnRowSelected(target);
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            Detail.OnFinalize();
        }

        // -- bindings --------------------------------------------------------

        [DataSourceProperty] public MBBindingList<HeroRowVM> Rows { get; }
        [DataSourceProperty] public MBBindingList<SAAFilterGroupVM> FilterGroups { get; }

        [DataSourceProperty] public HeroDetailVM Detail => _detail;

        [DataSourceProperty]
        public SelectorVM<SelectorItemVM>? SortSelection
        {
            get => _sortSelection;
            set => SetRef(ref _sortSelection, value);
        }

        [DataSourceProperty]
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetString(ref _searchText, value))
                {
                    IsSearchEmpty = string.IsNullOrEmpty(_searchText);
                    ApplyFilters();
                }
            }
        }

        [DataSourceProperty]
        public string SearchPlaceholderText
        {
            get => _searchPlaceholderText;
            set => SetString(ref _searchPlaceholderText, value);
        }

        [DataSourceProperty]
        public string ResultCountText
        {
            get => _resultCountText;
            set => SetString(ref _resultCountText, value);
        }

        [DataSourceProperty]
        public string EmptyText
        {
            get => _emptyText;
            set => SetString(ref _emptyText, value);
        }

        [DataSourceProperty]
        public string ResetFiltersText
        {
            get => _resetFiltersText;
            set => SetString(ref _resetFiltersText, value);
        }

        [DataSourceProperty]
        public string NameColumnText
        {
            get => _nameColumnText;
            set => SetString(ref _nameColumnText, value);
        }

        [DataSourceProperty]
        public string SortByLabel
        {
            get => _sortByLabel;
            set => SetString(ref _sortByLabel, value);
        }

        [DataSourceProperty]
        public bool IsEmpty
        {
            get => _isEmpty;
            set => SetBool(ref _isEmpty, value);
        }

        [DataSourceProperty]
        public bool IsSearchEmpty
        {
            get => _isSearchEmpty;
            set => SetBool(ref _isSearchEmpty, value);
        }

        [DataSourceProperty]
        public bool CanResetFilters
        {
            get => _canResetFilters;
            set => SetBool(ref _canResetFilters, value);
        }
    }
}
