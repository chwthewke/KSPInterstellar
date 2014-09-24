using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InterstellarPlugin.PartUpgrades
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT, GameScenes.EDITOR, GameScenes.SPH)]
    public class PartUpgradeScenario : ScenarioModule
    {
        public const string ScenarioName = "PartUpgrades";

        public EventData<string, Part> onUpgradeUnlock = new EventData<string, Part>("onUpgradeUnlock");

        private const string UnlockedKey = "unlocked";

        private readonly ICollection<string> unlockedUpgrades = new HashSet<string>();


        public static PartUpgradeScenario Instance
        {
            get
            {
                var game = HighLogic.CurrentGame;
                if (game == null)
                    return null;
                return game.scenarios
                    .Select(s => s.moduleRef)
                    .OfType<PartUpgradeScenario>()
                    .FirstOrDefault();
            }
        }


        public void Unlock(UpgradeModule module)
        {
            var wasUnlocked = IsUnlocked(module);
            unlockedUpgrades.Add(module.id);
            if (!wasUnlocked)
                onUpgradeUnlock.Fire(module.id, module.part);
        }

        public bool IsUnlocked(UpgradeModule module)
        {
            return unlockedUpgrades.Contains(module.id);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            node.ClearData();

            foreach (var unlocked in unlockedUpgrades)
            {
                node.AddValue(UnlockedKey, unlocked);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            foreach (var unlocked in node.GetValues(UnlockedKey))
            {
                unlockedUpgrades.Add(unlocked);
            }

#if DEBUG
            Debug.Log("[Interstellar] PartUpgradeScenario loaded with unlocked upgrades: " +
                string.Join(", ", unlockedUpgrades.ToArray()));
#endif
        }
    }

}
