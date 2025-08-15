using System.Collections.Generic;
using System.Xml.Serialization;

namespace Updater.DataContractModels
{
	[XmlRoot("Folder")]
	public class FolderModel
	{
		public string Name
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


		[XmlArray("Files")]
		public List<FileModel> Files
		{
			get;
			set;
		} = new List<FileModel>();

	}
}
