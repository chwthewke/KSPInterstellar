using System;
using System.Collections.Generic;
using System.Linq;
#if DEBUG
using System.Runtime.CompilerServices;
#endif
using UnityEngine;

namespace InterstellarPlugin.PartUpgrades
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT, GameScenes.EDITOR, GameScenes.SPH)]
    public class PartUpgrades : ScenarioModule
    {
        public const string ModName = "PartUpgradeToolkit";

        public static readonly Action<Func<string>> LogDebug =
#if DEBUG
            s => Debug.Log(s());
#else
            s => {};
#endif

        public EventData<string, Part> onUpgradeUnlock = new EventData<string, Part>("onUpgradeUnlock");

        private const string UnlockedKey = "unlocked";
        private const string ScenarioName = "PartUpgrades";
        private const string NameKey = "name";

        private readonly ICollection<string> unlockedUpgrades = new HashSet<string>();


        public static PartUpgrades Instance
        {
            get
            {
                var game = HighLogic.CurrentGame;
                if (game == null)
                    return null;
                return game.scenarios
                    .Select(s => s.moduleRef)
                    .OfType<PartUpgrades>()
                    .FirstOrDefault();
            }
        }


        public void Unlock(UpgradeModule module)
        {
            var wasUnlocked = IsUnlocked(module);
            unlockedUpgrades.Add(module.id);
            if (!wasUnlocked)
                onUpgradeUnlock.Fire(module.id, module.part);

            LogDebug(() => string.Format("[{0}] {1} Upgrade unlocked: {2} for {3}", ModName, GetType().Name, module.id, module.part.OriginalName()));
        }

        public bool IsUnlocked(UpgradeModule module)
        {
            return unlockedUpgrades.Contains(module.id);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            node.RemoveValues(UnlockedKey);

            foreach (var unlocked in unlockedUpgrades)
                node.AddValue(UnlockedKey, unlocked);

            LogDebug(() => string.Format("[{0}] {3} ({2}) saved with unlocked upgrades: {1} -> <{4}>.",
                ModName, UpgradesText(), RuntimeHelpers.GetHashCode(this), GetType().Name, node));
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            foreach (var unlocked in node.GetValues(UnlockedKey))
                unlockedUpgrades.Add(unlocked);

            LogDebug(() => string.Format("[{0}] {3} ({2}) loaded with unlocked upgrades: {1}.",
                ModName, UpgradesText(), RuntimeHelpers.GetHashCode(this), GetType().Name));
        }

        private string UpgradesText()
        {
            return string.Join(", ", unlockedUpgrades.ToArray());
        }

    }

}
