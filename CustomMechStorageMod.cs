using System;
using System.IO;
using System.Reflection;
using HarmonyLib;

namespace CustomMechStorage
{
    public static class CustomMechStorageMod
    {
        public static void Init(string directory, string settingsJSON)
        {
            try
            {
                string logPath = Path.Combine(
                    directory,
                    "CustomMechStorage_DIAGNOSTIC.log");

                File.AppendAllText(
                    logPath,
                    DateTime.Now.ToString("HH:mm:ss.fff") +
                    " CustomMechStorage.Init reached\r\n");

                var harmony = new Harmony(
                    "com.scottie.custommechstorage");

                harmony.PatchAll(
                    Assembly.GetExecutingAssembly());

                File.AppendAllText(
                    logPath,
                    DateTime.Now.ToString("HH:mm:ss.fff") +
                    " PatchAll completed\r\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[CustomMechStorage] INITIALIZATION ERROR:");

                Console.WriteLine(ex.ToString());
            }
        }
    }
}