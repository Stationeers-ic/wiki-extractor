using System;
using System.IO;
using Assets.Scripts;
using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace WikiExtractorMod
{
    #region BepInEx

    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class ExtractorBepInEx : BaseUnityPlugin
    {
        public const string pluginGuid = "net.elmo.stationeers.Extractor";
        public const string pluginName = "Extractor";
        public const string pluginVersion = "1.2";

        private void Awake()
        {
            try
            {
                var harmony = new Harmony(pluginGuid);
                harmony.PatchAll();
                Log("Patch WikiExtractorMod succeeded");
                var lang = Localization.CurrentLanguage;
                var folderPath = Path.Combine(Application.dataPath, "wiki_data");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            }
            catch (Exception e)
            {
                Log("Patch WikiExtractorMod Failed");
                Log(e.ToString());
            }
        }

        public static void Log(string line)
        {
            Debug.Log("[" + pluginName + "]: " + line);
        }

        public static void SaveJson(string name, object obj, string folder = "wiki_data")
        {
            var lang = Localization.CurrentLanguage;
            name += ".json";
            var json = JsonConvert.SerializeObject(obj);
            var path = Path.Combine(
                Application.dataPath,
                folder,
                lang.ToString(),
                name
            );
            var folderPath = Path.Combine(Application.dataPath, folder, lang.ToString());

            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine(json);
            }
        }

        public static string SpriteToBase64(Sprite sprite)
        {
            var lang = Localization.CurrentLanguage;
            if (lang != LanguageCode.EN)
                // убираем избыточноть в остальных языках 
                return null;

            if (sprite == null) return null;

            if (sprite.texture == null) return null;

            var renderTexture = new RenderTexture(sprite.texture.width, sprite.texture.height, 0);
            Graphics.Blit(sprite.texture, renderTexture);

            var texture = new Texture2D(sprite.texture.width, sprite.texture.height);
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            var bytes = texture.EncodeToPNG();
            var base64String = Convert.ToBase64String(bytes);

            return base64String;
        }
    }

    #endregion
}