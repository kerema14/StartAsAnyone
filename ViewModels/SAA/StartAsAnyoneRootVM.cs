using System;
using StoryMode;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace StartAsAnyone
{
    /// <summary>
    /// Owns the three screens and the navigation between them. Holds no campaign state and
    /// mutates nothing; committing the choice is <see cref="StartAsAnyoneApplier"/>'s job.
    /// </summary>
    public class StartAsAnyoneRootVM : CharacterCreationStageBaseVM
    {
        private enum Screen
        {
            Path,
            Faction,
            Character
        }

        private readonly bool _isStoryMode;

        private bool _ready;
        private Screen _screen = Screen.Path;
        private InputKeyItemVM? _cancelInputKey;
        private InputKeyItemVM? _doneInputKey;
        private string _screenTitle = string.Empty;
        private string _screenSubtitle = string.Empty;
        private string _forwardText = string.Empty;
        private bool _isPathScreen = true;
        private bool _isFactionScreen;
        private bool _isCharacterScreen;
        private bool _canGoBack;
        private bool _canGoForward = true;

        public StartAsAnyoneRootVM(
            CharacterCreationManager characterCreationManager,
            Action affirmativeAction,
            TextObject affirmativeActionText,
            Action negativeAction,
            TextObject negativeActionText)
            : base(characterCreationManager, affirmativeAction, affirmativeActionText, negativeAction, negativeActionText)
        {
            _isStoryMode = Game.Current?.GameType is CampaignStoryMode;

            SubModule.startAsAnyone = false;
            SubModule.heroInit = false;

            // The child constructors call back into RefreshNavigation while the later
            // fields are still null, so navigation stays inert until all three exist.
            Path = new PathScreenVM(_isStoryMode, RefreshNavigation);
            Factions = new FactionScreenVM(RefreshNavigation);
            Characters = new CharacterScreenVM(RefreshNavigation);

            _ready = true;
            GoTo(Screen.Path);
        }

        // -- navigation ------------------------------------------------------

        public void ExecuteForward()
        {
            if (!CanGoForward)
            {
                return;
            }

            switch (_screen)
            {
                case Screen.Path:
                    if (Path.StartAsAnyone)
                    {
                        GoTo(Screen.Faction);
                    }
                    else
                    {
                        SubModule.startAsAnyone = false;
                        _affirmativeAction();
                    }
                    break;

                case Screen.Faction:
                    Characters.SetCandidates(FactionScreenVM.HeroesOf(Factions.SelectedFaction));
                    GoTo(Screen.Character);
                    break;

                case Screen.Character:
                    Confirm();
                    break;
            }
        }

        public void ExecuteBack()
        {
            switch (_screen)
            {
                case Screen.Path:
                    _negativeAction();
                    break;
                case Screen.Faction:
                    GoTo(Screen.Path);
                    break;
                case Screen.Character:
                    GoTo(Screen.Faction);
                    break;
            }
        }

        private void Confirm()
        {
            Hero? hero = Characters.SelectedHero;
            if (hero == null)
            {
                return;
            }

            StartAsAnyoneApplier.Apply(hero);

            // Jump to the last stage; the ApplyFinalEffects patch does the rest.
            if (GameStateManager.Current?.ActiveState is CharacterCreationState state
                && state.CharacterCreationManager != null)
            {
                int total = state.CharacterCreationManager.GetTotalStagesCount();
                state.CharacterCreationManager.GoToStage(Math.Max(total - 1, 0));
            }
            else
            {
                // Another mod replaced the state; fall through the normal path instead of
                // dropping the player's choice on the floor.
                _affirmativeAction();
            }
        }

        private void GoTo(Screen screen)
        {
            _screen = screen;
            IsPathScreen = screen == Screen.Path;
            IsFactionScreen = screen == Screen.Faction;
            IsCharacterScreen = screen == Screen.Character;
            RefreshNavigation();
        }

        private void RefreshNavigation()
        {
            if (!_ready)
            {
                return;
            }

            switch (_screen)
            {
                case Screen.Path:
                    ScreenTitle = new TextObject("{=start_as_anyone_title}Choose Your Path").ToString();
                    ScreenSubtitle = new TextObject("{=SAA_sub_path}How do you want to enter the world of Calradia?").ToString();
                    ForwardText = new TextObject("{=xWS4WB54I}Next").ToString();
                    CanGoBack = true;
                    CanGoForward = true;
                    break;

                case Screen.Faction:
                    ScreenTitle = new TextObject("{=SAA_title_faction}Choose a Faction").ToString();
                    ScreenSubtitle = new TextObject("{=KKt69IHkk}Select a kingdom to choose your character from.").ToString();
                    ForwardText = new TextObject("{=xWS4WB54I}Next").ToString();
                    CanGoBack = true;
                    CanGoForward = Factions.SelectedFaction != null;
                    break;

                case Screen.Character:
                    ScreenTitle = new TextObject("{=SAA_title_character}Choose Your Character").ToString();
                    ScreenSubtitle = new TextObject("{=Fa7kegZKb}Choose a character to play as.").ToString();
                    ForwardText = new TextObject("{=SAA_begin}Begin").ToString();
                    CanGoBack = true;
                    CanGoForward = Characters.SelectedHero != null;
                    break;
            }

            base.Title = ScreenTitle;
            base.Description = ScreenSubtitle;
            base.AnyItemSelected = CanGoForward;
            OnPropertyChangedWithValue(CanGoForward, nameof(CanAdvance));
        }

        // -- stage plumbing --------------------------------------------------

        public override void OnNextStage() => ExecuteForward();

        public override void OnPreviousStage() => ExecuteBack();

        public override bool CanAdvanceToNextStage() => CanGoForward;

        public void SetCancelInputKey(HotKey hotKey) => CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);

        public void SetDoneInputKey(HotKey hotKey) => DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);

        public override void OnFinalize()
        {
            base.OnFinalize();
            CancelInputKey?.OnFinalize();
            DoneInputKey?.OnFinalize();
            Characters.OnFinalize();
        }

        // -- bindings --------------------------------------------------------

        [DataSourceProperty] public PathScreenVM Path { get; }
        [DataSourceProperty] public FactionScreenVM Factions { get; }
        [DataSourceProperty] public CharacterScreenVM Characters { get; }

        [DataSourceProperty]
        public InputKeyItemVM? CancelInputKey
        {
            get => _cancelInputKey;
            set => SetRefProperty(ref _cancelInputKey, value, nameof(CancelInputKey));
        }

        [DataSourceProperty]
        public InputKeyItemVM? DoneInputKey
        {
            get => _doneInputKey;
            set => SetRefProperty(ref _doneInputKey, value, nameof(DoneInputKey));
        }

        [DataSourceProperty]
        public string ScreenTitle
        {
            get => _screenTitle;
            set => SetStringProperty(ref _screenTitle, value, nameof(ScreenTitle));
        }

        [DataSourceProperty]
        public string ScreenSubtitle
        {
            get => _screenSubtitle;
            set => SetStringProperty(ref _screenSubtitle, value, nameof(ScreenSubtitle));
        }

        [DataSourceProperty]
        public string ForwardText
        {
            get => _forwardText;
            set => SetStringProperty(ref _forwardText, value, nameof(ForwardText));
        }

        [DataSourceProperty]
        public bool IsPathScreen
        {
            get => _isPathScreen;
            set => SetBoolProperty(ref _isPathScreen, value, nameof(IsPathScreen));
        }

        [DataSourceProperty]
        public bool IsFactionScreen
        {
            get => _isFactionScreen;
            set => SetBoolProperty(ref _isFactionScreen, value, nameof(IsFactionScreen));
        }

        [DataSourceProperty]
        public bool IsCharacterScreen
        {
            get => _isCharacterScreen;
            set => SetBoolProperty(ref _isCharacterScreen, value, nameof(IsCharacterScreen));
        }

        [DataSourceProperty]
        public bool CanGoBack
        {
            get => _canGoBack;
            set => SetBoolProperty(ref _canGoBack, value, nameof(CanGoBack));
        }

        [DataSourceProperty]
        public bool CanGoForward
        {
            get => _canGoForward;
            set => SetBoolProperty(ref _canGoForward, value, nameof(CanGoForward));
        }

        // CharacterCreationStageBaseVM is our base, so the SAAViewModel helpers are not
        // available here; these are the same one-property-one-name setters.
        private void SetBoolProperty(ref bool field, bool value, string propertyName)
        {
            if (field != value)
            {
                field = value;
                OnPropertyChangedWithValue(value, propertyName);
            }
        }

        private void SetStringProperty(ref string field, string value, string propertyName)
        {
            value ??= string.Empty;
            if (field != value)
            {
                field = value;
                OnPropertyChangedWithValue<string>(value, propertyName);
            }
        }

        private void SetRefProperty<T>(ref T? field, T? value, string propertyName) where T : class
        {
            if (!ReferenceEquals(field, value))
            {
                field = value;
                OnPropertyChangedWithValue<T?>(value, propertyName);
            }
        }
    }
}
