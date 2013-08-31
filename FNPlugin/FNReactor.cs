﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FNPlugin {
    class FNReactor : PartModule    {
        [KSPField(isPersistant = false)]
        public float ReactorTemp;
        [KSPField(isPersistant = false)]
        public float ThermalPower;
        [KSPField(isPersistant = false)]
        public float upgradedReactorTemp;
        [KSPField(isPersistant = false)]
        public float upgradedThermalPower;
        [KSPField(isPersistant = false)]
        public float upgradedUF6Rate;
        [KSPField(isPersistant = false)]
        public float AntimatterRate;
        [KSPField(isPersistant = false)]
        public float upgradedAntimatterRate;
        [KSPField(isPersistant = false)]
        public float UF6Rate;
        [KSPField(isPersistant = true)]
        public bool IsEnabled = true;
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = false)]
        public string upgradedName;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public float upgradeCost;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
        public string reactorType;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Core Temp")]
        public string coretempStr;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
		public string statusStr;
        //[KSPField(isPersistant = false, guiActive = true, guiName = "Thermal Isp")]
        //public string thermalISPStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade")]
        public string upgradeCostStr;
        [KSPField(isPersistant = true)]
        public float last_active_time;

        protected bool hasScience = false;

        protected bool isNuclear = false;

        protected float myScience = 0;

		protected float powerPcnt = 0;

        protected bool responsible_for_thermalmanager = false;
        protected FNResourceManager thermalmanager;
        

        [KSPEvent(guiActive = true, guiName = "Activate Reactor", active = false)]
        public void ActivateReactor() {
            if (isNuclear) { return; }
            IsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Reactor", active = true)]
        public void DeactivateReactor() {
            if (isNuclear) { return; }
            IsEnabled = false;
        }

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitReactor() {
            if (isupgraded || !hasScience || myScience < upgradeCost) { return; } 
            isupgraded = true;
            ThermalPower = upgradedThermalPower;
            ReactorTemp = upgradedReactorTemp;
            UF6Rate = upgradedUF6Rate;
            AntimatterRate = upgradedAntimatterRate;
            List<Part> vessel_parts = this.vessel.parts;
            foreach (Part vessel_part in vessel_parts) {
                var thisModule = vessel_part.Modules["FNNozzleController"] as FNNozzleController;
                if (thisModule != null) {
                    thisModule.setupPropellants();
                }
                var thisModule2 = vessel_part.Modules["FNGenerator"] as FNGenerator;
                if (thisModule2 != null) {
                    thisModule2.recalculatePower();
                }
            }
            reactorType = upgradedName;
            part.RequestResource("Science", upgradeCost);
            //IsEnabled = false;
        }

        [KSPAction("Activate Reactor")]
        public void ActivateReactorAction(KSPActionParam param) {
            if (isNuclear) { return; }
            ActivateReactor();
        }

        [KSPAction("Deactivate Reactor")]
        public void DeactivateReactorAction(KSPActionParam param) {
            if (isNuclear) { return; }
            DeactivateReactor();
        }

        [KSPAction("Toggle Reactor")]
        public void ToggleReactorAction(KSPActionParam param) {
            if (isNuclear) { return; }
            IsEnabled = !IsEnabled;
        }

        private bool init = false;

        public override void OnLoad(ConfigNode node) {
            if (isupgraded) {
                ThermalPower = upgradedThermalPower;
                ReactorTemp = upgradedReactorTemp;
                UF6Rate = upgradedUF6Rate;
                reactorType = upgradedName;
                AntimatterRate = upgradedAntimatterRate;
            }else {
                reactorType = originalName;
            }
        }

        public override void OnStart(PartModule.StartState state) {
            Actions["ActivateReactorAction"].guiName = Events["ActivateReactor"].guiName = String.Format("Activate Reactor");
            Actions["DeactivateReactorAction"].guiName = Events["DeactivateReactor"].guiName = String.Format("Deactivate Reactor");
            Actions["ToggleReactorAction"].guiName = String.Format("Toggle Reactor");
            
            if (state == StartState.Editor) { return; }

            if (FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_THERMALPOWER).hasManagerForVessel(vessel)) {
                thermalmanager = FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_THERMALPOWER).getManagerForVessel(vessel);
                responsible_for_thermalmanager = false;

            }else {
                thermalmanager = FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_THERMALPOWER).createManagerForVessel(this);
                responsible_for_thermalmanager = true;
                print("[WarpPlugin] Creating ThermalPower Manager for Vessel");
            }

            List<PartResource> partresources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Science").id, partresources);
            if (partresources.Count > 0) {
                hasScience = true;
            }
            //hasScience = true;
            this.part.force_activate();

            //print(last_active_time);
            if (IsEnabled && last_active_time != 0) {
                double now = Planetarium.GetUniversalTime();
                double time_diff = now - last_active_time;
                //print(time_diff);
                if (UF6Rate <= 0) {
                    List<PartResource> antimatter_resources = new List<PartResource>();
                    part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Antimatter").id, antimatter_resources);
                    float antimatter_current_amount = 0;
                    foreach (PartResource antimatter_resource in antimatter_resources) {
                        antimatter_current_amount += (float)antimatter_resource.amount;
                    }
                    float antimatter_to_take = (float) Math.Min(antimatter_current_amount, AntimatterRate * time_diff);
                    part.RequestResource("Antimatter", antimatter_to_take);
                    //print(antimatter_to_take);
                }else {
                    List<PartResource> uf6_resources = new List<PartResource>();
                    part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("UF6").id, uf6_resources);
                    float uf6_current_amount = 0;
                    foreach (PartResource uf6_resource in uf6_resources) {
                        uf6_current_amount += (float)uf6_resource.amount;
                    }
                    float uf6_to_take = (float)Math.Min(uf6_current_amount, UF6Rate * time_diff);
                    part.RequestResource("UF6", uf6_to_take);
                    part.RequestResource("DUF6", -uf6_to_take);
                }
            }

            
        }

                
        public override void OnUpdate() {
            Events["ActivateReactor"].active = !IsEnabled && !isNuclear;
            Events["DeactivateReactor"].active = IsEnabled && !isNuclear;
            Events["RetrofitReactor"].active = !isupgraded && hasScience && myScience >= upgradeCost;
            Fields["upgradeCostStr"].guiActive = !isupgraded;

            coretempStr = ReactorTemp.ToString("0") + "K";
            //thermalISPStr = (Math.Sqrt(ReactorTemp) * 17).ToString("0.0") + "s";

            

            //if (isNuclear) {
                List<PartResource> partresources = new List<PartResource>();
                part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Science").id, partresources);
                float currentscience = 0;
                foreach (PartResource partresource in partresources) {
                    currentscience += (float)partresource.amount;
                }
                myScience = currentscience;

                upgradeCostStr = currentscience.ToString("0") + "/" + upgradeCost.ToString("0") + " Science";
            //}

			if (IsEnabled) {
				if (powerPcnt > 0) {
					statusStr = "Active (" + powerPcnt.ToString ("0.0") + "%)";
				} else {
					if (isNuclear) {
						statusStr = "Antimatter Deprived.";
					}else {
						statusStr = "UF6 Deprived.";
					}
				}
			} else {
				statusStr = "Reactor Offline.";
			}
        }

        public float getReactorTemp() {
            return ReactorTemp;
        }

        public float getReactorThermalPower() {
            return ThermalPower;
        }

        
        public override void OnFixedUpdate() {
            if (thermalmanager.getVessel() != vessel) {
                FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_THERMALPOWER).deleteManager(thermalmanager);
            }

            if (!FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_THERMALPOWER).hasManagerForVessel(vessel)) {
                thermalmanager = FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_THERMALPOWER).createManagerForVessel(this);
                responsible_for_thermalmanager = true;
                print("[WarpPlugin] Creating ThermalPower Manager for Vessel");
            }

            if (responsible_for_thermalmanager) {
                thermalmanager.update();
            }

            if (UF6Rate > 0) {
                isNuclear = true;
            }

            if (IsEnabled) {
                if (!isNuclear) {
                    float antimatter_provided = part.RequestResource("Antimatter", AntimatterRate * TimeWarp.fixedDeltaTime);

                    float antimatter_pcnt = antimatter_provided / AntimatterRate / TimeWarp.fixedDeltaTime;
					powerPcnt = antimatter_pcnt*100.0f;
                    //part.RequestResource("ThermalPower", -ThermalPower * TimeWarp.fixedDeltaTime * antimatter_pcnt);
                    thermalmanager.powerSupply(ThermalPower * TimeWarp.fixedDeltaTime * antimatter_pcnt);
                }else {
                    float uf6_provided = part.RequestResource("UF6", UF6Rate * TimeWarp.fixedDeltaTime);
                    part.RequestResource("DUF6", -uf6_provided);

                    float uf6_pcnt = uf6_provided / UF6Rate / TimeWarp.fixedDeltaTime;
					powerPcnt = uf6_pcnt * 100.0f;
                    //part.RequestResource("ThermalPower", -ThermalPower * TimeWarp.fixedDeltaTime * uf6_pcnt);
                    thermalmanager.powerSupply(ThermalPower * TimeWarp.fixedDeltaTime * uf6_pcnt);
                }
                if (Planetarium.GetUniversalTime() != 0) {
                    last_active_time = (float) Planetarium.GetUniversalTime();
                }
                
            }
            
        }

        public override string GetInfo() {
			if (UF6Rate > 0) {
				float uf6_rate_per_day = UF6Rate * 86400;
				return String.Format ("Core Temperature: {0}K\n Thermal Power: {1}MW\n UF6 Consumption Rate: {2}L/day\n", ReactorTemp, ThermalPower, uf6_rate_per_day);
			} else {
				return String.Format ("Core Temperature: {0}K\n Thermal Power: {1}MW\n Antimatter Consumption Rate: {2}mg/sec\n", ReactorTemp, ThermalPower, AntimatterRate);
			}
        }
    }
}
