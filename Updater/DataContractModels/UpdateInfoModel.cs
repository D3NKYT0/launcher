using System.Collections.Generic;
using System.Xml.Serialization;

namespace Updater.DataContractModels
{
	[XmlRoot("UpdateInfo")]
	public class UpdateInfoModel
	{
		public int Version
		{
			get;
			set;
		}

		public required FolderModel Folder
		{
			get;
			set;
		}

		[XmlArray("Folders")]
		public List<FolderModel> Folders
		{
			get;
			set;
		} = new List<FolderModel>();
	}
}
