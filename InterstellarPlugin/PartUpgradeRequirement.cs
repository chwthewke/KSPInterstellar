using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterstellarPlugin
{
    public abstract class PartUpgradeRequirement
    {
        private readonly PartUpgradeModule module;

        protected PartUpgradeRequirement(PartUpgradeModule module)
        {
            this.module = module;
        }

        protected PartUpgradeModule Module
        {
            get { return module; }
        }

        public virtual void OnStart()
        {
        }

        public abstract bool IsFulfilled();

        public override string ToString()
        {
            return string.Format("{0} for {1}", GetType().Name, module.part.partName);
        }
    }

    class PartUpgradeRequirements
    {
        private delegate PartUpgradeRequirement RequirementFactory(PartUpgradeModule module, ConfigNode config);

        private const string TypeKey = "name";
        public static PartUpgradeRequirement CreateRequirement(PartUpgradeModule module, ConfigNode node)
        {
            var requirementType = node.GetValue(TypeKey);
            var factory = factories[requirementType];
            if (factory == null)
                throw new ArgumentException(string.Format("Unknown part upgrade requirement type {0}.", requirementType));

            return factory.Invoke(module, node);
        }

        private static readonly IDictionary<string, RequirementFactory> factories =
            new Dictionary<string, RequirementFactory>
            {
                {"UnlockTech", (m, n) => new UnlockTech(m, n)}
            };

        // TODO allow registration of external requirement types?
    }

    class UnlockTech : PartUpgradeRequirement
    {
        private const string TechIdKey = "techID";

        private readonly string techId;

        public UnlockTech(PartUpgradeModule module, ConfigNode node)
            : base(module)
        {
            techId = node.GetValue(TechIdKey);
        }

        public override bool IsFulfilled()
        {
            Game currentGame = HighLogic.CurrentGame;
            if (currentGame == null)
                return false;

            if (currentGame.Mode != Game.Modes.CAREER && currentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
                return true;

            return ResearchAndDevelopment.Instance.GetTechState(techId).state == RDTech.State.Available;
        }

        public override string ToString()
        {
            return string.Format("{0}, tech = {1}", base.ToString(), techId);
        }
    }

    abstract class PersistentRequirement : PartUpgradeRequirement
    {
        private const string IdKey = "id";

        private readonly string requirementId;

        public override bool IsFulfilled()
        {
            var scenario = PartUpgradeScenario.Instance;
            return scenario != null && scenario.IsFulfilled(AsFulfilled);
        }

        public abstract void OnStart(PartUpgradeModule parent);

        public bool Fulfill()
        {
            var scenario = PartUpgradeScenario.Instance;
            if (scenario == null)
                return false;

            scenario.FulfillRequirement(AsFulfilled);

            return true;
        }

        // TODO validate null or empty, warn
        protected PersistentRequirement(PartUpgradeModule module, ConfigNode node)
            : base(module)
        {
            requirementId = node.GetValue(IdKey);
        }

        private FulfilledRequirement AsFulfilled
        {
            get { return new FulfilledRequirement(Module.part.partName, requirementId); }
        }

        public override string ToString()
        {
            return base.ToString() + string.Format(", id = {0}", requirementId);
        }
    }

    class OneTimeResearch : PersistentRequirement
    {
        private int funds;
        private int science;

        private BaseAction researchAction;
        private PartUpgradeModule module;

        // TODO validate or move to ConfigNode.Create/Load ?
        public OneTimeResearch(PartUpgradeModule module, ConfigNode node)
            : base(module, node)
        {
            funds = int.Parse(node.GetValue("funds") ?? "0");
            science = int.Parse(node.GetValue("science") ?? "0");
        }

        public override void OnStart(PartUpgradeModule parent)
        {
            if (IsFulfilled())
                return;
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            module = parent;

            // TODO Game modes
            AddUpgradeAction();
        }

        private void AddUpgradeAction()
        {
            researchAction = new BaseAction(module.Actions, "research", OnAction,
                new KSPAction(String.Format("Research ({0}F, {1}S)", funds, science)));

            module.Actions.Add(researchAction);
        }

        private void RemoveUpgradeAction()
        {
            module.Actions.Remove(researchAction);
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
            Module.IsUpgraded = true;
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
