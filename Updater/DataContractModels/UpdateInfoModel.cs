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

		public FolderModel Folder
		{
			get;
			set;
		}
	}
}
