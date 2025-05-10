using SandBox.View.CharacterCreation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.ViewModelCollection;
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
        private MBBindingList<CharacterCreationHeroVM> _heroes;
        private CharacterCreationKingdomVM _currentSelectedKingdom;
        private bool _canGoBackToPreviousSelection;
        private int _selectionStageIndex;
        private int _furthestSelectionStageIndex;
        
        private KingdomInfoPageVM _kingdomInfo;
        private bool _isKingdomStageActive;
        private bool _isHeroStageActive;
        private CharacterCreationHeroVM _currentSelectedHero;
        private HeroInfoPageVM _heroInfo;

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
            this.IsKingdomStage = false;
            this.IsHeroStage = true;
            this.Kingdoms = new MBBindingList<CharacterCreationKingdomVM>();
            this.Heroes = new MBBindingList<CharacterCreationHeroVM>();
            
            this._onStartAsAnyoneSelected = onStartAsAnyoneSelected; //implemet logic for this
            base.Title = new TextObject("{=start_as_anyone_title}Choose Your Path", null).ToString();
            base.Description = new TextObject("{=start_as_anyone_description}Would you like to start as a random character or create a new one?", null).ToString();
            base.SelectionText = new TextObject("{=start_as_anyone_selection}Character Creation Path", null).ToString();
            _canAdvanceToNextSelection = true;
            _canGoBackToPreviousSelection = false;
            _selectionStageIndex = 0;
            _furthestSelectionStageIndex = 0+1;
            
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
            this.KingdomInfo = new KingdomInfoPageVM(CurrentSelectedKingdom.Kingdom);

            
            


        }
        public void OnHeroSelection(CharacterCreationHeroVM selectedHero)
        {
            // Clear previous selections
            foreach (CharacterCreationHeroVM heroVM in from h in this.Heroes
                                                       where h.IsSelected
                                                       select h)
            {
                heroVM.IsSelected = false;
            }

            // Set new selection
            selectedHero.IsSelected = true;
            this.CurrentSelectedHero = selectedHero;
            this.AnyItemSelected = true;

            // Update game state

            this.HeroInfo = new HeroInfoPageVM(selectedHero.Hero);
            //InfoPage implementation goes here

            // Notify listeners
            Action<Hero> onHeroSelected = this._onHeroSelected;
            if (onHeroSelected == null)
            {
                return;
            }
            onHeroSelected(selectedHero.Hero);
        }

        private void _onHeroSelected(Hero hero)
        {
            string message = String.Format("I see...{0}, a Gourmet choice", hero.Name.ToString());
            InformationManager.DisplayMessage(new InformationMessage(message));
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
            this.KingdomInfo = new KingdomInfoPageVM(selectedKingdom.Kingdom);
            this.CurrentSelectedHero = new CharacterCreationHeroVM(selectedKingdom.Kingdom.Leader, OnHeroSelection);
            this.HeroInfo = new HeroInfoPageVM(selectedKingdom.Kingdom.Leader);

            // Notify listeners
            Action<Kingdom> onKingdomSelected = this._onKingdomSelected;
            if (onKingdomSelected == null)
            {
                return;
            }
            onKingdomSelected(selectedKingdom.Kingdom);
        }

        private void _onKingdomSelected(Kingdom kingdom)
        {
            this.Heroes.Clear();

            
            List<Hero> aliveHeroes = kingdom.Heroes
                .Where(hero => hero.IsAlive &&
                              hero.HeroState == Hero.CharacterStates.Active &&
                              hero.IsLord)
                .GroupBy(hero => hero.Id)
                .Select(group => group.First())
                .ToList();
            List<Hero> sortedHeroes = aliveHeroes
                .OrderByDescending(hero => hero.Power)
                .ToList();
            foreach (Hero hero in sortedHeroes)
            {
                this.Heroes.Add(new CharacterCreationHeroVM(hero,OnHeroSelection));
            }
        }

        public void ExecuteNextSelectionStage()
        {
            // Logic for handling the "Next" button click
            // You can put your implementation here
            
            CanGoBackToPreviousSelection = true;
            _selectionStageIndex++;
            if (_selectionStageIndex == _furthestSelectionStageIndex)
            {
                CanAdvanceToNextSelection = false;
            }
            if(_selectionStageIndex == 1)
            {
                IsHeroStage = true;
                IsKingdomStage = false;
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
            if (_selectionStageIndex <1) { 
                CanGoBackToPreviousSelection = false; 
                IsHeroStage = false;
                IsKingdomStage = true;
            }
            
            
        }
        public void ExecuteMe()
        {
            GameState gm = GameStateManager.Current.ActiveState;
            CharacterCreationState characterCreationState = (gm.GetType().Equals(typeof(CharacterCreationState)))? (CharacterCreationState)gm:null;

            if(CurrentSelectedHero != null)
            {
                SAASubModule.heroToBeSet = CurrentSelectedHero.Hero;
            } else if(CurrentSelectedKingdom != null)
            {
                SAASubModule.heroToBeSet = CurrentSelectedKingdom.Kingdom.Leader;
            }
            


            
            
            
            
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
        public KingdomInfoPageVM KingdomInfo
        {
            get
            {
                return this._kingdomInfo;
            }
            set
            {
                if (value != this._kingdomInfo)
                {
                    this._kingdomInfo = value;
                    base.OnPropertyChangedWithValue<KingdomInfoPageVM>(value, "KingdomInfo");
                }
            }
        }
        [DataSourceProperty]
        public HeroInfoPageVM HeroInfo
        {
            get
            {
                return this._heroInfo;
            }
            set
            {
                if (value != this._heroInfo)
                {
                    this._heroInfo = value;
                    base.OnPropertyChangedWithValue<HeroInfoPageVM>(value, "HeroInfo");
                }
            }
        }
        [DataSourceProperty]
        public MBBindingList<CharacterCreationHeroVM> Heroes
        {
            get
            {
                return this._heroes;
            }
            set
            {
                if (value != this._heroes)
                {
                    this._heroes = value;
                    base.OnPropertyChangedWithValue<MBBindingList<CharacterCreationHeroVM>>(value, "Heroes");
                }
            }
        }
        [DataSourceProperty]
        public CharacterCreationHeroVM CurrentSelectedHero
        {
            get
            {
                return this._currentSelectedHero;
            }
            set
            {
                if (value != this._currentSelectedHero)
                {
                    this._currentSelectedHero = value;
                    base.OnPropertyChangedWithValue<CharacterCreationHeroVM>(value, "CurrentSelectedHero");

                }
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

        [DataSourceProperty]
        public bool IsKingdomStage
        {
            get
            {
                return this._selectionStageIndex == 0;
            }
            set
            {
                if (value != this._isKingdomStageActive)
                {
                    this._isKingdomStageActive = value;
                    base.OnPropertyChangedWithValue(value,nameof(IsKingdomStage));
                }
            }
        }
        [DataSourceProperty]
        public bool IsHeroStage
        {
            get
            {
                return this._selectionStageIndex == 1;
            }
            set
            {
                if (value != this._isHeroStageActive)
                {
                    this._isHeroStageActive = value;
                    base.OnPropertyChangedWithValue(value, nameof(IsHeroStage));
                    
                }
            }
        }
        
    }
} 