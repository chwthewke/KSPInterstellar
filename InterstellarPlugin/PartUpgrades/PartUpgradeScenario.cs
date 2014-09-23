﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InterstellarPlugin.PartUpgrades
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT, GameScenes.EDITOR, GameScenes.SPH)]
    public class PartUpgradeScenario : ScenarioModule
    {
        public const string ScenarioName = "PartUpgrades";

        internal const string UnlockedKey = "unlocked";

        private readonly ICollection<string> unlockedUpgrades = new HashSet<string>();

        private static PartUpgradeScenario instance;

        public static PartUpgradeScenario Instance
        {
            get { return instance; }
        }

        public override void OnAwake()
        {
            if (instance != null)
                return;
            instance = this;
        }

        public void Unlock(UpgradeModule module)
        {
            unlockedUpgrades.Add(module.id);
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
