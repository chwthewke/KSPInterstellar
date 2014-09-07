
namespace InterstellarPlugin {
    interface FNUpgradeableModule {
        string UpgradeTechReq { get; }
        void upgradePartModule();
    }

    internal static class FNUpgradeableModuleEx
    {
        public static bool HasRequiredTechForUpgrade(this FNUpgradeableModule module)
        {
            var partModule = module as PartModule;
            var moduleName = partModule == null ? "???" : partModule.moduleName;

            var game = HighLogic.CurrentGame;
            if (game == null)
                return false;

            var mode = game.Mode;
            if (mode != Game.Modes.CAREER && mode != Game.Modes.SCIENCE_SANDBOX)
            {
#if DEBUG
                partModule.PrintD(moduleName + " Upgrade Tech check: Game is Sandbox -> true");
#endif
                return true;
            }

            if (module.UpgradeTechReq == null)
            {
#if DEBUG
                partModule.PrintD(moduleName + " Upgrade Tech check:  Upgrade tech is null -> false");
#endif
                return false;
            }

            bool hasTech = PluginHelper.hasTech(module.UpgradeTechReq);
#if DEBUG
            partModule.PrintD(moduleName + " Upgrade Tech check:  Upgrade tech is " +
                              module.UpgradeTechReq + " -> " + hasTech);
#endif
            return hasTech;
        }

        public static bool IsUpgradeable(this FNUpgradeableModule module)
        {
            var partModule = module as PartModule;
            if (partModule == null)
                return false;
            var upgradeModule = partModule.part.FindModuleImplementing<FNUpgradeModule>();
#if DEBUG
            partModule.PrintD("IsUpgradeable: upgradeModule = " + upgradeModule);
#endif
            return upgradeModule != null &&
                   module.UpgradeTechReq != null;
        }
    }

}
