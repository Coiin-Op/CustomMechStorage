// CustomMechStorage.cs

using BattleTech;
using BattleTech.Save;
using BattleTech.Save.Test;
using BattleTech.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace CustomMechStorage
{
    public static class CustomMechStorageMod
    {
        public static string LogPath;

        public static Dictionary<string, MechDef> StoredMechs =
            new Dictionary<string, MechDef>(
                StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, HashSet<string>>
            StoredMechKeysByChassis =
                new Dictionary<string, HashSet<string>>(
                    StringComparer.OrdinalIgnoreCase);

        private static StreamWriter logWriter;
        private static MethodInfo mechSetter;
        private static bool sortByTonnageDiagnosticComplete;

        public static void Init(
            string directory,
            string settingsJSON)
        {
            LogPath = Path.Combine(
                directory,
                "CustomMechStorage_DIAGNOSTIC.log");

            InitializeLogger();

            WriteLog(
                "CustomMechStorage.Init reached");

            try
            {
                CacheMechSetter();

                DiagnoseSortByTonnageAssemblies();

                var harmony = new Harmony(
                    "com.scottie.custommechstorage");

                harmony.PatchAll(
                    Assembly.GetExecutingAssembly());

                WriteLog(
                    "PatchAll completed");
            }
            catch (Exception ex)
            {
                WriteLog(
                    $"INITIALIZATION ERROR: {ex}");
            }
        }

        private static void InitializeLogger()
        {
            try
            {
                if (logWriter != null)
                {
                    return;
                }

                string directory =
                    Path.GetDirectoryName(LogPath);

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                logWriter = new StreamWriter(
                    new FileStream(
                        LogPath,
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.Read),
                    System.Text.Encoding.UTF8);

                logWriter.AutoFlush = true;
            }
            catch
            {
                logWriter = null;
            }
        }

        private static void CacheMechSetter()
        {
            try
            {
                PropertyInfo mechProperty =
                    AccessTools.Property(
                        typeof(WorkOrderEntry_ReadyMech),
                        "Mech");

                mechSetter =
                    mechProperty?.GetSetMethod(true);

                if (mechSetter == null)
                {
                    WriteLog(
                        "READY PATCH ERROR: Could not locate Mech setter");
                }
                else
                {
                    WriteLog(
                        "READY PATCH: Cached WorkOrderEntry_ReadyMech.Mech setter");
                }
            }
            catch (Exception ex)
            {
                WriteLog(
                    $"READY PATCH ERROR: Failed to cache Mech setter | {ex}");
            }
        }

        public static MethodInfo GetCachedMechSetter()
        {
            return mechSetter;
        }

        public static void DiagnoseSortByTonnageAssemblies()
        {
            if (sortByTonnageDiagnosticComplete)
            {
                return;
            }

            sortByTonnageDiagnosticComplete = true;

            try
            {
                WriteLog(
                    "SORT DIAG: Starting loaded-assembly scan");

                foreach (Assembly assembly
                    in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types = null;

                    try
                    {
                        types = assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        types = ex.Types;
                    }
                    catch (Exception ex)
                    {
                        string location =
                            GetAssemblyLocation(assembly);

                        WriteLog(
                            $"SORT DIAG: Could not inspect assembly | " +
                            $"Assembly={assembly.FullName} | " +
                            $"Location={location} | " +
                            $"Exception={ex.GetType().FullName}: {ex.Message}");

                        continue;
                    }

                    if (types == null)
                    {
                        continue;
                    }

                    foreach (Type type in types)
                    {
                        if (type == null ||
                            string.IsNullOrEmpty(type.FullName))
                        {
                            continue;
                        }

                        if (type.FullName.IndexOf(
                            "SortByTonnage",
                            StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        string location =
                            GetAssemblyLocation(assembly);

                        WriteLog(
                            $"SORT DIAG: FOUND | " +
                            $"Assembly={assembly.FullName} | " +
                            $"Location={location} | " +
                            $"Type={type.FullName}");
                    }
                }

                WriteLog(
                    "SORT DIAG: Loaded-assembly scan complete");
            }
            catch (Exception ex)
            {
                WriteLog(
                    $"SORT DIAG: Scanner exception | {ex}");
            }
        }

        private static string GetAssemblyLocation(
            Assembly assembly)
        {
            try
            {
                return assembly.Location;
            }
            catch
            {
                return "<unavailable>";
            }
        }

        public static void StoreMech(
            MechDef preservedMech)
        {
            if (preservedMech == null)
            {
                return;
            }

            if (preservedMech.Description == null)
            {
                WriteLog(
                    "CUSTOM STORE ERROR: MechDef has no Description");

                return;
            }

            string key =
                !string.IsNullOrEmpty(preservedMech.GUID)
                    ? preservedMech.GUID
                    : preservedMech.Description.Id;

            string chassisId =
                GetChassisId(preservedMech);

            if (string.IsNullOrEmpty(key))
            {
                WriteLog(
                    "CUSTOM STORE ERROR: MechDef has no usable key");

                return;
            }

            if (StoredMechs.ContainsKey(key))
            {
                RemoveChassisIndexEntry(key);
            }

            StoredMechs[key] =
                preservedMech;

            if (string.IsNullOrEmpty(chassisId))
            {
                return;
            }

            if (!StoredMechKeysByChassis.TryGetValue(
                chassisId,
                out HashSet<string> keys))
            {
                keys = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

                StoredMechKeysByChassis[chassisId] =
                    keys;
            }

            keys.Add(key);
        }

        public static bool TryGetStoredMech(
            string chassisId,
            out string foundKey,
            out MechDef preservedMech)
        {
            foundKey = null;
            preservedMech = null;

            if (string.IsNullOrEmpty(chassisId))
            {
                return false;
            }

            if (!StoredMechKeysByChassis.TryGetValue(
                chassisId,
                out HashSet<string> keys))
            {
                return false;
            }

            string staleKey = null;

            foreach (string key in keys)
            {
                if (!StoredMechs.TryGetValue(
                    key,
                    out MechDef candidate))
                {
                    staleKey = key;
                    break;
                }

                if (candidate == null)
                {
                    staleKey = key;
                    break;
                }

                if (string.Equals(
                    GetChassisId(candidate),
                    chassisId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    foundKey = key;
                    preservedMech = candidate;
                    return true;
                }
            }

            if (staleKey != null)
            {
                keys.Remove(staleKey);

                if (keys.Count == 0)
                {
                    StoredMechKeysByChassis.Remove(
                        chassisId);
                }
            }

            return false;
        }

        public static void RemoveStoredMech(
            string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            StoredMechs.Remove(key);

            RemoveChassisIndexEntry(key);
        }

        private static void RemoveChassisIndexEntry(
            string key)
        {
            string emptyChassisId = null;

            foreach (KeyValuePair<string, HashSet<string>> entry
                in StoredMechKeysByChassis)
            {
                HashSet<string> keys =
                    entry.Value;

                if (!keys.Remove(key))
                {
                    continue;
                }

                if (keys.Count == 0)
                {
                    emptyChassisId =
                        entry.Key;
                }

                break;
            }

            if (emptyChassisId != null)
            {
                StoredMechKeysByChassis.Remove(
                    emptyChassisId);
            }
        }

        public static void RebuildChassisIndex()
        {
            StoredMechKeysByChassis.Clear();

            foreach (KeyValuePair<string, MechDef> entry
                in StoredMechs)
            {
                MechDef mech = entry.Value;

                if (mech == null)
                {
                    continue;
                }

                string chassisId =
                    GetChassisId(mech);

                if (string.IsNullOrEmpty(chassisId))
                {
                    continue;
                }

                if (!StoredMechKeysByChassis.TryGetValue(
                    chassisId,
                    out HashSet<string> keys))
                {
                    keys = new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase);

                    StoredMechKeysByChassis[chassisId] =
                        keys;
                }

                keys.Add(entry.Key);
            }
        }

        private static string GetChassisId(
            MechDef mech)
        {
            if (mech?.Chassis?.Description == null)
            {
                return null;
            }

            return mech.Chassis.Description.Id;
        }

        public static void WriteLog(
            string message)
        {
            try
            {
                if (logWriter == null)
                {
                    InitializeLogger();
                }

                if (logWriter == null)
                {
                    return;
                }

                logWriter.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss.fff}] {message}");
            }
            catch
            {
            }
        }

        public static void CloseLogger()
        {
            try
            {
                if (logWriter == null)
                {
                    return;
                }

                logWriter.Flush();
                logWriter.Dispose();
                logWriter = null;
            }
            catch
            {
                logWriter = null;
            }
        }
    }


    [HarmonyPatch(typeof(SimGameState), "UnreadyMech")]
    public static class Patch_SimGameState_UnreadyMech
    {
        public static bool Prefix(
            SimGameState __instance,
            int baySlot,
            MechDef def)
        {
            if (def == null)
            {
                CustomMechStorageMod.WriteLog(
                    "UnreadyMech received NULL MechDef");

                return true;
            }

            try
            {
                MechDef preservedMech =
                    new MechDef(
                        def,
                        def.GUID,
                        true);

                CustomMechStorageMod.StoreMech(
                    preservedMech);

                int inventoryCount =
                    preservedMech.Inventory == null
                        ? 0
                        : preservedMech.Inventory.Length;

                CustomMechStorageMod.WriteLog(
                    $"CUSTOM STORE: {def.Description.Id} | " +
                    $"Name={def.Description.Name} | " +
                    $"GUID={def.GUID} | " +
                    $"Inventory={inventoryCount} | " +
                    $"Bay={baySlot}");
            }
            catch (Exception ex)
            {
                CustomMechStorageMod.WriteLog(
                    $"CUSTOM STORE ERROR: {ex}");
            }

            return true;
        }
    }


    public static class CustomMechStoragePatch
    {
        public static void RefreshMechBayStorage()
        {
            try
            {
                MechBayPanel[] panels =
                    Resources.FindObjectsOfTypeAll<MechBayPanel>();

                CustomMechStorageMod.WriteLog(
                    $"UI REFRESH: MechBayPanel count={panels.Length}");

                foreach (MechBayPanel panel in panels)
                {
                    if (panel != null &&
                        panel.gameObject.activeInHierarchy)
                    {
                        CustomMechStorageMod.WriteLog(
                            "UI REFRESH: MechBayPanel.RefreshData() called");

                        panel.RefreshData(false);
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMechStorageMod.WriteLog(
                    $"UI REFRESH ERROR: {ex}");
            }
        }
    }


    [HarmonyPatch(typeof(SimGameState), "ML_ReadyMech")]
    public static class Patch_SimGameState_ML_ReadyMech
    {
        public static void Prefix(
            SimGameState __instance,
            WorkOrderEntry_ReadyMech order)
        {
            if (order == null ||
                order.Mech == null)
            {
                CustomMechStorageMod.WriteLog(
                    "READY PATCH: NULL order or Mech");

                return;
            }

            if (order.IsMechLabComplete)
            {
                CustomMechStorageMod.WriteLog(
                    "READY PATCH: Order already complete; skipping");

                return;
            }

            try
            {
                if (order.Mech.Chassis == null ||
                    order.Mech.Chassis.Description == null)
                {
                    CustomMechStorageMod.WriteLog(
                        "READY PATCH: Mech has no chassis description");

                    return;
                }

                string chassisId =
                    order.Mech.Chassis.Description.Id;

                CustomMechStorageMod.WriteLog(
                    $"READY PATCH: Looking for preserved mech | " +
                    $"Chassis={chassisId}");

                bool found =
                    CustomMechStorageMod.TryGetStoredMech(
                        chassisId,
                        out string foundKey,
                        out MechDef preservedMech);

                if (!found ||
                    preservedMech == null)
                {
                    CustomMechStorageMod.WriteLog(
                        $"READY PATCH: No preserved mech found | " +
                        $"Chassis={chassisId}");

                    return;
                }

                MechDef originalOrderMech =
                    order.Mech;

                int readyingKey =
                    int.MinValue;

                foreach (KeyValuePair<int, MechDef> kvp
                    in __instance.ReadyingMechs)
                {
                    if (kvp.Value == originalOrderMech)
                    {
                        readyingKey =
                            kvp.Key;

                        break;
                    }
                }

                if (readyingKey != int.MinValue)
                {
                    __instance.ReadyingMechs[readyingKey] =
                        preservedMech;

                    CustomMechStorageMod.WriteLog(
                        $"READY PATCH: Updated ReadyingMechs reference | " +
                        $"Bay={readyingKey}");
                }
                else
                {
                    CustomMechStorageMod.WriteLog(
                        "READY PATCH WARNING: Could not find original " +
                        "MechDef in ReadyingMechs");
                }

                MethodInfo cachedSetter =
                    CustomMechStorageMod.GetCachedMechSetter();

                if (cachedSetter == null)
                {
                    CustomMechStorageMod.WriteLog(
                        "READY PATCH ERROR: Cached Mech setter unavailable");

                    return;
                }

                cachedSetter.Invoke(
                    order,
                    new object[]
                    {
                        preservedMech
                    });

                int inventoryCount =
                    preservedMech.Inventory == null
                        ? 0
                        : preservedMech.Inventory.Length;

                CustomMechStorageMod.WriteLog(
                    $"READY PATCH: Preserved MechDef substituted | " +
                    $"Name={preservedMech.Description.Name} | " +
                    $"GUID={preservedMech.GUID} | " +
                    $"Inventory={inventoryCount}");

                if (!string.IsNullOrEmpty(foundKey))
                {
                    CustomMechStorageMod.RemoveStoredMech(
                        foundKey);

                    CustomMechStorageMod.WriteLog(
                        $"READY PATCH: Preserved entry consumed | " +
                        $"Key={foundKey}");
                }
            }
            catch (Exception ex)
            {
                CustomMechStorageMod.WriteLog(
                    $"READY PATCH ERROR: {ex}");
            }
        }

        public static void Postfix(
            SimGameState __instance,
            WorkOrderEntry_ReadyMech order)
        {
            try
            {
                if (order == null)
                {
                    CustomMechStorageMod.WriteLog(
                        "READY POSTFIX: ML_ReadyMech returned | " +
                        "Order=NULL");

                    return;
                }

                CustomMechStorageMod.WriteLog(
                    $"READY POSTFIX: ML_ReadyMech returned | " +
                    $"Complete={order.IsMechLabComplete}");
            }
            catch (Exception ex)
            {
                CustomMechStorageMod.WriteLog(
                    $"READY POSTFIX ERROR: {ex}");
            }
        }

        public static Exception Finalizer(
            Exception __exception)
        {
            try
            {
                if (__exception != null)
                {
                    CustomMechStorageMod.WriteLog(
                        $"READY FINALIZER: ML_ReadyMech EXCEPTION | " +
                        $"{__exception}");
                }
                else
                {
                    CustomMechStorageMod.WriteLog(
                        "READY FINALIZER: ML_ReadyMech completed without exception");
                }
            }
            catch (Exception ex)
            {
                CustomMechStorageMod.WriteLog(
                    $"READY FINALIZER ERROR: {ex}");
            }

            return __exception;
        }
    }


    [HarmonyPatch(typeof(SimGameSave), "Dehydrate")]
    public static class Patch_SimGameSave_Dehydrate
    {
        private const string StoredMechsDictionaryName =
            "CustomMechStorage";

        public static void Prefix(
            SerializableReferenceContainer references)
        {
            try
            {
                references.AddItemDictionary(
                    StoredMechsDictionaryName,
                    CustomMechStorageMod.StoredMechs);

                CustomMechStorageMod.WriteLog(
                    $"CUSTOM SAVE: Registered StoredMechs | " +
                    $"Count={CustomMechStorageMod.StoredMechs.Count}");
            }
            catch (Exception ex)
            {
                CustomMechStorageMod.WriteLog(
                    $"CUSTOM SAVE ERROR: Failed to register StoredMechs | {ex}");
            }
        }
    }


    [HarmonyPatch(typeof(SimGameState), "Rehydrate")]
    public static class Patch_SimGameState_Rehydrate
    {
        private const string StoredMechsDictionaryName =
            "CustomMechStorage";

        public static void Postfix(
            GameInstanceSave gameInstanceSave)
        {
            try
            {
                SimGameSave save =
                    gameInstanceSave.SimGameSave;

                Dictionary<string, MechDef> restoredMechs =
                    save.GlobalReferences
                        .GetItemDictionary<string, MechDef>(
                            StoredMechsDictionaryName);

                CustomMechStorageMod.StoredMechs =
                    restoredMechs ??
                    new Dictionary<string, MechDef>(
                        StringComparer.OrdinalIgnoreCase);

                CustomMechStorageMod.RebuildChassisIndex();

                CustomMechStorageMod.WriteLog(
                    $"CUSTOM LOAD: Restored StoredMechs | " +
                    $"Count={CustomMechStorageMod.StoredMechs.Count}");
            }
            catch (Exception ex)
            {
                CustomMechStorageMod.WriteLog(
                    $"CUSTOM LOAD ERROR: Failed to restore StoredMechs | {ex}");
            }
        }
    }
}