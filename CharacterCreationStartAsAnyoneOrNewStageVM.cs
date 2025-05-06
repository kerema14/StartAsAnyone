using SandBox.View.CharacterCreation;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace StartAsAnyone

{
    public class CharacterCreationStartAsAnyoneOrNewStageVM : CharacterCreationStageBaseVM
    {
        private Action<bool> _onStartAsAnyoneSelected;
        private InputKeyItemVM _cancelInputKey;
        private InputKeyItemVM _doneInputKey;
        
        private bool _isActive;
        private bool _startAsAnyone;
        
        private MBBindingList<KingdomItemVM> _kingdoms;

        public CharacterCreationStartAsAnyoneOrNewStageVM(
            CharacterCreation characterCreation,
            Action affirmativeAction,
            TextObject affirmativeActionText,
            Action negativeAction,
            TextObject negativeActionText,
            int currentStageIndex,
            int totalStagesCount,
            int furthestIndex,
            Action<int> goToIndex,
            Action<bool> onStartAsAnyoneSelected) : base(
                characterCreation,
                affirmativeAction,
                affirmativeActionText,
                negativeAction,
                negativeActionText,
                currentStageIndex,
                totalStagesCount,
                furthestIndex,
                goToIndex)
        {
            
            
            this._onStartAsAnyoneSelected = onStartAsAnyoneSelected; //implemet logic for this
            base.Title = new TextObject("{=start_as_anyone_title}Choose Your Path", null).ToString();
            base.Description = new TextObject("{=start_as_anyone_description}Would you like to start as a random character or create a new one?", null).ToString();
            base.SelectionText = new TextObject("{=start_as_anyone_selection}Character Creation Path", null).ToString();
        }

        public void ExecuteMe()
        {
            GameState gm = GameStateManager.Current.ActiveState;
            CharacterCreationState characterCreationState = (gm.GetType().Equals(typeof(CharacterCreationState)))? (CharacterCreationState)gm:null;
            List<Hero> kingdomLeaders = new List<Hero>();
            foreach (Hero hero1 in Hero.AllAliveHeroes)
            {
                if (hero1.IsKingdomLeader)
                {
                    kingdomLeaders.Add(hero1);
                }
            }
            SubModule.heroToBeSet = kingdomLeaders.GetRandomElement();
            InformationManager.DisplayMessage(new InformationMessage("Porno"));
            characterCreationState.FinalizeCharacterCreation();
            InformationManager.DisplayMessage(new InformationMessage("Finalized Character Creation"));


            //this worked!
        }
        public override void OnNextStage()
        {
            
            this._affirmativeAction(); //1st
        }

        public override void OnPreviousStage()
        {
            this._negativeAction();
        }

        public override bool CanAdvanceToNextStage()
        {
            return base.AnyItemSelected;
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            InputKeyItemVM cancelInputKey = this.CancelInputKey;
            if (cancelInputKey != null)
            {
                cancelInputKey.OnFinalize();
            }
            InputKeyItemVM doneInputKey = this.DoneInputKey;
            if (doneInputKey != null)
            {
                doneInputKey.OnFinalize();
            }
        }

        public void SetCancelInputKey(HotKey hotKey)
        {
            this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
        }

        public void SetDoneInputKey(HotKey hotKey)
        {
            this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
        }

        [DataSourceProperty]
        public InputKeyItemVM CancelInputKey
        {
            get
            {
                return this._cancelInputKey;
            }
            set
            {
                if (value != this._cancelInputKey)
                {
                    this._cancelInputKey = value;
                    base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "CancelInputKey");
                }
            }
        }

        [DataSourceProperty]
        public InputKeyItemVM DoneInputKey
        {
            get
            {
                return this._doneInputKey;
            }
            set
            {
                if (value != this._doneInputKey)
                {
                    this._doneInputKey = value;
                    base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "DoneInputKey");
                }
            }
        }

        [DataSourceProperty]
        public bool IsActive
        {
            get
            {
                return this._isActive;
            }
            set
            {
                if (value != this._isActive)
                {
                    this._isActive = value;
                    base.OnPropertyChangedWithValue(value, "IsActive");
                }
            }
        }

        // The single source-of-truth property:
        [DataSourceProperty]
        public bool StartAsAnyone
        {
            get => _startAsAnyone;
            set
            {
                if (value == _startAsAnyone) return;       // no-op if unchanged
                _startAsAnyone = value;
                // tell the UI that both this and its inverse changed:
                OnPropertyChanged(nameof(StartAsAnyone));
                OnPropertyChanged(nameof(NotStartAsAnyone));
                // if you have a delegate to fire, do it here too:
                
                // if you need to enable “Advance”:
                AnyItemSelected = true;
                OnPropertyChanged(nameof(CanAdvance));
                
            }
        }

        // Expose the opposite option as read-only:
        [DataSourceProperty]
        public bool NotStartAsAnyone
        {
            get => !_startAsAnyone;
            set
            {
                if (value ==  !_startAsAnyone) return;
                _startAsAnyone = !value;

                OnPropertyChanged(nameof(NotStartAsAnyone));
                OnPropertyChanged(nameof(StartAsAnyone));

                AnyItemSelected = true;
                OnPropertyChanged(nameof(CanAdvance));
                
            }
        }
        
    }
} 