using StartAsAnyone;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Library;

namespace StartAsAnyone
{

    public class CharacterCreationHeroFamilyVM : CharacterCreationHeroVM
    {

        public CharacterCreationHeroFamilyVM(Hero hero, Hero baseHero) : base(hero, false)
        {
            this._baseHero = baseHero;
            this.RefreshValues();
        }


        public override void RefreshValues()
        {
            base.RefreshValues();
            if (this._baseHero != null)
            {
                this.Role = ConversationHelper.GetHeroRelationToHeroTextShort(base.Hero, this._baseHero, true);
            }
        }


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
