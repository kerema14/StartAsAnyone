using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace StartAsAnyone
{
    
    [EncyclopediaViewModel(typeof(Kingdom))]
    public class KingdomInfoPageVM : ViewModel
    {
        
        public KingdomInfoPageVM(Kingdom kingdom)
        {
            this._faction = kingdom;
            this.Clans = new MBBindingList<KingdomInfoVM>();
            this.Enemies = new MBBindingList<KingdomInfoVM>();
            this.Settlements = new MBBindingList<EncyclopediaSettlementVM>();
            this.History = new MBBindingList<EncyclopediaHistoryEventVM>();
            
            this.RefreshValues();
        }

        
        public override void RefreshValues()
        {
            base.RefreshValues();
            this.StrengthHint = new HintViewModel(GameTexts.FindText("str_strength", null), null);
            this.ProsperityHint = new HintViewModel(GameTexts.FindText("str_prosperity", null), null);
            this.MembersText = GameTexts.FindText("str_members", null).ToString();
            this.ClansText = new TextObject("{=bfQLwMUp}Clans", null).ToString();
            this.EnemiesText = new TextObject("{=zZlWRZjO}Wars", null).ToString();
            this.SettlementsText = new TextObject("{=LBNzsqyb}Fiefs", null).ToString();
            this.VillagesText = GameTexts.FindText("str_villages", null).ToString();
            TextObject encyclopediaText = this._faction.EncyclopediaText;
            this.InformationText = (((encyclopediaText != null) ? encyclopediaText.ToString() : null) ?? string.Empty);
            
            this.Refresh();
        }

        
        public void Refresh()
        {
            
            this.Clans.Clear();
            this.Enemies.Clear();
            this.Settlements.Clear();
            this.History.Clear();
            this.Leader = new HeroVM(this._faction.Leader, false);
            this.LeaderText = GameTexts.FindText("str_leader", null).ToString();
            this.NameText = this._faction.Name.ToString();
            this.DescriptorText = GameTexts.FindText("str_kingdom_faction", null).ToString();
            int num = 0;
            float num2 = 0f;
            EncyclopediaPage pageOf = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero));
            foreach (Hero hero in this._faction.Lords)
            {
                if (pageOf.IsValidEncyclopediaItem(hero))
                {
                    num += hero.Gold;
                }
            }
            this.Banner = new ImageIdentifierVM(BannerCode.CreateFrom(this._faction.Banner), true);
            foreach (MobileParty mobileParty in MobileParty.AllLordParties)
            {
                if (mobileParty.MapFaction == this._faction && !mobileParty.IsDisbanding)
                {
                    num2 += mobileParty.Party.TotalStrength;
                }
            }
            this.ProsperityText = num.ToString();
            this.StrengthText = num2.ToString();
            for (int i = Campaign.Current.LogEntryHistory.GameActionLogs.Count - 1; i >= 0; i--)
            {
                IEncyclopediaLog encyclopediaLog;
                if ((encyclopediaLog = (Campaign.Current.LogEntryHistory.GameActionLogs[i] as IEncyclopediaLog)) != null && encyclopediaLog.IsVisibleInEncyclopediaPageOf<Kingdom>(this._faction))
                {
                    this.History.Add(new EncyclopediaHistoryEventVM(encyclopediaLog));
                }
            }
            EncyclopediaPage pageOf2 = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Clan));
            using (IEnumerator<IFaction> enumerator3 = (from x in Campaign.Current.Factions
                                                        orderby !x.IsKingdomFaction
                                                        select x).ThenBy((IFaction f) => f.Name.ToString()).GetEnumerator())
            {
                while (enumerator3.MoveNext())
                {
                    IFaction factionObject = enumerator3.Current;
                    if (pageOf2.IsValidEncyclopediaItem(factionObject) && factionObject != this._faction && !factionObject.IsBanditFaction && FactionManager.IsAtWarAgainstFaction(this._faction, factionObject.MapFaction) && !this.Enemies.Any((KingdomInfoVM x) => x.Faction == factionObject.MapFaction))
                    {
                        this.Enemies.Add(new KingdomInfoVM(factionObject.MapFaction));
                    }
                }
            }
            foreach (Clan faction in from c in Campaign.Current.Clans
                                     where c.Kingdom == this._faction
                                     select c)
            {
                this.Clans.Add(new KingdomInfoVM(faction));
            }
            EncyclopediaPage pageOf3 = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Settlement));
            foreach (Settlement settlement in from s in Settlement.All
                                              where s.IsTown || s.IsCastle
                                              orderby s.IsCastle, s.IsTown
                                              select s)
            {
                if ((settlement.MapFaction == this._faction || (settlement.OwnerClan == this._faction.RulingClan && settlement.OwnerClan.Leader != null)) && pageOf3.IsValidEncyclopediaItem(settlement))
                {
                    this.Settlements.Add(new EncyclopediaSettlementVM(settlement));
                }
            }
            
        }

        
        public string GetName()
        {
            return this._faction.Name.ToString();
        }

        
        

        
        

        
        // (get) Token: 0x0600121E RID: 4638 RVA: 0x00047AD4 File Offset: 0x00045CD4
        // (set) Token: 0x0600121F RID: 4639 RVA: 0x00047ADC File Offset: 0x00045CDC
        [DataSourceProperty]
        public MBBindingList<KingdomInfoVM> Clans
        {
            get
            {
                return this._clans;
            }
            set
            {
                if (value != this._clans)
                {
                    this._clans = value;
                    base.OnPropertyChangedWithValue<MBBindingList<KingdomInfoVM>>(value, "Clans");
                }
            }
        }

        
        // (get) Token: 0x06001220 RID: 4640 RVA: 0x00047AFA File Offset: 0x00045CFA
        // (set) Token: 0x06001221 RID: 4641 RVA: 0x00047B02 File Offset: 0x00045D02
        [DataSourceProperty]
        public MBBindingList<KingdomInfoVM> Enemies
        {
            get
            {
                return this._enemies;
            }
            set
            {
                if (value != this._enemies)
                {
                    this._enemies = value;
                    base.OnPropertyChangedWithValue<MBBindingList<KingdomInfoVM>>(value, "Enemies");
                }
            }
        }

        
        // (get) Token: 0x06001222 RID: 4642 RVA: 0x00047B20 File Offset: 0x00045D20
        // (set) Token: 0x06001223 RID: 4643 RVA: 0x00047B28 File Offset: 0x00045D28
        [DataSourceProperty]
        public MBBindingList<EncyclopediaSettlementVM> Settlements
        {
            get
            {
                return this._settlements;
            }
            set
            {
                if (value != this._settlements)
                {
                    this._settlements = value;
                    base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaSettlementVM>>(value, "Settlements");
                }
            }
        }

        
        // (get) Token: 0x06001224 RID: 4644 RVA: 0x00047B46 File Offset: 0x00045D46
        // (set) Token: 0x06001225 RID: 4645 RVA: 0x00047B4E File Offset: 0x00045D4E
        [DataSourceProperty]
        public MBBindingList<EncyclopediaHistoryEventVM> History
        {
            get
            {
                return this._history;
            }
            set
            {
                if (value != this._history)
                {
                    this._history = value;
                    base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaHistoryEventVM>>(value, "History");
                }
            }
        }

        
        // (get) Token: 0x06001226 RID: 4646 RVA: 0x00047B6C File Offset: 0x00045D6C
        // (set) Token: 0x06001227 RID: 4647 RVA: 0x00047B74 File Offset: 0x00045D74
        [DataSourceProperty]
        public HeroVM Leader
        {
            get
            {
                return this._leader;
            }
            set
            {
                if (value != this._leader)
                {
                    this._leader = value;
                    base.OnPropertyChangedWithValue<HeroVM>(value, "Leader");
                }
            }
        }

        
        // (get) Token: 0x06001228 RID: 4648 RVA: 0x00047B92 File Offset: 0x00045D92
        // (set) Token: 0x06001229 RID: 4649 RVA: 0x00047B9A File Offset: 0x00045D9A
        [DataSourceProperty]
        public ImageIdentifierVM Banner
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
                    base.OnPropertyChangedWithValue<ImageIdentifierVM>(value, "Banner");
                }
            }
        }

        
        // (get) Token: 0x0600122A RID: 4650 RVA: 0x00047BB8 File Offset: 0x00045DB8
        // (set) Token: 0x0600122B RID: 4651 RVA: 0x00047BC0 File Offset: 0x00045DC0
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

        
        // (get) Token: 0x0600122C RID: 4652 RVA: 0x00047BE3 File Offset: 0x00045DE3
        // (set) Token: 0x0600122D RID: 4653 RVA: 0x00047BEB File Offset: 0x00045DEB
        [DataSourceProperty]
        public string MembersText
        {
            get
            {
                return this._membersText;
            }
            set
            {
                if (value != this._membersText)
                {
                    this._membersText = value;
                    base.OnPropertyChangedWithValue<string>(value, "MembersText");
                }
            }
        }

        
        // (get) Token: 0x0600122E RID: 4654 RVA: 0x00047C0E File Offset: 0x00045E0E
        // (set) Token: 0x0600122F RID: 4655 RVA: 0x00047C16 File Offset: 0x00045E16
        [DataSourceProperty]
        public string EnemiesText
        {
            get
            {
                return this._enemiesText;
            }
            set
            {
                if (value != this._enemiesText)
                {
                    this._enemiesText = value;
                    base.OnPropertyChangedWithValue<string>(value, "EnemiesText");
                }
            }
        }

        
        // (get) Token: 0x06001230 RID: 4656 RVA: 0x00047C39 File Offset: 0x00045E39
        // (set) Token: 0x06001231 RID: 4657 RVA: 0x00047C41 File Offset: 0x00045E41
        [DataSourceProperty]
        public string ClansText
        {
            get
            {
                return this._clansText;
            }
            set
            {
                if (value != this._clansText)
                {
                    this._clansText = value;
                    base.OnPropertyChangedWithValue<string>(value, "ClansText");
                }
            }
        }

        
        // (get) Token: 0x06001232 RID: 4658 RVA: 0x00047C64 File Offset: 0x00045E64
        // (set) Token: 0x06001233 RID: 4659 RVA: 0x00047C6C File Offset: 0x00045E6C
        [DataSourceProperty]
        public string SettlementsText
        {
            get
            {
                return this._settlementsText;
            }
            set
            {
                if (value != this._settlementsText)
                {
                    this._settlementsText = value;
                    base.OnPropertyChangedWithValue<string>(value, "SettlementsText");
                }
            }
        }

        
        // (get) Token: 0x06001234 RID: 4660 RVA: 0x00047C8F File Offset: 0x00045E8F
        // (set) Token: 0x06001235 RID: 4661 RVA: 0x00047C97 File Offset: 0x00045E97
        [DataSourceProperty]
        public string VillagesText
        {
            get
            {
                return this._villagesText;
            }
            set
            {
                if (value != this._villagesText)
                {
                    this._villagesText = value;
                    base.OnPropertyChangedWithValue<string>(value, "VillagesText");
                }
            }
        }

        
        // (get) Token: 0x06001236 RID: 4662 RVA: 0x00047CBA File Offset: 0x00045EBA
        // (set) Token: 0x06001237 RID: 4663 RVA: 0x00047CC2 File Offset: 0x00045EC2
        [DataSourceProperty]
        public string LeaderText
        {
            get
            {
                return this._leaderText;
            }
            set
            {
                if (value != this._leaderText)
                {
                    this._leaderText = value;
                    base.OnPropertyChangedWithValue<string>(value, "LeaderText");
                }
            }
        }

        
        // (get) Token: 0x06001238 RID: 4664 RVA: 0x00047CE5 File Offset: 0x00045EE5
        // (set) Token: 0x06001239 RID: 4665 RVA: 0x00047CED File Offset: 0x00045EED
        [DataSourceProperty]
        public string DescriptorText
        {
            get
            {
                return this._descriptorText;
            }
            set
            {
                if (value != this._descriptorText)
                {
                    this._descriptorText = value;
                    base.OnPropertyChangedWithValue<string>(value, "DescriptorText");
                }
            }
        }

        
        // (get) Token: 0x0600123A RID: 4666 RVA: 0x00047D10 File Offset: 0x00045F10
        // (set) Token: 0x0600123B RID: 4667 RVA: 0x00047D18 File Offset: 0x00045F18
        [DataSourceProperty]
        public string InformationText
        {
            get
            {
                return this._informationText;
            }
            set
            {
                if (value != this._informationText)
                {
                    this._informationText = value;
                    base.OnPropertyChangedWithValue<string>(value, "InformationText");
                }
            }
        }

        
        // (get) Token: 0x0600123C RID: 4668 RVA: 0x00047D3B File Offset: 0x00045F3B
        // (set) Token: 0x0600123D RID: 4669 RVA: 0x00047D43 File Offset: 0x00045F43
        [DataSourceProperty]
        public string ProsperityText
        {
            get
            {
                return this._prosperityText;
            }
            set
            {
                if (value != this._prosperityText)
                {
                    this._prosperityText = value;
                    base.OnPropertyChangedWithValue<string>(value, "ProsperityText");
                }
            }
        }

        
        // (get) Token: 0x0600123E RID: 4670 RVA: 0x00047D66 File Offset: 0x00045F66
        // (set) Token: 0x0600123F RID: 4671 RVA: 0x00047D6E File Offset: 0x00045F6E
        [DataSourceProperty]
        public string StrengthText
        {
            get
            {
                return this._strengthText;
            }
            set
            {
                if (value != this._strengthText)
                {
                    this._strengthText = value;
                    base.OnPropertyChangedWithValue<string>(value, "StrengthText");
                }
            }
        }

        
        // (get) Token: 0x06001240 RID: 4672 RVA: 0x00047D91 File Offset: 0x00045F91
        // (set) Token: 0x06001241 RID: 4673 RVA: 0x00047D99 File Offset: 0x00045F99
        [DataSourceProperty]
        public HintViewModel ProsperityHint
        {
            get
            {
                return this._prosperityHint;
            }
            set
            {
                if (value != this._prosperityHint)
                {
                    this._prosperityHint = value;
                    base.OnPropertyChangedWithValue<HintViewModel>(value, "ProsperityHint");
                }
            }
        }

        
        // (get) Token: 0x06001242 RID: 4674 RVA: 0x00047DB7 File Offset: 0x00045FB7
        // (set) Token: 0x06001243 RID: 4675 RVA: 0x00047DBF File Offset: 0x00045FBF
        [DataSourceProperty]
        public HintViewModel StrengthHint
        {
            get
            {
                return this._strengthHint;
            }
            set
            {
                if (value != this._strengthHint)
                {
                    this._strengthHint = value;
                    base.OnPropertyChangedWithValue<HintViewModel>(value, "StrengthHint");
                }
            }
        }

        
        private Kingdom _faction;

        
        private MBBindingList<KingdomInfoVM> _clans;

        
        private MBBindingList<KingdomInfoVM> _enemies;

        
        private MBBindingList<EncyclopediaSettlementVM> _settlements;

        
        private MBBindingList<EncyclopediaHistoryEventVM> _history;

        
        private HeroVM _leader;

        
        private ImageIdentifierVM _banner;

        
        private string _membersText;

        
        private string _enemiesText;

        
        private string _clansText;

        
        private string _settlementsText;

        
        private string _villagesText;

        
        private string _leaderText;

        
        private string _descriptorText;

        
        private string _prosperityText;

        
        private string _strengthText;

        
        private string _informationText;

        
        private HintViewModel _prosperityHint;

        
        private HintViewModel _strengthHint;

        
        private string _nameText;
    }
}
