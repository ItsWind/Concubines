using BastardChildren.Models;
using Concubines.Models;
using Concubines.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using MCM.Abstractions.Base.Global;

namespace Concubines {
    public class ConcubineCampaignBehavior : CampaignBehaviorBase {
        public static ConcubineCampaignBehavior Instance;

        public List<ConcubineList> ConcubineData = new();

        public ConcubineCampaignBehavior(CampaignGameStarter starter) {
            Instance = this;

            AddDialogs(starter);
        }

        public override void RegisterEvents() {
            CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, DailyTickHero);
            CampaignEvents.BeforeHeroKilledEvent.AddNonSerializedListener(this, OnHeroDiedOrDisappeared);
        }

        public override void SyncData(IDataStore dataStore) {
            dataStore.SyncData("ConcubineData", ref ConcubineData);

            // BUG FIX
            if (dataStore.IsLoading) {
                foreach (ConcubineList data in ConcubineData.ToList()) {
                    if (!Campaign.Current.AliveHeroes.Contains(data.Hero)) {
                        data.Delete(true);
                        return;
                    }

                    foreach (Hero concubine in data.Concubines.Keys.ToList())
                        if (concubine.Occupation != Occupation.Lord)
                            concubine.SetNewOccupation(Occupation.Lord);
                }
            }
        }

        private void DailyTickHero(Hero hero) {
            if (!hero.IsLord || (hero.IsFemale && hero.IsPregnant))
                return;

            ParamourDailyTick(hero);
            HeroSearchForConcubinesDailyTick(hero);
        }

        private void ParamourDailyTick(Hero hero) {
            ConcubineList? data = hero.IsParamour();
            if (data == null)
                return;

            if (data.Concubines.Count <= 0) {
                data.Delete();
                return;
            }

            foreach (Hero concubine in data.Concubines.Keys) {
                if (concubine.IsFemale == hero.IsFemale)
                    continue;

                Hero femaleHero = hero.IsFemale ? hero : concubine;
                if (femaleHero.IsPregnant)
                    if (hero == femaleHero)
                        break;
                    else if (concubine == femaleHero)
                        continue;
                Hero maleHero = hero.IsFemale ? concubine : hero;

                if (CheckAreNearby(hero, concubine) && MBRandom.RandomFloat <= GetDailyChanceOfPregnancyForConcubine(femaleHero, maleHero))
                    new Bastard(maleHero, femaleHero);
            }
        }

        private void HeroSearchForConcubinesDailyTick(Hero hero) {
            if (!GlobalSettings<MCMConfig>.Instance.EnableAIConcubines || hero == Hero.MainHero)
                return;

            int randNum = SubModule.Random.Next(1, 101);
            if (randNum > 5)
                return;

            List<Hero> nearbyHeroes = Campaign.Current.AliveHeroes.Where(h => CheckAreNearby(h, hero)).ToList();
            foreach (Hero nearbyHero in nearbyHeroes) {
                if (nearbyHero == Hero.MainHero || nearbyHero == hero)
                    continue;
                if (!nearbyHero.SuitableToBeConcubineOf(hero))
                    continue;

                if (hero.AIWouldTakeConcubine(nearbyHero, randNum))
                    nearbyHero.BecomeConcubineOf(hero);
            }
        }

        private void OnHeroDiedOrDisappeared(Hero hero, Hero hero2, KillCharacterAction.KillCharacterActionDetail detail, bool someBool) {
            // Remove if concubine
            hero.DisposeAsConcubine(true, true);

            // Remove if paramour
            ConcubineList? data = hero.IsParamour();
            if (data != null)
                data.Delete(true, true);
        }

        private void AddDialogs(CampaignGameStarter starter) {
            // hero_main_options = self explanatory
            // lord_pretalk = make them ask "anything else?"
            // close_window = EXIT // seems to cause attack bug when done on map, so avoid.
            // lord_talk_speak_diplomacy_2 = "There is something I'd like to discuss."

            // NOBLE CONCUBINE ASK

            starter.AddPlayerLine("ConcubineAsk", "hero_main_options", "ConcubineAskOutput",
                "{=ConcubineAsk}I'm not a firm believer in marriage. Are you?",
            () => Hero.OneToOneConversationHero.SuitableToBeConcubineOf(Hero.MainHero), null, 500);

            starter.AddDialogLine("ConcubineAskConfirm", "ConcubineAskOutput", "ConcubineAskConfirmOutput",
                "{=ConcubineAskConfirm}What..?",
            null, null, 100);

            starter.AddPlayerLine("ConcubineAskConfirmYes", "ConcubineAskConfirmOutput", "ConcubineAskConfirmedComment",
                "{=ConcubineAskConfirmYes}Would you like to be bound to me by something more than just marriage?",
            null, null, 100, (out TextObject explain) => {
                explain = new TextObject("{=ConcubineAskConfirmYesExplanation}Taking a concubine will harm relations with their clan leader, if they have one, if you are not their friend!");
                return true;
            });
            starter.AddPlayerLine("ConcubineAskConfirmNo", "ConcubineAskConfirmOutput", "lord_pretalk",
                "{=ConcubineAskConfirmNo}Oh, it's nothing..",
            null, null, 99);

            starter.AddDialogLine("ConcubineAskAccepted", "ConcubineAskConfirmedComment", "lord_pretalk",
                "{=ConcubineAskAccepted}Yes, I think I would like that.[rf:positive, rb:unsure]",
            () => Hero.OneToOneConversationHero.WouldBecomeConcubineOfPlayer(), () => Hero.OneToOneConversationHero.BecomeConcubineOf(Hero.MainHero), 100);
            starter.AddDialogLine("ConcubineAskDeclined", "ConcubineAskConfirmedComment", "lord_pretalk",
                "{=ConcubineAskDeclined}Absolutely not.[rf:very_negative_ag, rb:negative]",
            null, null, 99);

            // NOBLE CONCUBINE DISPOSE

            starter.AddPlayerLine("ConcubineDispose", "hero_main_options", "ConcubineDisposeOutput",
                "{=ConcubineDispose}I think it's time for us to split apart.",
            () => Hero.OneToOneConversationHero.ConcubineOf() == Hero.MainHero, null, 500);

            starter.AddDialogLine("ConcubineDisposeConfirm", "ConcubineDisposeOutput", "ConcubineDisposeConfirmOutput",
                "{=ConcubineDisposeConfirm}What? Why?[rf:very_negative_ag, rb:negative]",
            null, null, 100);

            starter.AddPlayerLine("ConcubineDisposeConfirmYes", "ConcubineDisposeConfirmOutput", "ConcubineDisposeConfirmedComment",
                "{=ConcubineDisposeConfirmYes}It's time to part ways.",
            null, null, 100, (out TextObject explain) => {
                explain = new TextObject("{=ConcubineDisposeConfirmYesExplanation}Disposing of a concubine will harm relations with the concubine!");
                return true;
            });
            starter.AddPlayerLine("ConcubineDisposeConfirmNo", "ConcubineDisposeConfirmOutput", "lord_pretalk",
                "{=ConcubineDisposeConfirmNo}I think maybe I should give it some more thought.",
            null, null, 99);

            starter.AddDialogLine("ConcubineDisposeConfirmed", "ConcubineDisposeConfirmedComment", "close_window",
                "{=ConcubineDisposeConfirmed}If you insist. Farewell.[rf:very_negative_ag, rb:negative]",
            null, () => Hero.OneToOneConversationHero.DisposeAsConcubine(), 100);

            // CONCUBINE MARRY

            starter.AddPlayerLine("ConcubineMarry", "hero_main_options", "ConcubineMarryOutput",
                "{=ConcubineMarry}Will you marry me?",
            () => Hero.OneToOneConversationHero.ConcubineOf() == Hero.MainHero && Hero.MainHero.Spouse == null, null, 600);

            starter.AddDialogLine("ConcubineMarryConfirm", "ConcubineMarryOutput", "ConcubineMarryConfirmOutput",
                "{=ConcubineMarryConfirm}What?[rf:positive, rb:unsure]",
            null, null, 100);

            starter.AddPlayerLine("ConcubineMarryConfirmYes", "ConcubineMarryConfirmOutput", "ConcubineMarryConfirmedComment",
                "{=ConcubineMarryConfirmYes}Marry me and be bound to me by oath.",
            null, () => Hero.OneToOneConversationHero.ConcubineBecomeSpouse(), 100, null);
            starter.AddPlayerLine("ConcubineMarryConfirmNo", "ConcubineMarryConfirmOutput", "lord_pretalk",
                "{=ConcubineMarryConfirmNo}I think maybe I should give it some more thought.",
            null, null, 99);

            starter.AddDialogLine("ConcubineMarryConfirmed", "ConcubineMarryConfirmedComment", "close_window",
                "{=ConcubineMarryConfirmed}Let us prepare then![rf:positive, rb:unsure]",
            null, null, 100);
        }

        private float GetDailyChanceOfPregnancyForConcubine(Hero concubine, Hero lover) {
            int num = concubine.Children.Count + 1;
            float num2 = (float)(4 + 4 * concubine.Clan.Tier);
            float num3 = (concubine != Hero.MainHero) ? Math.Min(1f, (2f * num2 - (float)concubine.Clan.Lords.Count) / num2) : 1f;
            float num4 = (1.2f - (concubine.Age - 18f) * 0.04f) / (float)(num * num) * 0.12f * num3;
            float baseNumber = concubine.Age >= 18f && concubine.Age <= 45f ? num4 : 0f;
            ExplainedNumber explainedNumber = new ExplainedNumber(baseNumber, false, null);
            if (concubine.GetPerkValue(DefaultPerks.Charm.Virile) || lover.GetPerkValue(DefaultPerks.Charm.Virile)) {
                explainedNumber.AddFactor(DefaultPerks.Charm.Virile.PrimaryBonus, DefaultPerks.Charm.Virile.Name);
            }
            return explainedNumber.ResultNumber;
        }

        private bool CheckAreNearby(Hero hero, Hero concubine) {
            if (hero.Clan != Hero.MainHero.Clan && MBRandom.RandomFloat < 0.2f)
                return true;

            Settlement settlement;
            MobileParty mobileParty;
            GetLocation(hero, out settlement, out mobileParty);
            Settlement settlement2;
            MobileParty mobileParty2;
            GetLocation(concubine, out settlement2, out mobileParty2);
            return (settlement != null && settlement == settlement2) || (mobileParty != null && mobileParty == mobileParty2);
        }

        private void GetLocation(Hero hero, out Settlement heroSettlement, out MobileParty heroParty) {
            heroSettlement = hero.CurrentSettlement;
            heroParty = hero.PartyBelongedTo;
            MobileParty mobileParty = heroParty;
            if ((mobileParty != null ? mobileParty.AttachedTo : null) != null) {
                heroParty = heroParty.AttachedTo;
            }
            if (heroSettlement == null) {
                MobileParty mobileParty2 = heroParty;
                heroSettlement = mobileParty2 != null ? mobileParty2.CurrentSettlement : null;
            }
        }
    }
    public class CustomSaveDefiner : SaveableTypeDefiner {
        public CustomSaveDefiner() : base(200882710) { }

        protected override void DefineClassTypes() {
            AddClassDefinition(typeof(ConcubineList), 1);
        }

        protected override void DefineContainerDefinitions() {
            ConstructContainerDefinition(typeof(List<ConcubineList>));
            ConstructContainerDefinition(typeof(Dictionary<Hero, Clan>));
        }
    }
}
