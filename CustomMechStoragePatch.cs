using System;
using System.IO;
using BattleTech;
using HarmonyLib;

namespace CustomMechStorage
{
    [HarmonyPatch(typeof(SimGameState), nameof(SimGameState.UnreadyMech))]
    public static class CustomMechStoragePatch
    {
        public static bool Prefix(
            SimGameState __instance,
            int baySlot,
            MechDef def)
        {
            try
            {
                string logPath = CustomMechStorageMod.LogPath;

                if (def == null)
                {
                    if (!string.IsNullOrEmpty(logPath))
                    {
                        File.AppendAllText(
                            logPath,
                            DateTime.Now.ToString("HH:mm:ss.fff") +
                            " UnreadyMech received NULL MechDef\r\n");
                    }

                    return true;
                }

                string storageId =
                    def.Chassis.Description.Id + "-" +
                    (CustomMechStorageRegistry.StoredMechs.Count + 1)
                        .ToString("000");

                MechDef storedMech =
                    new MechDef(def, def.GUID, copyInventory: true);

                CustomMechStorageRegistry.StoredMechs[storageId] =
                    storedMech;

                if (baySlot >= 0 &&
                    __instance.ActiveMechs.ContainsKey(baySlot))
                {
                    __instance.ActiveMechs.Remove(baySlot);
                }

                if (!string.IsNullOrEmpty(logPath))
                {
                    File.AppendAllText(
                        logPath,
                        DateTime.Now.ToString("HH:mm:ss.fff") +
                        " CUSTOM STORE: " +
                        storageId +
                        " | Name=" +
                        storedMech.Description.Name +
                        " | GUID=" +
                        storedMech.GUID +
                        " | Chassis=" +
                        storedMech.Chassis.Description.Id +
                        " | Inventory=" +
                        storedMech.Inventory.Length +
                        " | Damaged=" +
                        storedMech.IsDamaged +
                        " | Destroyed=" +
                        storedMech.IsDestroyed +
                        " | Bay=" +
                        baySlot +
                        "\r\n");
                }

                return false;
            }
            catch (Exception ex)
            {
                try
                {
                    if (!string.IsNullOrEmpty(CustomMechStorageMod.LogPath))
                    {
                        File.AppendAllText(
                            CustomMechStorageMod.LogPath,
                            DateTime.Now.ToString("HH:mm:ss.fff") +
                            " CUSTOM STORE ERROR: " +
                            ex +
                            "\r\n");
                    }
                }
                catch
                {
                }

                return true;
            }
        }
    }
}