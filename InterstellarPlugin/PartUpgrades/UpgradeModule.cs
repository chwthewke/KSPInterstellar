using System.Collections.Generic;
using UnityEngine;

namespace InterstellarPlugin.PartUpgrades
{
    public class UpgradeModule: PartModule
    {
        [SerializeField]
        private ProtoUpgradeField test;

        [KSPField(isPersistant = false)]
        public ConfigNode config;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            test = ProtoUpgradeField.Load(node);

            config = node;

            // NOTE catch exceptions here or bam!
#if DEBUG
            Debug.Log(string.Format("[Interstellar] Loaded {0}.", ToString()));
#endif
        }

        public override void OnInitialize()
        {
            base.OnInitialize();

            if (config == null)
            {
                Debug.Log("[Interstellar] OnInitialize: UpgradeModule config was null.");
                return;
            }

            test = ProtoUpgradeField.Load(config);

#if DEBUG
            Debug.Log(string.Format("[Interstellar] OnInitialize {0}.", ToString()));
#endif
        }

        public override void OnAwake()
        {
            base.OnAwake();

            if (config == null)
            {
                Debug.Log("[Interstellar] OnAwake: UpgradeModule config was null.");
                return;
            }

            test = ProtoUpgradeField.Load(config);

#if DEBUG
            Debug.Log(string.Format("[Interstellar] OnAwake {0}.", ToString()));
#endif
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

#if DEBUG
            Debug.Log(string.Format("[Interstellar] Started {0}.", ToString()));
#endif
        }

        public override string ToString()
        {
            return string.Format("{0} for {1}: {2}", GetType(), part.name, test);
        }
    }
}