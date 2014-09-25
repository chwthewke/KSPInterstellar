using System;
using System.Collections.Generic;
using System.Linq;

namespace InterstellarPlugin.PartUpgrades
{
    // TODO replace [KSPField] with [Persistent], also check save
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
                ? ""
                : string.Format(" of {0}/{1}", module.part.OriginalName(), module.name);
            return string.Format("{0}{1}", GetType().Name, moduleDesc);
        }

        private UpgradeModule module;
        private Action fulfilledAction;
    }

    public class UnlockTech : UpgradeRequirement
    {
        [Persistent]
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

    public abstract class PersistentRequirement : UpgradeRequirement
    {
        [Persistent(isPersistant = true)]
        public bool fulfilled;

        public override string Validate(Part part)
        {
            return null;
        }

        public void Fulfill()
        {
            if (fulfilled)
                return;
            fulfilled = true;
            FulfilledAction();
        }

        public override bool IsFulfilled()
        {
            return fulfilled;
        }

    }

    public class OneTimeResearch : PersistentRequirement
    {
        [Persistent]
        public int funds;

        [Persistent]
        public int science;

        [Persistent]
        public string eventText;

        [Persistent]
        public bool showCost = true;

        private BaseEvent researchEvent;
        private List<IResearchCost> costs;

        public override string Validate(Part part)
        {
            string v = base.Validate(part);
            if (v != null)
                return v;
            if (string.IsNullOrEmpty(eventText))
                return "'eventText' must be set.";
            return null;
        }

        public override void OnStart()
        {
            base.OnStart();
            if (IsFulfilled())
                return;

            if (HighLogic.LoadedSceneIsFlight)
            {
                InitCosts();
                AddUpgradeEvent();
            }
        }

        public override void OnStop()
        {
            base.OnStop();

            if (HighLogic.LoadedSceneIsFlight)
            {
                RemoveUpgradeEvent();
            }
        }

        private void InitCosts()
        {
            costs = new List<IResearchCost> { FundsCost.Of(funds), ScienceCost.Of(science) };
        }

        // TODO test KSPEvent thingie
        private void AddUpgradeEvent()
        {
            researchEvent = new BaseEvent(Module.Events, "research", OnEvent, new KSPEvent { active = true, guiActive = true, guiName = EventGuiText });

            Module.Events.Add(researchEvent);
        }

        private string EventGuiText
        {
            get
            {
                var costsText = showCost ? " " + string.Join(", ", costs.Select(c => c.GuiText).ToArray()) : "";
                return eventText + costsText;
            }
        }

        private void RemoveUpgradeEvent()
        {
            Module.Events.Remove(researchEvent);
        }

        private void OnEvent(KSPActionParam param)
        {
            var canPay = costs.All(c => c.CanPay());
            if (!canPay)
            {
                ScreenMessages.PostScreenMessage("Cannot research upgrade", 3.0f);
                return;
            }

            foreach (var cost in costs)
                cost.Pay();

            Fulfill();
        }

        public override string ToString()
        {
            return base.ToString() + string.Format(", funds = {0}, science = {1}", funds, science);
        }
    }
}
