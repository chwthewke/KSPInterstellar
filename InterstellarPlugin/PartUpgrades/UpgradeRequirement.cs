using System;

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
                string.Format("{0}/{1}", module.part.OriginalName(), module.name);
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

    abstract class PersistentRequirement : UpgradeRequirement
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

    class OneTimeResearch : PersistentRequirement
    {
        [KSPField]
        public int funds;
        [KSPField]
        public int science;

        private BaseAction researchAction;

        public OneTimeResearch()
        {
        }

        public override void OnStart()
        {
            if (IsFulfilled())
                return;
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // TODO Game modes
            AddUpgradeAction();
        }

        private void AddUpgradeAction()
        {
            researchAction = new BaseAction(Module.Actions, "research", OnAction,
                new KSPAction(String.Format("Research ({0}F, {1}S)", funds, science)));

            Module.Actions.Add(researchAction);
        }

        private void RemoveUpgradeAction()
        {
            Module.Actions.Remove(researchAction);
        }

        private void OnAction(KSPActionParam param)
        {
            if (!ResearchCosts.CanPay(funds, ResearchCosts.Type.Funds) ||
                !ResearchCosts.CanPay(funds, ResearchCosts.Type.Science))
                return;
            ResearchCosts.Pay(funds, ResearchCosts.Type.Funds);
            ResearchCosts.Pay(science, ResearchCosts.Type.Science);

            Fulfill();

            RemoveUpgradeAction();

            // TODO change to event on PartUpgradeScenario
            //Module.IsUpgraded = true;
        }

        public override string ToString()
        {
            return base.ToString() + string.Format(", funds = {0}, science = {1}", funds, science);
        }
    }

    // TODO WET
    internal class ResearchCosts
    {
        internal enum Type
        {
            Funds,
            Science
        }

        internal static bool CanPay(int amount, Type type)
        {
            switch (type)
            {
                case Type.Funds:
                    return CanPayFunds(amount);
                case Type.Science:
                    return CanPayScience(amount);
                default:
                    throw new ArgumentException("Unknown cost type " + type);
            }
        }

        internal static void Pay(int amount, Type type)
        {
            switch (type)
            {
                case Type.Funds:
                    PayFunds(amount);
                    break;
                case Type.Science:
                    PayScience(amount);
                    break;
                default:
                    throw new ArgumentException("Unknown cost type " + type);
            }
        }

        private static void PayScience(int amount)
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER &&
                HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
                return;
            ResearchAndDevelopment.Instance.Science -= amount;
        }

        private static void PayFunds(int amount)
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
                return;
            Funding.Instance.Funds -= amount;
        }

        private static bool CanPayScience(int amount)
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER &&
                HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
                return true;
            return ResearchAndDevelopment.Instance.Science >= amount;
        }

        private static bool CanPayFunds(int amount)
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
                return true;
            return Funding.Instance.Funds >= amount;
        }
    }


}