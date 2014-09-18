namespace InterstellarPlugin.PartUpgrades
{
    internal static class Parts
    {
        public static string OriginalName(this Part part)
        {
            return part.partInfo == null ? part.name : part.partInfo.name;
        }
    }
}