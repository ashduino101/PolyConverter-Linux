﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace PolyConverter
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            bool hasArgs = args != null && args.Length > 0;
            string gamePath = null;

            if (!hasArgs) Console.WriteLine("[#] Booting up PolyConverter");

            if (File.Exists(ManualGamePath))
            {
                try
                {
                    gamePath = $"{File.ReadAllText(ManualGamePath).Trim()}";
                }
                catch (Exception e) // Could happen if it can't read files, I suppose
                {
                    Console.WriteLine($"[Fatal Error] Failed to grab Poly Bridge 2 location from {ManualGamePath}: {e.Message}");
                    if (!hasArgs)
                    {
                        Console.WriteLine("\n[#] The program will now close.");
                        Console.ReadLine();
                    }
                    return ExitCodeGamePathError;
                }

                Console.WriteLine($"[#] Grabbed Poly Bridge 2 install location from {ManualGamePath}");
            }
            else
            {
                var errors = new List<Exception>(2);

                try { gamePath = GetPolyBridge2SteamPath(); }
                catch (Exception e) { errors.Add(e); }
                if (gamePath == null)
                {
                    Console.WriteLine($"[Error] Failed to locate Poly Bridge 2 installation folder.");
                    Console.WriteLine($" You can manually set the location by creating a file here called \"{ManualGamePath}\" " +
                        "and writing the location of your game folder in that file, then restarting this program.");
                    foreach (var e in errors) Console.WriteLine($"\n[#] Error message: {e.Message}");
                    if (!hasArgs)
                    {
                        Console.WriteLine("\n[#] The program will now close.");
                        Console.ReadLine();
                    }
                    return ExitCodeGamePathError;
                }

                if (!hasArgs) Console.WriteLine($"[#] Automatically detected Poly Bridge 2 installation");
            }

            try
            {
                PolyBridge2Assembly = Assembly.LoadFrom($"{gamePath}/Poly Bridge 2_Data/Managed/Assembly-CSharp.dll");
                UnityAssembly = Assembly.LoadFrom($"{gamePath}/Poly Bridge 2_Data/Managed/UnityEngine.CoreModule.dll");
                SirenixAssembly = Assembly.LoadFrom($"{gamePath}/Poly Bridge 2_Data/Managed/Sirenix.Serialization.dll");
                SirenixConfigAssembly = Assembly.LoadFrom($"{gamePath}/Poly Bridge 2_Data/Managed/Sirenix.Serialization.Config.dll");

                object testObject = FormatterServices.GetUninitializedObject(VehicleProxy);
                VehicleProxy.GetField("m_Pos").SetValue(testObject, Activator.CreateInstance(Vector2));
                testObject = FormatterServices.GetUninitializedObject(DeserializationContext);
                testObject = FormatterServices.GetUninitializedObject(DataFormat);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Fatal Error] Failed to load Poly Bridge 2 libraries at \"{gamePath}\":\n {e}");
                if (!hasArgs)
                {
                    Console.WriteLine("\n[#] The program will now close.");
                    Console.ReadLine();
                }
                return ExitCodeGamePathError;
            }

            Console.WriteLine();

            if (hasArgs)
            {
                string filePath = string.Join(' ', args).Trim();

                if (PolyConverter.LayoutJsonRegex.IsMatch(filePath))
                {
                    string newPath = PolyConverter.LayoutJsonRegex.Replace(filePath, PolyConverter.LayoutExtension);
                    string backupPath = PolyConverter.LayoutJsonRegex.Replace(filePath, PolyConverter.LayoutBackupExtension);

                    string result = new PolyConverter().JsonToLayout(filePath, newPath, backupPath);

                    Console.WriteLine(result);
                    if (result.Contains("Invalid json")) return ExitCodeJsonError;
                    if (result.Contains("Error") && result.Contains("file")) return ExitCodeFileError;
                    if (result.Contains("Error")) return ExitCodeConversionError;
                    return ExitCodeSuccessful;
                }
                else if (PolyConverter.LayoutRegex.IsMatch(filePath))
                {
                    string newPath = PolyConverter.LayoutRegex.Replace(filePath, PolyConverter.LayoutJsonExtension);

                    string result = new PolyConverter().LayoutToJson(filePath, newPath);

                    Console.WriteLine(result);
                    if (result.Contains("Error") && result.Contains("file")) return ExitCodeFileError;
                    if (result.Contains("Error")) return ExitCodeConversionError;
                    return ExitCodeSuccessful;
                }
                else if (PolyConverter.SlotJsonRegex.IsMatch(filePath))
                {
                    string newPath = PolyConverter.SlotJsonRegex.Replace(filePath, PolyConverter.SlotExtension);
                    string backupPath = PolyConverter.SlotJsonRegex.Replace(filePath, PolyConverter.SlotBackupExtension);

                    string result = new PolyConverter().JsonToSlot(filePath, newPath, backupPath);

                    Console.WriteLine(result);
                    if (result.Contains("Invalid json")) return ExitCodeJsonError;
                    if (result.Contains("Error") && result.Contains("file")) return ExitCodeFileError;
                    if (result.Contains("Error")) return ExitCodeConversionError;
                    return ExitCodeSuccessful;
                }
                else if (PolyConverter.SlotRegex.IsMatch(filePath))
                {
                    string newPath = PolyConverter.SlotRegex.Replace(filePath, PolyConverter.SlotJsonExtension);

                    string result = new PolyConverter().SlotToJson(filePath, newPath);

                    Console.WriteLine(result);
                    if (result.Contains("Error") && result.Contains("file")) return ExitCodeFileError;
                    if (result.Contains("Error")) return ExitCodeConversionError;
                    return ExitCodeSuccessful;
                }
                else
                {
                    Console.WriteLine($"[Error] The only supported file extensions are: " +
                        $"{PolyConverter.LayoutExtension}, {PolyConverter.LayoutJsonExtension}, " +
                        $"{PolyConverter.SlotExtension}, {PolyConverter.SlotJsonExtension}");
                    return ExitCodeFileError;
                }
            }
            else
            {
                while (true)
                {
                    Console.WriteLine("\n");

                    new PolyConverter().ConvertAll();

                    Console.WriteLine("\n[#] Press Enter to run the program again.");
                    Console.ReadLine();
                }
            }
        }


        static string GetPolyBridge2SteamPath()
        {
            if (Directory.Exists($"/home/{Environment.UserName}/.local/share/Steam/steamapps/common/Poly Bridge 2")) {
                return $"/home/{Environment.UserName}/.local/share/Steam/steamapps/common/Poly Bridge 2";
            } else if (Directory.Exists($"/home/{Environment.UserName}/.var/app/com.valvesoftware.Steam/.local/share/Steam/steamapps/common/Poly Bridge 2/")) {
                return $"/home/{Environment.UserName}/.var/app/com.valvesoftware.Steam/.local/share/Steam/steamapps/common/Poly Bridge 2/";
            }
            return null;
        }

        const int ExitCodeSuccessful = 0;
        const int ExitCodeJsonError = 1;
        const int ExitCodeConversionError = 2;
        const int ExitCodeFileError = 3;
        const int ExitCodeGamePathError = 4;

        const string ManualGamePath = "gamepath.txt";

        public static Assembly PolyBridge2Assembly { get; private set; }
        public static Assembly UnityAssembly { get; private set; }
        public static Assembly SirenixAssembly { get; private set; }
        public static Assembly SirenixConfigAssembly { get; private set; }

        public static Type SandboxLayoutData => PolyBridge2Assembly.GetType("SandboxLayoutData");
        public static Type BridgeSaveData => PolyBridge2Assembly.GetType("BridgeSaveData");
        public static Type BridgeSaveSlotData => PolyBridge2Assembly.GetType("BridgeSaveSlotData");
        public static Type ByteSerializer => PolyBridge2Assembly.GetType("ByteSerializer");
        public static Type VehicleProxy => PolyBridge2Assembly.GetType("VehicleProxy");
        public static Type BudgetProxy => PolyBridge2Assembly.GetType("BudgetProxy");
        public static Type SandboxSettingsProxy => PolyBridge2Assembly.GetType("SandboxSettingsProxy");
        public static Type WorkshopProxy => PolyBridge2Assembly.GetType("WorkshopProxy");

        public static Type Vector2 => UnityAssembly.GetType("UnityEngine.Vector2");
        public static Type Vector3 => UnityAssembly.GetType("UnityEngine.Vector3");
        public static Type Quaternion => UnityAssembly.GetType("UnityEngine.Quaternion");
        public static Type Color => UnityAssembly.GetType("UnityEngine.Color");

        public static Type SerializationUtility => SirenixAssembly.GetType("Sirenix.Serialization.SerializationUtility");
        public static Type DeserializationContext => SirenixAssembly.GetType("Sirenix.Serialization.DeserializationContext");
        public static Type DataFormat => SirenixConfigAssembly.GetType("Sirenix.Serialization.DataFormat");

    }
}
