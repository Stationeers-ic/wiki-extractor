using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.UI;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace WikiExtractorMod
{

	[HarmonyPatch(typeof(Stationpedia), nameof(Stationpedia.Register))]
	public class Extractor
	{

		[HarmonyPrefix]
		public static void Prefix(StationpediaPage page, bool fallback = false)
		{
			//ExtractorBepInEx.Parcer.process(page);
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