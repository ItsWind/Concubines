using HarmonyLib;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Concubines {
    public class SubModule : MBSubModuleBase {
        public static Random Random = new();

        protected override void OnSubModuleLoad() {
            new Harmony("Concubines").PatchAll();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter) {
            if (game.GameType is Campaign) {
                CampaignGameStarter campaignStarter = (CampaignGameStarter)gameStarter;

                campaignStarter.AddBehavior(new ConcubineCampaignBehavior(campaignStarter));
            }
        }
    }
}