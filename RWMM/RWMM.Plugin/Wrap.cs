using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RWMM
{
	[Serializable]
	[DataContract]
	public class Wrap<T>
	{
		[DataMember(Order = 0)]
		public string _comment = "";

		[DataMember(Order = 1)]
		public string type;

		[DataMember(Order = 2)]
		public string refName; //null = new

		[DataMember(Order = 3)]
		public string cloneFrom; //null = no clone

		[DataMember(Order = 4,EmitDefaultValue = false)]
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string image;

//		[DataMember(Order = 5)]
//		public string load_order;  //not implemented

		[DataMember(Order = 6)]
		public T obj;

		//Not serialized fields
		public string base_folder;
		public Wrap(string type, T obj, string cloneFrom = null)
		{
			this.type = type;
			this.obj = obj;
			this.cloneFrom = cloneFrom;
		}
	}
}
