using SandBox.View.CharacterCreation;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.Core;
using TaleWorlds.GauntletUI.BaseTypes;
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
        private bool _canAdvanceToNextSelection;
        private bool _isActive;
        private bool _startAsAnyone;

        private MBBindingList<CharacterCreationKingdomVM> _kingdoms;
        private CharacterCreationKingdomVM _currentSelectedKingdom;
        private bool _canGoBackToPreviousSelection;
        private int _selectionStageIndex;
        private int _furthestSelectionStageIndex;
        private Action<Kingdom> _onKingdomSelected;

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

            this.Kingdoms = new MBBindingList<CharacterCreationKingdomVM>();
            this._onStartAsAnyoneSelected = onStartAsAnyoneSelected; //implemet logic for this
            base.Title = new TextObject("{=start_as_anyone_title}Choose Your Path", null).ToString();
            base.Description = new TextObject("{=start_as_anyone_description}Would you like to start as a random character or create a new one?", null).ToString();
            base.SelectionText = new TextObject("{=start_as_anyone_selection}Character Creation Path", null).ToString();
            _canAdvanceToNextSelection = true;
            _canGoBackToPreviousSelection = false;
            _selectionStageIndex = 0;
            _furthestSelectionStageIndex = 0+1 + 1;
            
            foreach (Kingdom kingdom in Kingdom.All)
            {
                CharacterCreationKingdomVM item = new CharacterCreationKingdomVM(kingdom, OnKingdomSelection);
                this.Kingdoms.Add(item);
            }
            
            CharacterCreationKingdomVM characterCreationKingdomVM = this.Kingdoms.First();
            if (characterCreationKingdomVM != null)
            {
                this.OnKingdomSelection(characterCreationKingdomVM);
            }

            
        }
        public void OnKingdomSelection(CharacterCreationKingdomVM selectedKingdom)
        {
            // Clear previous selections
            foreach (CharacterCreationKingdomVM kingdomVM in from k in this.Kingdoms
                                                             where k.IsSelected
                                                             select k)
            {
                kingdomVM.IsSelected = false;
            }

            // Set new selection
            selectedKingdom.IsSelected = true;
            this.CurrentSelectedKingdom = selectedKingdom;
            base.AnyItemSelected = true;

            // Update game state

            this.CanAdvanceToNextSelection = true;

            // Notify listeners
            Action<Kingdom> onKingdomSelected = this._onKingdomSelected;
            if (onKingdomSelected == null)
            {
                return;
            }
            onKingdomSelected(selectedKingdom.Kingdom);
        }
        public void ExecuteNextSelectionStage()
        {
            // Logic for handling the "Next" button click
            // You can put your implementation here
            InformationManager.DisplayMessage(new InformationMessage("Wow, congrats, you've clicked a button..."));
            CanGoBackToPreviousSelection = true;
            _selectionStageIndex++;
            if (_selectionStageIndex == _furthestSelectionStageIndex)
            {
                CanAdvanceToNextSelection = false;
            }
        }
        public void ExecutePreviousSelectionStage()
        {
            // Logic for handling the "Previous" button click
            // You can put your implementation here
            
            
            _selectionStageIndex--;
            if ((_selectionStageIndex + 1) == _furthestSelectionStageIndex)
            {
                CanAdvanceToNextSelection=true;
            }
            if (_selectionStageIndex <1) { CanGoBackToPreviousSelection = false; }

            InformationManager.DisplayMessage(new InformationMessage("Wow, congrats, you've clicked a button... but back"));
        }
        public void ExecuteMe()
        {
            GameState gm = GameStateManager.Current.ActiveState;
            CharacterCreationState characterCreationState = (gm.GetType().Equals(typeof(CharacterCreationState)))? (CharacterCreationState)gm:null;

            Hero hero = CurrentSelectedKingdom.Kingdom.Leader;
            
            
            SAASubModule.heroToBeSet = hero;
            
            
            
            characterCreationState.FinalizeCharacterCreation();
            


            //this worked!
        }
        
        
        public override void OnNextStage()
        {
            if (StartAsAnyone)
            {
                SAASubModule.startAsAnyone = true;
                ExecuteMe();
                
            }
            else
            {
                this._affirmativeAction(); //1st
            }
            
            
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
        public bool CanAdvanceToNextSelection
        {
            get { return _canAdvanceToNextSelection; }
            set
            {
                if (_canAdvanceToNextSelection != value)
                {
                    _canAdvanceToNextSelection = value;
                    OnPropertyChanged(nameof(CanAdvanceToNextSelection));
                }
            }
        }
        [DataSourceProperty]
        public bool CanGoBackToPreviousSelection
        {
            get { return _canGoBackToPreviousSelection; }
            set
            {
                if(_canGoBackToPreviousSelection != value)
                {
                    _canGoBackToPreviousSelection = value;
                    OnPropertyChanged(nameof(CanGoBackToPreviousSelection));
                }
            }
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

        [DataSourceProperty]
        public MBBindingList<CharacterCreationKingdomVM> Kingdoms
        {
            get
            {
                return this._kingdoms;
            }
            set
            {
                if (value != this._kingdoms)
                {
                    this._kingdoms = value;
                    base.OnPropertyChangedWithValue<MBBindingList<CharacterCreationKingdomVM>>(value, "Kingdoms");
                }
            }
        }
        [DataSourceProperty]
        public CharacterCreationKingdomVM CurrentSelectedKingdom
        {
            get
            {
                return this._currentSelectedKingdom;
            }
            set
            {
                if (value != this._currentSelectedKingdom)
                {
                    this._currentSelectedKingdom = value;
                    base.OnPropertyChangedWithValue<CharacterCreationKingdomVM>(value, "CurrentSelectedKingdom");
                }
            }
        }

    }
} 