using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using Util.Commands;
using static Assets.Scripts.Localization;


namespace WikiExtractorMod
{

	#region BepInEx

	[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
	public class ExtractorBepInEx : BaseUnityPlugin
	{
		public const string pluginGuid = "net.elmo.stationeers.Extractor";
		public const string pluginName = "Extractor";
		public const string pluginVersion = "1.4";

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

				CommandLine.AddCommand("stationpedia_export", new StationpediaExportCommand());

				CommandLine.AddCommand("generate_dev_lang", new GenerateDevLangCommand());
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
		public static string GetHashFromByteArray(byte[] data)
		{
			using (SHA256 sha256Hash = SHA256.Create())
			{
				// ComputeHash - возвращает байтовый массив
				byte[] bytes = sha256Hash.ComputeHash(data);

				// Преобразуем байтовый массив в строку
				StringBuilder builder = new StringBuilder();
				for (int i = 0; i < bytes.Length; i++)
				{
					builder.Append(bytes[i].ToString("x2"));
				}
				return builder.ToString();
			}
		}
		public static string SavePng(string name, byte[] bytes, string folder = "wiki_images")
		{
			name += ".png";
			var path = Path.Combine(
				Application.dataPath,
				folder,
				name
			);
			if (File.Exists(path)) return name;
			var folderPath = Path.Combine(Application.dataPath, folder);

			if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

			File.WriteAllBytes(path, bytes);
			return name;
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
		public static string SpriteToFile(Sprite sprite)
		{
			if (sprite == null) return null;

			if (sprite.texture == null) return null;


			var renderTexture = new RenderTexture(sprite.texture.width, sprite.texture.height, 0);
			Graphics.Blit(sprite.texture, renderTexture);

			var texture = new Texture2D(sprite.texture.width, sprite.texture.height);
			RenderTexture.active = renderTexture;
			texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
			texture.Apply();

			byte[] bytes = texture.EncodeToPNG();
			string fileName = GetHashFromByteArray(bytes);
			return SavePng(fileName, bytes);
		}

		class StationpediaExportCommand : CommandBase
		{
			public override string HelpText => "Export Stationpedia";

			public override string[] Arguments { get; } = new string[] { };

			public override bool IsLaunchCmd { get; }

			public override string Execute(string[] args)
			{
				foreach (var page in Stationpedia.StationpediaPages)
				{
					Parcer.process(page);
				}
				return Path.Combine(Application.dataPath, "wiki_data");
			}
		}

		class GenerateDevLangCommand : CommandBase
		{


			public override string HelpText => "Generate Dev Lang";

			public override string[] Arguments { get; } = new string[] { };

			public override bool IsLaunchCmd { get; }

			public override string Execute(string[] args)
			{
				var devLangPath = Path.Combine(Application.streamingAssetsPath, "Language", "devlang.xml");
				var EnLangPath = Path.Combine(Application.streamingAssetsPath, "Language", "english.xml");
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(Localization.Language));
				if (XmlSerialization.Deserialize(xmlSerializer, EnLangPath) is Localization.Language EN)
				{
					var language = new Localization.Language();
				
					Type type = EN.GetType();
					FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

					foreach (FieldInfo field in fields)
					{
						object value = field.GetValue(EN);
						if (value is List<RecordReagent> val1)
						{
							field.SetValue(language, replaceReagent(val1));
						}
						else if (value is List<RecordThing> val2)
						{
							field.SetValue(language, replaceRecordThing(val2));
						}
						else if (value is List<Record> val3)
						{
							field.SetValue(language, replaceRecord(val3));
						}
						else if (value is List<StationpediaPage> val4)
						{
							field.SetValue(language, replaceStationpediaPage(val4));
						}
						else if (value is List<SPDAThingOverideData> val5)
						{
							field.SetValue(language, replaceSPDAThingOverideData(val5));
						}
						else if (value is List<SPDAHomePageButtonOverride> val6)
						{
							field.SetValue(language, replaceSPDAHomePageButtonOverride(val6));
						}
						else {
							field.SetValue(language, value);
						}
					}
					language.Name = "devlang";
					language.Code = LanguageCode.AA;
					xmlSerializer.Serialize(new StreamWriter(devLangPath), language);
					try
					{
						Localization.GetLanguages();
					}catch (Exception e)
					{
						Log( e.ToString());
					}
					Localization.SetLanguage(LanguageCode.AA, true);
					return Path.Combine(Application.streamingAssetsPath, "Language");
				};
				return "error 33";
			}

			public List<Record> replaceRecord(List<Record> input)
			{
				List<Record> t = new List<Record>();
				foreach (var item in input)
				{
					var d = new Record();
					d.Key = item.Key;
					d.Value = item.Key;
					t.Add(d);
				}
				return t;
			}
			public List<RecordThing> replaceRecordThing(List<RecordThing> input)
			{
				List<RecordThing> t = new List<RecordThing>();
				foreach (var item in input)
				{
					var d = new RecordThing();
					d.Key = item.Key;
					d.Value = item.Key;
					d.ThingDescription = item.Key + ".description";
					t.Add(d);
				}
				return t;
			}
			public List<RecordReagent> replaceReagent(List<RecordReagent> input)
			{
				List<RecordReagent> t = new List<RecordReagent>();
				foreach (var item in input)
				{
					var d = new RecordReagent();
					d.Key = item.Key;
					d.Value = item.Key;
					d.Unit = item.Key + ".unit";
					t.Add(d);
				}
				return t;
			}
			public List<StationpediaPage> replaceStationpediaPage(List<StationpediaPage> input)
			{
				return input;
			}

			public List<SPDAThingOverideData> replaceSPDAThingOverideData(List<SPDAThingOverideData> input)
			{
				return input;
			}

			public List<SPDAHomePageButtonOverride> replaceSPDAHomePageButtonOverride(List<SPDAHomePageButtonOverride> input)
			{
				return input;
			}

		}

		public class Parcer
		{
			public static void process(StationpediaPage page, string folder = "wiki_data")
			{
				var obj = new Dictionary<string, dynamic>();
				string prefab = null;
				var tags = new List<string>();
				if (page.PrefabName != null)
				{
					prefab = page.PrefabName;
					obj.Add("TYPE", "object");
					if (prefab.StartsWith("Structure")) tags.Add("structure");
					else if (prefab.StartsWith("Item")) tags.Add("item");
					else if (prefab.StartsWith("AccessCard")) tags.Add("item");
					else if (prefab.EndsWith("Ingot")) tags.Add("ingot");
					//"Tool",
					//"Plant",
					else if (prefab.EndsWith("Ore")) tags.Add("Ore");
					else if (prefab.Contains("Battery") && prefab.Contains("Cell")) tags.Add("Battery");
					else if (prefab == "ItemIntegratedCircuit10") tags.Add("ProgrammableChip");
					else if (prefab.EndsWith("Cartridge")) tags.Add("Cartridge");
					else if (prefab.Contains("GasCanister")) tags.Add("GasCanister");
					else if (prefab.Contains("GasFilter")) tags.Add("GasFilter");
					else if (prefab.StartsWith("Motherboard")) tags.Add("Motherboard");
					else if (prefab.Contains("Disk")) tags.Add("DataDisk");
					else if (prefab.Contains("Circuitboard")) tags.Add("Circuitboard");
					else if (prefab.StartsWith("Entity")) tags.Add("Entity");
					else if (prefab.EndsWith("Helmet")) tags.Add("Helmet");
					else if (prefab.Contains("Jetpack") || prefab.Contains("Backpack")) tags.Add("Back");
					else if (prefab.EndsWith("Suit")) tags.Add("Suit");

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
				obj.Add("prefab", prefab);
				obj.Add("Lang", lang.ToString());
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
				obj.Add("CustomSpriteToUse", ExtractorBepInEx.SpriteToFile(page.CustomSpriteToUse));
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
				obj.Add("MainImage", ExtractorBepInEx.SpriteToFile(mainImage));
				if (mainImage != null) tags.Add("hasImage");

				obj.Add("tags", tags);
				string fileName;
				if (prefab != null)
					fileName = prefab;
				else
					fileName = page.Key;

				obj.Add("ID", fileName);

				ExtractorBepInEx.SaveJson(fileName, obj);
			}

			private static List<Dictionary<string, string>> ParseBuild(List<StationBuildCostInsert> buildStates)
			{
				var newBuildStates = new List<Dictionary<string, string>>();
				foreach (var stationBuildCostInsert in buildStates)
				{
					var base64String = ExtractorBepInEx.SpriteToFile(stationBuildCostInsert.PrinterImage);
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
					var base64String = ExtractorBepInEx.SpriteToFile(stationCategoryInsert.InsertImage);
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
					var base64String = ExtractorBepInEx.SpriteToFile(stationCategoryInsert.SlotIcon);
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
					var base64String = ExtractorBepInEx.SpriteToFile(stationCategoryInsert.StructureImage);
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
		}
	}

	#endregion
}