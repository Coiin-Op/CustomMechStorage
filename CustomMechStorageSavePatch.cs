using System;
using System.Collections.Generic;
using System.IO;
using BattleTech;
using BattleTech.Save;
using BattleTech.Save.Test;
using HarmonyLib;

namespace CustomMechStorage
{
    [HarmonyPatch(typeof(SimGameSave), nameof(SimGameSave.Dehydrate))]
    public static class CustomMechStorageSavePatch
    {
        public static void Postfix(
            SimGameSave __instance,
            SerializableReferenceContainer references)
        {
            try
            {
                int count =
                    CustomMechStorageRegistry.StoredMechs.Count;

                WriteLog(
                    "CUSTOM SAVE PATCH FIRED | StoredMechs=" +
                    count);

                if (count == 0)
                {
                    return;
                }

                references.AddItemDictionary(
                    "CustomMechStorage",
                    CustomMechStorageRegistry.StoredMechs);

                WriteLog(
                    "CUSTOM SAVE: dictionary added | Count=" +
                    count);
            }
            catch (Exception ex)
            {
                WriteLog(
                    "CUSTOM SAVE ERROR: " +
                    ex);
            }
        }

        [HarmonyPatch(
            typeof(SimGameState),
            nameof(SimGameState.Rehydrate))]
        public static class CustomMechStorageLoadPatch
        {
            public static void Postfix(
                SimGameState __instance,
                GameInstanceSave gameInstanceSave)
            {
                try
                {
                    WriteLog("CUSTOM LOAD PATCH FIRED");

                    SerializableReferenceContainer references =
                        gameInstanceSave.GlobalReferences;

                    Dictionary<string, MechDef> loadedMechs =
                        references.GetItemDictionary<string, MechDef>(
                            "CustomMechStorage");

                    WriteLog(
                        "CUSTOM LOAD DICTIONARY FOUND | Count=" +
                        (loadedMechs == null
                            ? "NULL"
                            : loadedMechs.Count.ToString()));

                    CustomMechStorageRegistry.StoredMechs.Clear();

                    if (loadedMechs == null)
                    {
                        return;
                    }

                    foreach (
                        KeyValuePair<string, MechDef> pair
                        in loadedMechs)
                    {
                        if (pair.Value == null)
                        {
                            WriteLog(
                                "CUSTOM LOAD: " +
                                pair.Key +
                                " | MechDef=NULL");
                            continue;
                        }

                        CustomMechStorageRegistry.StoredMechs[
                            pair.Key] = pair.Value;

                        WriteLog(
                            "CUSTOM LOAD: " +
                            pair.Key +
                            " | Name=" +
                            pair.Value.Description.Name +
                            " | GUID=" +
                            pair.Value.GUID +
                            " | Chassis=" +
                            pair.Value.Chassis.Description.Id +
                            " | Inventory=" +
                            pair.Value.Inventory.Length +
                            " | Damaged=" +
                            pair.Value.IsDamaged +
                            " | Destroyed=" +
                            pair.Value.IsDestroyed);
                    }

                    WriteLog(
                        "CUSTOM LOAD COMPLETE | StoredMechs=" +
                        CustomMechStorageRegistry.StoredMechs.Count);
                }
                catch (Exception ex)
                {
                    WriteLog(
                        "CUSTOM LOAD ERROR: " +
                        ex);
                }
            }
        }

        private static void WriteLog(string message)
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