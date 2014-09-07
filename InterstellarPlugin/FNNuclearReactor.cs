using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterstellarPlugin {
    [KSPModule("Fission Reactor")]
    class FNNuclearReactor : FNReactor {
        //Persistent True
        [KSPField(isPersistant = true)]
        public bool upgradedToV08 = false;
        [KSPField(isPersistant = true)]
        public bool uranium_fuel = true;
        [KSPField(isPersistant = true)]
        public bool upgradedToV10 = false;
        
        //Internal
        protected PartResource thf4;
        protected PartResource eu;
        protected PartResource fuel_resource;
        protected PartResource actinides;
        protected double initial_thermal_power = 0;
        protected double initial_resource_rate = 0;

        [KSPEvent(guiName = "Manual Restart", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void ManualRestart() {
            if (fuel_resource.amount > 0.001) {
                IsEnabled = true;
            }
        }

        [KSPEvent(guiName = "Manual Shutdown", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void ManualShutdown() {
            IsEnabled = false;
        }

        [KSPEvent(guiName = "Refuel Enriched Uranium", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void RefuelUranium() {
            List<PartResource> eu_resources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("EnrichedUranium").id, ResourceFlowMode.ALL_VESSEL, eu_resources);
            double spare_capacity_for_eu = Math.Max(eu.maxAmount - eu.amount - actinides.amount, 0);
            foreach (PartResource eu_resource in eu_resources) {
                if (eu_resource.part.FindModulesImplementing<FNNuclearReactor>().Count == 0) {
                    double eu_available = eu_resource.amount;
                    double eu_added = Math.Min(eu_available, spare_capacity_for_eu);
                    eu.amount += eu_added;
                    eu_resource.amount -= eu_added;
                    spare_capacity_for_eu -= eu_added;
                }
            }
        }

        //[KSPEvent(guiName = "Refuel ThF4", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void RefuelThorium() {
            List<PartResource> th4_resources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("ThF4").id, PartResourceLibrary.Instance.GetDefinition("ThF4").resourceFlowMode, th4_resources);
            double spare_capacity_for_thf4 = Math.Max(thf4.maxAmount - thf4.amount - actinides.amount, 0);
            foreach (PartResource thf4_resource in th4_resources) {
                if (thf4_resource.part.FindModulesImplementing<FNNuclearReactor>().Count == 0) {
                    double thf4_available = thf4_resource.amount;
                    double thf4_added = Math.Min(thf4_available, spare_capacity_for_thf4);
                    thf4.amount += thf4_added;
                    thf4_resource.amount -= thf4_added;
                    spare_capacity_for_thf4 -= thf4_added;
                }
            }
        }

        //[KSPEvent(guiName = "Swap Fuel", externalToEVAOnly = true, guiActiveUnfocused = true, guiActive = false, unfocusedRange = 3.0f)]
        public void SwapFuel() {
            if (actinides.amount <= 0.01) {
                if (uranium_fuel) {
                    defuelUranium();
                    if (eu.amount > 0) { return; }
                    setThoriumFuel();
                    RefuelThorium();
                } else {
                    defuelThorium();
                    if (thf4.amount > 0) { return; }
                    setUraniumFuel();
                    RefuelUranium();
                }
            }
        }

        //[KSPEvent(guiName = "Swap Fuel", guiActiveEditor = true, guiActiveUnfocused = false, guiActive = false)]
        public void EditorSwapFuel() {
            if (uranium_fuel) {
                uranium_fuel = !uranium_fuel;
                eu.amount = 0;
                thf4.amount = thf4.maxAmount;
                fuelmodeStr = "Thorium";
            } else {
                uranium_fuel = !uranium_fuel;
                thf4.amount = 0;
                eu.amount = eu.maxAmount;
                fuelmodeStr = "Uranium";
            }
        }

        public override bool getIsNuclear() {
            return true;
        }

        public override bool isNeutronRich() {
            return true;
        }

        public override bool shouldScaleDownJetISP() {
            return true;
        }

        public override float getCoreTemp() {
            if (uranium_fuel) {
                return ReactorTemp;
            } else {
                return (float) (ReactorTemp * GameConstants.thorium_temperature_ratio_factor);
            }
        }

        public override float getMinimumThermalPower() {
            return getThermalPower() * minimumThrottle;
        }

        public override void OnStart(PartModule.StartState state) {
            eu = part.Resources["EnrichedUranium"];
            thf4 = part.Resources["ThF4"];
            actinides = part.Resources["Actinides"];
            Fields["fuelmodeStr"].guiActiveEditor = false;
            //if (double.IsNaN(uf4.amount)) {
            //    uf4.amount = 0;
            //}
            //if (double.IsNaN(thf4.amount)) {
            //    thf4.amount = 0;
            //}
            //if (double.IsNaN(actinides.amount)) {
            //    actinides.amount = actinides.maxAmount;
            //}
            //if (!upgradedToV08) {
            //    upgradedToV08 = true;
            //    actinides.amount = actinides.maxAmount - uf4.amount;
            //}
            //if (!upgradedToV10 && state != PartModule.StartState.Editor) {
            //    upgradedToV10 = true;
            //    actinides.amount = actinides.amount * 1000;
            //    actinides.maxAmount = actinides.maxAmount * 1000;
            //    uf4.amount = uf4.amount * 1000;
            //    uf4.maxAmount = uf4.maxAmount * 1000;
            //    thf4.amount = thf4.amount * 1000;
            //    thf4.maxAmount = thf4.maxAmount * 1000;
            //} else if (!upgradedToV10 && state == PartModule.StartState.Editor) {
            //    upgradedToV10 = true;
            //}
            if (uranium_fuel)
            {
                fuel_resource = eu;
            }
            else
            {
                fuel_resource = thf4;
            }
            base.OnStart(state);
            initial_thermal_power = ThermalPower;
            initial_resource_rate = resourceRate;
            if (uranium_fuel) {
                setUraniumFuel();
            } else {
                setThoriumFuel();
            }
        }

        public override void OnUpdate() {
            Events["ManualRestart"].active = Events["ManualRestart"].guiActiveUnfocused = !IsEnabled && !decay_products_ongoing;
            Events["ManualShutdown"].active = Events["ManualShutdown"].guiActiveUnfocused = IsEnabled;
            Events["RefuelUranium"].active = Events["RefuelUranium"].guiActiveUnfocused = !IsEnabled && !decay_products_ongoing && uranium_fuel;
            //Events["RefuelThorium"].active = Events["RefuelThorium"].guiActiveUnfocused = !IsEnabled && !decay_products_ongoing && !uranium_fuel;
            //Events["SwapFuel"].active = Events["SwapFuel"].guiActiveUnfocused = !IsEnabled && !decay_products_ongoing;
            base.OnUpdate();
        }

        public override void OnFixedUpdate() {
            base.OnFixedUpdate();
        }

        protected override double consumeReactorResource(double resource) {
            double fuel_to_actinides_ratio = fuel_resource.amount / (actinides.amount + fuel_resource.amount) * fuel_resource.amount / (actinides.amount + fuel_resource.amount);
            if (!uranium_fuel) {
                if (!double.IsInfinity(fuel_to_actinides_ratio) && !double.IsNaN(fuel_to_actinides_ratio)) {
                    resource = resource * Math.Min(Math.Exp(-GameConstants.thorium_actinides_ratio_factor / fuel_to_actinides_ratio + 1), 1);
                }
            }
            double actinides_max_amount = actinides.maxAmount;
            resource = Math.Min(fuel_resource.amount, resource);
            fuel_resource.amount -= resource;
            actinides.amount += resource;
            if (actinides.amount > actinides_max_amount) {
                actinides.amount = actinides_max_amount;
            }
            return resource;
        }

        protected override double returnReactorResource(double resource) {
            fuel_resource.amount += resource;
            double actinides_current_amount = actinides.amount;
            if (fuel_resource.amount > fuel_resource.maxAmount) {
                fuel_resource.amount = fuel_resource.maxAmount;
            }
            actinides.amount -= Math.Min(resource, actinides_current_amount);
            return resource;
        }
        
        protected override string getResourceDeprivedMessage() {
            if (uranium_fuel) {
                return "Enriched Uranium Deprived";
            } else {
                return "ThF4 Deprived";
            }
        }

        protected void setThoriumFuel() {
            fuel_resource = thf4;
            fuelmodeStr = "Thorium";
            ThermalPower = (float)(initial_thermal_power * GameConstants.thorium_power_output_ratio);
            resourceRate = (float)(initial_resource_rate * GameConstants.thorium_resource_burnrate_ratio);
            uranium_fuel = false;
        }

        protected void setUraniumFuel() {
            fuel_resource = eu;
            fuelmodeStr = "Uranium";
            ThermalPower = (float)(initial_thermal_power);
            resourceRate = (float)(initial_resource_rate);
            uranium_fuel = true;
        }

        protected void defuelThorium() {
            List<PartResource> swap_resource_list = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("ThF4").id, PartResourceLibrary.Instance.GetDefinition("ThF4").resourceFlowMode, swap_resource_list);
            foreach (PartResource thf4_resource in swap_resource_list) {
                if (thf4_resource.part.FindModulesImplementing<FNNuclearReactor>().Count == 0) {
                    double spare_capacity_for_thf4 = thf4_resource.maxAmount - thf4_resource.amount;
                    double thf4_added = Math.Min(thf4.amount, spare_capacity_for_thf4);
                    thf4.amount -= thf4_added;
                    thf4_resource.amount += thf4_added;
                }
            }
        }

        protected void defuelUranium() {
            List<PartResource> swap_resource_list = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("EnrichedUranium").id, ResourceFlowMode.ALL_VESSEL, swap_resource_list);
            foreach (PartResource eu_resource in swap_resource_list) {
                if (eu_resource.part.FindModulesImplementing<FNNuclearReactor>().Count == 0) {
                    double spare_capacity_for_eu = eu_resource.maxAmount - eu_resource.amount;
                    double eu_added = Math.Min(eu.amount, spare_capacity_for_eu);
                    eu.amount -= eu_added;
                    eu_resource.amount += eu_added;
                }
            }
        }

        
    }
}
