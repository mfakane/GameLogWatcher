using System.Runtime.Serialization;

namespace GameLogWatcher.Eve
{
	[DataContract]
	class EsiSearchResult
	{
		[DataMember(Name = "character")]
		public long[] Characters { get; set; }
	}
}
