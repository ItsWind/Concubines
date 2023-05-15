using SandBox;
using SandBox.Conversation;
using SandBox.Missions.AgentBehaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace Concubines.Models {
    public class ConcubineList {
        [SaveableField(1)]
        public Hero Hero;
        [SaveableField(2)]
        public Dictionary<Hero, Clan> Concubines = new();

        public static ConcubineList GetFor(Hero hero) {
            ConcubineList data;
            try {
                data = ConcubineCampaignBehavior.Instance.ConcubineData.First(c => c.Hero == hero);
            }
            catch (Exception) {
                data = new ConcubineList(hero);
            }
            return data;
        }

        public ConcubineList(Hero heroFor) {
            Hero = heroFor;

            ConcubineCampaignBehavior.Instance.ConcubineData.Add(this);
        }

        public void AddConcubine(Hero concubine) {
            if (Concubines.ContainsKey(concubine))
                return;

            Clan toReturnTo = concubine.Clan;
            if (concubine.Occupation == Occupation.Wanderer) {
                concubine.CompanionOf = null;
                concubine.SetNewOccupation(Occupation.Lord);
            }
            Concubines[concubine] = toReturnTo;

            concubine.Clan = Hero.Clan;
        }

        public void RemoveConcubine(Hero concubine, bool dontSendToOldClan = false) {
            if (!Concubines.ContainsKey(concubine))
                return;

            Clan clanToReturnTo = Concubines[concubine];

            if (!dontSendToOldClan)
                concubine.Clan = clanToReturnTo;

            Concubines.Remove(concubine);

            if (Concubines.Count <= 0)
                Delete();
        }

        public void Delete(bool completely = false, bool dontSendToOldClan = false) {
            if (completely)
                foreach (Hero concubine in Concubines.Keys.ToList())
                    RemoveConcubine(concubine, dontSendToOldClan);
            else
                ConcubineCampaignBehavior.Instance.ConcubineData.Remove(this);
        }
    }
}
