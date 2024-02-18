using System.IO;
using Assets.Scripts.UI;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using System;

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
            List<string> images = new List<String>();
            foreach (var stationBuildCostInsert in page.BuildStates)
            {
                string base64String = SpriteToBase64(stationBuildCostInsert.PrinterImage);
                images.Add(base64String);
            }
            obj.Add("images",images);

            obj.Add("PrefabName", page.PrefabName);
            obj.Add("PrefabHashString", page.PrefabHashString);
            obj.Add("PrefabHash", page.PrefabHash);
            string json = JsonConvert.SerializeObject(obj);
            string path = Path.Combine(
                Path.Combine(Application.dataPath, "wiki_data"),
                page.Key + ".json"
            );
            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine(json);
            }

            ExtractorBepInEx.Log(path);
        }

       public static string SpriteToBase64(Sprite sprite)
       {
           RenderTexture renderTexture = RenderTexture.GetTemporary(sprite.texture.width, sprite.texture.height);
           Graphics.Blit(sprite.texture, renderTexture);

           Texture2D texture = new Texture2D(sprite.texture.width, sprite.texture.height);
           RenderTexture.active = renderTexture;
           texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
           texture.Apply();

           byte[] bytes = texture.EncodeToPNG();
           string base64String = Convert.ToBase64String(bytes);

           RenderTexture.ReleaseTemporary(renderTexture);

           return base64String;
       }
    }
}