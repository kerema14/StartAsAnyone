using System;
using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.TwoDimension;


namespace StartAsAnyone
{
    /// <summary>
    /// Widget for handling kingdom visual elements in character creation
    /// </summary>
    public class CharacterCreationKingdomVisualBrushWidget : BrushWidget
    {
        /// <summary>
        /// Determines whether to use small visuals for kingdoms
        /// </summary>
        public bool UseSmallVisuals { get; set; } = true;

        /// <summary>
        /// First parallax layer widget
        /// </summary>
        public ParallaxItemBrushWidget Layer1Widget { get; set; }

        /// <summary>
        /// Second parallax layer widget
        /// </summary>
        public ParallaxItemBrushWidget Layer2Widget { get; set; }

        /// <summary>
        /// Third parallax layer widget
        /// </summary>
        public ParallaxItemBrushWidget Layer3Widget { get; set; }

        /// <summary>
        /// Fourth parallax layer widget
        /// </summary>
        public ParallaxItemBrushWidget Layer4Widget { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">UI context</param>
        public CharacterCreationKingdomVisualBrushWidget(UIContext context) : base(context)
        {
        }

        /// <summary>
        /// Called every frame after regular update
        /// </summary>
        /// <param name="dt">Delta time</param>
        protected override void OnLateUpdate(float dt)
        {
            base.OnLateUpdate(dt);
            if (this._isFirstFrame)
            {
                this._alphaTarget = (float)(string.IsNullOrEmpty(this.CurrentKingdomId) ? 0 : 1);
                this.SetGlobalAlphaRecursively(this._alphaTarget);
                ParallaxItemBrushWidget layer1Widget = this.Layer1Widget;
                if (layer1Widget != null)
                {
                    layer1Widget.RegisterBrushStatesOfWidget();
                }
                ParallaxItemBrushWidget layer2Widget = this.Layer2Widget;
                if (layer2Widget != null)
                {
                    layer2Widget.RegisterBrushStatesOfWidget();
                }
                ParallaxItemBrushWidget layer3Widget = this.Layer3Widget;
                if (layer3Widget != null)
                {
                    layer3Widget.RegisterBrushStatesOfWidget();
                }
                ParallaxItemBrushWidget layer4Widget = this.Layer4Widget;
                if (layer4Widget != null)
                {
                    layer4Widget.RegisterBrushStatesOfWidget();
                }
                this._isFirstFrame = false;
            }
            this.SetGlobalAlphaRecursively(Mathf.Lerp(base.ReadOnlyBrush.GlobalAlphaFactor, this._alphaTarget, dt * 10f));
        }

        /// <summary>
        /// Sets the kingdom visual based on the kingdom ID
        /// </summary>
        /// <param name="newKingdomId">New kingdom ID to set</param>
        private void SetKingdomVisual(string newKingdomId)
        {
            if (string.IsNullOrEmpty(newKingdomId))
            {
                this._alphaTarget = 0f;
                return;
            }
            if (this.UseSmallVisuals)
            {
                using (Dictionary<string, Style>.ValueCollection.Enumerator enumerator = base.Brush.Styles.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Style style = enumerator.Current;
                        StyleLayer[] layers = style.GetLayers();
                        for (int i = 0; i < layers.Length; i++)
                        {
                            //layers[i].Sprite = //implement here!!!!!!!!  //base.Context.SpriteData.GetSprite("CharacterCreation\\Kingdom\\" + newKingdomId);
                            
                        }
                    }
                    goto IL_CE;
                }
            }
            ParallaxItemBrushWidget layer1Widget = this.Layer1Widget;
            if (layer1Widget != null)
            {
                layer1Widget.SetState(newKingdomId);
            }
            ParallaxItemBrushWidget layer2Widget = this.Layer2Widget;
            if (layer2Widget != null)
            {
                layer2Widget.SetState(newKingdomId);
            }
            ParallaxItemBrushWidget layer3Widget = this.Layer3Widget;
            if (layer3Widget != null)
            {
                layer3Widget.SetState(newKingdomId);
            }
            ParallaxItemBrushWidget layer4Widget = this.Layer4Widget;
            if (layer4Widget != null)
            {
                layer4Widget.SetState(newKingdomId);
            }
        IL_CE:
            this._alphaTarget = 1f;
        }

        /// <summary>
        /// Current kingdom ID
        /// </summary>
        [Editor(false)]
        public string CurrentKingdomId
        {
            get
            {
                return this._currentKingdomId;
            }
            set
            {
                if (this._currentKingdomId != value)
                {
                    this._currentKingdomId = value;
                    base.OnPropertyChanged<string>(value, "CurrentKingdomId");
                    this.SetKingdomVisual(value);
                    this.SetGlobalAlphaRecursively(1f);
                }
            }
        }

        

        private float _alphaTarget;
        private bool _isFirstFrame = true;
        private string _currentKingdomId;
        
    }
}