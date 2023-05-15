using Concubines.Extensions;
using Concubines.Models;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Library;

namespace Concubines.Patches {
    [HarmonyPatch(typeof(ConversationHelper), nameof(ConversationHelper.GetHeroRelationToHeroTextShort))]
    internal class EncyclopediaLabelPatch {
        [HarmonyPostfix]
        private static void Postfix(ref string __result, Hero queriedHero, Hero baseHero) {
            Hero? concubineTo = queriedHero.ConcubineOf();
            if (concubineTo != null && concubineTo == baseHero) {
                __result = Utils.GetLocalizedString("{=EncyclopediaConcubineTag}Concubine");
                return;
            }

            ConcubineList? data = queriedHero.IsParamour();
            if (data != null && data.Concubines.Keys.Contains(baseHero)) {
                __result = Utils.GetLocalizedString("{=EncyclopediaParamourTag}Paramour");
                return;
            }
        }
    }

    [HarmonyPatch(typeof(EncyclopediaHeroPageVM), nameof(EncyclopediaHeroPageVM.Refresh))]
    internal class EncyclopediaPagePatch {
        [HarmonyPostfix]
        private static void Postfix(EncyclopediaHeroPageVM __instance) {
            Hero hero = (Hero)AccessTools.Field(typeof(EncyclopediaHeroPageVM), "_hero").GetValue(__instance);
            MBBindingList<EncyclopediaFamilyMemberVM> family = (MBBindingList<EncyclopediaFamilyMemberVM>)AccessTools.Field(typeof(EncyclopediaHeroPageVM), "_family").GetValue(__instance);
            if (hero == null || family == null)
                return;

            Hero? concubineOf = hero.ConcubineOf();
            if (concubineOf != null && !family.Any(x => x.Hero == concubineOf))
                family.Add(new EncyclopediaFamilyMemberVM(concubineOf, hero));

            ConcubineList? data = hero.IsParamour();
            if (data != null)
                foreach (Hero concubine in data.Concubines.Keys)
                    if (!family.Any(x => x.Hero == concubine))
                        family.Add(new EncyclopediaFamilyMemberVM(concubine, hero));
        }
    }
}
