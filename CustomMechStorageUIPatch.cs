using System;
using System.Collections.Generic;
using System.IO;
using BattleTech;
using HarmonyLib;

namespace CustomMechStorage
{
    [HarmonyPatch(
        typeof(SimGameState),
        nameof(SimGameState.GetAllInventoryMechDefs))]
    public static class CustomMechStorageUIPatch
    {
        [HarmonyPostfix]
        public static void GetAllInventoryMechDefs_Postfix(
            ref List<MechDef> __result)
        {
            try
            {
                if (__result == null)
                {
                    Log(
                        "UI PATCH ERROR: GetAllInventoryMechDefs returned NULL");

                    return;
                }

                if (CustomMechStorageRegistry.StoredMechs.Count == 0)
                {
                    Log(
                        "UI PATCH: No custom stored mechs to add");

                    return;
                }

                int originalCount =
                    __result.Count;

                Log(
                    "UI PATCH: GetAllInventoryMechDefs | VanillaCount=" +
                    originalCount +
                    " | CustomCount=" +
                    CustomMechStorageRegistry.StoredMechs.Count);

                foreach (
                    KeyValuePair<string, MechDef> pair
                    in CustomMechStorageRegistry.StoredMechs)
                {
                    MechDef mechDef = pair.Value;

                    if (mechDef == null)
                    {
                        Log(
                            "UI PATCH: Skipping NULL MechDef | Key=" +
                            pair.Key);

                        continue;
                    }

                    bool alreadyPresent = false;

                    foreach (MechDef existingMech in __result)
                    {
                        if (existingMech == null)
                        {
                            continue;
                        }

                        if (existingMech.GUID == mechDef.GUID)
                        {
                            alreadyPresent = true;
                            break;
                        }
                    }

                    if (alreadyPresent)
                    {
                        Log(
                            "UI PATCH: Already in inventory result | Key=" +
                            pair.Key +
                            " | GUID=" +
                            mechDef.GUID);

                        continue;
                    }

                    __result.Add(mechDef);

                    Log(
                        "UI PATCH: Added custom mech to inventory result | Key=" +
                        pair.Key +
                        " | Name=" +
                        mechDef.Description.Name +
                        " | GUID=" +
                        mechDef.GUID);
                }

                Log(
                    "UI PATCH COMPLETE | FinalInventoryResultCount=" +
                    __result.Count);
            }
            catch (Exception ex)
            {
                Log(
                    "UI PATCH ERROR: " +
                    ex);
            }
        }

        private static void Log(string message)
        {
            try
            {
                if (string.IsNullOrEmpty(
                    CustomMechStorageMod.LogPath))
                {
                    return;
                }

                File.AppendAllText(
                    CustomMechStorageMod.LogPath,
                    DateTime.Now.ToString("HH:mm:ss.fff") +
                    " " +
                    message +
                    "\r\n");
            }
            catch
            {
            }
        }
    }
}