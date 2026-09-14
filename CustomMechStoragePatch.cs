using System;
using BattleTech;
using HarmonyLib;

namespace CustomMechStorage
{
    [HarmonyPatch(typeof(SimGameState), nameof(SimGameState.UnreadyMech))]
    public static class CustomMechStoragePatch
    {
        public static void Prefix(
            int baySlot,
            MechDef def)
        {
            if (def == null)
            {
                Console.WriteLine(
                    "[CustomMechStorage] UnreadyMech called with NULL MechDef.");

                return;
            }

            Console.WriteLine(
                "[CustomMechStorage] UnreadyMech intercepted: " +
                def.Description.Name +
                " | GUID=" +
                def.GUID +
                " | Chassis=" +
                def.Chassis.Description.Id +
                " | Bay=" +
                baySlot);
        }
    }
}