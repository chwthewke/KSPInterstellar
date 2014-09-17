using System;
using System.Linq;
using UnityEngine;

namespace InterstellarPlugin
{
    class AtmosphericIntake : PartModule
    {
        [KSPField(isPersistant = false)]
        public float area;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Intake Atmosphere")]
        public string intakeval;
        protected float airf;

        protected PartResource intake_atm = null;

        public override void OnStart(PartModule.StartState state)
        {

            if (state == StartState.Editor) { return; }
            this.part.force_activate();

            PartResourceList prl = part.Resources;

            foreach (PartResource wanted_resource in prl)
            {
                if (wanted_resource.resourceName == "IntakeAtm")
                {
                    intake_atm = wanted_resource;
                }
            }

            if (intake_atm == null)
            {
                Debug.LogWarning(string.Format("Part {0} has AtmosphericIntake but no IntakeAtm resource (has {1})",
                    part, string.Join(", ", prl.list.Select(r => r.ToString()).ToArray())));
            }
        }

        public override void OnUpdate()
        {
            intakeval = airf.ToString("0.00") + " kg";
        }

        public override void OnFixedUpdate()
        {
            if (intake_atm == null)
                return;

            double resourcedensity = PartResourceLibrary.Instance.GetDefinition("IntakeAtm").density;
            double airdensity = part.vessel.atmDensity / 1000;

            double airspeed = part.vessel.srf_velocity.magnitude + 100.0;
            double air = airspeed * airdensity * area / resourcedensity * TimeWarp.fixedDeltaTime;

            airf = (float)(1000.0 * air / TimeWarp.fixedDeltaTime * resourcedensity);


            air = intake_atm.amount = Math.Min(air / TimeWarp.fixedDeltaTime, intake_atm.maxAmount);

            part.RequestResource("IntakeAtm", -air);
        }

        public float getAtmosphericOutput()
        {
            return airf;
        }

    }
}
