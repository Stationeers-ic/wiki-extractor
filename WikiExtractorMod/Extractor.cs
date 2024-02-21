using System.IO;
using Assets.Scripts.UI;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using System;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Objects.Items;

namespace WikiExtractorMod
{
    using System.Collections.Generic;

    [HarmonyPatch(typeof(Stationpedia), nameof(Stationpedia.Register))]
    public class Extractor
    {
        static List<string> _ids = new List<string>();

        [HarmonyPrefix]
        public static void Prefix(StationpediaPage page, bool fallback = false)
        {
            var obj = new Dictionary<string, dynamic>();
            obj.Add("Key", page.Key);
            obj.Add("Title", page.Title);
            obj.Add("Description", page.Description);
            obj.Add("SortPriority", page.SortPriority);
            obj.Add("ImportantPage", page.ImportantPage);
            obj.Add("Text", page.Text);
            obj.Add("ConstructWithText", page.ConstructWithText);
            obj.Add("PrefabName", page.PrefabName);
            obj.Add("PrefabHash", page.PrefabHash);
            obj.Add("PrefabHashString", page.PrefabHashString);
            obj.Add("PaintableText", page.PaintableText);
            obj.Add("StackSizeText", page.StackSizeText);
            obj.Add("ReagentsHash", page.ReagentsHash);
            obj.Add("ReagentsType", page.ReagentsType);
            obj.Add("UnitText", page.UnitText);
            obj.Add("ReagentsText", page.ReagentsText);
            obj.Add("SpecificHeatText", page.SpecificHeatText);
            obj.Add("MaxLiquidTemperatureText", page.MaxLiquidTemperatureText);
            obj.Add("FreezeTemperatureText", page.FreezeTemperatureText);
            obj.Add("BoilingTemperatureText", page.BoilingTemperatureText);
            obj.Add("MinLiquidPressure", page.MinLiquidPressure);
            obj.Add("LatentHeatText", page.LatentHeatText);
            obj.Add("MolesPerLitreText", page.MolesPerLitreText);
            obj.Add("FlashpointText", page.FlashpointText);
            obj.Add("AutoIgnitionText", page.AutoIgnitionText);
            obj.Add("ConvectionFactorText", page.ConvectionFactorText);
            obj.Add("RadiationFactorText", page.RadiationFactorText);
            obj.Add("BasePowerDraw", page.BasePowerDraw);
            obj.Add("MaxPressure", page.MaxPressure);
            obj.Add("Volume", page.Volume);
            obj.Add("Nutrition", page.Nutrition);
            obj.Add("GrowthTime", page.GrowthTime);
            obj.Add("PlaceableInRocket", page.PlaceableInRocket);
            obj.Add("RocketMass", page.RocketMass);
            obj.Add("RocketEngineForce", page.RocketEngineForce);
            obj.Add("RocketEngineEfficiency", page.RocketEngineEfficiency);
            obj.Add("RocketEngineExhaustVelocity", page.RocketEngineExhaustVelocity);
            obj.Add("PressureBreakText", page.PressureBreakText);
            obj.Add("CableBreakText", page.CableBreakText);
            obj.Add("InternalAtmosInfoText", page.InternalAtmosInfoText);
            obj.Add("DrillHeadProperties", page.DrillHeadProperties);
            obj.Add("GasType", page.GasType);
            obj.Add("DisplayFilter", page.DisplayFilter);
            obj.Add("CustomSpriteToUse", SpriteToBase64(page.CustomSpriteToUse));
            obj.Add("PageCustomCategories", page.PageCustomCategories);
            obj.Add("SlotInserts", ParseSlot(page.SlotInserts));
            obj.Add("HowToBuild", ParseBuild(page.HowToBuild));
            obj.Add("BuildStates", ParseBuild(page.BuildStates));
            obj.Add("StructVersionInsert", ParseStructureVersion(page.StructVersionInsert));
            obj.Add("LogicInsert", page.LogicInsert);
            obj.Add("LogicSlotInsert", page.LogicSlotInsert);
            obj.Add("ModeInsert", page.ModeInsert);
            obj.Add("ConnectionInsert", page.ConnectionInsert);
            obj.Add("FoundInOre", page.FoundInOre);
            obj.Add("FoundInGas", page.FoundInGas);
            obj.Add("ConstructedThings", ParseCategory(page.ConstructedThings));
            obj.Add("ProducedThingsInserts", ParseCategory(page.ProducedThingsInserts));
            obj.Add("ConstructedByKits", ParseCategory(page.ConstructedByKits));
            obj.Add("ResourcesUsed", ParseCategory(page.ResourcesUsed));
            obj.Add("UsedIn", ParseCategory(page.UsedIn));
            obj.Add("LifeRequirements", page.LifeRequirements);
            Sprite mainImage = page.CustomSpriteToUse;
            try
            {
                Thing thing = Prefab.Find(page.PrefabHash);
                if (mainImage == null && thing != null)
                {
                    mainImage = thing.Thumbnail;
                }

                if (mainImage == null)
                {
                    mainImage = page.BuildStates[page.BuildStates.Count - 1].PrinterImage;
                }
            }
            catch (Exception)
            {
                ExtractorBepInEx.Log("Failed to find main image");
            }

            obj.Add("MainImageName", mainImage.name);
            obj.Add("MainImage", SpriteToBase64(mainImage));

            string id = page.Key;
            if (page.PrefabHash != 0)
            {
                id = page.PrefabHash.ToString();
            }

            if (page.ReagentsHash != 0)
            {
                id = page.ReagentsHash.ToString();
            }

            //Крайний случай
            if (id == page.Key && mainImage != null)
            {
                id = mainImage.name;
            }

            if (!_ids.Contains(id))
            {
                _ids.Add(id);
            }
            else
            {
                id = page.Key;
                _ids.Add(id);
            }

            obj.Add("ID", id); //Very unique id

            string json = JsonConvert.SerializeObject(obj);

            LanguageCode lang = Localization.CurrentLanguage;
            string path = Path.Combine(
                Application.dataPath,
                "wiki_data",
                lang.ToString(),
                id + ".json"
            );
            string folderPath = Path.Combine(Application.dataPath, "wiki_data", lang.ToString());
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine(json);
            }

            ExtractorBepInEx.Log(path);
        }

        private static List<Dictionary<string, string>> ParseBuild(List<StationBuildCostInsert> buildStates)
        {
            List<Dictionary<string, string>> newBuildStates = new List<Dictionary<string, string>>();
            foreach (var stationBuildCostInsert in buildStates)
            {
                string base64String = SpriteToBase64(stationBuildCostInsert.PrinterImage);
                Dictionary<string, string> buildState = new Dictionary<string, string>();
                buildState.Add("PrinterName", stationBuildCostInsert.PrinterName);
                buildState.Add("TierName", stationBuildCostInsert.TierName);
                buildState.Add("Details", stationBuildCostInsert.Details);
                buildState.Add("Description", stationBuildCostInsert.Description);
                buildState.Add("PageLink", stationBuildCostInsert.PageLink);
                if (base64String != "")
                {
                    buildState.Add("image", base64String);
                }

                newBuildStates.Add(buildState);
            }

            return newBuildStates;
        }

        private static List<Dictionary<string, dynamic>> ParseCategory(List<StationCategoryInsert> buildStates)
        {
            List<Dictionary<string, dynamic>> newBuildStates = new List<Dictionary<string, dynamic>>();
            foreach (var stationCategoryInsert in buildStates)
            {
                string base64String = SpriteToBase64(stationCategoryInsert.InsertImage);
                Dictionary<string, dynamic> buildState = new Dictionary<string, dynamic>();
                buildState.Add("NameOfThing", stationCategoryInsert.NameOfThing);
                buildState.Add("PrefabHash", stationCategoryInsert.PrefabHash);
                buildState.Add("PageLink", stationCategoryInsert.PageLink);
                if (base64String != "")
                {
                    buildState.Add("image", base64String);
                }

                newBuildStates.Add(buildState);
            }

            return newBuildStates;
        }

        private static List<Dictionary<string, dynamic>> ParseSlot(List<StationSlotsInsert> buildStates)
        {
            List<Dictionary<string, dynamic>> newBuildStates = new List<Dictionary<string, dynamic>>();
            foreach (var stationCategoryInsert in buildStates)
            {
                string base64String = SpriteToBase64(stationCategoryInsert.SlotIcon);
                Dictionary<string, dynamic> buildState = new Dictionary<string, dynamic>();
                buildState.Add("SlotName", stationCategoryInsert.SlotName);
                buildState.Add("SlotType", stationCategoryInsert.SlotType);
                buildState.Add("SlotIndex", stationCategoryInsert.SlotIndex);
                if (base64String != "")
                {
                    buildState.Add("image", base64String);
                }

                newBuildStates.Add(buildState);
            }

            return newBuildStates;
        }

        private static List<Dictionary<string, dynamic>> ParseStructureVersion(
            List<StationStructureVersionInsert> buildStates)
        {
            List<Dictionary<string, dynamic>> newBuildStates = new List<Dictionary<string, dynamic>>();
            foreach (var stationCategoryInsert in buildStates)
            {
                string base64String = SpriteToBase64(stationCategoryInsert.StructureImage);
                Dictionary<string, dynamic> buildState = new Dictionary<string, dynamic>();
                buildState.Add("StructureVersion", stationCategoryInsert.StructureVersion);
                buildState.Add("CreationMultiplier", stationCategoryInsert.CreationMultiplier);
                buildState.Add("EnergyCostMultiplier", stationCategoryInsert.EnergyCostMultiplier);
                buildState.Add("MaterialCostMultiplier", stationCategoryInsert.MaterialCostMultiplier);
                buildState.Add("BuildTimeMultiplier", stationCategoryInsert.BuildTimeMultiplier);
                if (base64String != "")
                {
                    buildState.Add("image", base64String);
                }

                newBuildStates.Add(buildState);
            }

            return newBuildStates;
        }

        private static string SpriteToBase64(Sprite sprite)
        {
            if (sprite == null)
            {
                ExtractorBepInEx.Log("Sprite is null");
                return null;
            }

            if (sprite.texture == null)
            {
                ExtractorBepInEx.Log("sprite texture is null");
                return null;
            }

            RenderTexture renderTexture = new RenderTexture(sprite.texture.width, sprite.texture.height, 0);
            Graphics.Blit(sprite.texture, renderTexture);

            Texture2D texture = new Texture2D(sprite.texture.width, sprite.texture.height);
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            byte[] bytes = texture.EncodeToPNG();
            string base64String = Convert.ToBase64String(bytes);

            return base64String;
        }
    }
}