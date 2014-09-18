namespace InterstellarPlugin.PartUpgrades
{
    public abstract class UpgradeRequirement
    {
        [KSPField]
        public string id;

        public string Id
        {
            get { return id; }
        }

        public virtual string Validate(Part part)
        {
            return null;
        }

        public void Start(UpgradeModule parent)
        {
            module = parent;
            OnStart();
        }

        public virtual void OnStart()
        {
        }

        public abstract bool IsFulfilled();

        public UpgradeModule Module
        {
            get { return module; }
        }

        public override string ToString()
        {
            var moduleDesc = module == null ? "[unknown]" : 
                string.Format("{0}/{1}", module.part.partInfo.partPrefab.name, module.name);
            return string.Format("{0} of {1}", typeof(UpgradeRequirement).Name, moduleDesc);
        }

        private UpgradeModule module;
    }

    class UnlockTech : UpgradeRequirement
    {
        [KSPField]
        public string techID;

        public override bool IsFulfilled()
        {
            Game currentGame = HighLogic.CurrentGame;
            if (currentGame == null)
                return false;

            if (currentGame.Mode != Game.Modes.CAREER && currentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
                return true;

            return ResearchAndDevelopment.Instance.GetTechState(techID) != null;
        }

        public override string ToString()
        {
            return string.Format("{0}, tech = {1}", base.ToString(), techID);
        }
    }


}