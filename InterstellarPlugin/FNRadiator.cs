using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace InterstellarPlugin
{
    [KSPModule("Radiator")]
    class FNRadiator : FNResourceSuppliableModule, FNUpgradeableModule
    {
        [KSPField(isPersistant = true)]
        public bool radiatorIsEnabled;
        [KSPField(isPersistant = true)]
        public bool isupgraded;
        [KSPField(isPersistant = true)]
        public bool radiatorInit;

        [KSPField(isPersistant = false)]
        public string upgradeTechReq;
        public string UpgradeTechReq { get { return upgradeTechReq; } }
        [KSPField(isPersistant = false)]
        public bool isDeployable = true;
        [KSPField(isPersistant = false)]
        public float convectiveBonus = 1.0f;
        [KSPField(isPersistant = false)]
        public string animName;
        [KSPField(isPersistant = false)]
        public float radiatorTemp;
        [KSPField(isPersistant = false)]
        public float radiatorArea;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public float upgradeCost = 100;
        [KSPField(isPersistant = false)]
        public string upgradedName;
        [KSPField(isPersistant = false)]
        public float upgradedRadiatorTemp;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Type")]
        public string radiatorType;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Temperature")]
        public string radiatorTempStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power Radiated")]
        public string thermalPowerDissipStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power Convected")]
        public string thermalPowerConvStr;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Upgrade")]
        public string upgradeCostStr;

        //public static double stefan_const = 5.6704e-8;
        protected static float rad_const_h = 1000;
        protected static double alpha = 0.001998001998001998001998001998;

        protected Animation anim;
        protected float radiatedThermalPower;
        protected float convectedThermalPower;
        protected double current_rad_temp;
        protected float myScience = 0;
        protected float directionrotate = 1;
        protected float oldangle = 0;
        protected Vector3 original_eulers;
        protected Transform pivot;
        protected long last_draw_update = 0;
        protected long update_count = 0;
        protected bool hasrequiredupgrade;
        protected int explode_counter = 0;

        protected static List<FNRadiator> list_of_radiators = new List<FNRadiator>();


        public static List<FNRadiator> getRadiatorsForVessel(Vessel vess)
        {
            List<FNRadiator> list_of_radiators_for_vessel = new List<FNRadiator>();
            list_of_radiators.RemoveAll(item => item == null);
            foreach (FNRadiator radiator in list_of_radiators)
            {
                if (radiator.vessel == vess)
                {
                    list_of_radiators_for_vessel.Add(radiator);
                }
            }
            return list_of_radiators_for_vessel;
        }

        public static bool hasRadiatorsForVessel(Vessel vess)
        {
            list_of_radiators.RemoveAll(item => item == null);
            bool has_radiators = false;
            foreach (FNRadiator radiator in list_of_radiators)
            {
                if (radiator.vessel == vess)
                {
                    has_radiators = true;
                }
            }
            return has_radiators;
        }

        public static double getAverageRadiatorTemperatureForVessel(Vessel vess)
        {
            list_of_radiators.RemoveAll(item => item == null);
            double average_temp = 0;
            double n_radiators = 0;
            foreach (FNRadiator radiator in list_of_radiators)
            {
                if (radiator.vessel == vess)
                {
                    average_temp += radiator.getRadiatorTemperature();
                    n_radiators += 1.0f;
                }
            }

            if (n_radiators > 0)
            {
                average_temp = average_temp / n_radiators;
            }
            else
            {
                average_temp = 0;
            }

            return average_temp;
        }

        public static double getAverageMaximumRadiatorTemperatureForVessel(Vessel vess)
        {
            list_of_radiators.RemoveAll(item => item == null);
            double average_temp = 0;
            double n_radiators = 0;
            foreach (FNRadiator radiator in list_of_radiators)
            {
                if (radiator.vessel == vess)
                {
                    average_temp += radiator.radiatorTemp;
                    n_radiators += 1.0f;
                }
            }

            if (n_radiators > 0)
            {
                average_temp = average_temp / n_radiators;
            }
            else
            {
                average_temp = 0;
            }

            return average_temp;
        }


        [KSPEvent(guiActive = true, guiName = "Deploy Radiator", active = true)]
        public void DeployRadiator()
        {
            if (!isDeployable)
            {
                return;
            }
            anim[animName].speed = 1f;
            anim[animName].normalizedTime = 0f;
            anim.Blend(animName, 2f);
            radiatorIsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "Retract Radiator", active = false)]
        public void RetractRadiator()
        {
            if (!isDeployable)
            {
                return;
            }
            anim[animName].speed = -1f;
            anim[animName].normalizedTime = 1f;
            anim.Blend(animName, 2f);
            radiatorIsEnabled = false;
        }

        //[KSPEvent(guiActive = false, guiName = "Retrofit", active = true)]
        //public void RetrofitRadiator()
        //{
        //    if (ResearchAndDevelopment.Instance == null) { return; }
        //    if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; }

        //    isupgraded = true;
        //    radiatorType = upgradedName;
        //    radiatorTemp = upgradedRadiatorTemp;
        //    radiatorTempStr = radiatorTemp + "K";

        //    ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science - upgradeCost;
        //}

        public void upgradePartModule()
        {
            isupgraded = true;

            radiatorType = upgradedName;
            radiatorTemp = upgradedRadiatorTemp;
            radiatorTempStr = radiatorTemp + "K";
        }

        [KSPAction("Deploy Radiator")]
        public void DeployRadiatorAction(KSPActionParam param)
        {
            DeployRadiator();
        }

        [KSPAction("Retract Radiator")]
        public void RetractRadiatorAction(KSPActionParam param)
        {
            RetractRadiator();
        }

        [KSPAction("Toggle Radiator")]
        public void ToggleRadiatorAction(KSPActionParam param)
        {
            if (radiatorIsEnabled)
            {
                RetractRadiator();
            }
            else
            {
                DeployRadiator();
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            Fields["radiatorType"].guiActive = Fields["radiatorType"].guiActiveEditor = this.IsUpgradeable();

            Actions["DeployRadiatorAction"].guiName = Events["DeployRadiator"].guiName = String.Format("Deploy Radiator");
            Actions["RetractRadiatorAction"].guiName = Events["RetractRadiator"].guiName = String.Format("Retract Radiator");
            Actions["ToggleRadiatorAction"].guiName = String.Format("Toggle Radiator");


            FNRadiator.list_of_radiators.Add(this);

            anim = part.FindModelAnimators(animName).FirstOrDefault();
            if (anim != null)
            {
                anim[animName].layer = 1;

                if (radiatorIsEnabled)
                {
                    anim.Blend(animName, 1, 0);
                }
                else
                {
                    //anim.Blend (animName, 0, 0);
                }
                //anim.Play ();
            }

            if (isDeployable)
            {
                pivot = part.FindModelTransform("suntransform");
                original_eulers = pivot.transform.localEulerAngles;
            }
            else
            {
                radiatorIsEnabled = true;
            }

            //if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER | HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
            //{
            //    if (PluginHelper.hasTech(upgradeTechReq))
            //    {
            //        hasrequiredupgrade = true;
            //    }
            //}
            //else
            //{
            //    hasrequiredupgrade = true;
            //}

            if (radiatorInit == false)
            {
                radiatorInit = true;
            }

                radiatorType = originalName;

            radiatorTempStr = radiatorTemp + "K";
            this.part.force_activate();
        }

        public override void OnUpdate()
        {
            Events["DeployRadiator"].active = !radiatorIsEnabled && isDeployable;
            Events["RetractRadiator"].active = radiatorIsEnabled && isDeployable;
            //if (ResearchAndDevelopment.Instance != null)
            //{
            //    Events["RetrofitRadiator"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
            //}
            //else
            //{
            //    Events["RetrofitRadiator"].active = false;
            //}
            //Fields["upgradeCostStr"].guiActive = !isupgraded && hasrequiredupgrade;

            //if (ResearchAndDevelopment.Instance != null)
            //{
            //    upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString("0") + " Science";
            //}


            if (update_count - last_draw_update > 8)
            {
                thermalPowerDissipStr = radiatedThermalPower.ToString("0.000") + "MW";
                thermalPowerConvStr = convectedThermalPower.ToString("0.000") + "MW";
                radiatorTempStr = current_rad_temp.ToString("0.0") + "K / " + radiatorTemp.ToString("0.0") + "K";

                last_draw_update = update_count;
            }


            if (!PluginHelper.isRadiatorEmissiveGlowDisabled())
                colorHeat();

            update_count++;

        }

        public void colorHeat()
        {
            const String KSPShader = "KSP/Emissive/Bumped Specular";
            float currentTemperature = getRadiatorTemperature();

            double temperatureRatio = currentTemperature / radiatorTemp;
            Color emissiveColor = new Color((float)(Math.Pow(temperatureRatio, 3)), 0.0f, 0.0f, 1.0f);

            Renderer[] array = part.FindModelComponents<Renderer>();


            for (int i = 0; i < array.Length; i++)
            {
                Renderer renderer = array[i];
                if (renderer.material.shader.name != KSPShader)
                    renderer.material.shader = Shader.Find(KSPShader);

                else if (part.name.Equals("RadialRadiator"))
                {

                    if (renderer.material.GetTexture("_Emissive") == null)
                        renderer.material.SetTexture("_Emissive", GameDatabase.Instance.GetTexture("Interstellar/Parts/Electrical/RadialRadiator/d_glow", false));

                    //Debug.Log("rd _Emissive: " + renderer.material.GetTexture("_Emissive"));

                }

                else if (part.name.Equals("DeployableRadiator"))
                {
                    // deployable radiators have already everything set up
                }
                else // unknown radiator
                {
                    return;
                }

                renderer.material.SetColor("_EmissiveColor", emissiveColor);
            }
        }

        public override void OnFixedUpdate()
        {
            float atmosphere_height = vessel.mainBody.maxAtmosphereAltitude;
            float vessel_height = (float)vessel.mainBody.GetAltitude(vessel.transform.position);
            float conv_power_dissip = 0;
            if (vessel.altitude <= PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody))
            {
                float pressure = (float)FlightGlobals.getStaticPressure(vessel.transform.position);
                float dynamic_pressure = (float)(0.5 * pressure * 1.2041 * vessel.srf_velocity.sqrMagnitude / 101325.0);
                pressure += dynamic_pressure;
                float low_temp = FlightGlobals.getExternalTemperature(vessel.transform.position);

                float delta_temp = Mathf.Max(0, radiatorTemp - low_temp);
                conv_power_dissip = pressure * delta_temp * radiatorArea * rad_const_h / 1e6f * TimeWarp.fixedDeltaTime * convectiveBonus;
                if (!radiatorIsEnabled)
                {
                    conv_power_dissip = conv_power_dissip / 2.0f;
                }
                convectedThermalPower = consumeFNResource(conv_power_dissip, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;

                if (radiatorIsEnabled && dynamic_pressure > 1.4854428818159388107574636072046e-3 && isDeployable)
                {
                    part.deactivate();

                    //part.breakingForce = 1;
                    //part.breakingTorque = 1;
                    part.decouple(1);
                }
            }
            else
            {
                convectedThermalPower = 0;
            }


            if (radiatorIsEnabled)
            {
                if (getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT) >= 1 && current_rad_temp >= radiatorTemp)
                {
                    explode_counter++;
                    if (explode_counter > 25)
                    {
                        part.explode();
                    }
                }
                else
                {
                    explode_counter = 0;
                }

                double radiator_temperature_temp_val = radiatorTemp * Math.Pow(getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT), 0.25);
                if (FNReactor.hasActiveReactors(vessel))
                {
                    radiator_temperature_temp_val = Math.Min(FNReactor.getTemperatureofColdestReactor(vessel) / 1.01, radiator_temperature_temp_val);
                }

                double thermal_power_dissip = (GameConstants.stefan_const * radiatorArea * Math.Pow(radiator_temperature_temp_val, 4) / 1e6) * TimeWarp.fixedDeltaTime;
                radiatedThermalPower = consumeFNResource(thermal_power_dissip, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;
                double instantaneous_rad_temp = (Math.Min(Math.Pow(radiatedThermalPower * 1e6 / (GameConstants.stefan_const * radiatorArea), 0.25), radiatorTemp));
                instantaneous_rad_temp = Math.Max(instantaneous_rad_temp, Math.Max(FlightGlobals.getExternalTemperature((float)vessel.altitude, vessel.mainBody) + 273.16, 2.7));
                if (current_rad_temp <= 0)
                {
                    current_rad_temp = instantaneous_rad_temp;
                }
                else
                {
                    current_rad_temp = instantaneous_rad_temp * alpha + (1.0 - alpha) * instantaneous_rad_temp;
                }

                if (isDeployable)
                {
                    Vector3 pivrot = pivot.rotation.eulerAngles;

                    pivot.Rotate(Vector3.up * 5f * TimeWarp.fixedDeltaTime * directionrotate);

                    Vector3 sunpos = FlightGlobals.Bodies[0].transform.position;
                    Vector3 flatVectorToTarget = sunpos - transform.position;

                    flatVectorToTarget = flatVectorToTarget.normalized;
                    float dot = Mathf.Asin(Vector3.Dot(pivot.transform.right, flatVectorToTarget)) / Mathf.PI * 180.0f;

                    float anglediff = -dot;
                    oldangle = dot;
                    //print (dot);
                    directionrotate = anglediff / 5 / TimeWarp.fixedDeltaTime;
                    directionrotate = Mathf.Min(3, directionrotate);
                    directionrotate = Mathf.Max(-3, directionrotate);

                    part.maximum_drag = 0.8f;
                    part.minimum_drag = 0.8f;
                }

            }
            else
            {
                if (isDeployable)
                {
                    pivot.transform.localEulerAngles = original_eulers;
                }

                double radiator_temperature_temp_val = radiatorTemp * Math.Pow(getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT), 0.25);
                if (FNReactor.hasActiveReactors(vessel))
                {
                    radiator_temperature_temp_val = Math.Min(FNReactor.getTemperatureofColdestReactor(vessel) / 1.01, radiator_temperature_temp_val);
                }

                double thermal_power_dissip = (GameConstants.stefan_const * radiatorArea * Math.Pow(radiator_temperature_temp_val, 4) / 1e7) * TimeWarp.fixedDeltaTime;
                radiatedThermalPower = consumeFNResource(thermal_power_dissip, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;
                double instantaneous_rad_temp = (Math.Min(Math.Pow(radiatedThermalPower * 1e7 / (GameConstants.stefan_const * radiatorArea), 0.25), radiatorTemp));
                instantaneous_rad_temp = Math.Max(instantaneous_rad_temp, Math.Max(FlightGlobals.getExternalTemperature((float)vessel.altitude, vessel.mainBody) + 273.16, 2.7));
                if (current_rad_temp <= 0)
                {
                    current_rad_temp = instantaneous_rad_temp;
                }
                else
                {
                    current_rad_temp = instantaneous_rad_temp * alpha + (1.0 - alpha) * instantaneous_rad_temp;
                }

                part.maximum_drag = 0.2f;
                part.minimum_drag = 0.2f;
            }



        }

        //public bool hasTechsRequiredToUpgrade()
        //{
        //    if (HighLogic.CurrentGame != null)
        //    {
        //        if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER | HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
        //        {
        //            if (upgradeTechReq != null)
        //            {
        //                if (PluginHelper.hasTech(upgradeTechReq))
        //                {
        //                    return true;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        public float getRadiatorTemperature()
        {
            return (float)current_rad_temp;
        }

        public override string GetInfo()
        {
            var temps = new SortedDictionary<float, float>();
            const int tempScale = 500;
            
            for (int temp = tempScale; temp < (this.IsUpgradeable() ? upgradedRadiatorTemp : radiatorTemp); temp += tempScale)
                AddTempPoint(temps, temp);
            AddTempPoint(temps, radiatorTemp);
            if (this.IsUpgradeable())
                AddTempPoint(temps, upgradedRadiatorTemp);

            const string variantInfoFormat = "Part Name: {0}\nHeat Radiated (max): {1:n0} MW";
            const string tempPointFormat = "\n  at {0}K: {1:n0} MW";

            var b = new StringBuilder();
            b.AppendFormat(variantInfoFormat, originalName, HeatDissipation(radiatorTemp));
            b.Append("\nRadiator Performance");
            foreach (var tempPoint in temps)
                b.AppendFormat(tempPointFormat, tempPoint.Key, tempPoint.Value);
            if (this.IsUpgradeable())
            {
                b.AppendFormat("\n\n[Upgraded with {0} to:]\n", PluginHelper.GetTechName(upgradeTechReq));
                b.AppendFormat(variantInfoFormat, upgradedName, HeatDissipation(upgradedRadiatorTemp));
            }

            return b.ToString();
        }

        private void AddTempPoint(IDictionary<float, float> temps, float temp)
        {
            temps[temp] = HeatDissipation(temp);
        }

        private float HeatDissipation(float temp)
        {
            return (float) (GameConstants.stefan_const * radiatorArea * Math.Pow(temp, 4) / 1e6);
        }

        public override int getPowerPriority()
        {
            return 3;
        }

    }
}

