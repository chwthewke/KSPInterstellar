using System;

namespace InterstellarPlugin.PartUpgrades
{
    interface IResearchCost
    {
        bool CanPay();
        void Pay();
        string GuiText { get; }
    }

    class NoCost : IResearchCost
    {

        public bool CanPay()
        {
            return true;
        }

        public void Pay()
        {
        }

        public string GuiText { get { return ""; } }

        public static IResearchCost Instance { get { return instance; } }

        private static readonly NoCost instance = new NoCost();
    }

    class ScienceCost : IResearchCost
    {
        public static IResearchCost Of(int cost)
        {
            if (cost <= 0)
                return NoCost.Instance;
            var mode = HighLogic.CurrentGame.Mode;
            if (mode == Game.Modes.CAREER || mode == Game.Modes.SCIENCE_SANDBOX)
                return new ScienceCost(cost);
            return NoCost.Instance;
        }

        private ScienceCost(int cost)
        {
            this.cost = cost;
        }

        public bool CanPay()
        {
            return ResearchAndDevelopment.Instance.Science >= cost;
        }

        public void Pay()
        {
            ResearchAndDevelopment.Instance.Science -= cost;
        }

        public string GuiText
        {
            get { return string.Format("{0} Sci", cost); }
        }

        private int cost;
    }

    class FundsCost : IResearchCost
    {

        public static IResearchCost Of(int cost)
        {
            if (cost <= 0)
                return NoCost.Instance;
            var mode = HighLogic.CurrentGame.Mode;
            if (mode == Game.Modes.CAREER)
                return new FundsCost(cost);
            return NoCost.Instance;
        }

        public FundsCost(int cost)
        {
            this.cost = cost;
        }

        public bool CanPay()
        {
            return Funding.Instance.Funds >= cost;
        }

        public void Pay()
        {
            Funding.Instance.Funds -= cost;
        }

        public string GuiText
        {
            get { return string.Format("{0} √", cost); }
        }

        private int cost;
    }
}