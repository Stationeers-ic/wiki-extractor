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
			result = result.Replace("<b>", "<b style=\"font-weight: bold;\">");
			result = result.Replace("</b>", "</b>");
			result = result.Replace("<i>", "<i style=\"font-style: italic;\">");
			result = result.Replace("</i>", "</i>");
			result = result.Replace("<size=", "<span style=\"font-size: ");
			result = result.Replace("</size>", "</span>");
			result = result.Replace("<color=", "<span style=\"color: ");
			result = result.Replace("</color>", "</span>");
			result = result.Replace("<u>", "<u style=\"text-decoration: underline;\">");
			result = result.Replace("</u>", "</u>");
			result = result.Replace("<s>", "<s style=\"text-decoration: line-through;\">");
			result = result.Replace("</s>", "</s>");
			result = result.Replace("<sub>", "<sub style=\"vertical-align: sub;\">");
			result = result.Replace("</sub>", "</sub>");
			result = result.Replace("<sup>", "<sup style=\"vertical-align: super;\">");
			result = result.Replace("</sup>", "</sup>");
			result = result.Replace("<mark>", "<mark style=\"background-color: yellow;\">");
			result = result.Replace("</mark>", "</mark>");
			result = result.Replace("<link=", "<a href=\"");
			result = result.Replace("</link>", "</a>");
			result = result.Replace("<nobr>", "<nobr style=\"white-space: nowrap;\">");
			result = result.Replace("</nobr>", "</nobr>");
			result = result.Replace("<br>", "<br />");
			result = result.Replace("<br>", "<br />");

			result = result.Replace("<indent=", "<span data-indent=\"");
			result = result.Replace("</indent>", "</span>");

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
