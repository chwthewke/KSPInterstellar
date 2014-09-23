using System;
using System.Collections.Generic;
using System.Linq;

namespace InterstellarPlugin.PartUpgrades
{
    public abstract class UpgradeRequirement
    {
        public const string FulfilledKey = "fulfilled";

        public void Start(UpgradeModule parent, Action onFulfill)
        {
            module = parent;
            fulfilledAction = onFulfill;
            OnStart();
        }

        public void Stop()
        {
            OnStop();
        }

        public virtual string Validate(Part part)
        {
            return null;
        }

        public virtual void OnLoad(ConfigNode node)
        {
        }

        public virtual void OnSave(ConfigNode node)
        {
        }

        public virtual void OnStart()
        {
        }

        public virtual void OnStop()
        {
        }

        public abstract bool IsFulfilled();

        public UpgradeModule Module
        {
            get { return module; }
        }

        protected Action FulfilledAction
        {
            get { return fulfilledAction; }
        }

        public override string ToString()
        {
            var moduleDesc = module == null
                ? "[unknown]"
                : string.Format("{0}/{1}", module.part.OriginalName(), module.name);
            return string.Format("{0} of {1}", typeof (UpgradeRequirement).Name, moduleDesc);
        }

        // TODO useful? not sure
        private UpgradeModule module;
        private Action fulfilledAction;
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
        public override string Validate(Part part)
        {
            return null;
        }

        // TODO replace with Fulfill() method perhaps; this is ridiculous.
        public bool Fulfilled
        {
            get { return fulfilled; }
            set
            {
                if (!value)
                    return;
                fulfilled = true;
                FulfilledAction();
            }
        }

        public override bool IsFulfilled()
        {
            return fulfilled;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            bool.TryParse(node.GetValue(FulfilledKey), out fulfilled);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.SetValue(FulfilledKey, fulfilled.ToString());
        }

        private bool fulfilled;
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

            Fulfilled = true;
        }

        public override string ToString()
        {
            return base.ToString() + string.Format(", funds = {0}, science = {1}", funds, science);
        }
    }
}
