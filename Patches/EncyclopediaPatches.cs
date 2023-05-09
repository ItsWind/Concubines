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
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;

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

    [HarmonyPatch(typeof(EncyclopediaHeroPageVM), nameof(EncyclopediaHeroPageVM.RefreshValues))]
    internal class EncyclopediaPagePatch {
        [HarmonyPostfix]
        private static void Postfix(EncyclopediaHeroPageVM __instance) {
            Hero hero = (Hero)AccessTools.Field(typeof(HeroViewModel), "_hero").GetValue(__instance.HeroCharacter);
            List<Hero> relatedHeroes = (List<Hero>)AccessTools.Field(typeof(EncyclopediaHeroPageVM), "_allRelatedHeroes").GetValue(__instance);
            if (hero == null || relatedHeroes == null)
                return;

            Hero? concubineOf = hero.ConcubineOf();
            if (concubineOf != null && !relatedHeroes.Contains(concubineOf))
                relatedHeroes.Add(concubineOf);

            ConcubineList? data = hero.IsParamour();
            if (data != null)
                foreach (Hero concubine in data.Concubines.Keys)
                    if (!relatedHeroes.Contains(concubine))
                        relatedHeroes.Add(concubine);
        }
    }
}
