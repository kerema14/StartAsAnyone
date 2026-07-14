using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace StartAsAnyone
{
    // Token: 0x020000D4 RID: 212
    public class CharacterCreationHeroRelationComparer : IComparer<CharacterCreationHeroVM>
    {
        // Token: 0x06001482 RID: 5250 RVA: 0x00051E22 File Offset: 0x00050022
        public CharacterCreationHeroRelationComparer(Hero pageHero, bool isAscending, bool showLeadersFirst)
        {
            this._pageHero = pageHero;
            this._isAscending = isAscending;
            this._showLeadersFirst = showLeadersFirst;
        }

        // Token: 0x06001483 RID: 5251 RVA: 0x00051E40 File Offset: 0x00050040
        int IComparer<CharacterCreationHeroVM>.Compare(CharacterCreationHeroVM x, CharacterCreationHeroVM y)
        {
            int num;
            if (this._showLeadersFirst)
            {
                num = y.Hero.IsKingdomLeader.CompareTo(x.Hero.IsKingdomLeader);
                if (num != 0)
                {
                    return num;
                }
            }
            int relation = this._pageHero.GetRelation(x.Hero);
            int relation2 = this._pageHero.GetRelation(y.Hero);
            num = relation.CompareTo(relation2) * (this._isAscending ? 1 : -1);
            if (num == 0)
            {
                num = x.NameText.CompareTo(y.NameText);
            }
            return num;
        }

        // Token: 0x04000965 RID: 2405
        private readonly Hero _pageHero;

        // Token: 0x04000966 RID: 2406
        private readonly bool _isAscending;

        // Token: 0x04000967 RID: 2407
        private readonly bool _showLeadersFirst;
    }
}
