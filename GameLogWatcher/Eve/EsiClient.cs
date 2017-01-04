using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace GameLogWatcher.Eve
{
	static class EsiClient
	{
		const string ApiHost = "https://esi.tech.ccp.is/latest/";

		public static async Task<EsiSearchResult> SearchAsync(string search, string[] categories = null)
		{
			using (var hc = new HttpClient())
			{
				var serializer = new DataContractJsonSerializer(typeof(EsiSearchResult));
				var res = await hc.GetAsync(ApiHost + "search/" + CreateQueryString(new Dictionary<string, string>
				{
					["search"] = search,
					["categories"] = string.Join(",", categories),
				}));

				using (var s = await res.Content.ReadAsStreamAsync())
					return (EsiSearchResult)serializer.ReadObject(s);
			}
		}

		static string CreateQueryString(IDictionary<string, string> parameters) =>
			"?" + string.Join("&", parameters.Where(i => i.Value != null).Select(i => i.Key + "=" + Uri.EscapeDataString(i.Value)));
	}
}
