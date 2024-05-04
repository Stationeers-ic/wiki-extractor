using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UI.PhaseChange;
using UnityEngine;

namespace WikiExtractorMod
{
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

	}

	/*[HarmonyPatch(typeof(PhaseChangeDiagram))] // Указываем класс, который хотим патчить
	public class PhaseChangeDiagramPatch
	{
		public static Dictionary<Chemistry.GasType, dynamic> graphs;

		[HarmonyPatch("GenerateData")]
		[HarmonyPostfix] // Этот метод будет вызван перед оригинальным методом Setup
		public static void PrefixSetupWithScriptCommand(PhaseChangeDiagram __instance)
		{
			FieldInfo field = typeof(PhaseChangeDiagram).GetField("graphs", BindingFlags.NonPublic | BindingFlags.Static);
			if (field != null)
			{
				var graphs = (Dictionary<Chemistry.GasType, dynamic>)field.GetValue(null);
				foreach (var item in graphs)
				{
					try
					{
						if (item.Key is Chemistry.GasType key)
						{
							var val = item.Value;
							var obj = new Dictionary<string, dynamic>();
							obj.Add("regularCurve", val.Curve(false));
							obj.Add("logCurve", val.Curve(true));
							obj.Add("regularFreeze", val.Freeze(false));
							obj.Add("logFreeze", val.Freeze(true));
							obj.Add("maxPressure", val.MaxPressure(false));
							obj.Add("maxPressureLog", val.MaxPressure(true));
							graphs.Add(key, obj);
						}
					}
					catch (Exception e)
					{
						// Обработка случая, когда метод Curve не найден или вызвал исключение
					}
				}
				ExtractorBepInEx.SaveJson("test", graphs, "wiki_test");
			}
		}

	}*/

}