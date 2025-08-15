using System.IO;
using Updater.DataContractModels;

namespace Updater.UtillsClasses
{
	public static class FolderUtills
	{
		public static void CheckOrCreateFolders(FolderModel rootFolder, string destination)
		{
			CheckOrCreateFolder(rootFolder, destination, isRoot: true);
			if (!Directory.Exists(destination))
			{
				Directory.CreateDirectory(destination);
			}
		}

		private static void CheckOrCreateFolder(FolderModel rootFolder, string destination, bool isRoot = false)
		{
			string text = destination;
			if (!isRoot)
			{
				text = Path.Combine(destination, rootFolder.Name);
				if (!Directory.Exists(text))
				{
					Directory.CreateDirectory(text);
				}
			}
			foreach (FolderModel folder in rootFolder.Folders)
			{
				CheckOrCreateFolder(folder, text);
			}
		}
	}
}
