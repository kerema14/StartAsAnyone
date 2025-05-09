using System;
using System.Collections.Generic;
using SandBox.View.CharacterCreation;
using SandBox.View.Missions;
using TaleWorlds.CampaignSystem;
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
        private readonly IGauntletMovie _movie;
        private GauntletLayer GauntletLayer;
        private CharacterCreationStartAsAnyoneOrNewStageVM _dataSource;
        private SpriteCategory _characterCreationCategory;
        private readonly CharacterCreation _characterCreation;
        private EscapeMenuVM _escapeMenuDatasource;
        private IGauntletMovie _escapeMenuMovie;

        public CharacterCreationStartAsAnyoneOrNewStageView(
            CharacterCreation characterCreation,
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
            this._characterCreation = characterCreation;
            this.GauntletLayer = new GauntletLayer(1, "GauntletLayer", true)
            {
                IsFocusLayer = true
            };
            this.GauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            this.GauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            ScreenManager.TrySetFocus(this.GauntletLayer);
            this._dataSource = new CharacterCreationStartAsAnyoneOrNewStageVM(
                this._characterCreation,
                new Action(this.NextStage),
                affirmativeActionText,
                new Action(this.PreviousStage),
                negativeActionText,
                getCurrentStageIndexAction(),
                getTotalStageCountAction(),
                getFurthestIndexAction(),
                new Action<int>(this.GoToIndex),
                new Action<bool>(this.OnStartAsAnyoneSelected));

            this._movie = this.GauntletLayer.LoadMovie("CharacterCreationStartAsAnyoneOrNewStage", this._dataSource);
            this._dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
            this._dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
            SpriteData spriteData = UIResourceManager.SpriteData;
            TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
            ResourceDepot uiresourceDepot = UIResourceManager.UIResourceDepot;
            this._characterCreationCategory = spriteData.SpriteCategories["ui_charactercreation"];
            this._characterCreationCategory.Load(resourceContext, uiresourceDepot);


        }

        protected override void OnFinalize()
        {
            base.OnFinalize();
            this.GauntletLayer = null;
            CharacterCreationStartAsAnyoneOrNewStageVM dataSource = this._dataSource;
            if (dataSource != null)
            {
                dataSource.OnFinalize();
            }
            this._dataSource = null;
            this._characterCreationCategory.Unload();
        }

        private void HandleLayerInput()
        {
            if (this.GauntletLayer.Input.IsHotKeyReleased("Exit"))
            {
                UISoundsHelper.PlayUISound("event:/ui/panels/next");
                this._dataSource.OnPreviousStage();
                return;
            }
            if (this.GauntletLayer.Input.IsHotKeyReleased("Confirm") && this._dataSource.CanAdvance)
            {
                UISoundsHelper.PlayUISound("event:/ui/panels/next");
                this._dataSource.OnNextStage();
            }
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (this._dataSource.IsActive)
            {
                base.HandleEscapeMenu(this, this.GauntletLayer);
                this.HandleLayerInput();
            }
        }

        public override void NextStage()
        {
            this._affirmativeAction();
        }

        private void OnStartAsAnyoneSelected(bool startAsAnyone)
        {
            // Handle the selection of starting as anyone or creating new character
            // This will be implemented based on your specific requirements
            InformationManager.DisplayMessage(new InformationMessage("Wow you did it"));
        }

        public override void PreviousStage()
        {
            Game.Current.GameStateManager.PopState(0);
        }

        public override int GetVirtualStageCount()
        {
            return 1;
        }

        public override IEnumerable<ScreenLayer> GetLayers()
        {
            return new List<ScreenLayer>
            {
                this.GauntletLayer
            };
        }

        public override void LoadEscapeMenuMovie()
        {
            this._escapeMenuDatasource = new EscapeMenuVM(base.GetEscapeMenuItems(this), null);
            this._escapeMenuMovie = this.GauntletLayer.LoadMovie("EscapeMenu", this._escapeMenuDatasource);
        }

        public override void ReleaseEscapeMenuMovie()
        {
            this.GauntletLayer.ReleaseMovie(this._escapeMenuMovie);
            this._escapeMenuDatasource = null;
            this._escapeMenuMovie = null;
        }
    }
} 