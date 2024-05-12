using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using Objects.Rockets;
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
using static Assets.Scripts.Networking.NetworkUpdateType.Thing.LogicUnit;


namespace WikiExtractorMod
{

	#region BepInEx

	[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
	public class ExtractorBepInEx : BaseUnityPlugin
	{
		public const string pluginGuid = "net.elmo.stationeers.Extractor";
		public const string pluginName = "Extractor";
		public const string pluginVersion = "1.6";
		public static bool debug = false;


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
				Warn(e.ToString());
			}
		}
		public static void Log(string line)
		{
			Debug.Log("[" + pluginName + "]: " + line);
		}
		public static void Warn(string line)
		{
			if (ExtractorBepInEx.debug == true) {
				Debug.Log("[" + pluginName + "]: " + line);
			}
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
		public static string SavePng(string name, byte[] bytes, string folder = "wiki_data/images")
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
				Log("----------------------GetData--------------------------");
				GetData.process();
				Log("----------------------GetConstants--------------------------");
				GetConstants.process();
				Log("----------------------GetColors--------------------------");
				GetColors.process();
				Log("----------------------GetCommands--------------------------");
				GetCommands.process();
				Log("----------------------DONE--------------------------");
				return Path.Combine(Application.dataPath, "wiki_data");
			}


			public class Parcer
			{
				public class Resonnse
				{
					public string name;
					public Dictionary<string, dynamic> obj;

					public Resonnse(string name, Dictionary<string, dynamic> obj)
					{
						this.name = name;
						this.obj = obj;
					}
				}
				public static Resonnse process(StationpediaPage page, string folder = "wiki_data")
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
						ExtractorBepInEx.Warn("Warning: is page" + page.Key);
						obj.Add("TYPE", "page");
						tags.Add("page");
					}

					if (page.LogicInsert.Count > 0) tags.Add("hasLogic");
					if (page.LogicInstructions.Count > 0) tags.Add("hasLogicInstructions");

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
					obj.Add("LogicInsert", ParseLogic(page.LogicInsert));
					obj.Add("LogicSlotInsert", ParseLogic(page.LogicSlotInsert));
					obj.Add("LogicInstructions", ParseLogicInstructions(page.LogicInstructions));
					obj.Add("ModeInsert", ParseLogic(page.ModeInsert));
					obj.Add("ConnectionInsert", ParseLogic(page.ConnectionInsert));
					obj.Add("FoundInOre", ParseFound(page.FoundInOre));
					obj.Add("FoundInGas", ParseFound(page.FoundInGas));
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
						ExtractorBepInEx.Warn("Failed to find main image");
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
					var __tmp = new Dictionary<string, dynamic>();
					foreach (var val in obj)
					{
						if (val.Value is String value)
						{
							__tmp.Add(val.Key, TextMeshProParcer.Parce(value));
						}
					}
					foreach (var val in __tmp)
					{
						obj[val.Key] = val.Value;
					}

					return new Resonnse(fileName, obj);
				}

				private static List<Dictionary<string, dynamic>> ParseLogic(List<StationLogicInsert> data)
				{
					var result = new List<Dictionary<string, dynamic>>();
					foreach (var value in data)
					{
						var obj = new Dictionary<string, dynamic>();
						obj.Add("LogicName", TextMeshProParcer.Parce(value.LogicName));
						obj.Add("LogicAccessTypes", TextMeshProParcer.Parce(value.LogicAccessTypes));
						result.Add(obj);
					}

					return result;
				}
				private static List<Dictionary<string, dynamic>> ParseFound(List<StationFoundInInsert> data)
				{
					var result = new List<Dictionary<string, dynamic>>();
					foreach (var value in data)
					{
						var obj = new Dictionary<string, dynamic>();
						obj.Add("NameOfThing", TextMeshProParcer.Parce(value.NameOfThing));
						if (Int32.TryParse(value.QuantityOfThing, out int e))
						{
							obj.Add("QuantityOfThing", e);
						}
						else
						{
							obj.Add("QuantityOfThing", value.QuantityOfThing);
						}
						result.Add(obj);
					}

					return result;
				}

				private static List<Dictionary<string, string>> ParseBuild(List<StationBuildCostInsert> buildStates)
				{
					var newBuildStates = new List<Dictionary<string, string>>();
					foreach (var stationBuildCostInsert in buildStates)
					{
						var base64String = ExtractorBepInEx.SpriteToFile(stationBuildCostInsert.PrinterImage);
						var buildState = new Dictionary<string, string>();
						buildState.Add("PrinterName", TextMeshProParcer.Parce(stationBuildCostInsert.PrinterName));
						buildState.Add("TierName", TextMeshProParcer.Parce(stationBuildCostInsert.TierName));
						buildState.Add("Details", TextMeshProParcer.Parce(stationBuildCostInsert.Details));
						buildState.Add("Description", TextMeshProParcer.Parce(stationBuildCostInsert.Description));
						buildState.Add("PageLink", TextMeshProParcer.Parce(stationBuildCostInsert.PageLink));
						if (base64String != "") buildState.Add("image", base64String);

						newBuildStates.Add(buildState);
					}

					return newBuildStates;
				}

				private static List<Dictionary<string, dynamic>> ParseLogicInstructions(List<StationInstruction> data)
				{
					var newBuildStates = new List<Dictionary<string, dynamic>>();
					foreach (var value in data)
					{
						var obj = new Dictionary<string, dynamic>();
						obj.Add("Text", TextMeshProParcer.RawText(value.Text));
						int i;
						bool success = int.TryParse(value.Index, out i);
						if (success)
						{
							obj.Add("Index", i);
						}
						else {
							obj.Add("Index", value.Index);
						}
						obj.Add("Info", TextMeshProParcer.Markdown(value.Info));
						newBuildStates.Add(obj);
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
						buildState.Add("NameOfThing", TextMeshProParcer.Parce(stationCategoryInsert.NameOfThing));
						buildState.Add("PrefabHash", stationCategoryInsert.PrefabHash);
						buildState.Add("PageLink", TextMeshProParcer.Parce(stationCategoryInsert.PageLink));
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
						buildState.Add("SlotName", TextMeshProParcer.Parce(stationCategoryInsert.SlotName));
						buildState.Add("SlotType", TextMeshProParcer.Parce(stationCategoryInsert.SlotType));
						buildState.Add("SlotIndex", TextMeshProParcer.Parce(stationCategoryInsert.SlotIndex));
						if (base64String != "") buildState.Add("image", base64String);

						newBuildStates.Add(buildState);
					}

					return newBuildStates;
				}

				private static List<Dictionary<string, dynamic>> ParseStructureVersion(List<StationStructureVersionInsert> buildStates)
				{
					var newBuildStates = new List<Dictionary<string, dynamic>>();
					foreach (var stationCategoryInsert in buildStates)
					{
						var base64String = ExtractorBepInEx.SpriteToFile(stationCategoryInsert.StructureImage);
						var buildState = new Dictionary<string, dynamic>();
						buildState.Add("StructureVersion", TextMeshProParcer.Parce(stationCategoryInsert.StructureVersion));
						buildState.Add("CreationMultiplier", TextMeshProParcer.Parce(stationCategoryInsert.CreationMultiplier));
						buildState.Add("EnergyCostMultiplier", TextMeshProParcer.Parce(stationCategoryInsert.EnergyCostMultiplier));
						buildState.Add("MaterialCostMultiplier", TextMeshProParcer.Parce(stationCategoryInsert.MaterialCostMultiplier));
						buildState.Add("BuildTimeMultiplier", TextMeshProParcer.Parce(stationCategoryInsert.BuildTimeMultiplier));
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

			public class GetData
			{
				public static void process()
				{
					var data = new Dictionary<string, Dictionary<string, dynamic>>();

					foreach (var page in Stationpedia.StationpediaPages)
					{
						var a = Parcer.process(page);
						data.Add(a.name, a.obj);

					}
					ExtractorBepInEx.SaveJson("data", data);
				}
			}

			public class GetConstants
			{
				public class ConstEnum
				{
					public Type enumType;
					public string prefix = null;

					public ConstEnum(Type enumType, string prefix)
					{
						this.enumType = enumType;
						this.prefix = prefix;
					}
				}

				public static void process()
				{
					var constDictionary = new Dictionary<string, Dictionary<string, dynamic>>();
					var consts = new Dictionary<string, dynamic>();
					var enums = new List<ConstEnum>();

					enums.Add(new ConstEnum(typeof(LogicType), null));
					enums.Add(new ConstEnum(typeof(LogicSlotType), null));
					enums.Add(new ConstEnum(typeof(LogicReagentMode), null));
					enums.Add(new ConstEnum(typeof(LogicBatchMethod), null));
					enums.Add(new ConstEnum(typeof(SoundAlert), "Sound"));
					enums.Add(new ConstEnum(typeof(LogicTransmitterMode), "TransmitterMode"));
					enums.Add(new ConstEnum(typeof(ElevatorMode), "ElevatorMode"));
					enums.Add(new ConstEnum(typeof(ColorType), "Color"));
					enums.Add(new ConstEnum(typeof(EntityState), "EntityState"));
					enums.Add(new ConstEnum(typeof(AirControlMode), "AirControl"));
					enums.Add(new ConstEnum(typeof(DaylightSensor.DaylightSensorMode), "DaylightSensorMode"));
					enums.Add(new ConstEnum(typeof(ConditionOperation), null));
					enums.Add(new ConstEnum(typeof(AirConditioningMode), "AirCon"));
					enums.Add(new ConstEnum(typeof(VentDirection), "Vent"));
					enums.Add(new ConstEnum(typeof(PowerMode), "PowerMode"));
					enums.Add(new ConstEnum(typeof(RobotMode), "RobotMode"));
					enums.Add(new ConstEnum(typeof(SortingClass), "SortingClass"));
					enums.Add(new ConstEnum(typeof(Slot.Class), "SlotClass"));
					enums.Add(new ConstEnum(typeof(Chemistry.GasType), "GasType"));
					enums.Add(new ConstEnum(typeof(RocketMode), "GasType"));
					enums.Add(new ConstEnum(typeof(ReEntryProfile), "ReEntryProfile"));
					enums.Add(new ConstEnum(typeof(SorterInstruction), "SorterInstruction"));

					foreach (var constant in ProgrammableChip.AllConstants)
					{
						var obj = new Dictionary<string, dynamic>();
						obj.Add("literal", constant.Literal);
						obj.Add("name", ProgrammableChip.StripColorTags(constant.GetName()));
						obj.Add("value", constant.Value);
						obj.Add("referenceType", "Constant");
						obj.Add("depricated", false);
						obj.Add("description", ProgrammableChip.StripColorTags(constant.Description));
						constDictionary.Add(constant.Literal, obj);
						consts.Add(constant.Literal, constant.Value);
					}

					foreach (var eType in enums)
					{
						try
						{
							foreach (var value in Enum.GetValues(eType.enumType))
							{
								var obj = new Dictionary<string, dynamic>();
								var name = value.ToString();
								if (eType.prefix != null)
								{
									name = eType.prefix + '.' + value.ToString();
								}
								bool depricated = false;


								if (value is LogicType e)
								{
									depricated = LogicBase.IsDeprecated(e);
								}
								else if
									(value is LogicSlotType e1)
								{
									depricated = LogicBase.IsDeprecated(e1);
								}
								else if (value is LogicReagentMode e2)
								{
									depricated = LogicBase.IsDeprecated(e2);
								}
								else if (value is LogicBatchMethod e3)
								{
									depricated = LogicBase.IsDeprecated(e3);
								}
								else if (value is SoundAlert e4)
								{
									depricated = LogicBase.IsDeprecated(e4);
								}

								obj.Add("literal", name);
								obj.Add("name", name);
								obj.Add("referenceType", "Enum");
								obj.Add("description", "");
								obj.Add("depricated", depricated);
								if (eType.prefix != null)
								{
									consts.Add(name, value);
								}
								if (!constDictionary.ContainsKey(name))
								{
									constDictionary.Add(name, obj);
								}
								else
								{
									constDictionary[name]["depricated"] = depricated;
								}
							}
						}
						catch (Exception e)
						{
							ExtractorBepInEx.Warn("⚠ [" + eType.prefix + "]" + e.ToString());
						}


					}


					ExtractorBepInEx.SaveJson("constants", constDictionary);
					ExtractorBepInEx.SaveJson("consts", consts);
				}
			}

			public class GetColors
			{
				public static void process()
				{
					var obj = new Dictionary<string, Dictionary<string, dynamic>>();
					int colors = GameManager.ColorCount;
					for (int i = 0; i < colors; i++)
					{
						var c = GameManager.GetColorSwatch(i);
						obj.Add(c.Name, new Dictionary<string, dynamic>
					{
						{ "RGB", new float[] { c.Color.r, c.Color.g, c.Color.b } },
						{ "Hex", ColorUtility.ToHtmlStringRGB(c.Color) }
					});
					}
					ExtractorBepInEx.SaveJson("colors", obj);
				}
			}

			public class GetCommands
			{
				public static void process()
				{
					var obj = new Dictionary<string, Dictionary<string, dynamic>>();

					foreach (ScriptCommand cmd in EnumCollections.ScriptCommands.Values)
					{
						var c = new Dictionary<string, dynamic>();
						c.Add("name", Enum.GetName(typeof(ScriptCommand), cmd));
						c.Add("description", TextMeshProParcer.Markdown(ProgrammableChip.GetCommandDescription(cmd)));
						c.Add("example", TextMeshProParcer.RawText(ProgrammableChip.GetCommandExample(cmd)));
						obj.Add(cmd.ToString(), c);
					}
					ExtractorBepInEx.SaveJson("instructions", obj);
				}
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
						else
						{
							field.SetValue(language, value);
						}
					}
					language.Name = "devlang";
					language.Code = LanguageCode.AA;
					xmlSerializer.Serialize(new StreamWriter(devLangPath), language);
					try
					{
						Localization.GetLanguages();
					}
					catch (Exception e)
					{
						ExtractorBepInEx.Warn(e.ToString());
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
	}

	#endregion
}