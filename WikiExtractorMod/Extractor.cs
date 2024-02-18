using System.IO;
using Assets.Scripts.UI;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace WikiExtractorMod
{
    using System.Collections.Generic;

    [HarmonyPatch(typeof(Stationpedia), nameof(Stationpedia.Register))]
    public class Extractor
    {
        [HarmonyPrefix]
        public static void Prefix(StationpediaPage page, bool fallback = false)
        {
            var obj = new Dictionary<string, dynamic>();
            obj.Add("Key", page.Key);
            obj.Add("Title", page.Title);

            obj.Add("LogicInsert", page.LogicInsert);
            obj.Add("LogicSlotInsert", page.LogicSlotInsert);
            obj.Add("ModeInsert", page.ModeInsert);
            obj.Add("ConnectionInsert", page.ConnectionInsert);
            obj.Add("ResourcesUsed", page.ResourcesUsed);
            obj.Add("BuildStates", page.BuildStates);

            obj.Add("PrefabName", page.PrefabName);
            obj.Add("PrefabHashString", page.PrefabHashString);
            obj.Add("PrefabHash", page.PrefabHash);
            string json = JsonConvert.SerializeObject(obj);
            string path = Path.Combine(
                Path.Combine(Application.dataPath, "wiki_data"),
                Application.dataPath, page.Key + ".json"
            );
            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine(json);
            }

            ExtractorBepInEx.Log(path);
        }
    }
}