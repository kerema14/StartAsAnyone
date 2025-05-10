using StartAsAnyone;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Library;

namespace StartAsAnyone
{
    // Token: 0x020000CB RID: 203
    public class CharacterCreationHeroFamilyVM : CharacterCreationHeroVM
    {
        // Token: 0x060013A6 RID: 5030 RVA: 0x0004BA86 File Offset: 0x00049C86
        public CharacterCreationHeroFamilyVM(Hero hero, Hero baseHero) : base(hero, false)
        {
            this._baseHero = baseHero;
            this.RefreshValues();
        }

        // Token: 0x060013A7 RID: 5031 RVA: 0x0004BA9D File Offset: 0x00049C9D
        public override void RefreshValues()
        {
            base.RefreshValues();
            if (this._baseHero != null)
            {
                this.Role = ConversationHelper.GetHeroRelationToHeroTextShort(base.Hero, this._baseHero, true);
            }
        }

        // Token: 0x17000692 RID: 1682
        // (get) Token: 0x060013A8 RID: 5032 RVA: 0x0004BAC5 File Offset: 0x00049CC5
        // (set) Token: 0x060013A9 RID: 5033 RVA: 0x0004BACD File Offset: 0x00049CCD
        [DataSourceProperty]
        public string Role
        {
            get
            {
                return this._role;
            }
            set
            {
                if (value != this._role)
                {
                    this._role = value;
                    base.OnPropertyChangedWithValue<string>(value, "Role");
                }
            }
        }

        // Token: 0x04000916 RID: 2326
        private readonly Hero _baseHero;

        // Token: 0x04000917 RID: 2327
        private string _role;
    }
}
