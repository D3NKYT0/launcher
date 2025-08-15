using System.Xml.Serialization;

namespace Updater.DataContractModels
{
	[XmlRoot("FileUpdate")]
	public class FileUpdateModel
	{
		public required string Name
		{
			get;
			set;
		}

		public long Size
		{
			get;
			set;
		}

		public required string Hash
		{
			get;
			set;
		}

		public bool QuickUpdate
		{
			get;
			set;
		}

		public bool CheckHash
		{
			get;
			set;
		}
	}
}
