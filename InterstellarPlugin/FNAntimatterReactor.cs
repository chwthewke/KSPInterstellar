using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterstellarPlugin {
    [KSPModule("Antimatter Reactor")]
    class FNAntimatterReactor : FNReactor {
        public override bool getIsNuclear() {
            return false;
        }

        protected override double consumeReactorResource(double resource) {
            List<PartResource> antimatter_resources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Antimatter").id, PartResourceLibrary.Instance.GetDefinition("Antimatter").resourceFlowMode, antimatter_resources);
            double antimatter_provided = 0;
            foreach (PartResource antimatter_resource in antimatter_resources) {
                double antimatter_consumed_here = Math.Min(antimatter_resource.amount, resource);
                antimatter_provided += antimatter_consumed_here;
                antimatter_resource.amount -= antimatter_consumed_here;
                resource -= antimatter_consumed_here;
            }
            return antimatter_provided;
        }

        protected override double returnReactorResource(double resource) {
            List<PartResource> antimatter_resources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Antimatter").id, PartResourceLibrary.Instance.GetDefinition("Antimatter").resourceFlowMode, antimatter_resources);
            double antimatter_returned = 0;
            foreach (PartResource antimatter_resource in antimatter_resources) {
                double antimatter_returned_here = Math.Min(antimatter_resource.maxAmount - antimatter_resource.amount, resource);
                antimatter_returned += antimatter_returned_here;
                antimatter_resource.amount += antimatter_returned_here;
                resource -= antimatter_returned_here;
            }
            return antimatter_returned;
        }
        
        protected override string getResourceDeprivedMessage() {
            return "Antimatter Deprived";
        }

    }
}
