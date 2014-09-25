using System.Linq;
using System.Runtime.CompilerServices;

namespace InterstellarPlugin.PartUpgrades
{
    public class LifecycleModule: PartModule
    {
        [KSPField(isPersistant = true)]
        public string persistent;

        [KSPField]
        public string transient;

        public ConfigNode config;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            PartUpgrades.LogDebug(() => string.Format(
                "[LIFECYCLE] OnLoad[{4}.{5}.{6}-{7}] persistent = {0}, transient = {1}, config = <{2}>, node = <{3}>",
                persistent, transient, config, node,
                VesselName, PartName, GetType().Name, RuntimeHelpers.GetHashCode(this)));

            config = new ConfigNode();
            foreach (var configNode in node.nodes.OfType<ConfigNode>())
                config.AddNode(configNode);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            PartUpgrades.LogDebug(() => string.Format(
                "[LIFECYCLE] OnSave[{4}.{5}.{6}-{7}] persistent = {0}, transient = {1}, config = <{2}>, node = <{3}>",
                persistent, transient, config, node,
                VesselName, PartName, GetType().Name, RuntimeHelpers.GetHashCode(this)));
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            PartUpgrades.LogDebug(() => string.Format(
                "[LIFECYCLE] OnStart({7})[{3}.{4}.{5}-{6}] persistent = {0}, transient = {1}, config = <{2}>",
                persistent, transient, config,
                VesselName, PartName, GetType().Name, RuntimeHelpers.GetHashCode(this), state));
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