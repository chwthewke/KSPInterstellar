using System;
using System.Collections.Generic;
using System.Linq;
using TweakScale;
using UnityEngine;

namespace InterstellarPlugin.PartUpgrades
{
    public class UpgradeModule : PartModule
    {
        // Used for persistence. If two or more UpgradeModules share the same id, they will be unlocked when the first is researched.
        [KSPField]
        public string id;

        // Used for passing existing config from OnLoad to OnStart
        [KSPField(isPersistant = false)]
        public ConfigNode Config;

        // Sets whether this upgrade is applied to the part.
        [KSPField(isPersistant = true)]
        public bool isApplied = false;

        // What parts to automatically upgrade when first unlocking the tech
        // Possible values are None, Part, Vessel, All.
        [KSPField]
        public UnlockMode onUnlock = UnlockMode.None;

        // When true, the part never starts upgraded after research.
        [KSPField]
        public bool mustRetrofit;

        // When true, all requirements must be met to unlock, otherwise a single requirement is enough
        [KSPField]
        public bool requireAllToUnlock = true;

        // When true, all retrofitting requirements must be met to retrofit, otherwise a single requirement is enough
        [KSPField]
        public bool requireAllToRetrofit = true;

        private float scaleFactor = 1;

        // Internal state tracking
        private State upgradeState = State.InitPending;

        private State UpgradeState
        {
            get { return upgradeState; }
            set
            {
                var previousState = upgradeState;
                upgradeState = value;
                switch (value)
                {
                    case State.Locked:
                        Lock(previousState);
                        break;
                    case State.Unlocked:
                        Unlock(previousState);
                        break;
                    case State.Applied:
                        Apply(previousState);
                        break;
                }
            }
        }


        public override void OnLoad(ConfigNode node)
        {
            // AFAIK, this is more or less the only way to copy complex data structures from the loaded part prefab
            // to an instance in editor/flight (other that packing data to strings).
            Config = new ConfigNode();

            CopyNodes(node, UpgradeConfig.UpgradeKey, Config);
            CopyNodes(node, RequirementConfig.RequirementKey, Config);

            var retrofitNode = node.GetNode(RequirementConfig.RetrofitKey);
            var retrofitCopy = Config.AddNode(RequirementConfig.RetrofitKey);
            if (retrofitNode != null)
                CopyNodes(retrofitNode, RequirementConfig.RetrofitKey, retrofitCopy);


#if DEBUG
            Debug.Log(string.Format("[Interstellar] Loaded {0}.", this));
#endif
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            if (!SaveRequirements(node.GetNodes(RequirementConfig.RequirementKey), unlockRequirements))
                Debug.LogWarning(RequirementConfig.RequirementKey + " nodes modified, not saving.");

            if (
                !SaveRequirements(
                    node.GetNode(RequirementConfig.RetrofitKey).GetNodes(RequirementConfig.RequirementKey),
                    retrofitRequirements))
                Debug.LogWarning(RequirementConfig.RequirementKey + " nodes (" + RequirementConfig.RetrofitKey +
                                 ") modified, not saving.");

#if DEBUG
            Debug.Log(string.Format("[Interstellar] Saved {0}: <{1}>", this, node));
#endif
        }

        public override void OnStart(StartState state)
        {
            // Load requirements and upgrades
            upgrades = Config.GetNodes(UpgradeConfig.UpgradeKey)
                .SelectMany((n, i) => new UpgradeConfig(part, n, i).Load())
                .ToList();
            unlockRequirements = LoadRequirements(Config);
            retrofitRequirements = LoadRequirements(Config.GetNode(RequirementConfig.RetrofitKey));

            // TODO check if scenario is reinitialised on scene change (and thus sheds its event listeners)
            if (state != StartState.Editor)
                PartUpgradeScenario.Instance.onUpgradeUnlock.Add(OnGlobalUnlock);
#if DEBUG
            Debug.Log(string.Format("[Interstellar] Starting {0}.", this));
#endif

            UpgradeState = CheckUpgradeState(state);
        }

        // check if the unlock/applied state should change based on requirements and start conditions.
        private State CheckUpgradeState(StartState startState)
        {
            if (isApplied)
                return State.Applied;

            if (IsUnlocked() ||
                CheckRequirements(unlockRequirements, requireAllToUnlock))
            {
                if (ShouldStartUpgraded(startState) ||
                    CheckRequirements(retrofitRequirements, requireAllToRetrofit))
                    return State.Applied;
                return State.Unlocked;
            }

            return State.Locked;
        }

        // Go to the "Locked" state (ie upgrade must be researched)
        private void Lock(State previous)
        {
            if (previous != State.InitPending)
                return;

            foreach (var upgradeRequirement in unlockRequirements)
                upgradeRequirement.Start(this, OnUnlockRequirementFulfilled);
        }

        // Go to the "Unlocked" state (ie upgrade is researched but not applied to this part)
        private void Unlock(State previous)
        {
            if (previous == State.Locked)
            {
                foreach (var unlockRequirement in unlockRequirements)
                    unlockRequirement.Stop();
            }

            if (previous < State.Unlocked)
                foreach (var retrofitRequirement in retrofitRequirements)
                    retrofitRequirement.Start(this, OnRetrofitRequirementFulfilled);

            PartUpgradeScenario.Instance.Unlock(this);
        }

        // Go to the "Applied" state (ie upgrade is applied to this part)
        private void Apply(State previous)
        {
            if (previous == State.Locked)
            {
                foreach (var unlockRequirement in unlockRequirements)
                    unlockRequirement.Stop();
            }
            else if (previous == State.Unlocked)
            {
                foreach (var retrofitRequirement in retrofitRequirements)
                    retrofitRequirement.Stop();
            }

            PartUpgradeScenario.Instance.Unlock(this);

            isApplied = true;
            foreach (var upgrade in upgrades)
                upgrade.Apply(scaleFactor);
        }

        private void OnGlobalUnlock(string upgradeId, Part upgraded)
        {
            if (upgradeId != id)
                return;

            if (UpgradeState == State.Applied)
                return;

            if (onUnlock == UnlockMode.None)
                return;

            if (onUnlock == UnlockMode.Part && upgraded != part)
                return;

            if (onUnlock == UnlockMode.Vessel && upgraded.vessel != part.vessel)
                return;

            UpgradeState = State.Applied;
        }

        // TweakScale integration
        internal void OnRescale(float factor)
        {
            scaleFactor = factor;
        }

        private void OnUnlockRequirementFulfilled()
        {
            if (UpgradeState != State.Locked)
                return;

            UpgradeState = CheckUpgradeState(StartState.None);
        }

        private void OnRetrofitRequirementFulfilled()
        {
            if (UpgradeState != State.Unlocked)
                return;

            UpgradeState = CheckUpgradeState(StartState.None);
        }


        private List<UpgradeRequirement> LoadRequirements(ConfigNode requirementNode)
        {
            return requirementNode.GetNodes(RequirementConfig.RequirementKey)
                .SelectMany((n, i) => new RequirementConfig(part, n, i).Load()).ToList();
        }

        // TODO maybe not the most robust way to align config and objects
        private static bool SaveRequirements(IList<ConfigNode> requirementNodes, IList<UpgradeRequirement> requirements)
        {
            if (requirementNodes.Count != requirements.Count)
                return false;

            for (int index = 0; index < requirementNodes.Count; index++)
                requirements[index].OnSave(requirementNodes[index]);

            return true;
        }

        private bool IsUnlocked()
        {
            return PartUpgradeScenario.Instance.IsUnlocked(this);
        }


        private bool CheckRequirements(IEnumerable<UpgradeRequirement> requirements, bool requireAll)
        {
            return requireAll
                ? requirements.All(req => req.IsFulfilled())
                : requirements.Any(req => req.IsFulfilled());
        }

        private bool ShouldStartUpgraded(StartState state)
        {
            if (state == StartState.Editor || state == StartState.PreLaunch)
                return !mustRetrofit;
            return onUnlock == UnlockMode.All;
        }

        public override string ToString()
        {
            return
                string.Format(
                    "{0} for {1}: upgrades = [{2}], unlockRequirements = [{3}], retrofitRequirements = {4}, config = <{5}>",
                    GetType(), part.OriginalName(),
                    upgrades == null ? "null" : string.Join(", ", upgrades.Select(o => o.ToString()).ToArray()),
                    unlockRequirements == null
                        ? "null"
                        : string.Join(", ", unlockRequirements.Select(o => o.ToString()).ToArray()),
                    retrofitRequirements == null
                        ? "null"
                        : string.Join(", ", retrofitRequirements.Select(o => o.ToString()).ToArray()),
                    Config);
        }


        private static void CopyNodes(ConfigNode node, string name, ConfigNode target)
        {
            foreach (var upgradeNode in node.GetNodes(name))
                target.AddData(upgradeNode);
        }

        private IList<Upgrade> upgrades;
        private IList<UpgradeRequirement> unlockRequirements;
        private IList<UpgradeRequirement> retrofitRequirements;

        private enum State
        {
            InitPending,
            Locked,
            Unlocked,
            Applied
        }
    }
}