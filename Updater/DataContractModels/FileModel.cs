using System.Collections.Generic;
using System.Xml.Serialization;

namespace Updater.DataContractModels
{
	[XmlRoot("File")]
	public class FileModel
	{
		public string Name
		{
			get;
			set;
		}

		public string Path
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

		[XmlIgnore]
		public string SavePath
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

		[XmlArray("FileUpdates")]
		public List<FileUpdateModel> FileUpdates
		{
			get;
			set;
		} = new List<FileUpdateModel>();

	}
}
