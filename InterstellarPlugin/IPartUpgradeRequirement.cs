using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterstellarPlugin
{
    public interface IPartUpgradeRequirement
    {
        void OnStart(PartUpgradeModule parent);

        bool IsFulfilled();

    }

    class PartUpgradeRequirements
    {
        private const string TypeKey = "type";
        public static IPartUpgradeRequirement CreateRequirement(ConfigNode node)
        {
            var requirementType = node.GetValue(TypeKey);
            var factory = Factories[requirementType];
            if (factory == null)
                throw new ArgumentException(string.Format("Unknown part upgrade requirement type {0}.", requirementType));

            return factory.Invoke(node);
        }

        private static readonly IDictionary<string, Func<ConfigNode, IPartUpgradeRequirement>> Factories =
            new Dictionary<string, Func<ConfigNode, IPartUpgradeRequirement>>()
            {
                {"UnlockTech", n => new UnlockTech(n)}
            };

        // TODO allow registration of external requirement types?
    }

    class UnlockTech : IPartUpgradeRequirement
    {
        private const string TechIdKey = "tech";

        private readonly string techId;
        private bool fulfilled;

        public UnlockTech(ConfigNode node)
        {
            techId = node.GetValue(TechIdKey);
        }

        public void OnStart(PartUpgradeModule parent)
        {
            fulfilled = CheckFulfilled();
        }

        public bool IsFulfilled()
        {
            return fulfilled;
        }

        private bool CheckFulfilled()
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
            return string.Format("Requires tech {0}", techId);
        }
    }

    abstract class PersistentRequirement : IPartUpgradeRequirement
    {
        private const string IdKey = "id";

        private readonly string partName;
        private readonly string requirementId;

        public bool IsFulfilled()
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
        protected PersistentRequirement(Part part, ConfigNode node)
        {
            partName = part.partName;
            requirementId = node.GetValue(IdKey);
        }

        private FulfilledRequirement AsFulfilled
        {
            get { return new FulfilledRequirement(partName, requirementId); }
        }
    }

    class OneTimeResearch : PersistentRequirement
    {
        private int funds;
        private int science;

        private BaseAction researchAction;

        // TODO validate or move to ConfigNode.Create/Load ?
        public OneTimeResearch(Part part, ConfigNode node)
            : base(part, node)
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

            // TODO Game modes
            researchAction = new Action(parent.Actions, "research", OnAction,
                new KSPAction(String.Format("Research ({0}F, {1}S)", funds, science)));

            parent.Actions.Add(researchAction);

        }

        private void OnAction(KSPActionParam param)
        {
            if (!ResearchCosts.CanPay(funds, ResearchCosts.Type.FUNDS) ||
                !ResearchCosts.CanPay(funds, ResearchCosts.Type.SCIENCE))
                return;
            ResearchCosts.Pay(funds, ResearchCosts.Type.FUNDS);
            ResearchCosts.Pay(science, ResearchCosts.Type.SCIENCE);

            Fulfill();
            // TODO
            // parent.IsUpgraded = true;
        }

        class Action : BaseAction
        {
            public Action(BaseActionList listParent, string name, BaseActionDelegate onEvent, KSPAction actionAttr)
                : base(listParent, name, onEvent, actionAttr)
            {
            }
        }
    }

    // TODO WET
    internal class ResearchCosts
    {
        internal enum Type
        {
            FUNDS,
            SCIENCE
        }

        internal static bool CanPay(int amount, Type type)
        {
            switch (type)
            {
                case Type.FUNDS:
                    return CanPayFunds(amount);
                case Type.SCIENCE:
                    return CanPayScience(amount);
                default:
                    throw new ArgumentException("Unknown cost type " + type);
            }
        }

        internal static void Pay(int amount, Type type)
        {
            switch (type)
            {
                case Type.FUNDS:
                    PayFunds(amount);
                    break;
                case Type.SCIENCE:
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
