using System;
using System.Security.Policy;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace StartAsAnyone
{
    public class CharacterCreationHeroVM : ViewModel
    {
        // Property to get the Hero object
        public Hero Hero { get; }

        // Constructor for the class
        public CharacterCreationHeroVM(Hero hero, Action<CharacterCreationHeroVM> onSelection, bool useCivilian = false)
        {
            this._onSelection = onSelection;
            this.Hero = hero;

            if (hero != null)
            {
                //CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(hero.CharacterObject, useCivilian);
                this.ImageIdentifier = new ImageIdentifierVM(CharacterCode.CreateFrom(hero.CharacterObject));
                this.ClanBanner = new ImageIdentifierVM(hero.ClanBanner);
                this.ClanBanner_9 = new ImageIdentifierVM(BannerCode.CreateFrom(hero.ClanBanner), true);
                this.Relation = HeroVM.GetRelation(hero);
                this.IsDead = !hero.IsAlive;
                TextObject textObject;
                this.IsChild = (!CampaignUIHelper.IsHeroInformationHidden(hero, out textObject) &&
                               FaceGen.GetMaturityTypeWithAge(hero.Age) <= BodyMeshMaturityType.Child);

                this.NameText = hero.Name.ToString();
                this.HeroID = hero.Id.ToString();
                this.ClanName = (hero.Clan != null) ? hero.Clan.Name.ToString() : "";
                this.KingdomName = (hero.Clan != null && hero.Clan.Kingdom != null) ? hero.Clan.Kingdom.Name.ToString() : "";
                this.IsLeader = hero.IsKingdomLeader;
                this.EncyclopediaText = hero.EncyclopediaText.ToString();
            }
            else
            {
                this.ImageIdentifier = new ImageIdentifierVM(ImageIdentifierType.Null);
                this.ClanBanner = new ImageIdentifierVM(ImageIdentifierType.Null);
                this.ClanBanner_9 = new ImageIdentifierVM(ImageIdentifierType.Null);
                this.Relation = 0;
                this.NameText = "";
                this.HeroID = "";
                this.ClanName = "";
                this.KingdomName = "";
            }
            
            RefreshValues();
        }
        public CharacterCreationHeroVM(Hero hero, bool useCivilian = false)
        {
            
            this.Hero = hero;

            if (hero != null)
            {
                //CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(hero.CharacterObject, useCivilian);

                this.ImageIdentifier = new ImageIdentifierVM(CharacterCode.CreateFrom(hero.CharacterObject));
                this.ClanBanner = new ImageIdentifierVM(hero.ClanBanner);
                this.ClanBanner_9 = new ImageIdentifierVM(BannerCode.CreateFrom(hero.ClanBanner), true);
                this.Relation = HeroVM.GetRelation(hero);
                this.IsDead = !hero.IsAlive;
                TextObject textObject;
                this.IsChild = (!CampaignUIHelper.IsHeroInformationHidden(hero, out textObject) &&
                               FaceGen.GetMaturityTypeWithAge(hero.Age) <= BodyMeshMaturityType.Child);

                this.NameText = hero.Name.ToString();
                this.HeroID = hero.Id.ToString();
                this.ClanName = (hero.Clan != null) ? hero.Clan.Name.ToString() : "";
                this.KingdomName = (hero.Clan != null && hero.Clan.Kingdom != null) ? hero.Clan.Kingdom.Name.ToString() : "";
                this.IsLeader = hero.IsKingdomLeader;
                this.EncyclopediaText = (hero.EncyclopediaText!=null)? hero.EncyclopediaText.ToString():"";
            }
            else
            {
                this.ImageIdentifier = new ImageIdentifierVM(ImageIdentifierType.Null);
                this.ClanBanner = new ImageIdentifierVM(ImageIdentifierType.Null);
                this.ClanBanner_9 = new ImageIdentifierVM(ImageIdentifierType.Null);
                this.Relation = 0;
                this.NameText = "";
                this.HeroID = "";
                this.ClanName = "";
                this.KingdomName = "";
            }

            RefreshValues();
        }
        public virtual void ExecuteBeginHint()
        {
            if (this.Hero != null)
            {
                InformationManager.ShowTooltip(typeof(Hero), new object[]
                {
                    this.Hero,
                    false
                });
            }
        }

        // Token: 0x06000169 RID: 361 RVA: 0x0000B1A9 File Offset: 0x000093A9
        public virtual void ExecuteEndHint()
        {
            if (this.Hero != null)
            {
                MBInformationManager.HideInformations();
            }
        }
        // Method to execute when hero is selected
        public void ExecuteSelectHero()
        {
            this._onSelection(this);
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            if (this.Hero != null)
            {
                this.NameText = this.Hero.Name.ToString();
                this.ImageIdentifier = new ImageIdentifierVM(CharacterCode.CreateFrom(this.Hero.CharacterObject));
                
            }
        }

        private string _encyclopediaText;
        [DataSourceProperty]
        public string EncyclopediaText
        {
            get { return this._encyclopediaText;}
            set
            {
                if (value != this._encyclopediaText)
                {
                    this._heroID = value;
                    base.OnPropertyChangedWithValue<string>(value, "EncyclopediaText");
                }
            }
        }

        // Hero ID property
        private string _heroID;
        [DataSourceProperty]
        public string HeroID
        {
            get
            {
                return this._heroID;
            }
            set
            {
                if (value != this._heroID)
                {
                    this._heroID = value;
                    base.OnPropertyChangedWithValue<string>(value, "HeroID");
                }
            }
        }
        private bool _isLeader;
        [DataSourceProperty]
        public bool IsLeader
        {
            get => this._isLeader;
            set
            {
                if(value!= this._isLeader)
                {
                    this._isLeader = value;
                    base.OnPropertyChangedWithValue(value, "IsLeader");
                }
            }
        }

        // Hero name property
        private string _name;
        [DataSourceProperty]
        public string NameText
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
                    base.OnPropertyChangedWithValue<string>(value, "NameText");
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

        // Clan name property
        private string _clanName;
        [DataSourceProperty]
        public string ClanName
        {
            get
            {
                return this._clanName;
            }
            set
            {
                if (value != this._clanName)
                {
                    this._clanName = value;
                    base.OnPropertyChangedWithValue<string>(value, "ClanName");
                }
            }
        }

        // Kingdom name property
        private string _kingdomName;
        [DataSourceProperty]
        public string KingdomName
        {
            get
            {
                return this._kingdomName;
            }
            set
            {
                if (value != this._kingdomName)
                {
                    this._kingdomName = value;
                    base.OnPropertyChangedWithValue<string>(value, "KingdomName");
                }
            }
        }

        // Image identifier property
        private ImageIdentifierVM _imageIdentifier;
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

        // Clan banner properties
        private ImageIdentifierVM _clanBanner;
        [DataSourceProperty]
        public ImageIdentifierVM ClanBanner
        {
            get
            {
                return this._clanBanner;
            }
            set
            {
                if (value != this._clanBanner)
                {
                    this._clanBanner = value;
                    base.OnPropertyChangedWithValue<ImageIdentifierVM>(value, "ClanBanner");
                }
            }
        }

        private ImageIdentifierVM _clanBanner_9;
        [DataSourceProperty]
        public ImageIdentifierVM ClanBanner_9
        {
            get
            {
                return this._clanBanner_9;
            }
            set
            {
                if (value != this._clanBanner_9)
                {
                    this._clanBanner_9 = value;
                    base.OnPropertyChangedWithValue<ImageIdentifierVM>(value, "ClanBanner_9");
                }
            }
        }

        // IsDead property
        private bool _isDead;
        [DataSourceProperty]
        public bool IsDead
        {
            get
            {
                return this._isDead;
            }
            set
            {
                if (value != this._isDead)
                {
                    this._isDead = value;
                    base.OnPropertyChangedWithValue(value, "IsDead");
                }
            }
        }

        // IsChild property
        private bool _isChild;
        [DataSourceProperty]
        public bool IsChild
        {
            get
            {
                return this._isChild;
            }
            set
            {
                if (value != this._isChild)
                {
                    this._isChild = value;
                    base.OnPropertyChangedWithValue(value, "IsChild");
                }
            }
        }

        // Relation property
        private int _relation;
        [DataSourceProperty]
        public int Relation
        {
            get
            {
                return this._relation;
            }
            set
            {
                if (value != this._relation)
                {
                    this._relation = value;
                    base.OnPropertyChangedWithValue(value, "Relation");
                }
            }
        }

        // Fields
        private readonly Action<CharacterCreationHeroVM> _onSelection;
    }
}