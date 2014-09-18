using System.Collections.Generic;
using System.Linq;

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
            var moduleDesc = module == null
                ? "[unknown]"
                : string.Format("{0}/{1}", module.part.OriginalName(), module.name);
            return string.Format("{0} of {1}", typeof (UpgradeRequirement).Name, moduleDesc);
        }

        private UpgradeModule module;
    }

    internal class UnlockTech : UpgradeRequirement
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

    internal abstract class PersistentRequirement : UpgradeRequirement
    {
        public override bool IsFulfilled()
        {
            var scenario = PartUpgradeScenario.Instance;
            return scenario != null && scenario.IsFulfilled(AsFulfilled);
        }

        public bool Fulfill()
        {
            var scenario = PartUpgradeScenario.Instance;
            if (scenario == null)
                return false;

            scenario.FulfillRequirement(AsFulfilled);

            return true;
        }

        private FulfilledRequirement AsFulfilled
        {
            get { return new FulfilledRequirement(Module.part.partName, id); }
        }

        public override string ToString()
        {
            return base.ToString() + string.Format(", id = {0}", id);
        }
    }

    internal class OneTimeResearch : PersistentRequirement
    {
        [KSPField]
        public int funds;

        [KSPField]
        public int science;

        private BaseAction researchAction;
        private List<IResearchCost> costs;

        public override void OnStart()
        {
            if (IsFulfilled())
                return;
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            InitCosts();
            AddUpgradeAction();
        }

        private void InitCosts()
        {
            costs = new List<IResearchCost> {FundsCost.Of(funds), ScienceCost.Of(science)};
        }

        private void AddUpgradeAction()
        {
            var costStr = "Research upgrade " + string.Join(", ", costs.Select(c => c.GuiText).ToArray());
            researchAction = new BaseAction(Module.Actions, "research", OnAction, new KSPAction(costStr));

            Module.Actions.Add(researchAction);
        }

        private void RemoveUpgradeAction()
        {
            Module.Actions.Remove(researchAction);
        }

        private void OnAction(KSPActionParam param)
        {
            var canPay = costs.Aggregate(true, (b, c) => b && c.CanPay());
            if (!canPay)
            {
                ScreenMessages.PostScreenMessage("Cannot research upgrade", 3.0f);
                return;
            }

            foreach (var cost in costs)
                cost.Pay();


            RemoveUpgradeAction();

            Fulfill();
            Module.CheckRequirements();
        }

        public override string ToString()
        {
            return base.ToString() + string.Format(", funds = {0}, science = {1}", funds, science);
        }
    }
}