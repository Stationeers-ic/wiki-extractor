using ReverseMarkdown;
using System.Text.RegularExpressions;

namespace WikiExtractorMod
{
	internal class TextMeshProParcer
	{
		public static string Parce(string text)
		{
			if (text == null)
			{
				return null;
			}
			string result = text;
			result = Regex.Replace(result, "<size=(.+?)>", "<span style=\"font-size: $1\">");
			result = result.Replace("</size>", "</span>");

			result = Regex.Replace(result, "<color=(.+?)>", "<span style=\"color: $1\">");
			result = result.Replace("</color>", "</span>");

			result = result.Replace("<u>", "<u style=\"text-decoration: underline;\">");
			result = result.Replace("<s>", "<s style=\"text-decoration: line-through;\">");
			result = result.Replace("<sub>", "<sub style=\"vertical-align: sub;\">");
			result = result.Replace("<sup>", "<sup style=\"vertical-align: super;\">");
			result = result.Replace("<mark>", "<mark style=\"background-color: yellow;\">");

			result = Regex.Replace(result, "<link=(.+?)>", "<a data-href=\"$1\">");
			result = result.Replace("</link>", "</a>");

			result = result.Replace("<nobr>", "<nobr style=\"white-space: nowrap;\">");

			result = Regex.Replace(result, "<indent=(.+?)>", "<span data-indent=\"$1\">");
			result = result.Replace("</indent>", "</span>");

			result = Regex.Replace(result, "<pos=(.+?)>", string.Empty);
			return result;
		}

		public static string Markdown(string text) { 
			var html = TextMeshProParcer.Parce(text);
			var config = new ReverseMarkdown.Config
			{
				// Include the unknown tag completely in the result (default as well)
				UnknownTags = Config.UnknownTagsOption.Drop,
				// generate GitHub flavoured markdown, supported for BR, PRE and table tags
				GithubFlavored = true,
				// will ignore all comments
				RemoveComments = true,
				// remove markdown output for links where appropriate
				SmartHrefHandling = false
			};
			var converter = new ReverseMarkdown.Converter(config);
			var markdown = converter.Convert(html);
			return markdown;
		}

		public static string RawText(string text)
		{
			var html = TextMeshProParcer.Parce(text);
			return RemoveHtmlTags(html);
		}

		private static string RemoveHtmlTags(string html)
		{
			return Regex.Replace(html, "<.*?>", string.Empty);
		}
	}
}
