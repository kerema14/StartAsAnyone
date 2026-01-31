using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace StartAsAnyone
{
    
    public class KingdomInfoVM : ViewModel
    {
        
        // (get) Token: 0x0600139B RID: 5019 RVA: 0x0004B942 File Offset: 0x00049B42
        // (set) Token: 0x0600139C RID: 5020 RVA: 0x0004B94A File Offset: 0x00049B4A
        public IFaction Faction { get; private set; }

        
        public KingdomInfoVM(IFaction faction)
        {
            this.Faction = faction;
            if (faction != null)
            {
                this.ImageIdentifier = new BannerImageIdentifierVM(faction.Banner, true); 
                this.IsDestroyed = faction.IsEliminated;
            }
            else
            {
                this.ImageIdentifier = new BannerImageIdentifierVM(Banner.CreateRandomBanner()); //new ImageIdentifierVM(ImageIdentifierType.Null);
                this.IsDestroyed = false;
            }
            this.RefreshValues();
        }

        
        public override void RefreshValues()
        {
            base.RefreshValues();
            if (this.Faction != null)
            {
                this.NameText = this.Faction.Name.ToString();
                return;
            }
            this.NameText = new TextObject("{=2abtb4xu}Independent", null).ToString();
        }

        
        public void ExecuteLink()
        {
            if (this.Faction != null)
            {
                Campaign.Current.EncyclopediaManager.GoToLink(this.Faction.EncyclopediaLink);
            }
        }

        
        // (get) Token: 0x060013A0 RID: 5024 RVA: 0x0004BA10 File Offset: 0x00049C10
        // (set) Token: 0x060013A1 RID: 5025 RVA: 0x0004BA18 File Offset: 0x00049C18
        [DataSourceProperty]
        public BannerImageIdentifierVM ImageIdentifier
        {
            get
            {
                return this._imageIdentifier;
            }
            set
            {
                if (value != this._imageIdentifier)
                {
                    this._imageIdentifier = value;
                    base.OnPropertyChanged("Banner");
                }
            }
        }

        
        // (get) Token: 0x060013A2 RID: 5026 RVA: 0x0004BA35 File Offset: 0x00049C35
        // (set) Token: 0x060013A3 RID: 5027 RVA: 0x0004BA3D File Offset: 0x00049C3D
        [DataSourceProperty]
        public string NameText
        {
            get
            {
                return this._nameText;
            }
            set
            {
                if (value != this._nameText)
                {
                    this._nameText = value;
                    base.OnPropertyChangedWithValue<string>(value, "NameText");
                }
            }
        }

        
        // (get) Token: 0x060013A4 RID: 5028 RVA: 0x0004BA60 File Offset: 0x00049C60
        // (set) Token: 0x060013A5 RID: 5029 RVA: 0x0004BA68 File Offset: 0x00049C68
        [DataSourceProperty]
        public bool IsDestroyed
        {
            get
            {
                return this._isDestroyed;
            }
            set
            {
                if (value != this._isDestroyed)
                {
                    this._isDestroyed = value;
                    base.OnPropertyChangedWithValue(value, "IsDestroyed");
                }
            }
        }

        
        private BannerImageIdentifierVM _imageIdentifier;

        
        private string _nameText;

        
        private bool _isDestroyed;
    }
}
