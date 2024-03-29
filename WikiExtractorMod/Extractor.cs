﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using HarmonyLib;
using UnityEngine;

namespace WikiExtractorMod
{
    [HarmonyPatch(typeof(Stationpedia), nameof(Stationpedia.Register))]
    public class Extractor
    {
        [HarmonyPrefix]
        public static bool Prefix(StationpediaPage page, bool fallback = false)
        {
            var obj = new Dictionary<string, dynamic>();
            string prefab = null;
            var tags = new List<string>();
            if (page.PrefabName != null)
            {
                prefab = page.PrefabName;
                obj.Add("TYPE", "object");
                if (prefab.StartsWith("Structure"))
                    tags.Add("structure");
                else if (prefab.StartsWith("Item")) tags.Add("item");

                if (prefab.EndsWith("Ingot")) tags.Add("ingot");

                if (prefab.StartsWith("ItemKit")) tags.Add("kit");

                if (prefab.StartsWith("StructureCable")) tags.Add("cable");
            }
            else if (page.ReagentsType != null)
            {
                prefab = page.ReagentsType;
                obj.Add("TYPE", "reagent");
                tags.Add("reagent");
            }
            else
            {
                ExtractorBepInEx.Log("Warning: is page" + page.Key);
                obj.Add("TYPE", "page");
                tags.Add("page");
            }


            if (page.LogicInsert.Count > 0) tags.Add("hasLogic");

            if (page.SlotInserts.Count > 0)
            {
                tags.Add("hasSlot");
                foreach (var slotInsert in page.SlotInserts)
                    if (slotInsert.SlotType == "ProgrammableChip")
                    {
                        tags.Add("hasChip");
                        break;
                    }
            }

            if (page.BuildStates.Count > 0) tags.Add("buildable");

            if (page.ModeInsert.Count > 0) tags.Add("hasMode");

            if (page.FoundInOre.Count > 0) tags.Add("hasOre");

            if (page.FoundInGas.Count > 0) tags.Add("hasGas");

            if (page.PaintableText == "Yes") tags.Add("paintable");

            if (page.GasType > 0) tags.Add("gas");

            if (page.ProducedThingsInserts.Count > 0) tags.Add("hasReciepe");

            if (prefab != null) tags.Add("hasPrefab");

            var lang = Localization.CurrentLanguage;
            obj.Add("prefab", prefab); //Very unique id
            obj.Add("Lang", lang.ToString()); //Very unique id
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
            obj.Add("CustomSpriteToUse", ExtractorBepInEx.SpriteToBase64(page.CustomSpriteToUse));
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

            var deviceConnectCount = 0;
            var mainImage = page.CustomSpriteToUse;
            try
            {
                var thing = Prefab.Find(page.PrefabHash);
                if (thing != null)
                {
                    if (mainImage == null) mainImage = thing.GetThumbnail();

                    if (tags.Contains("hasChip"))
                        foreach (var slot in thing.Slots)
                            if (slot.Type == Slot.Class.ProgrammableChip)
                            {
                                if (thing is CircuitHousing)
                                    deviceConnectCount = 6;
                                else if (thing is DeviceInputOutputCircuit) deviceConnectCount = 2;

                                break;
                            }
                }

                if (mainImage == null) mainImage = page.BuildStates[page.BuildStates.Count - 1].PrinterImage;
            }
            catch (Exception)
            {
                ExtractorBepInEx.Log("Failed to find main image");
            }

            obj.Add("DeviceConnectCount", deviceConnectCount);
            obj.Add("MainImage", ExtractorBepInEx.SpriteToBase64(mainImage));
            if (mainImage != null) tags.Add("hasImage");

            obj.Add("tags", tags);
            string fileName;
            if (prefab != null)
                fileName = prefab;
            else
                fileName = page.Key;

            obj.Add("ID", fileName);

            ExtractorBepInEx.SaveJson(fileName, obj);
            return true;
        }

        private static List<Dictionary<string, string>> ParseBuild(List<StationBuildCostInsert> buildStates)
        {
            var newBuildStates = new List<Dictionary<string, string>>();
            foreach (var stationBuildCostInsert in buildStates)
            {
                var base64String = ExtractorBepInEx.SpriteToBase64(stationBuildCostInsert.PrinterImage);
                var buildState = new Dictionary<string, string>();
                buildState.Add("PrinterName", stationBuildCostInsert.PrinterName);
                buildState.Add("TierName", stationBuildCostInsert.TierName);
                buildState.Add("Details", stationBuildCostInsert.Details);
                buildState.Add("Description", stationBuildCostInsert.Description);
                buildState.Add("PageLink", stationBuildCostInsert.PageLink);
                if (base64String != "") buildState.Add("image", base64String);

                newBuildStates.Add(buildState);
            }

            return newBuildStates;
        }

        private static List<Dictionary<string, dynamic>> ParseCategory(List<StationCategoryInsert> buildStates)
        {
            var newBuildStates = new List<Dictionary<string, dynamic>>();
            foreach (var stationCategoryInsert in buildStates)
            {
                var base64String = ExtractorBepInEx.SpriteToBase64(stationCategoryInsert.InsertImage);
                var buildState = new Dictionary<string, dynamic>();
                buildState.Add("NameOfThing", stationCategoryInsert.NameOfThing);
                buildState.Add("PrefabHash", stationCategoryInsert.PrefabHash);
                buildState.Add("PageLink", stationCategoryInsert.PageLink);
                if (base64String != "") buildState.Add("image", base64String);

                newBuildStates.Add(buildState);
            }

            return newBuildStates;
        }

        private static List<Dictionary<string, dynamic>> ParseSlot(List<StationSlotsInsert> buildStates)
        {
            var newBuildStates = new List<Dictionary<string, dynamic>>();
            foreach (var stationCategoryInsert in buildStates)
            {
                var base64String = ExtractorBepInEx.SpriteToBase64(stationCategoryInsert.SlotIcon);
                var buildState = new Dictionary<string, dynamic>();
                buildState.Add("SlotName", stationCategoryInsert.SlotName);
                buildState.Add("SlotType", stationCategoryInsert.SlotType);
                buildState.Add("SlotIndex", stationCategoryInsert.SlotIndex);
                if (base64String != "") buildState.Add("image", base64String);

                newBuildStates.Add(buildState);
            }

            return newBuildStates;
        }

        private static List<Dictionary<string, dynamic>> ParseStructureVersion(
            List<StationStructureVersionInsert> buildStates)
        {
            var newBuildStates = new List<Dictionary<string, dynamic>>();
            foreach (var stationCategoryInsert in buildStates)
            {
                var base64String = ExtractorBepInEx.SpriteToBase64(stationCategoryInsert.StructureImage);
                var buildState = new Dictionary<string, dynamic>();
                buildState.Add("StructureVersion", stationCategoryInsert.StructureVersion);
                buildState.Add("CreationMultiplier", stationCategoryInsert.CreationMultiplier);
                buildState.Add("EnergyCostMultiplier", stationCategoryInsert.EnergyCostMultiplier);
                buildState.Add("MaterialCostMultiplier", stationCategoryInsert.MaterialCostMultiplier);
                buildState.Add("BuildTimeMultiplier", stationCategoryInsert.BuildTimeMultiplier);
                if (base64String != "") buildState.Add("image", base64String);

                newBuildStates.Add(buildState);
            }

            return newBuildStates;
        }

        public static byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = MD5.Create())
            {
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
            }
        }

        public static string GetHashString(string inputString)
        {
            var sb = new StringBuilder();
            foreach (var b in GetHash(inputString))
                sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
    }


    [HarmonyPatch(typeof(HelpReference))] // Указываем класс, который хотим патчить
    public class HelpReferencePatch
    {
        [HarmonyPatch("Setup", typeof(ScriptCommand), typeof(Sprite))]
        [HarmonyPrefix] // Этот метод будет вызван перед оригинальным методом Setup
        public static bool PrefixSetupWithScriptCommand(HelpReference __instance,
            ScriptCommand command,
            Sprite defaultItemImage)
        {
            var obj = new Dictionary<string, dynamic>();
            obj.Add("name", command.ToString());
            obj.Add("example", ProgrammableChip.StripColorTags(ProgrammableChip.GetCommandExample(command)));
            obj.Add("referenceType", "Instruction");
            obj.Add("description", ProgrammableChip.StripColorTags(ProgrammableChip.GetCommandDescription(command)));
            ExtractorBepInEx.SaveJson(command.ToString(), obj, "wiki_instruction");
            return true;
        }

        [HarmonyPatch("Setup", typeof(ProgrammableChip.Constant), typeof(Sprite))]
        [HarmonyPrefix] // Этот метод будет вызван перед оригинальным методом Setup
        public static bool PrefixSetupWithScriptCommand(HelpReference __instance,
            ProgrammableChip.Constant constant,
            Sprite defaultItemImage)
        {
            var obj = new Dictionary<string, dynamic>();
            obj.Add("literal", constant.Literal);
            obj.Add("name", ProgrammableChip.StripColorTags(constant.GetName()));
            obj.Add("value", constant.Value);
            obj.Add("referenceType", "Constant");
            obj.Add("description", ProgrammableChip.StripColorTags(constant.Description));
            ExtractorBepInEx.SaveJson(constant.Literal, obj, "wiki_constant");
            return true;
        }
    }
}