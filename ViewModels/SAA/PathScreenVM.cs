using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace StartAsAnyone
{
    /// <summary>One of the two option cards on the opening screen.</summary>
    public class PathCardVM : SAAViewModel
    {
        private readonly Action _onSelect;

        private HintViewModel? _disabledHint;
        private string _titleText = string.Empty;
        private string _descriptionText = string.Empty;
        private string _iconSprite = string.Empty;
        private bool _isSelected;
        private bool _isDisabled;

        public PathCardVM(string title, string description, string iconSprite, Action onSelect)
        {
            _onSelect = onSelect;
            TitleText = title;
            DescriptionText = description;
            IconSprite = iconSprite;
            DisabledHint = new HintViewModel(TextObject.GetEmpty(), null);
        }

        public void ExecuteSelect()
        {
            if (!IsDisabled)
            {
                _onSelect();
            }
        }

        public void Disable(TextObject reason)
        {
            IsDisabled = true;
            DisabledHint = new HintViewModel(reason, null);
        }

        [DataSourceProperty]
        public HintViewModel? DisabledHint
        {
            get => _disabledHint;
            set => SetRef(ref _disabledHint, value);
        }

        [DataSourceProperty]
        public string TitleText
        {
            get => _titleText;
            set => SetString(ref _titleText, value);
        }

        [DataSourceProperty]
        public string DescriptionText
        {
            get => _descriptionText;
            set => SetString(ref _descriptionText, value);
        }

        [DataSourceProperty]
        public string IconSprite
        {
            get => _iconSprite;
            set => SetString(ref _iconSprite, value);
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetBool(ref _isSelected, value);
        }

        [DataSourceProperty]
        public bool IsDisabled
        {
            get => _isDisabled;
            set => SetBool(ref _isDisabled, value);
        }
    }

    /// <summary>The opening screen: play an existing lord, or create a character as usual.</summary>
    public class PathScreenVM : SAAViewModel
    {
        private readonly Action _onChanged;

        private PathCardVM? _existingHeroCard;
        private PathCardVM? _newCharacterCard;
        private bool _startAsAnyone;

        public PathScreenVM(bool isStoryMode, Action onChanged)
        {
            _onChanged = onChanged;

            ExistingHeroCard = new PathCardVM(
                new TextObject("{=AHA80TyDA}Start as an existing Hero").ToString(),
                new TextObject("{=SAA_card_existing}Take over any living lord or lady in Calradia. Their clan, fiefs, army and relationships become yours.").ToString(),
                "SPGeneral\\SPScoreboard\\leader_crown_icon",
                () => SetStartAsAnyone(true));

            NewCharacterCard = new PathCardVM(
                new TextObject("{=CJFeItmnG}Create New Character").ToString(),
                new TextObject("{=SAA_card_new}The standard beginning. Build your own backstory and rise from nothing.").ToString(),
                "Encyclopedia\\hero_silhouette",
                () => SetStartAsAnyone(false));

            if (isStoryMode)
            {
                ExistingHeroCard.Disable(new TextObject("{=IX1nIsFTe}This option is disabled for story mode, play SandBox mode"));
            }

            // Playing an existing lord is what the mod is for; default to it.
            SetStartAsAnyone(!isStoryMode);
        }

        public bool StartAsAnyone => _startAsAnyone;

        private void SetStartAsAnyone(bool value)
        {
            _startAsAnyone = value;
            if (ExistingHeroCard != null)
            {
                ExistingHeroCard.IsSelected = value;
            }
            if (NewCharacterCard != null)
            {
                NewCharacterCard.IsSelected = !value;
            }
            _onChanged();
        }

        [DataSourceProperty]
        public PathCardVM? ExistingHeroCard
        {
            get => _existingHeroCard;
            set => SetRef(ref _existingHeroCard, value);
        }

        [DataSourceProperty]
        public PathCardVM? NewCharacterCard
        {
            get => _newCharacterCard;
            set => SetRef(ref _newCharacterCard, value);
        }
    }
}
