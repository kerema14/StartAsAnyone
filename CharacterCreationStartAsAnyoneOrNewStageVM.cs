using Helpers;
using StoryMode.CharacterCreationContent;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
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
        private string _selectionStageText;
        private bool _isStoryMode;
        private bool _isNonKingdomFactionStage;
        private bool _NonKingdomFactionSelectionStage;
        private HintViewModel _storyModeDisabledHint;
        private MBBindingList<CharacterCreationKingdomVM> _nonKingdomFactions;
        private MBBindingList<CharacterCreationKingdomVM> _savedKingdoms;

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
            this.NonKingdomFactions = new MBBindingList<CharacterCreationKingdomVM>();
            this.Heroes = new MBBindingList<CharacterCreationHeroVM>();
            SAASubModule.startAsAnyone = false;
            this._onStartAsAnyoneSelected = onStartAsAnyoneSelected; 
            base.Title = new TextObject("{=start_as_anyone_title}Choose Your Path", null).ToString();
            base.Description = new TextObject("{=start_as_anyone_description}Would you like to start as an existing lord/lady or create a new one?", null).ToString();
            base.SelectionText = new TextObject("{=start_as_anyone_selection}Character Creation Path", null).ToString();
            _canAdvanceToNextSelection = true;
            _canGoBackToPreviousSelection = false;
            _selectionStageIndex = 0;
            _furthestSelectionStageIndex = 0+1;
            //yellow model chick, yellow bottle sippin'
            
            foreach (Kingdom kingdom in Kingdom.All)
            {
                CharacterCreationKingdomVM item = new CharacterCreationKingdomVM(kingdom, OnKingdomSelection);
                this.Kingdoms.Add(item);
            }
            var clansWithoutKingdom = Clan.All.Where(clan => clan.Kingdom == null && clan.Leader != null && clan.Leader != Hero.MainHero);
            foreach (Clan clan in clansWithoutKingdom)
            {
                CharacterCreationKingdomVM item = new CharacterCreationKingdomVM(clan,OnKingdomSelection);
                this.NonKingdomFactions.Add(item);
            }

            this.Kingdoms.Add(new CharacterCreationKingdomVM(OnKingdomSelection));
            this._savedKingdoms = Kingdoms;

            CharacterCreationKingdomVM characterCreationKingdomVM = this.Kingdoms.First();
            if (characterCreationKingdomVM != null)
            {
                this.OnKingdomSelection(characterCreationKingdomVM);
            }
            this.KingdomInfo = new KingdomInfoPageVM(CurrentSelectedKingdom.Kingdom);
            this.SelectionStageText = "Select a kingdom to choose your character from.";

            isStoryMode = getCharacterCreationState().CurrentCharacterCreationContent is StoryModeCharacterCreationContent;
            
            TextObject empty = TextObject.Empty;
            TextObject storyModeDisabledText = new TextObject("This option is disabled for story mode, play SandBox mode");
            this.StoryModeDisabledHint = new HintViewModel((isStoryMode)?storyModeDisabledText:empty, null);

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
            OnPropertyChangedWithValue(true, nameof(CanAdvance));
        }

        public void OnKingdomSelection(CharacterCreationKingdomVM selectedKingdom)
        {
            foreach (CharacterCreationKingdomVM kingdomVM in from k in this.Kingdoms
                                                             where k.IsSelected
                                                             select k)
            {
                kingdomVM.IsSelected = false;
            }


            selectedKingdom.IsSelected = true;
            this.CurrentSelectedKingdom = selectedKingdom;
            this.CanAdvanceToNextSelection = true;
            if (this.CurrentSelectedKingdom.Kingdom == null && this.CurrentSelectedKingdom.Clan == null)
            {
                this._isNonKingdomFactionStage = true;
                OnNonKingdomFactionSelection();
                return; 
            }
            else if(this.CurrentSelectedKingdom.Clan != null)
            {

                this.KingdomInfo = new KingdomInfoPageVM(this.CurrentSelectedKingdom.Clan);
                this.CurrentSelectedHero = new CharacterCreationHeroVM(this.CurrentSelectedKingdom.Clan.Leader, OnHeroSelection);
                this.HeroInfo = new HeroInfoPageVM(CurrentSelectedHero.Hero);
                Action<Clan> onClanSelected = this._onClanSelected;
                if (onClanSelected == null)
                {
                    return;
                }
                onClanSelected(selectedKingdom.Clan);
                return;
            }
            else
            {
                this._isNonKingdomFactionStage = false;
            }
            
            // Clear previous selections
            

            // Set new selection
            
            base.AnyItemSelected = true;

            // Update game state

            
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

        public void OnNonKingdomFactionSelection()
        {
            foreach (CharacterCreationKingdomVM kingdomVM in from k in this.NonKingdomFactions
                                                             where k.IsSelected
                                                             select k)
            {
                kingdomVM.IsSelected = false;
            }

            this.NonKingdomFactions.First().IsSelected = true;
            
            this.KingdomInfo = new KingdomInfoPageVM(NonKingdomFactions);
            
        }
        private void _onClanSelected(Clan clan)
        {
            this.Heroes.Clear();


            List<Hero> aliveHeroes = clan.Heroes
                .Where(hero => hero.IsAlive &&
                              hero.HeroState == Hero.CharacterStates.Active &&
                              hero.IsLord)
                .GroupBy(hero => hero.Id)
                .Select(group => group.First())
                .ToList();
            
            foreach (Hero hero in aliveHeroes)
            {
                this.Heroes.Add(new CharacterCreationHeroVM(hero, OnHeroSelection));
            }
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
            if (this._isNonKingdomFactionStage && _selectionStageIndex == 0)
            {
                this.Kingdoms = NonKingdomFactions;
                this.KingdomInfo = new KingdomInfoPageVM(NonKingdomFactions.First().Clan);
                this.CurrentSelectedKingdom = this.Kingdoms.FirstOrDefault();
                
                this.CurrentSelectedHero = new CharacterCreationHeroVM(this.CurrentSelectedKingdom.Clan.Leader, OnHeroSelection);
                this.HeroInfo = new HeroInfoPageVM(CurrentSelectedHero.Hero);
                Action<Clan> onClanSelected = this._onClanSelected;
                if (onClanSelected == null)
                {
                    return;
                }
                onClanSelected(CurrentSelectedHero.Hero.Clan);

                this._isNonKingdomFactionStage = false;
                this._NonKingdomFactionSelectionStage = true;
                CanGoBackToPreviousSelection = true;
                return;
            }
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
            this.SelectionStageText = "Choose a character to play as.";
        }
        public void ExecutePreviousSelectionStage()
        {
            // Logic for handling the "Previous" button click
            // You can put your implementation here
            if(this._NonKingdomFactionSelectionStage && _selectionStageIndex == 0)
            {
                this.Kingdoms = this._savedKingdoms;
                OnKingdomSelection(Kingdoms.First());
                this._NonKingdomFactionSelectionStage = false;
                CanGoBackToPreviousSelection = false;
                return;
            }
            
            
            _selectionStageIndex--;
            if ((_selectionStageIndex + 1) == _furthestSelectionStageIndex)
            {
                CanAdvanceToNextSelection=true;
            }
            if (_selectionStageIndex <1) { 
                CanGoBackToPreviousSelection = this._NonKingdomFactionSelectionStage; 
                
                IsHeroStage = false;
                IsKingdomStage = true;
            }
            this.SelectionStageText = "Select a kingdom to choose your character from.";
            OnPropertyChangedWithValue(false, nameof(CanAdvance));

        }
        private CharacterCreationState getCharacterCreationState()
        {
            GameState gm = GameStateManager.Current.ActiveState;
            CharacterCreationState characterCreationState = (gm.GetType().Equals(typeof(CharacterCreationState))) ? (CharacterCreationState)gm : null;
            return characterCreationState;
        }
        
        public void ExecuteMe()
        {
            
            CharacterCreationState characterCreationState = getCharacterCreationState();
            
            if(CurrentSelectedHero != null)
            {
                SAASubModule.heroToBeSet = CurrentSelectedHero.Hero;
            } else if(CurrentSelectedKingdom != null)
            {
                SAASubModule.heroToBeSet = CurrentSelectedKingdom.Kingdom.Leader;
            }
            spawnParty(CurrentSelectedHero.Hero);


            
            
            
            
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
        private void spawnParty(Hero hero)
        {
            if (hero.PartyBelongedTo == null)
            {
                //HeroSpawnCampaignBehavior.SpawnLordParty
                if (hero.GovernorOf != null)
                {
                    ChangeGovernorAction.RemoveGovernorOf(hero);
                }
                Settlement settlement = hero.Clan.Kingdom.FactionMidSettlement;
                if(hero.Clan.Kingdom == null)
                {
                    foreach(Hero allyhero in hero.Clan.Heroes)
                    {
                        if((allyhero.PartyBelongedTo != null))
                        {
                            MobilePartyHelper.SpawnLordParty(hero, allyhero.PartyBelongedTo.Position2D, 30f);
                            return;
                        }
                    }
                }
                MobileParty result;
                if (settlement != null && settlement.MapFaction == hero.MapFaction)
                {

                    result = MobilePartyHelper.SpawnLordParty(hero, settlement);
                }
                else if (hero.MapFaction.InitialPosition.IsValid)
                {
                    result = MobilePartyHelper.SpawnLordParty(hero, hero.MapFaction.InitialPosition, 30f);
                }
                else
                {
                    foreach (Settlement settlement2 in Settlement.All)
                    {
                        if (settlement2.Culture == hero.Culture)
                        {
                            settlement = settlement2;
                            break;
                        }
                    }
                    if (settlement != null)
                    {
                        result = MobilePartyHelper.SpawnLordParty(hero, settlement);
                    }
                    else
                    {
                        result = MobilePartyHelper.SpawnLordParty(hero, Settlement.All.GetRandomElement<Settlement>());
                    }
                }
            }
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
                if(!_startAsAnyone){
                    OnPropertyChangedWithValue(true, nameof(CanAdvance));

                }
                else
                {
                    OnPropertyChangedWithValue(false, nameof(CanAdvance));
                }

            }
        }

        
        [DataSourceProperty]
        public bool NotStartAsAnyone
        {
            get => !_startAsAnyone;
            set
            {
                if(isStoryMode) { return; }
                if (value ==  !_startAsAnyone) return;
                _startAsAnyone = !value;

                OnPropertyChanged(nameof(NotStartAsAnyone));
                OnPropertyChanged(nameof(StartAsAnyone));
                
                AnyItemSelected = true;
                if(!_startAsAnyone){
                    OnPropertyChangedWithValue(true, nameof(CanAdvance));
                }
                else
                {

                    OnPropertyChangedWithValue(false, nameof(CanAdvance));
                }

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
        public MBBindingList<CharacterCreationKingdomVM> NonKingdomFactions
        {
            get
            {
                return this._nonKingdomFactions;
            }
            set
            {
                if(value != this._nonKingdomFactions)
                {
                    this._nonKingdomFactions = value;
                    base.OnPropertyChangedWithValue<MBBindingList<CharacterCreationKingdomVM>>(value, "NonKingdomFactions");
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
        [DataSourceProperty]
        public string SelectionStageText {
            get
            {
                return this._selectionStageText;
            }
            set
            {
                if (value != this._selectionStageText)
                {
                    this._selectionStageText = value;
                    base.OnPropertyChangedWithValue<string>(value, "SelectionStagetext");

                }
            }
        }

        [DataSourceProperty]
        public bool isStoryMode
        {
            get
            {
                return this._isStoryMode;
            }
            set
            {
                if (value != this._isStoryMode)
                {
                    this._isStoryMode = value;
                    base.OnPropertyChangedWithValue(value, "isStoryMode");
                }
            }
        }
        [DataSourceProperty]
        public HintViewModel StoryModeDisabledHint
        {
            get
            {
                return this._storyModeDisabledHint;
            }
            set
            {
                if (value != this._storyModeDisabledHint)
                {
                    this._storyModeDisabledHint = value;
                    base.OnPropertyChangedWithValue<HintViewModel>(value, "StoryModeDisabledHint");
                }
            }
        }


    }
} 