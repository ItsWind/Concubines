using Concubines.Models;
using Concubines.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Settlements;

namespace Concubines {
    public class ConsoleCommands {
        [CommandLineFunctionality.CommandLineArgumentFunction("debug_clear_npc_concubines", "concubines")]
        private static string DebugClearNPCConcubines(List<string> args) {
            foreach (ConcubineList data in ConcubineCampaignBehavior.Instance.ConcubineData.ToList()) {
                Hero heroFor = data.Hero;
                if (heroFor == Hero.MainHero)
                    continue;

                foreach (Hero concubine in data.Concubines.Keys.ToList())
                    concubine.DisposeAsConcubine();

                if (!ConcubineCampaignBehavior.Instance.ConcubineData.Contains(data))
                    Utils.PrintToMessages(heroFor.Name.ToString() + " CLEARED");
            }

            return "we good.";
        }


        [CommandLineFunctionality.CommandLineArgumentFunction("debug_clear_bad_governors", "concubines")]
        private static string DebugClearBadGovernors(List<string> args) {
            foreach (Hero hero in Campaign.Current.AliveHeroes) {
                Town? governorTown = hero.GovernorOf;
                if (governorTown != null) {
                    if (hero.Clan != governorTown.OwnerClan)
                        ChangeGovernorAction.RemoveGovernorOf(hero);
                }
            }

            return "we good.";
        }
    }
}
