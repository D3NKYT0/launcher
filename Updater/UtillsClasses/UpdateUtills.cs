using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Updater.DataContractModels;
using Updater.HashCalc;
using Updater.Models;

namespace Updater.UtillsClasses
{
	public static class UpdateUtills
	{
		private static readonly List<string> _exeptFiles = new List<string>
		{
			"Option.ini",
			"WindowsInfo.ini",
			"chatfilter.ini",
			"User.ini"
		};

		public static bool CheckFile(string rootPath, FileModel file, UpdateTypes type, bool postCheck = false)
		{
			string text = Path.Combine(rootPath, file.SavePath, file.Name);
			if (!File.Exists(text))
			{
				return false;
			}
			if (type == UpdateTypes.Quick && _exeptFiles.Any((string c) => c.ToLowerInvariant().Contains(file.Name.ToLowerInvariant())))
			{
				return true;
			}
			if (file.CheckHash || postCheck)
			{
				long length = new FileInfo(text).Length;
				if (length != file.Size)
				{
					return false;
				}
				Crc32 crc = new Crc32();
				string a = crc.Get(text);
				if (a != file.Hash)
				{
					return false;
				}
			}
			return true;
		}

		public static List<FileModel> GetAllFileInfos(FolderModel folder, UpdateTypes type, string rootString)
		{
			List<FileModel> list = new List<FileModel>();
			IEnumerable<FileModel> enumerable;
			if (type != UpdateTypes.Full)
			{
				enumerable = folder.Files.Where((FileModel f) => f.QuickUpdate);
			}
			else
			{
				IEnumerable<FileModel> files = folder.Files;
				enumerable = files;
			}
			IEnumerable<FileModel> enumerable2 = enumerable;
			foreach (FileModel item in enumerable2)
			{
				string input = Regex.Replace(item.Path, "^" + rootString, "");
				string text2 = item.SavePath = Regex.Replace(input, "^\\\\", "");
			}
			list.AddRange(enumerable2);
			foreach (FolderModel folder2 in folder.Folders)
			{
				list.AddRange(GetAllFileInfos(folder2, type, rootString));
			}
			return list;
		}
	}
}
