using System;
using System.Runtime.CompilerServices;

namespace InterstellarPlugin.PartUpgrades
{
    public class LifecycleModule: PartModule
    {
        [KSPField(isPersistant = true)]
        public int persistent;

        [KSPField]
        public int transient;

        [KSPField]
        public ConfigNode config;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            config = new ConfigNode("CONFIG");
            foreach (ConfigNode innerNode in node.nodes)
            {
                config.AddNode(innerNode.CreateCopy());
            }

            PartUpgrades.LogDebug(() => string.Format(
                "[LIFECYCLE] OnLoad[{4}.{5}.{6}-{7}] persistent = {0}, transient = {1}, part.isClone = {9}, config = <{2}>, node = <{3}> {8}",
                persistent, transient, config, node,
                VesselName, PartName, GetType().Name, RuntimeHelpers.GetHashCode(this), Environment.StackTrace, part != null && part.isClone));
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            ConfigNode copy = config.CreateCopy();

            var configNode = node.GetNode("CONFIG");
            if (configNode == null)
            {

                node.AddNode(copy);
            }
            else
            {
                foreach (ConfigNode innerNode in copy.nodes)
                {
                    var savedInnerNode = configNode.GetNode(innerNode.name);
                    if (savedInnerNode == null)
                    {
                        configNode.AddNode(innerNode);
                    }
                    else
                    {
                        savedInnerNode.ClearData();
                        savedInnerNode.AddData(innerNode);
                    }
                }
            }



            PartUpgrades.LogDebug(() => string.Format(
                "[LIFECYCLE] OnSave[{4}.{5}.{6}-{7}] persistent = {0}, transient = {1}, part.isClone = {9}, config = <{2}>, node = <{3}> {8}",
                persistent, transient, config, node,
                VesselName, PartName, GetType().Name, RuntimeHelpers.GetHashCode(this), Environment.StackTrace, part != null && part.isClone));
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            PartUpgrades.LogDebug(() => string.Format(
                "[LIFECYCLE] OnStart({7})[{3}.{4}.{5}-{6}] persistent = {0}, transient = {1}, part.isClone = {9}, config = <{2}> {8}",
                persistent, transient, config,
                VesselName, PartName, GetType().Name, RuntimeHelpers.GetHashCode(this), state, Environment.StackTrace, part != null && part.isClone));
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true)]
        public void IncreasePersistent()
        {
            persistent += 1;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true)]
        public void IncreaseTransient()
        {
            transient += 1;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true)]
        public void AddValue()
        {
            config.AddNode("NODE" + (config.nodes.Count));
            foreach (ConfigNode innerNode in config.nodes)
            {
                var valueStr = innerNode.GetValue("value");
                int value;
                if (int.TryParse(valueStr, out value))
                {
                    innerNode.SetValue("value", (value + 1).ToString());
                }
                else
                {
                    innerNode.AddValue("value", "0");
                }
            }
        }

        private string PartName { get { return part == null ? "null" : part.OriginalName(); }}
        private string VesselName { get
        {
            return part == null
                ? "null"
                : (part.vessel == null
                    ? null
                    : string.Format("{0} (initially {1}) #{2}", part.vessel.GetName(), part.initialVesselName,
                        part.vessel.id));
        }}
    }
}