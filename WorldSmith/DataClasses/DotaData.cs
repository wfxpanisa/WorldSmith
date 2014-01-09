﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using KVLib;

namespace WorldSmith.DataClasses
{
    static class DotaData
    {
        public static string KVHeader = "//This script generated by WorldSmith" + Environment.NewLine
            + "//Get WorldSmith at TODO: Put a URL here" + Environment.NewLine;


        public static string VPKPath = "dota" + Path.DirectorySeparatorChar + "pak01_dir.vpk";

        #region Unit Data Lists
        public static List<DotaUnit> DefaultUnits = new List<DotaUnit>();

        public static List<DotaUnit> OverriddenUnits = new List<DotaUnit>();

        public static List<DotaUnit> CustomUnits = new List<DotaUnit>();

        public static List<DotaHero> DefaultHeroes = new List<DotaHero>();

        public static List<DotaHero> OverridenHeroes = new List<DotaHero>();

        public static List<DotaHero> CustomHeroes = new List<DotaHero>();

        public static IEnumerable<DotaBaseUnit> AllDefaultUnits = DefaultUnits.Cast<DotaBaseUnit>()
            .Union(DefaultHeroes.Cast<DotaBaseUnit>());

        public static IEnumerable<DotaBaseUnit> AllOverridenUnits = OverriddenUnits.Cast<DotaBaseUnit>()
            .Union(OverridenHeroes.Cast<DotaBaseUnit>());

        public static IEnumerable<DotaBaseUnit> AllCustomUnits = CustomUnits.Cast<DotaBaseUnit>()
            .Union(CustomHeroes.Cast<DotaBaseUnit>());

        public static IEnumerable<DotaBaseUnit> AllSaveableUnits = AllOverridenUnits
            .Union(AllCustomUnits);

        public static IEnumerable<DotaBaseUnit> AllUnits = AllDefaultUnits
            .Union(AllOverridenUnits)
            .Union(CustomUnits.Cast<DotaBaseUnit>())
            .Union(CustomUnits.Cast<DotaBaseUnit>());
            
        #endregion

        #region Ability Data Lists
        public static List<DotaAbility> DefaultAbilities = new List<DotaAbility>();

        public static List<DotaAbility> CustomAbilities = new List<DotaAbility>();

        public static IEnumerable<DotaAbility> AllAbilities = DefaultAbilities.Union(CustomAbilities);

        #endregion

        public static IEnumerable<DotaDataObject> AllClasses = AllUnits.Cast<DotaDataObject>()
            .Union(AllAbilities.Cast<DotaDataObject>());

        public static string NPCScriptPath = "scripts" + Path.DirectorySeparatorChar + "npc" + Path.DirectorySeparatorChar;
        public static string CustomHeroesFile = NPCScriptPath + "npc_heroes_custom.txt";
        public static string CustomUnitsFile = NPCScriptPath + "npc_units_custom.txt";
        public static string CustomAbilityFile = NPCScriptPath + "npc_abilities_custom.txt";

        public const string DefaultUnitsFile = "scripts/npc/npc_units.txt";
        public const string DefaultHeroesFile = "scripts/npc/npc_heroes.txt";
        public const string DefaultAbilitiesFile = "scripts/npc/npc_abilities.txt";

        #region HLLib Usage
        public static void LoadFromVPK(string vpkPath)
        {
            if(!Directory.Exists("cache")) Directory.CreateDirectory("cache");

            string path = Properties.Settings.Default.dotadir + Path.DirectorySeparatorChar + VPKPath;
            HLLib.hlInitialize();

            // Get the package type from the filename extension.
            HLLib.HLPackageType PackageType = HLLib.hlGetPackageTypeFromName(path);

            HLLib.HLFileMode OpenMode = HLLib.HLFileMode.HL_MODE_READ |
                HLLib.HLFileMode.HL_MODE_QUICK_FILEMAPPING |
                HLLib.HLFileMode.HL_MODE_VOLATILE;

            uint PackageID;

            ErrorCheck(HLLib.hlCreatePackage(PackageType, out PackageID));            

            ErrorCheck(HLLib.hlBindPackage(PackageID));

            ErrorCheck(HLLib.hlPackageOpenFile(path, (uint)OpenMode));           
            
        }


        private static string ReadTextFromHLLibStream(IntPtr Stream)
        {
            HLLib.HLFileMode mode = HLLib.HLFileMode.HL_MODE_READ;

            ErrorCheck(HLLib.hlStreamOpen(Stream, (uint)mode));

            StringBuilder str = new StringBuilder();

            char ch;
            while (HLLib.hlStreamReadChar(Stream, out ch))
            {
                str.Append(ch);
            }

            HLLib.hlStreamClose(Stream);

            return str.ToString();
        }


        public static void ErrorCheck(bool ret)
        {
            if (!ret)
            {
                MessageBox.Show("Error reading pak01_dir.vpk.\n\n The error reported was: " + HLLib.hlGetString(HLLib.HLOption.HL_ERROR_LONG_FORMATED), "Error opening .pak", MessageBoxButtons.OK);
                Shutdown();
                Properties.Settings.Default.ranonce = false;
                Properties.Settings.Default.Save();
                Environment.Exit(0);
            }
        }

        public static void Shutdown()
        {
            HLLib.hlShutdown();
        }
        #endregion       

        #region LoadData
        public static void ReadScriptFromVPK<T>(string filePath, List<T> ListToInsert) where T : DotaDataObject
        {
            IntPtr root = HLLib.hlPackageGetRoot();

            IntPtr file = HLLib.hlFolderGetItemByPath(root, filePath, HLLib.HLFindType.HL_FIND_FILES);

            IntPtr stream;
            ErrorCheck(HLLib.hlPackageCreateStream(file, out stream));

            string unitsText = ReadTextFromHLLibStream(stream);

            KeyValue rootkv = KVLib.KVParser.ParseKeyValueText(unitsText);

            foreach (KeyValue kv in rootkv.Children)
            {
                if (!kv.HasChildren) continue; //Get rid of that pesky "Version" "1" key

                T unit = typeof(T).GetConstructor(Type.EmptyTypes).Invoke(new object[] { }) as T;
                unit.LoadFromKeyValues(kv);
                ListToInsert.Add(unit);
            }
            return;
        }

       
        public static void ReadOverride<T>(string file, List<T> ListToLoadInto) where T : DotaDataObject
        {
            ListToLoadInto.Clear();
            
            
            KeyValue doc = KVParser.ParseKeyValueText(File.ReadAllText(Properties.Settings.Default.AddonPath + file));
            
            foreach(KeyValue hero in doc.Children)
            {
                if (!hero.HasChildren) continue;
                T unit = typeof(T).GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes) as T;
                unit.LoadFromKeyValues(hero);
                
                ListToLoadInto.Add(unit);
            }
        }
        #endregion

        #region SaveData
        public static void SaveUnits()
        {          
            SaveList(CustomUnits, "DOTAUnits", "npc_units_custom.txt");
            SaveList(OverridenHeroes, "DOTAHeroes", "npc_heroes_custom.txt");
            SaveList(CustomAbilities, "DOTAAbilities", "npc_abilities_custom.txt");
        }

        private static void SaveList<T>(List<T> list, string RootKey, string outputFileName) where T : DotaDataObject
        {
            string path = Properties.Settings.Default.AddonPath + Path.DirectorySeparatorChar
                + "scripts" + Path.DirectorySeparatorChar + "npc" + Path.DirectorySeparatorChar;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            KeyValue doc = new KeyValue(RootKey);

            foreach (T unit in list)
            {
                doc += unit.SaveToKV();
            }

            File.WriteAllText(path +outputFileName, KVHeader + doc.ToString());
        }
        #endregion


    }
}
