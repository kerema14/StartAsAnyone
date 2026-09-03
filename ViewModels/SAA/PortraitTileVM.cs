using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace StartAsAnyone
{
    /// <summary>Small portrait square used for leaders, family, friends and rivals.</summary>
    public class PortraitTileVM : SAAViewModel
    {
        private readonly Action<PortraitTileVM>? _onSelect;

        private CharacterImageIdentifierVM? _portrait;
        private HintViewModel? _hint;
        private string _nameText = string.Empty;
        private string _subText = string.Empty;
        private bool _isLeader;
        private bool _isChild;
        private bool _isSelected;

        public Hero? Hero { get; }

        public PortraitTileVM(Hero? hero, string subText = "", Action<PortraitTileVM>? onSelect = null, HintViewModel? hint = null)
        {
            Hero = hero;
            _onSelect = onSelect;
            SubText = subText ?? string.Empty;

            if (hero?.CharacterObject != null)
            {
                Portrait = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(hero.CharacterObject));
                NameText = hero.Name?.ToString() ?? string.Empty;
                IsLeader = hero.IsFactionLeader || hero.IsClanLeader;
                IsChild = FaceGen.GetMaturityTypeWithAge(hero.Age) <= BodyMeshMaturityType.Child;
                Hint = hint ?? new HintViewModel(new TextObject(NameText), null);
            }
            else
            {
                Portrait = new CharacterImageIdentifierVM(CharacterCode.CreateEmpty());
                Hint = hint ?? new HintViewModel(TextObject.GetEmpty(), null);
            }
        }

        public void ExecuteSelect() => _onSelect?.Invoke(this);

        public void ExecuteBeginHint()
        {
            if (Hero != null && Campaign.Current != null)
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
        public string SubText
        {
            get => _subText;
            set => SetString(ref _subText, value);
        }

        [DataSourceProperty]
        public bool IsLeader
        {
            get => _isLeader;
            set => SetBool(ref _isLeader, value);
        }

        [DataSourceProperty]
        public bool IsChild
        {
            get => _isChild;
            set => SetBool(ref _isChild, value);
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetBool(ref _isSelected, value);
        }
    }
}
