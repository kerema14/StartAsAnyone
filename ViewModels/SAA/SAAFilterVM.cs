using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace StartAsAnyone
{
    /// <summary>
    /// One checkbox in the filter rail. Matches the binding contract of the vanilla
    /// <c>EncyclopediaFilterListItem</c> prefab: Name, IsSelected, ExecuteOnFilterActivated.
    /// </summary>
    public class SAAFilterItemVM : SAAViewModel
    {
        private readonly Action _onChanged;
        private readonly Func<HeroFacts, bool> _predicate;

        private string _name = string.Empty;
        private bool _isSelected;

        public SAAFilterItemVM(string name, Func<HeroFacts, bool> predicate, Action onChanged)
        {
            _predicate = predicate;
            _onChanged = onChanged;
            Name = name;
        }

        public bool Matches(HeroFacts facts)
        {
            try
            {
                return _predicate(facts);
            }
            catch (Exception)
            {
                // A filter must never take the screen down over one malformed hero.
                return false;
            }
        }

        public void ExecuteOnFilterActivated()
        {
            // The widget is a Toggle: it has already flipped IsSelected through the
            // two-way binding by the time this runs. Flipping it again here cancelled
            // out every click, which is why no filter appeared to do anything.
            _onChanged();
        }

        public void Clear() => IsSelected = false;

        [DataSourceProperty]
        public string Name
        {
            get => _name;
            set => SetString(ref _name, value);
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetBool(ref _isSelected, value);
        }
    }

    /// <summary>
    /// A collapsible group of filters. Matches the vanilla <c>EncyclopediaFilterGroup</c>
    /// prefab: FilterName plus a Filters list.
    ///
    /// Checked items inside a group are OR-ed; groups are AND-ed together. A group with
    /// nothing checked does not filter at all, which is the encyclopedia convention.
    /// </summary>
    public class SAAFilterGroupVM : SAAViewModel
    {
        private string _filterName = string.Empty;
        private MBBindingList<SAAFilterItemVM> _filters = new MBBindingList<SAAFilterItemVM>();

        public SAAFilterGroupVM(string name)
        {
            FilterName = name;
        }

        public void Add(string name, Func<HeroFacts, bool> predicate, Action onChanged)
        {
            Filters.Add(new SAAFilterItemVM(name, predicate, onChanged));
        }

        public bool HasAnySelection => Filters.Any(f => f.IsSelected);

        public bool IsEmpty => Filters.Count == 0;

        public bool Matches(HeroFacts facts)
        {
            var active = Filters.Where(f => f.IsSelected).ToList();
            return active.Count == 0 || active.Any(f => f.Matches(facts));
        }

        public void ClearSelection()
        {
            foreach (SAAFilterItemVM filter in Filters)
            {
                filter.Clear();
            }
        }

        [DataSourceProperty]
        public string FilterName
        {
            get => _filterName;
            set => SetString(ref _filterName, value);
        }

        [DataSourceProperty]
        public MBBindingList<SAAFilterItemVM> Filters
        {
            get => _filters;
            set
            {
                if (!ReferenceEquals(_filters, value))
                {
                    _filters = value;
                    OnPropertyChangedWithValue<MBBindingList<SAAFilterItemVM>>(value, nameof(Filters));
                }
            }
        }
    }
}
