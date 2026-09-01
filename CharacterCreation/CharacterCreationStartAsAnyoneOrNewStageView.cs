using System;
using System.Collections.Generic;
using SandBox.View.CharacterCreation;
using SandBox.View.Missions;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace StartAsAnyone
{
    [CharacterCreationStageView(typeof(CharacterCreationStartAsAnyoneOrNewStage))]
    public class CharacterCreationStartAsAnyoneOrNewStageView : CharacterCreationStageViewBase
    {
        private readonly CharacterCreationManager _characterCreationManager;

        private GauntletLayer? _gauntletLayer;
        private GauntletMovieIdentifier? _movie;
        private StartAsAnyoneRootVM? _dataSource;
        private SpriteCategory? _characterCreationCategory;
        private SpriteCategory? _encyclopediaCategory;
        private EscapeMenuVM? _escapeMenuDataSource;
        private GauntletMovieIdentifier? _escapeMenuMovie;

        public CharacterCreationStartAsAnyoneOrNewStageView(
            CharacterCreationManager characterCreationManager,
            ControlCharacterCreationStage affirmativeAction,
            TextObject affirmativeActionText,
            ControlCharacterCreationStage negativeAction,
            TextObject negativeActionText,
            ControlCharacterCreationStage onRefresh,
            ControlCharacterCreationStageReturnInt getCurrentStageIndexAction,
            ControlCharacterCreationStageReturnInt getTotalStageCountAction,
            ControlCharacterCreationStageReturnInt getFurthestIndexAction,
            ControlCharacterCreationStageWithInt goToIndexAction) : base(
                affirmativeAction,
                negativeAction,
                onRefresh,
                getCurrentStageIndexAction,
                getTotalStageCountAction,
                getFurthestIndexAction,
                goToIndexAction)
        {
            _characterCreationManager = characterCreationManager;

            _gauntletLayer = new GauntletLayer("CharacterCreationStartAsAnyone", 1, true)
            {
                IsFocusLayer = true
            };
            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            ScreenManager.TrySetFocus(_gauntletLayer);

            LoadSpriteCategories();

            _dataSource = new StartAsAnyoneRootVM(
                _characterCreationManager,
                NextStage,
                affirmativeActionText,
                PreviousStage,
                negativeActionText);

            _movie = _gauntletLayer.LoadMovie("SAA.Root", _dataSource);

            var panelKeys = HotKeyManager.GetCategory("GenericPanelGameKeyCategory");
            _dataSource.SetCancelInputKey(panelKeys.GetHotKey("Exit"));
            _dataSource.SetDoneInputKey(panelKeys.GetHotKey("Confirm"));

#if SAA_CAPTURE
            CaptureDirector.OnStageReady(_dataSource);
#endif
        }

        private void LoadSpriteCategories()
        {
            SpriteData spriteData = UIResourceManager.SpriteData;
            TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
            ResourceDepot resourceDepot = UIResourceManager.ResourceDepot;

            if (spriteData.SpriteCategories.TryGetValue("ui_charactercreation", out SpriteCategory characterCreation))
            {
                _characterCreationCategory = characterCreation;
                _characterCreationCategory.Load(resourceContext, resourceDepot);
            }
            if (spriteData.SpriteCategories.TryGetValue("ui_encyclopedia", out SpriteCategory encyclopedia))
            {
                _encyclopediaCategory = encyclopedia;
                _encyclopediaCategory.Load(resourceContext, resourceDepot);
            }
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (_gauntletLayer == null || _dataSource == null)
            {
                return;
            }
            HandleEscapeMenu(this, _gauntletLayer);
            HandleLayerInput();
        }

        private void HandleLayerInput()
        {
            if (_gauntletLayer == null || _dataSource == null)
            {
                return;
            }
            if (_gauntletLayer.Input.IsHotKeyReleased("Exit"))
            {
                UISoundsHelper.PlayUISound("event:/ui/panels/next");
                _dataSource.ExecuteBack();
                return;
            }
            if (_gauntletLayer.Input.IsHotKeyReleased("Confirm") && _dataSource.CanGoForward)
            {
                UISoundsHelper.PlayUISound("event:/ui/panels/next");
                _dataSource.ExecuteForward();
            }
        }

        public override void GoToIndex(int index) => _goToIndexAction(index);

        public override void NextStage() => _affirmativeAction();

        public override void PreviousStage() => Game.Current.GameStateManager.PopState(0);

        public override int GetVirtualStageCount() => 1;

        public override IEnumerable<ScreenLayer> GetLayers()
        {
            var layers = new List<ScreenLayer>();
            if (_gauntletLayer != null)
            {
                layers.Add(_gauntletLayer);
            }
            return layers;
        }

        public override void LoadEscapeMenuMovie()
        {
            if (_gauntletLayer == null)
            {
                return;
            }
            _escapeMenuDataSource = new EscapeMenuVM(GetEscapeMenuItems(this), null);
            _escapeMenuMovie = _gauntletLayer.LoadMovie("EscapeMenu", _escapeMenuDataSource);
        }

        public override void ReleaseEscapeMenuMovie()
        {
            if (_gauntletLayer != null && _escapeMenuMovie != null)
            {
                _gauntletLayer.ReleaseMovie(_escapeMenuMovie);
            }
            _escapeMenuDataSource = null;
            _escapeMenuMovie = null;
        }

        protected override void OnFinalize()
        {
            base.OnFinalize();

#if SAA_CAPTURE
            CaptureDirector.OnStageClosed();
#endif

            if (_gauntletLayer != null && _movie != null)
            {
                _gauntletLayer.ReleaseMovie(_movie);
            }
            _movie = null;

            _dataSource?.OnFinalize();
            _dataSource = null;

            _characterCreationCategory?.Unload();
            _characterCreationCategory = null;
            _encyclopediaCategory?.Unload();
            _encyclopediaCategory = null;

            _gauntletLayer = null;
        }
    }
}
