using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace StartAsAnyone
{
    public class CharacterCreationKingdomVM : ViewModel
    {
        // Property to get the Kingdom object
        public Kingdom Kingdom { get; }
        public Clan Clan { get; }

        // Constructor for the class
        public CharacterCreationKingdomVM(Kingdom kingdom, Action<CharacterCreationKingdomVM> onSelection)
        {
            
            this._onSelection = onSelection;
            this.Kingdom = kingdom;
            this.Name = kingdom.Name.ToString();
            this.KingdomID = ((kingdom != null) ? kingdom.Id.ToString() : null) ?? "";
            this.KingdomColor1 = Color.FromUint((kingdom != null) ? kingdom.Color : Color.White.ToUnsignedInteger());
            this.KingdomColor2 = Color.FromUint((kingdom != null) ? kingdom.Color2 : Color.Black.ToUnsignedInteger());
            this.RulerName = ((kingdom != null && kingdom.Leader != null) ? kingdom.Leader.Name.ToString() : "");
            this.Banner = (kingdom?.Banner);
            this.ImageIdentifier = new ImageIdentifierVM(BannerCode.CreateFrom(kingdom.Banner),true);
            RefreshValues();
        }
        public CharacterCreationKingdomVM(Action<CharacterCreationKingdomVM> onSelection)
        {
            this._onSelection = onSelection;
            this.Name = "Non-kingdom clans";
            this.RulerName = "?";
            this.Banner = Banner.CreateRandomBanner();
            this.ImageIdentifier = new ImageIdentifierVM(BannerCode.CreateFrom(Banner), true);
            RefreshValues();
        }
        public CharacterCreationKingdomVM(Clan clan,Action<CharacterCreationKingdomVM>onSelection) {
            this.Clan = clan;
            this._onSelection = onSelection;
            this.Name = clan.Name.ToString();
            this.RulerName = (clan.Leader != null) ? clan.Leader.Name.ToString() : "";
            this.Banner = clan.Banner;
            this.ImageIdentifier = new ImageIdentifierVM(BannerCode.CreateFrom(clan.Banner), true);
            RefreshValues();
        }

        // Method to execute when kingdom is selected
        public void ExecuteSelectKingdom()
        {
            this._onSelection(this);
        }

        // Kingdom ID property
        private string _kingdomID;
        [DataSourceProperty]
        public string KingdomID
        {
            get
            {
                return this._kingdomID;
            }
            set
            {
                if (value != this._kingdomID)
                {
                    this._kingdomID = value;
                    base.OnPropertyChangedWithValue<string>(value, "KingdomID");
                }
            }
        }
        private string _name;
        [DataSourceProperty]
        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                if (value != this._name)
                {
                    this._name = value;
                    base.OnPropertyChangedWithValue<string>(value, "KingdomID");
                }
            }
        }

        // Kingdom primary color property
        private Color _kingdomColor1;
        [DataSourceProperty]
        public Color KingdomColor1
        {
            get
            {
                return this._kingdomColor1;
            }
            set
            {
                if (value != this._kingdomColor1)
                {
                    this._kingdomColor1 = value;
                    base.OnPropertyChangedWithValue(value, "KingdomColor1");
                }
            }
        }

        // Kingdom secondary color property
        private Color _kingdomColor2;
        [DataSourceProperty]
        public Color KingdomColor2
        {
            get
            {
                return this._kingdomColor2;
            }
            set
            {
                if (value != this._kingdomColor2)
                {
                    this._kingdomColor2 = value;
                    base.OnPropertyChangedWithValue(value, "KingdomColor2");
                }
            }
        }

        // Is selected property
        private bool _isSelected;
        [DataSourceProperty]
        public bool IsSelected
        {
            get
            {
                return this._isSelected;
            }
            set
            {
                if (value != this._isSelected)
                {
                    this._isSelected = value;
                    base.OnPropertyChangedWithValue(value, "IsSelected");
                }
            }
        }

        // Ruler name property
        private string _rulerName;
        [DataSourceProperty]
        public string RulerName
        {
            get
            {
                return this._rulerName;
            }
            set
            {
                if (value != this._rulerName)
                {
                    this._rulerName = value;
                    base.OnPropertyChangedWithValue<string>(value, "RulerName");
                }
            }
        }

        // Banner property
        private Banner _banner;
        private ImageIdentifierVM _imageIdentifier;

        [DataSourceProperty]
        public Banner Banner
        {
            get
            {
                return this._banner;
            }
            set
            {
                if (value != this._banner)
                {
                    this._banner = value;
                    base.OnPropertyChangedWithValue(value, "Banner");
                }
            }
        }
        [DataSourceProperty]
        public ImageIdentifierVM ImageIdentifier
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
                    base.OnPropertyChangedWithValue<ImageIdentifierVM>(value, "ImageIdentifier");
                }
            }
        }
        


        // Fields
        private readonly Action<CharacterCreationKingdomVM> _onSelection;
    }
}