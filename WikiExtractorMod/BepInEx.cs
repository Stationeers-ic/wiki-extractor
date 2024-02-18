using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.IO;

namespace WikiExtractorMod
{
    #region BepInEx

    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class ExtractorBepInEx : BaseUnityPlugin
    {
        public const string pluginGuid = "net.elmo.stationeers.Extractor";
        public const string pluginName = "Extractor";
        public const string pluginVersion = "1.1";

        public static void Log(string line)
        {
            Debug.Log("[" + pluginName + "]: " + line);
        }

        private void Awake()
        {
            try
            {
                var harmony = new Harmony(pluginGuid);
                harmony.PatchAll();
                Log("Patch WikiExtractorMod succeeded");
                string folderPath = Path.Combine(Application.dataPath, "wiki_data");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
            }
            catch (Exception e)
            {
                Log("Patch WikiExtractorMod Failed");
                Log(e.ToString());
            }
        }
    }

    #endregion
}