using System.ComponentModel.Design;
using System.Text;
using UnityEngine;

namespace InterstellarPlugin.PartUpgrades
{
    internal static class Parts
    {
        public static string PrefabName(this Part part)
        {
            if (part.partInfo == null)
                Debug.Log("partInfo == null in " + part.partName);
            if (part.partInfo.partPrefab == null)
                Debug.Log("partPrefab == null in " + part.partName);
            return part.partInfo.partPrefab.partName;
        }

        public static string LogPartName(this Part part)
        {
            return "PART INSPECT: " + InspectPart(part);
        }

        private static StringBuilder InspectPart(Part part)
        {
            var sb = new StringBuilder();
            sb
                .Append("ToString: ").Append(part.ToString())
                .Append(", name: ").Append(part.name)
                .Append(", partName: ").Append(part.partName)
                .Append(", ClassName").Append(part.ClassName);
            AvailablePart partInfo = part.partInfo;
            if (partInfo == null)
                return sb.Append(", partInfo = null");

            sb
                .Append(", partInfo.ToString: ").Append(partInfo)
                .Append(", partInfo.name").Append(partInfo.name);

            Part partPrefab = partInfo.partPrefab;
            if (partPrefab == null)
                return sb.Append(", partPrefab == null");

            sb
                .Append(", partPrefab.ToString: ").Append(partPrefab)
                .Append(", partPrefab.name: ").Append(partPrefab.name)
                .Append(", partPrefab.partName: ").Append(partPrefab.partName)
                .Append(", partPrefab.ClassName").Append(partPrefab.ClassName);

            return sb;
        }
    }
}