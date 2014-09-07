using System;
using System.Collections.Generic;
using System.Linq;

namespace InterstellarPlugin
{
    [KSPModule("KSPIUpgradeShim")]
    public class FNUpgradeModule : PartModule
    {
        [KSPField(isPersistant = true)]
        public string upgradedModules = "";

        private HashSet<string> upgradedModulesSet = new HashSet<string>();

        private EventData<ShipConstruct>.OnEvent onShipModified;

        private HashSet<string> UpgradedModulesSet
        {
            get { return upgradedModulesSet; }
            set
            {
                upgradedModulesSet = value;
                upgradedModules = string.Join(",", upgradedModulesSet.ToArray());
                printD("Set upgraded modules to " + upgradedModules);
            }
        }

        private void ParseUpgradedModules()
        {
            UpgradedModulesSet = new HashSet<string>(
                upgradedModules.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => s != string.Empty));

            printD("Loaded upgraded modules: " + string.Join(",", UpgradedModulesSet.ToArray()));
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            ParseUpgradedModules();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (state == StartState.Editor)
            {
                MarkUpgradedModules();

                onShipModified = _ => UpgradeModules();
                GameEvents.onEditorShipModified.Add(onShipModified);
                part.OnEditorDestroy += () => GameEvents.onEditorShipModified.Remove(onShipModified);
            }

            UpgradeModules();


        }

        private void printD(string message)
        {
#if DEBUG
            print("[KSPI] FNUpgradeModule for " + part.name + ": " + message);
#endif
        }

        private void MarkUpgradedModules()
        {
            printD("Finding upgraded modules for " + part.name);
            var newUpgradedModules = new HashSet<string>(UpgradedModulesSet);
            for (int ix = 0; ix < part.Modules.Count; ++ix)
            {
                var partModule = part.Modules[ix];
                var upgradeableModule = partModule as FNUpgradeableModule;
                if (upgradeableModule == null)
                    continue;

                var hasTech = upgradeableModule.HasRequiredTechForUpgrade();

                printD("Testing " + partModule.moduleName + ", upgraded = " + hasTech);

                if (hasTech)
                    newUpgradedModules.Add(partModule.moduleName);
            }
            UpgradedModulesSet = newUpgradedModules;
        }

        private void UpgradeModules()
        {
            printD("Upgrading modules: " + string.Join(",", UpgradedModulesSet.ToArray()));
            for (int ix = 0; ix < part.Modules.Count; ++ix)
            {
                var partModule = part.Modules[ix];
                var upgradeableModule = partModule as FNUpgradeableModule;
                if (upgradeableModule == null)
                    continue;

                if (UpgradedModulesSet.Contains(partModule.moduleName))
                {
                    upgradeableModule.upgradePartModule();
                    printD("Upgraded " + partModule.moduleName);
                }
            }

        }


    }
}
