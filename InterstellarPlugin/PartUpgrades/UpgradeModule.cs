using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InterstellarPlugin.PartUpgrades
{
    public class UpgradeModule : PartModule
    {
        // Used for persistence. If two or more UpgradeModules share the same id, they will be unlocked when the first is researched.
        [KSPField(isPersistant = true)]
        public string id;

        // Used for passing existing config from prefab OnLoad to OnStart
        [KSPField]
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
        // In the absence of any requirement, true means auto-unlock, false cannot unlock (dubious usefulness either way)
        [KSPField]
        public bool requireAllToUnlock = true;

        // When true, all retrofitting requirements must be met to retrofit, otherwise a single requirement is enough
        // In the absence of any requirement, true means auto-retrofit (equivalent to onUnlock = all), false cannot retrofit
        [KSPField]
        public bool requireAllToRetrofit = false;

        // Tracks Tweakscale tweaking
        private float scaleFactor = 1;

        // Internal state tracking
        private State upgradeState = State.InitPending;

        // Allow neutering if something is wrong.
        private bool valid = true;

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

                PartUpgrades.LogDebug(() => string.Format("[{0}] {1} for {2}: set UpgradeState to {3}.", PartUpgrades.ModName, GetType().Name, part.OriginalName(), upgradeState));
            }
        }


        public override void OnLoad(ConfigNode node)
        {
            // id is required
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning(string.Format("[{0}] {1} for {2}: Missing 'id', upgrades will not work.",
                    PartUpgrades.ModName, GetType().Name, part.OriginalName()));
                valid = false;
                return;
            }

            // AFAIK, this is more or less the only way to copy complex data structures from the loaded part prefab
            // to an instance in editor/flight (other that packing data to strings).
            Config = new ConfigNode();

            CopyNodes(node, UpgradeConfig.UpgradeKey, Config);
            CopyNodes(node, RequirementConfig.RequirementKey, Config);

            var retrofitNode = node.GetNode(RequirementConfig.RetrofitKey);
            var retrofitCopy = Config.AddNode(RequirementConfig.RetrofitKey);
            if (retrofitNode != null)
                CopyNodes(retrofitNode, RequirementConfig.RequirementKey, retrofitCopy);


            PartUpgrades.LogDebug(() => string.Format("[{1}] Loaded {2} for {3}: {0}.", this, PartUpgrades.ModName, GetType().Name, part.OriginalName()));
        }


        public override void OnSave(ConfigNode node)
        {
            if (!valid)
                return;

            node.AddData(Config);

            if (!SaveRequirements(node.GetNodes(RequirementConfig.RequirementKey), unlockRequirements))
                Debug.LogWarning(RequirementConfig.RequirementKey + " nodes modified, not saving.");

            if (!SaveRequirements(
                node.GetNode(RequirementConfig.RetrofitKey).GetNodes(RequirementConfig.RequirementKey),
                retrofitRequirements))
                Debug.LogWarning(RequirementConfig.RequirementKey + " nodes (" + RequirementConfig.RetrofitKey +
                                 ") modified, not saving.");

            PartUpgrades.LogDebug(() =>
                    string.Format("[{2}] Saved {3} for {4}: {0} -> <{1}>", this, node, PartUpgrades.ModName, GetType().Name,
                        part.OriginalName()));
        }

        public override void OnStart(StartState state)
        {
            if (!valid)
                return;
            // Load requirements and upgrades
            upgrades = Config.GetNodes(UpgradeConfig.UpgradeKey)
                .SelectMany((n, i) => new UpgradeConfig(part, n, i).Load())
                .ToList();
            unlockRequirements = LoadRequirements(Config);
            retrofitRequirements = LoadRequirements(Config.GetNode(RequirementConfig.RetrofitKey), " in " + RequirementConfig.RetrofitKey);

            // TODO check if scenario is reinitialised on scene change (and thus sheds its event listeners)
            if (state != StartState.Editor)
                WithScenario(s => s.onUpgradeUnlock.Add(OnGlobalUnlock));

            PartUpgrades.LogDebug(() => string.Format("[{1}] Started {2} for {3}: {0}.", this, PartUpgrades.ModName, GetType().Name, part.OriginalName()));

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

            WithScenario(s => s.Unlock(this));
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

            WithScenario(s => s.Unlock(this));

            isApplied = true;
            foreach (var upgrade in upgrades)
                upgrade.Apply(scaleFactor);
        }

        // TODO debug things

        private void WithScenario(Action<PartUpgrades> action)
        {
            var scenario = PartUpgrades.Instance;
            if (scenario == null)
                Debug.LogWarning(string.Format("[{0}] null PartUpgrades scenario at {1}", PartUpgrades.ModName, Environment.StackTrace));
            else
                action(scenario);
        }

        private T WithScenario<T>(Func<PartUpgrades, T> func)
        {
            var scenario = PartUpgrades.Instance;
            if (scenario == null)
            {
                Debug.LogWarning("null PartUpgrades scenario at " + Environment.StackTrace);
                return default(T);
            }
            return func(scenario);
        }

        //

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
        public void OnRescale(float factor)
        {
            if (!valid)
                return;
            scaleFactor = factor;
            PartUpgrades.LogDebug(() =>
                    string.Format("[{0}] {1} for {2}: scaleFactor <- {3}",
                        PartUpgrades.ModName, GetType().Name,
                        part.OriginalName(), scaleFactor));

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


        private List<UpgradeRequirement> LoadRequirements(ConfigNode requirementNode, string location = "")
        {
            return requirementNode.GetNodes(RequirementConfig.RequirementKey)
                .SelectMany((n, i) => new RequirementConfig(part, n, i + location).Load()).ToList();
        }

        // TODO maybe not the most robust way to align config and objects
        private bool SaveRequirements(IList<ConfigNode> requirementNodes, IList<UpgradeRequirement> requirements)
        {
            if (requirementNodes == null)
                return true;
            // TODO should return string like Validate
            if (requirements == null)
                return UpgradeState == State.InitPending;

            if (requirementNodes.Count != requirements.Count)
                return false;

            for (int index = 0; index < requirementNodes.Count; index++)
            {
                var requirement = requirements[index];
                ConfigNode requirementNode = requirementNodes[index];
                ConfigNode.CreateConfigFromObject(requirement, requirementNode);
                requirement.OnSave(requirementNode);
            }

            return true;
        }

        private bool IsUnlocked()
        {
            return WithScenario(s => s.IsUnlocked(this));
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
                target.AddNode(upgradeNode);
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