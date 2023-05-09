using Concubines.Models;
using MCM.Abstractions.Base.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;

namespace Concubines.Extensions {
    public static class HeroExtensions {
        public static ConcubineList? IsParamour(this Hero callingHero) {
            ConcubineList? data = null;
            try {
                data = ConcubineCampaignBehavior.Instance.ConcubineData.First(x => x.Hero == callingHero);
            }
            catch (Exception) { }
            return data;
        }

        public static void BecomeConcubineOf(this Hero callingHero, Hero otherHero) {
            ConcubineList data = ConcubineList.GetFor(otherHero);

            if (callingHero.Clan != null && callingHero.Clan.Leader != otherHero && !otherHero.IsFriend(callingHero.Clan.Leader))
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(otherHero, callingHero.Clan.Leader, -25);

            MobileParty? party = callingHero.PartyBelongedTo;
            if (otherHero == Hero.MainHero) {
                if (party != null && party.LeaderHero == callingHero) {
                    MergePartiesAction.Apply(MobileParty.MainParty.Party, party.Party);
                } else {
                    AddHeroToPartyAction.Apply(callingHero, otherHero.PartyBelongedTo);
                }
            } else {
                if (party != null && party.LeaderHero != callingHero && party.MapFaction != otherHero.MapFaction) {
                    party.MemberRoster.RemoveTroop(callingHero.CharacterObject);
                }
            }

            data.AddConcubine(callingHero);

            if (callingHero.Occupation == Occupation.Wanderer) {
                callingHero.CompanionOf = null;
            }

            Utils.PrintToMessages("{=MessageConcubineTakenByParamour}{PARAMOUR} took {CONCUBINE} as a concubine.", 255, 229, 204,
                ("PARAMOUR", otherHero.Name.ToString()), ("CONCUBINE", callingHero.Name.ToString()));
        }

        public static void ConcubineBecomeSpouse(this Hero callingHero) {
            Hero? concubineOf = callingHero.ConcubineOf();
            if (!callingHero.DisposeAsConcubine(true, true))
                return;

            callingHero.SetNewOccupation(Occupation.Lord);
            callingHero.Spouse = concubineOf;
        }

        public static bool AIWouldTakeConcubine(this Hero callingHero, Hero testingHero, int randNum) {
            if (testingHero.Age >= 40)
                return false;

            if (callingHero.Spouse != null && (callingHero.GetTraitLevel(DefaultTraits.Honor) >= 1 || callingHero.GetTraitLevel(DefaultTraits.Mercy) >= 1 ||
                callingHero.GetBaseHeroRelation(callingHero.Spouse) >= 30))
                return false;

            if (callingHero.IsFemale && randNum <= 90)
                return false;

            if (callingHero.Culture != testingHero.Culture && randNum <= 75)
                return false;

            int relationMod = (int)Math.Round((double)callingHero.GetBaseHeroRelation(testingHero) / 4);
            if (Campaign.Current.Models.RomanceModel.GetAttractionValuePercentage(callingHero, testingHero) + relationMod < GlobalSettings<MCMConfig>.Instance.AttractionRelationNeededAI ||
                Campaign.Current.Models.RomanceModel.GetAttractionValuePercentage(testingHero, callingHero) + relationMod < GlobalSettings<MCMConfig>.Instance.AttractionRelationNeededAI)
                return false;

            ConcubineList? data = callingHero.IsParamour();
            if (data != null && data.Concubines.Count >= 2)
                return false;

            return true;
        }

        public static bool SuitableToBeConcubineOf(this Hero callingHero, Hero testingHero) {
            // CONFIG LATER
            if (callingHero.IsFemale == testingHero.IsFemale)
                return false;

            if (callingHero == Hero.MainHero)
                return false;

            if (callingHero.Clan == null || testingHero.Clan == null)
                return false;

            if ((callingHero.Clan.IsMinorFaction && testingHero.Clan != Hero.MainHero.Clan) || (testingHero.Clan.IsMinorFaction && testingHero.Clan != Hero.MainHero.Clan))
                return false;

            if (callingHero.Clan.Leader == callingHero)
                return false;

            if (callingHero.Spouse != null)
                return false;

            if (callingHero.Age < 18f || testingHero.Age < 18f)
                return false;

            if (callingHero.ConcubineOf() != null || testingHero.ConcubineOf() != null || callingHero.IsParamour() != null)
                return false;

            if (BastardChildren.StaticUtils.Utils.HerosRelated(callingHero, testingHero))
                return false;

            return true;
        }

        public static bool WouldBecomeConcubineOfPlayer(this Hero callingHero) {
            int totalValue = Campaign.Current.Models.RomanceModel.GetAttractionValuePercentage(callingHero, Hero.MainHero) + callingHero.GetBaseHeroRelation(Hero.MainHero);
            if (totalValue >= GlobalSettings<MCMConfig>.Instance.AttractionRelationNeededPlayer)
                return true;
            return false;
        }

        public static Hero? ConcubineOf(this Hero callingHero) {
            try {
                ConcubineList? data = ConcubineCampaignBehavior.Instance.ConcubineData.First(c => c.Concubines.ContainsKey(callingHero));
                if (data != null)
                    return data.Hero;
            }
            catch (Exception) { }
            return null;
        }

        public static bool DisposeAsConcubine(this Hero callingHero, bool dontSendToOldClan = false, bool dontChangeRelation = false) {
            Hero? concubineOf = callingHero.ConcubineOf();
            if (concubineOf != null) {
                ConcubineList data = ConcubineList.GetFor(concubineOf);
                data.RemoveConcubine(callingHero, dontSendToOldClan);

                if (callingHero.Clan != concubineOf.Clan) {
                    if (callingHero.PartyBelongedTo != null && callingHero.PartyBelongedTo.LeaderHero == callingHero)
                        EnterSettlementAction.ApplyForParty(callingHero.PartyBelongedTo, callingHero.HomeSettlement);
                    else
                        TeleportHeroAction.ApplyImmediateTeleportToSettlement(callingHero, callingHero.HomeSettlement);
                }

                if (!dontChangeRelation)
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(concubineOf, callingHero, -25);

                return true;
            }
            return false;
        }
    }
}