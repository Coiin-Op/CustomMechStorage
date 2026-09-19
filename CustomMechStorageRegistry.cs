using System.Collections.Generic;
using BattleTech;

namespace CustomMechStorage
{
    public static class CustomMechStorageRegistry
    {
        public static readonly Dictionary<string, MechDef> StoredMechs =
            new Dictionary<string, MechDef>();
    }
}