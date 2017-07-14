using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Celedon
{
	[DataContract]
	public class AutoNumberPluginConfig
	{
		[DataMember]
		public string EntityName;

		[DataMember]
		public string EventName;
	}
}
