using System.Xml.Serialization;

namespace Updater.DataContractModels
{
	[XmlRoot("File")]
	public class FileUpdateModel
	{
		public string Name
		{
			get;
			set;
		}

		public long Size
		{
			get;
			set;
		}

		public string Hash
		{
			get;
			set;
		}

		public int Version
		{
			get;
			set;
		}
	}
}
