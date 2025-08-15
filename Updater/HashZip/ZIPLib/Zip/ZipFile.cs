using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CSharp;

namespace Updater.HashZip.ZIPLib.Zip
{
	[Guid("ebc25cf6-9120-4283-b972-0e5520d00005")]
	[ComVisible(true)]
	[ClassInterface(ClassInterfaceType.AutoDispatch)]
	public class ZipFile : IEnumerable, IEnumerable<ZipEntry>, IDisposable
	{
		private class ExtractorSettings
		{
			public SelfExtractorFlavor Flavor;

			public List<string> ReferencedAssemblies;

			public List<string> CopyThroughResources;

			public List<string> ResourcesToCompile;
		}

		public static readonly Encoding DefaultEncoding = Encoding.GetEncoding("IBM437");

		private TextWriter _StatusMessageTextWriter;

		private bool _CaseSensitiveRetrieval;

		private Stream _readstream;

		private Stream _writestream;

		private bool _disposed;

		private List<ZipEntry> _entries;

		private bool _ForceNoCompression;

		private string _name;

		private string _Comment;

		internal string _Password;

		private bool _emitNtfsTimes = true;

		private bool _emitUnixTimes;

		private CompressionStrategy _Strategy = CompressionStrategy.Default;

		private long _originPosition;

		private bool _fileAlreadyExists;

		private string _temporaryFileName;

		private bool _contentsChanged;

		private bool _hasBeenSaved;

		private string _TempFileFolder;

		private bool _ReadStreamIsOurs = true;

		private object LOCK = new object();

		private bool _saveOperationCanceled;

		private bool _extractOperationCanceled;

		private EncryptionAlgorithm _Encryption;

		private bool _JustSaved;

		private bool _NeedZip64CentralDirectory;

		private long _locEndOfCDS = -1L;

		private bool? _OutputUsesZip64;

		internal bool _inExtractAll;

		private Encoding _provisionalAlternateEncoding = Encoding.GetEncoding("IBM437");

		private int _BufferSize = 8192;

		internal Zip64Option _zip64 = Zip64Option.Default;

		private bool _SavingSfx;

		private long _lengthOfReadStream = -99L;

		private static ExtractorSettings[] SettingsList = new ExtractorSettings[2]
		{
			new ExtractorSettings
			{
				Flavor = SelfExtractorFlavor.WinFormsApplication,
				ReferencedAssemblies = new List<string>
				{
					"System.dll",
					"System.Windows.Forms.dll",
					"System.Drawing.dll"
				},
				CopyThroughResources = new List<string>
				{
					"Ionic.Zip.WinFormsSelfExtractorStub.resources",
					"Ionic.Zip.PasswordDialog.resources",
					"Ionic.Zip.ZipContentsDialog.resources"
				},
				ResourcesToCompile = new List<string>
				{
					"Ionic.Zip.Resources.WinFormsSelfExtractorStub.cs",
					"Ionic.Zip.WinFormsSelfExtractorStub",
					"Ionic.Zip.Resources.PasswordDialog.cs",
					"Ionic.Zip.PasswordDialog",
					"Ionic.Zip.Resources.ZipContentsDialog.cs",
					"Ionic.Zip.ZipContentsDialog",
					"Ionic.Zip.Resources.FolderBrowserDialogEx.cs"
				}
			},
			new ExtractorSettings
			{
				Flavor = SelfExtractorFlavor.ConsoleApplication,
				ReferencedAssemblies = new List<string>
				{
					"System.dll"
				},
				CopyThroughResources = null,
				ResourcesToCompile = new List<string>
				{
					"Ionic.Zip.Resources.CommandLineSelfExtractorStub.cs"
				}
			}
		};

		public bool FullScan
		{
			get;
			set;
		}

		public int BufferSize
		{
			get
			{
				return _BufferSize;
			}
			set
			{
				_BufferSize = value;
			}
		}

		public int CodecBufferSize
		{
			get;
			set;
		}

		public bool FlattenFoldersOnExtract
		{
			get;
			set;
		}

		public CompressionStrategy Strategy
		{
			get
			{
				return _Strategy;
			}
			set
			{
				_Strategy = value;
			}
		}

		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
			}
		}

		public CompressionLevel CompressionLevel
		{
			get;
			set;
		}

		public string Comment
		{
			get
			{
				return _Comment;
			}
			set
			{
				_Comment = value;
				_contentsChanged = true;
			}
		}

		public bool EmitTimesInWindowsFormatWhenSaving
		{
			get
			{
				return _emitNtfsTimes;
			}
			set
			{
				_emitNtfsTimes = value;
			}
		}

		public bool EmitTimesInUnixFormatWhenSaving
		{
			get
			{
				return _emitUnixTimes;
			}
			set
			{
				_emitUnixTimes = value;
			}
		}

		internal bool Verbose => _StatusMessageTextWriter != null;

		public bool CaseSensitiveRetrieval
		{
			get
			{
				return _CaseSensitiveRetrieval;
			}
			set
			{
				_CaseSensitiveRetrieval = value;
			}
		}

		public bool UseUnicodeAsNecessary
		{
			get
			{
				return _provisionalAlternateEncoding == Encoding.GetEncoding("UTF-8");
			}
			set
			{
				_provisionalAlternateEncoding = (value ? Encoding.GetEncoding("UTF-8") : DefaultEncoding);
			}
		}

		public Zip64Option UseZip64WhenSaving
		{
			get
			{
				return _zip64;
			}
			set
			{
				_zip64 = value;
			}
		}

		public bool? RequiresZip64
		{
			get
			{
				if (_entries.Count > 65534)
				{
					return true;
				}
				if (!_hasBeenSaved || _contentsChanged)
				{
					return null;
				}
				foreach (ZipEntry entry in _entries)
				{
					if (entry.RequiresZip64.Value)
					{
						return true;
					}
				}
				return false;
			}
		}

		public bool? OutputUsedZip64 => _OutputUsesZip64;

		public Encoding ProvisionalAlternateEncoding
		{
			get
			{
				return _provisionalAlternateEncoding;
			}
			set
			{
				_provisionalAlternateEncoding = value;
			}
		}

		public TextWriter StatusMessageTextWriter
		{
			get
			{
				return _StatusMessageTextWriter;
			}
			set
			{
				_StatusMessageTextWriter = value;
			}
		}

		public bool ForceNoCompression
		{
			get
			{
				return _ForceNoCompression;
			}
			set
			{
				_ForceNoCompression = value;
			}
		}

		public string TempFileFolder
		{
			get
			{
				return _TempFileFolder;
			}
			set
			{
				_TempFileFolder = value;
				if (value == null || Directory.Exists(value))
				{
					return;
				}
				throw new FileNotFoundException($"That directory ({value}) does not exist.");
			}
		}

		public string Password
		{
			set
			{
				_Password = value;
				if (_Password == null)
				{
					Encryption = EncryptionAlgorithm.None;
				}
				else if (Encryption == EncryptionAlgorithm.None)
				{
					Encryption = EncryptionAlgorithm.PkzipWeak;
				}
			}
		}

		public ExtractExistingFileAction ExtractExistingFile
		{
			get;
			set;
		}

		public ZipErrorAction ZipErrorAction
		{
			get;
			set;
		}

		public EncryptionAlgorithm Encryption
		{
			get
			{
				return _Encryption;
			}
			set
			{
				if (value == EncryptionAlgorithm.Unsupported)
				{
					throw new InvalidOperationException("You may not set Encryption to that value.");
				}
				_Encryption = value;
			}
		}

		public ReReadApprovalCallback WillReadTwiceOnInflation
		{
			get;
			set;
		}

		public WantCompressionCallback WantCompression
		{
			get;
			set;
		}

		public static Version LibraryVersion => Assembly.GetExecutingAssembly().GetName().Version;

		internal Stream ReadStream
		{
			get
			{
				if (_readstream == null && _name != null)
				{
					try
					{
						_readstream = File.OpenRead(_name);
						_ReadStreamIsOurs = true;
					}
					catch (IOException innerException)
					{
						throw new ZipException("Error opening the file", innerException);
					}
				}
				return _readstream;
			}
		}

		public ZipEntry this[int ix]
		{
			get
			{
				return _entries[ix];
			}
			set
			{
				if (value != null)
				{
					throw new ZipException("You may not set this to a non-null ZipEntry value.", new ArgumentException("this[int]"));
				}
				RemoveEntry(_entries[ix]);
			}
		}

		public ZipEntry this[string fileName]
		{
			get
			{
				foreach (ZipEntry entry in _entries)
				{
					if (CaseSensitiveRetrieval)
					{
						if (entry.FileName == fileName)
						{
							return entry;
						}
						if (fileName.Replace("\\", "/") == entry.FileName)
						{
							return entry;
						}
						if (entry.FileName.Replace("\\", "/") == fileName)
						{
							return entry;
						}
						if (entry.FileName.EndsWith("/"))
						{
							string text = entry.FileName.Trim("/".ToCharArray());
							if (text == fileName)
							{
								return entry;
							}
							if (fileName.Replace("\\", "/") == text)
							{
								return entry;
							}
							if (text.Replace("\\", "/") == fileName)
							{
								return entry;
							}
						}
						continue;
					}
					if (string.Compare(entry.FileName, fileName, StringComparison.CurrentCultureIgnoreCase) == 0)
					{
						return entry;
					}
					if (string.Compare(fileName.Replace("\\", "/"), entry.FileName, StringComparison.CurrentCultureIgnoreCase) == 0)
					{
						return entry;
					}
					if (string.Compare(entry.FileName.Replace("\\", "/"), fileName, StringComparison.CurrentCultureIgnoreCase) == 0)
					{
						return entry;
					}
					if (entry.FileName.EndsWith("/"))
					{
						string text2 = entry.FileName.Trim("/".ToCharArray());
						if (string.Compare(text2, fileName, StringComparison.CurrentCultureIgnoreCase) == 0)
						{
							return entry;
						}
						if (string.Compare(fileName.Replace("\\", "/"), text2, StringComparison.CurrentCultureIgnoreCase) == 0)
						{
							return entry;
						}
						if (string.Compare(text2.Replace("\\", "/"), fileName, StringComparison.CurrentCultureIgnoreCase) == 0)
						{
							return entry;
						}
					}
				}
				return null;
			}
			set
			{
				if (value != null)
				{
					throw new ArgumentException("You may not set this to a non-null ZipEntry value.");
				}
				RemoveEntry(fileName);
			}
		}

		public ReadOnlyCollection<string> EntryFileNames
		{
			get
			{
				List<string> list = _entries.ConvertAll((ZipEntry e) => e.FileName);
				return list.AsReadOnly();
			}
		}

		public ReadOnlyCollection<ZipEntry> Entries => _entries.AsReadOnly();

		public int Count => _entries.Count;

		private Stream WriteStream
		{
			get
			{
				if (_writestream == null && _name != null)
				{
					if (TempFileFolder == ".")
					{
						_temporaryFileName = SharedUtilities.GetTempFilename();
					}
					else if (TempFileFolder != null)
					{
						_temporaryFileName = Path.Combine(TempFileFolder, SharedUtilities.GetTempFilename());
					}
					else
					{
						string directoryName = Path.GetDirectoryName(_name);
						_temporaryFileName = Path.Combine(directoryName, SharedUtilities.GetTempFilename());
					}
					_writestream = new FileStream(_temporaryFileName, FileMode.CreateNew);
				}
				return _writestream;
			}
			set
			{
				if (value != null)
				{
					throw new ZipException("Whoa!", new ArgumentException("Cannot set the stream to a non-null value.", "value"));
				}
				_writestream = null;
			}
		}

		private string ArchiveNameForEvent => (_name != null) ? _name : "(stream)";

		private long LengthOfReadStream
		{
			get
			{
				if (_lengthOfReadStream == -99)
				{
					if (_ReadStreamIsOurs)
					{
						FileInfo fileInfo = new FileInfo(_name);
						_lengthOfReadStream = fileInfo.Length;
					}
					else
					{
						_lengthOfReadStream = -1L;
					}
				}
				return _lengthOfReadStream;
			}
		}

		internal long RelativeOffset => ReadStream.Position - _originPosition;

		public event EventHandler<SaveProgressEventArgs> SaveProgress;

		public event EventHandler<ReadProgressEventArgs> ReadProgress;

		public event EventHandler<ExtractProgressEventArgs> ExtractProgress;

		public event EventHandler<AddProgressEventArgs> AddProgress;

		public event EventHandler<ZipErrorEventArgs> ZipError;

		public ZipEntry AddItem(string fileOrDirectoryName)
		{
			return AddItem(fileOrDirectoryName, null);
		}

		public ZipEntry AddItem(string fileOrDirectoryName, string directoryPathInArchive)
		{
			if (File.Exists(fileOrDirectoryName))
			{
				return AddFile(fileOrDirectoryName, directoryPathInArchive);
			}
			if (Directory.Exists(fileOrDirectoryName))
			{
				return AddDirectory(fileOrDirectoryName, directoryPathInArchive);
			}
			throw new FileNotFoundException($"That file or directory ({fileOrDirectoryName}) does not exist!");
		}

		public ZipEntry AddFile(string fileName)
		{
			return AddFile(fileName, null);
		}

		public ZipEntry AddFile(string fileName, string directoryPathInArchive)
		{
			string nameInArchive = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
			ZipEntry zipEntry = ZipEntry.Create(fileName, nameInArchive);
			zipEntry.ForceNoCompression = ForceNoCompression;
			zipEntry.ExtractExistingFile = ExtractExistingFile;
			zipEntry.ZipErrorAction = ZipErrorAction;
			zipEntry.WillReadTwiceOnInflation = WillReadTwiceOnInflation;
			zipEntry.WantCompression = WantCompression;
			zipEntry.ProvisionalAlternateEncoding = ProvisionalAlternateEncoding;
			zipEntry._zipfile = this;
			zipEntry.Encryption = Encryption;
			zipEntry.Password = _Password;
			zipEntry.EmitTimesInWindowsFormatWhenSaving = _emitNtfsTimes;
			zipEntry.EmitTimesInUnixFormatWhenSaving = _emitUnixTimes;
			if (Verbose)
			{
				StatusMessageTextWriter.WriteLine("adding {0}...", fileName);
			}
			InsureUniqueEntry(zipEntry);
			_entries.Add(zipEntry);
			AfterAddEntry(zipEntry);
			_contentsChanged = true;
			return zipEntry;
		}

		public void RemoveEntries(ICollection<ZipEntry> entriesToRemove)
		{
			foreach (ZipEntry item in entriesToRemove)
			{
				RemoveEntry(item);
			}
		}

		public void RemoveEntries(ICollection<string> entriesToRemove)
		{
			foreach (string item in entriesToRemove)
			{
				RemoveEntry(item);
			}
		}

		public void AddFiles(IEnumerable<string> fileNames)
		{
			AddFiles(fileNames, null);
		}

		public void UpdateFiles(IEnumerable<string> fileNames)
		{
			UpdateFiles(fileNames, null);
		}

		public void AddFiles(IEnumerable<string> fileNames, string directoryPathInArchive)
		{
			AddFiles(fileNames, preserveDirHierarchy: false, directoryPathInArchive);
		}

		public void AddFiles(IEnumerable<string> fileNames, bool preserveDirHierarchy, string directoryPathInArchive)
		{
			OnAddStarted();
			if (preserveDirHierarchy)
			{
				foreach (string fileName in fileNames)
				{
					if (directoryPathInArchive != null)
					{
						string directoryPathInArchive2 = SharedUtilities.NormalizePath(Path.Combine(directoryPathInArchive, Path.GetDirectoryName(fileName)));
						AddFile(fileName, directoryPathInArchive2);
					}
					else
					{
						AddFile(fileName, null);
					}
				}
			}
			else
			{
				foreach (string fileName2 in fileNames)
				{
					AddFile(fileName2, directoryPathInArchive);
				}
			}
			OnAddCompleted();
		}

		public void UpdateFiles(IEnumerable<string> fileNames, string directoryPathInArchive)
		{
			OnAddStarted();
			foreach (string fileName in fileNames)
			{
				UpdateFile(fileName, directoryPathInArchive);
			}
			OnAddCompleted();
		}

		public ZipEntry UpdateFile(string fileName)
		{
			return UpdateFile(fileName, null);
		}

		public ZipEntry UpdateFile(string fileName, string directoryPathInArchive)
		{
			string fileName2 = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
			if (this[fileName2] != null)
			{
				RemoveEntry(fileName2);
			}
			return AddFile(fileName, directoryPathInArchive);
		}

		public ZipEntry UpdateDirectory(string directoryName)
		{
			return UpdateDirectory(directoryName, null);
		}

		public ZipEntry UpdateDirectory(string directoryName, string directoryPathInArchive)
		{
			return AddOrUpdateDirectoryImpl(directoryName, directoryPathInArchive, AddOrUpdateAction.AddOrUpdate);
		}

		public void UpdateItem(string itemName)
		{
			UpdateItem(itemName, null);
		}

		public void UpdateItem(string itemName, string directoryPathInArchive)
		{
			if (File.Exists(itemName))
			{
				UpdateFile(itemName, directoryPathInArchive);
				return;
			}
			if (Directory.Exists(itemName))
			{
				UpdateDirectory(itemName, directoryPathInArchive);
				return;
			}
			throw new FileNotFoundException($"That file or directory ({itemName}) does not exist!");
		}

		[Obsolete("Please use method AddEntry(string, string, System.IO.Stream))")]
		public ZipEntry AddFileFromStream(string fileName, string directoryPathInArchive, Stream stream)
		{
			return AddEntry(fileName, directoryPathInArchive, stream);
		}

		[Obsolete("Please use method AddEntry(string, string, System.IO.Stream))")]
		public ZipEntry AddFileStream(string fileName, string directoryPathInArchive, Stream stream)
		{
			return AddEntry(fileName, directoryPathInArchive, stream);
		}

		public ZipEntry AddEntry(string fileName, string directoryPathInArchive, string content)
		{
			return AddEntry(fileName, directoryPathInArchive, content, Encoding.Default);
		}

		public ZipEntry AddEntry(string fileName, string directoryPathInArchive, string content, Encoding encoding)
		{
			MemoryStream memoryStream = new MemoryStream();
			StreamWriter streamWriter = new StreamWriter(memoryStream, encoding);
			streamWriter.Write(content);
			streamWriter.Flush();
			memoryStream.Seek(0L, SeekOrigin.Begin);
			return AddEntry(fileName, directoryPathInArchive, memoryStream);
		}

		public ZipEntry AddEntry(string fileName, string directoryPathInArchive, Stream stream)
		{
			string nameInArchive = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
			ZipEntry zipEntry = ZipEntry.Create(fileName, nameInArchive, isStream: true, stream);
			zipEntry.ForceNoCompression = ForceNoCompression;
			zipEntry.ExtractExistingFile = ExtractExistingFile;
			zipEntry.ZipErrorAction = ZipErrorAction;
			zipEntry.WillReadTwiceOnInflation = WillReadTwiceOnInflation;
			zipEntry.WantCompression = WantCompression;
			zipEntry.ProvisionalAlternateEncoding = ProvisionalAlternateEncoding;
			zipEntry._zipfile = this;
			zipEntry.Encryption = Encryption;
			zipEntry.Password = _Password;
			zipEntry.EmitTimesInWindowsFormatWhenSaving = _emitNtfsTimes;
			zipEntry.EmitTimesInUnixFormatWhenSaving = _emitUnixTimes;
			if (Verbose)
			{
				StatusMessageTextWriter.WriteLine("adding {0}...", fileName);
			}
			InsureUniqueEntry(zipEntry);
			_entries.Add(zipEntry);
			AfterAddEntry(zipEntry);
			_contentsChanged = true;
			return zipEntry;
		}

		[Obsolete("Please use method AddEntry(string, String, string))")]
		public ZipEntry AddFileFromString(string fileName, string directoryPathInArchive, string content)
		{
			return AddEntry(fileName, directoryPathInArchive, content, Encoding.Default);
		}

		public ZipEntry UpdateEntry(string fileName, string directoryPathInArchive, string content)
		{
			return UpdateEntry(fileName, directoryPathInArchive, content, Encoding.Default);
		}

		public ZipEntry UpdateEntry(string fileName, string directoryPathInArchive, string content, Encoding encoding)
		{
			string fileName2 = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
			if (this[fileName2] != null)
			{
				RemoveEntry(fileName2);
			}
			return AddEntry(fileName, directoryPathInArchive, content, encoding);
		}

		public ZipEntry UpdateEntry(string fileName, string directoryPathInArchive, Stream stream)
		{
			string fileName2 = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
			if (this[fileName2] != null)
			{
				RemoveEntry(fileName2);
			}
			return AddEntry(fileName, directoryPathInArchive, stream);
		}

		[Obsolete("Please use method UpdateEntry()")]
		public ZipEntry UpdateFileStream(string fileName, string directoryPathInArchive, Stream stream)
		{
			return UpdateEntry(fileName, directoryPathInArchive, stream);
		}

		public ZipEntry AddEntry(string fileName, string directoryPathInArchive, byte[] byteContent)
		{
			if (byteContent == null)
			{
				throw new ArgumentException("bad argument", "byteContent");
			}
			MemoryStream stream = new MemoryStream(byteContent);
			return AddEntry(fileName, directoryPathInArchive, stream);
		}

		public ZipEntry UpdateEntry(string fileName, string directoryPathInArchive, byte[] byteContent)
		{
			string fileName2 = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
			if (this[fileName2] != null)
			{
				RemoveEntry(fileName2);
			}
			return AddEntry(fileName, directoryPathInArchive, byteContent);
		}

		private void InsureUniqueEntry(ZipEntry ze1)
		{
			foreach (ZipEntry entry in _entries)
			{
				if (SharedUtilities.TrimVolumeAndSwapSlashes(ze1.FileName) == entry.FileName)
				{
					throw new ArgumentException($"The entry '{ze1.FileName}' already exists in the zip archive.");
				}
			}
		}

		public ZipEntry AddDirectory(string directoryName)
		{
			return AddDirectory(directoryName, null);
		}

		public ZipEntry AddDirectory(string directoryName, string directoryPathInArchive)
		{
			return AddOrUpdateDirectoryImpl(directoryName, directoryPathInArchive, AddOrUpdateAction.AddOnly);
		}

		public ZipEntry AddDirectoryByName(string directoryNameInArchive)
		{
			ZipEntry zipEntry = ZipEntry.Create(directoryNameInArchive, directoryNameInArchive);
			zipEntry.MarkAsDirectory();
			zipEntry._Source = ZipEntrySource.Stream;
			zipEntry._zipfile = this;
			InsureUniqueEntry(zipEntry);
			_entries.Add(zipEntry);
			AfterAddEntry(zipEntry);
			_contentsChanged = true;
			return zipEntry;
		}

		private ZipEntry AddOrUpdateDirectoryImpl(string directoryName, string rootDirectoryPathInArchive, AddOrUpdateAction action)
		{
			if (rootDirectoryPathInArchive == null)
			{
				rootDirectoryPathInArchive = "";
			}
			return AddOrUpdateDirectoryImpl(directoryName, rootDirectoryPathInArchive, action, 0);
		}

		private ZipEntry AddOrUpdateDirectoryImpl(string directoryName, string rootDirectoryPathInArchive, AddOrUpdateAction action, int level)
		{
			if (Verbose)
			{
				StatusMessageTextWriter.WriteLine("{0} {1}...", (action == AddOrUpdateAction.AddOnly) ? "adding" : "Adding or updating", directoryName);
			}
			if (level == 0)
			{
				OnAddStarted();
			}
			string text = rootDirectoryPathInArchive;
			ZipEntry zipEntry = null;
			if (level > 0)
			{
				int num = directoryName.Length;
				for (int num2 = level; num2 > 0; num2--)
				{
					num = directoryName.LastIndexOfAny("/\\".ToCharArray(), num - 1, num - 1);
				}
				text = directoryName.Substring(num + 1);
				text = Path.Combine(rootDirectoryPathInArchive, text);
			}
			if (level > 0 || rootDirectoryPathInArchive != "")
			{
				zipEntry = ZipEntry.Create(directoryName, text);
				zipEntry.ProvisionalAlternateEncoding = ProvisionalAlternateEncoding;
				zipEntry.MarkAsDirectory();
				zipEntry._zipfile = this;
				ZipEntry zipEntry2 = this[zipEntry.FileName];
				if (zipEntry2 == null)
				{
					_entries.Add(zipEntry);
					_contentsChanged = true;
				}
				text = zipEntry.FileName;
			}
			string[] files = Directory.GetFiles(directoryName);
			string[] array = files;
			foreach (string fileName in array)
			{
				if (action == AddOrUpdateAction.AddOnly)
				{
					AddFile(fileName, text);
				}
				else
				{
					UpdateFile(fileName, text);
				}
			}
			string[] directories = Directory.GetDirectories(directoryName);
			string[] array2 = directories;
			foreach (string directoryName2 in array2)
			{
				AddOrUpdateDirectoryImpl(directoryName2, rootDirectoryPathInArchive, action, level + 1);
			}
			if (level == 0)
			{
				OnAddCompleted();
			}
			return zipEntry;
		}

		public static bool CheckZip(string zipFileName)
		{
			ReadOnlyCollection<string> messages;
			return CheckZip(zipFileName, fixIfNecessary: false, out messages);
		}

		public static bool CheckZip(string zipFileName, bool fixIfNecessary, out ReadOnlyCollection<string> messages)
		{
			List<string> list = new List<string>();
			ZipFile zipFile = null;
			ZipFile zipFile2 = null;
			bool flag = true;
			try
			{
				zipFile = new ZipFile();
				zipFile.FullScan = true;
				zipFile.Initialize(zipFileName);
				zipFile2 = Read(zipFileName);
				foreach (ZipEntry item in zipFile)
				{
					foreach (ZipEntry item2 in zipFile2)
					{
						if (item.FileName == item2.FileName)
						{
							if (item._RelativeOffsetOfLocalHeader != item2._RelativeOffsetOfLocalHeader)
							{
								flag = false;
								list.Add($"{item.FileName}: mismatch in RelativeOffsetOfLocalHeader  (0x{item._RelativeOffsetOfLocalHeader:X16} != 0x{item2._RelativeOffsetOfLocalHeader:X16})");
							}
							if (item._CompressedSize != item2._CompressedSize)
							{
								flag = false;
								list.Add($"{item.FileName}: mismatch in CompressedSize  (0x{item._CompressedSize:X16} != 0x{item2._CompressedSize:X16})");
							}
							if (item._UncompressedSize != item2._UncompressedSize)
							{
								flag = false;
								list.Add($"{item.FileName}: mismatch in UncompressedSize  (0x{item._UncompressedSize:X16} != 0x{item2._UncompressedSize:X16})");
							}
							if (item.CompressionMethod != item2.CompressionMethod)
							{
								flag = false;
								list.Add($"{item.FileName}: mismatch in CompressionMethod  (0x{item.CompressionMethod:X4} != 0x{item2.CompressionMethod:X4})");
							}
							if (item.Crc != item2.Crc)
							{
								flag = false;
								list.Add($"{item.FileName}: mismatch in Crc32  (0x{item.Crc:X4} != 0x{item2.Crc:X4})");
							}
							break;
						}
					}
				}
				zipFile2.Dispose();
				zipFile2 = null;
				if (!flag && fixIfNecessary)
				{
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(zipFileName);
					fileNameWithoutExtension = $"{fileNameWithoutExtension}_fixed.zip";
					zipFile.Save(fileNameWithoutExtension);
				}
			}
			finally
			{
				zipFile?.Dispose();
				zipFile2?.Dispose();
			}
			messages = list.AsReadOnly();
			return flag;
		}

		public static void FixZipDirectory(string zipFileName)
		{
			using ZipFile zipFile = new ZipFile();
			zipFile.FullScan = true;
			zipFile.Initialize(zipFileName);
			zipFile.Save(zipFileName);
		}

		public override string ToString()
		{
			return $"ZipFile/{Name}";
		}

		internal void NotifyEntryChanged()
		{
			_contentsChanged = true;
		}

		internal void Reset()
		{
			if (!_JustSaved)
			{
				return;
			}
			ZipFile zipFile = new ZipFile();
			zipFile._name = _name;
			zipFile.ProvisionalAlternateEncoding = ProvisionalAlternateEncoding;
			ReadIntoInstance(zipFile);
			foreach (ZipEntry item in zipFile)
			{
				using IEnumerator<ZipEntry> enumerator2 = GetEnumerator();
				while (enumerator2.MoveNext())
				{
					ZipEntry current2 = enumerator2.Current;
					if (item.FileName == current2.FileName)
					{
						current2.CopyMetaData(item);
					}
				}
			}
			_JustSaved = false;
		}

		public ZipFile(string fileName)
		{
			try
			{
				_InitInstance(fileName, null);
			}
			catch (Exception innerException)
			{
				throw new ZipException($"{fileName} is not a valid zip file", innerException);
			}
		}

		public ZipFile(string fileName, Encoding encoding)
		{
			try
			{
				_InitInstance(fileName, null);
				ProvisionalAlternateEncoding = encoding;
			}
			catch (Exception innerException)
			{
				throw new ZipException($"{fileName} is not a valid zip file", innerException);
			}
		}

		public ZipFile()
		{
			_InitInstance(null, null);
		}

		public ZipFile(Encoding encoding)
		{
			_InitInstance(null, null);
			ProvisionalAlternateEncoding = encoding;
		}

		public ZipFile(string fileName, TextWriter statusMessageWriter)
		{
			try
			{
				_InitInstance(fileName, statusMessageWriter);
			}
			catch (Exception innerException)
			{
				throw new ZipException($"{fileName} is not a valid zip file", innerException);
			}
		}

		public ZipFile(string fileName, TextWriter statusMessageWriter, Encoding encoding)
		{
			try
			{
				_InitInstance(fileName, statusMessageWriter);
				ProvisionalAlternateEncoding = encoding;
			}
			catch (Exception innerException)
			{
				throw new ZipException($"{fileName} is not a valid zip file", innerException);
			}
		}

		public void Initialize(string fileName)
		{
			try
			{
				_InitInstance(fileName, null);
			}
			catch (Exception innerException)
			{
				throw new ZipException($"{fileName} is not a valid zip file", innerException);
			}
		}

		private void _InitInstance(string zipFileName, TextWriter statusMessageWriter)
		{
			_name = zipFileName;
			_StatusMessageTextWriter = statusMessageWriter;
			_contentsChanged = true;
			CompressionLevel = CompressionLevel.Default;
			_entries = new List<ZipEntry>();
			if (File.Exists(_name))
			{
				if (FullScan)
				{
					ReadIntoInstance_Orig(this);
				}
				else
				{
					ReadIntoInstance(this);
				}
				_fileAlreadyExists = true;
			}
		}

		public void RemoveEntry(ZipEntry entry)
		{
			if (!_entries.Contains(entry))
			{
				throw new ArgumentException("The entry you specified does not exist in the zip archive.");
			}
			_entries.Remove(entry);
			_contentsChanged = true;
		}

		public void RemoveEntry(string fileName)
		{
			string fileName2 = ZipEntry.NameInArchive(fileName, null);
			ZipEntry zipEntry = this[fileName2];
			if (zipEntry == null)
			{
				throw new ArgumentException("The entry you specified was not found in the zip archive.");
			}
			RemoveEntry(zipEntry);
		}

		~ZipFile()
		{
			Dispose(disposeManagedResources: false);
		}

		public void Dispose()
		{
			Dispose(disposeManagedResources: true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposeManagedResources)
		{
			if (_disposed)
			{
				return;
			}
			if (disposeManagedResources)
			{
				if (_ReadStreamIsOurs && _readstream != null)
				{
					_readstream.Dispose();
					_readstream = null;
				}
				if (_temporaryFileName != null && _name != null && _writestream != null)
				{
					_writestream.Dispose();
					_writestream = null;
				}
			}
			_disposed = true;
		}

		internal bool OnSaveBlock(ZipEntry entry, long bytesXferred, long totalBytesToXfer)
		{
			if (this.SaveProgress != null)
			{
				lock (LOCK)
				{
					SaveProgressEventArgs saveProgressEventArgs = SaveProgressEventArgs.ByteUpdate(ArchiveNameForEvent, entry, bytesXferred, totalBytesToXfer);
					this.SaveProgress(this, saveProgressEventArgs);
					if (saveProgressEventArgs.Cancel)
					{
						_saveOperationCanceled = true;
					}
				}
			}
			return _saveOperationCanceled;
		}

		private void OnSaveEntry(int current, ZipEntry entry, bool before)
		{
			if (this.SaveProgress == null)
			{
				return;
			}
			lock (LOCK)
			{
				SaveProgressEventArgs saveProgressEventArgs = new SaveProgressEventArgs(ArchiveNameForEvent, before, _entries.Count, current, entry);
				this.SaveProgress(this, saveProgressEventArgs);
				if (saveProgressEventArgs.Cancel)
				{
					_saveOperationCanceled = true;
				}
			}
		}

		private void OnSaveEvent(ZipProgressEventType eventFlavor)
		{
			if (this.SaveProgress == null)
			{
				return;
			}
			lock (LOCK)
			{
				SaveProgressEventArgs saveProgressEventArgs = new SaveProgressEventArgs(ArchiveNameForEvent, eventFlavor);
				this.SaveProgress(this, saveProgressEventArgs);
				if (saveProgressEventArgs.Cancel)
				{
					_saveOperationCanceled = true;
				}
			}
		}

		private void OnSaveStarted()
		{
			if (this.SaveProgress != null)
			{
				lock (LOCK)
				{
					SaveProgressEventArgs e = SaveProgressEventArgs.Started(ArchiveNameForEvent);
					this.SaveProgress(this, e);
				}
			}
		}

		private void OnSaveCompleted()
		{
			if (this.SaveProgress != null)
			{
				lock (LOCK)
				{
					SaveProgressEventArgs e = SaveProgressEventArgs.Completed(ArchiveNameForEvent);
					this.SaveProgress(this, e);
				}
			}
		}

		private void OnReadStarted()
		{
			if (this.ReadProgress != null)
			{
				lock (LOCK)
				{
					ReadProgressEventArgs e = ReadProgressEventArgs.Started(ArchiveNameForEvent);
					this.ReadProgress(this, e);
				}
			}
		}

		private void OnReadCompleted()
		{
			if (this.ReadProgress != null)
			{
				lock (LOCK)
				{
					ReadProgressEventArgs e = ReadProgressEventArgs.Completed(ArchiveNameForEvent);
					this.ReadProgress(this, e);
				}
			}
		}

		internal void OnReadBytes(ZipEntry entry)
		{
			if (this.ReadProgress != null)
			{
				lock (LOCK)
				{
					ReadProgressEventArgs e = ReadProgressEventArgs.ByteUpdate(ArchiveNameForEvent, entry, ReadStream.Position, LengthOfReadStream);
					this.ReadProgress(this, e);
				}
			}
		}

		internal void OnReadEntry(bool before, ZipEntry entry)
		{
			if (this.ReadProgress != null)
			{
				lock (LOCK)
				{
					ReadProgressEventArgs e = before ? ReadProgressEventArgs.Before(ArchiveNameForEvent, _entries.Count) : ReadProgressEventArgs.After(ArchiveNameForEvent, entry, _entries.Count);
					this.ReadProgress(this, e);
				}
			}
		}

		private void OnExtractEntry(int current, bool before, ZipEntry currentEntry, string path)
		{
			if (this.ExtractProgress == null)
			{
				return;
			}
			lock (LOCK)
			{
				ExtractProgressEventArgs extractProgressEventArgs = new ExtractProgressEventArgs(ArchiveNameForEvent, before, _entries.Count, current, currentEntry, path);
				this.ExtractProgress(this, extractProgressEventArgs);
				if (extractProgressEventArgs.Cancel)
				{
					_extractOperationCanceled = true;
				}
			}
		}

		internal bool OnExtractBlock(ZipEntry entry, long bytesWritten, long totalBytesToWrite)
		{
			if (this.ExtractProgress != null)
			{
				lock (LOCK)
				{
					ExtractProgressEventArgs extractProgressEventArgs = ExtractProgressEventArgs.ByteUpdate(ArchiveNameForEvent, entry, bytesWritten, totalBytesToWrite);
					this.ExtractProgress(this, extractProgressEventArgs);
					if (extractProgressEventArgs.Cancel)
					{
						_extractOperationCanceled = true;
					}
				}
			}
			return _extractOperationCanceled;
		}

		internal bool OnSingleEntryExtract(ZipEntry entry, string path, bool before)
		{
			if (this.ExtractProgress != null)
			{
				lock (LOCK)
				{
					ExtractProgressEventArgs extractProgressEventArgs = before ? ExtractProgressEventArgs.BeforeExtractEntry(ArchiveNameForEvent, entry, path) : ExtractProgressEventArgs.AfterExtractEntry(ArchiveNameForEvent, entry, path);
					this.ExtractProgress(this, extractProgressEventArgs);
					if (extractProgressEventArgs.Cancel)
					{
						_extractOperationCanceled = true;
					}
				}
			}
			return _extractOperationCanceled;
		}

		internal bool OnExtractExisting(ZipEntry entry, string path)
		{
			if (this.ExtractProgress != null)
			{
				lock (LOCK)
				{
					ExtractProgressEventArgs extractProgressEventArgs = ExtractProgressEventArgs.ExtractExisting(ArchiveNameForEvent, entry, path);
					this.ExtractProgress(this, extractProgressEventArgs);
					if (extractProgressEventArgs.Cancel)
					{
						_extractOperationCanceled = true;
					}
				}
			}
			return _extractOperationCanceled;
		}

		private void OnExtractAllCompleted(string path)
		{
			if (this.ExtractProgress != null)
			{
				lock (LOCK)
				{
					ExtractProgressEventArgs e = ExtractProgressEventArgs.ExtractAllCompleted(ArchiveNameForEvent, path);
					this.ExtractProgress(this, e);
				}
			}
		}

		private void OnExtractAllStarted(string path)
		{
			if (this.ExtractProgress != null)
			{
				lock (LOCK)
				{
					ExtractProgressEventArgs e = ExtractProgressEventArgs.ExtractAllStarted(ArchiveNameForEvent, path);
					this.ExtractProgress(this, e);
				}
			}
		}

		private void OnAddStarted()
		{
			if (this.AddProgress != null)
			{
				lock (LOCK)
				{
					AddProgressEventArgs e = AddProgressEventArgs.Started(ArchiveNameForEvent);
					this.AddProgress(this, e);
				}
			}
		}

		private void OnAddCompleted()
		{
			if (this.AddProgress != null)
			{
				lock (LOCK)
				{
					AddProgressEventArgs e = AddProgressEventArgs.Completed(ArchiveNameForEvent);
					this.AddProgress(this, e);
				}
			}
		}

		internal void AfterAddEntry(ZipEntry entry)
		{
			if (this.AddProgress != null)
			{
				lock (LOCK)
				{
					AddProgressEventArgs e = AddProgressEventArgs.AfterEntry(ArchiveNameForEvent, entry, _entries.Count);
					this.AddProgress(this, e);
				}
			}
		}

		internal bool OnZipErrorSaving(ZipEntry entry, Exception exc)
		{
			if (this.ZipError != null)
			{
				lock (LOCK)
				{
					ZipErrorEventArgs zipErrorEventArgs = ZipErrorEventArgs.Saving(Name, entry, exc);
					this.ZipError(this, zipErrorEventArgs);
					if (zipErrorEventArgs.Cancel)
					{
						_saveOperationCanceled = true;
					}
				}
			}
			return _saveOperationCanceled;
		}

		public void ExtractAll(string path)
		{
			_InternalExtractAll(path, overrideExtractExistingProperty: true);
		}

		[Obsolete("Please use property ExtractExistingFile to specify overwrite behavior)")]
		public void ExtractAll(string path, bool wantOverwrite)
		{
			ExtractExistingFile = (wantOverwrite ? ExtractExistingFileAction.OverwriteSilently : ExtractExistingFileAction.Throw);
			_InternalExtractAll(path, overrideExtractExistingProperty: true);
		}

		public void ExtractAll(string path, ExtractExistingFileAction extractExistingFile)
		{
			ExtractExistingFile = extractExistingFile;
			_InternalExtractAll(path, overrideExtractExistingProperty: true);
		}

		private void _InternalExtractAll(string path, bool overrideExtractExistingProperty)
		{
			bool flag = Verbose;
			_inExtractAll = true;
			try
			{
				OnExtractAllStarted(path);
				int num = 0;
				foreach (ZipEntry entry in _entries)
				{
					if (flag)
					{
						StatusMessageTextWriter.WriteLine("\n{1,-22} {2,-8} {3,4}   {4,-8}  {0}", "Name", "Modified", "Size", "Ratio", "Packed");
						StatusMessageTextWriter.WriteLine(new string('-', 72));
						flag = false;
					}
					if (Verbose)
					{
						StatusMessageTextWriter.WriteLine("{1,-22} {2,-8} {3,4:F0}%   {4,-8} {0}", entry.FileName, entry.LastModified.ToString("yyyy-MM-dd HH:mm:ss"), entry.UncompressedSize, entry.CompressionRatio, entry.CompressedSize);
						if (!string.IsNullOrEmpty(entry.Comment))
						{
							StatusMessageTextWriter.WriteLine("  Comment: {0}", entry.Comment);
						}
					}
					entry.Password = _Password;
					OnExtractEntry(num, before: true, entry, path);
					if (overrideExtractExistingProperty)
					{
						entry.ExtractExistingFile = ExtractExistingFile;
					}
					entry.Extract(path);
					num++;
					OnExtractEntry(num, before: false, entry, path);
					if (_extractOperationCanceled)
					{
						break;
					}
				}
				foreach (ZipEntry entry2 in _entries)
				{
					if (entry2.IsDirectory || entry2.FileName.EndsWith("/"))
					{
						string fileOrDirectory = entry2.FileName.StartsWith("/") ? Path.Combine(path, entry2.FileName.Substring(1)) : Path.Combine(path, entry2.FileName);
						entry2._SetTimes(fileOrDirectory, isFile: false);
					}
				}
				OnExtractAllCompleted(path);
			}
			finally
			{
				_inExtractAll = false;
			}
		}

		[Obsolete("Please use method ZipEntry.Extract()")]
		public void Extract(string fileName)
		{
			ZipEntry zipEntry = this[fileName];
			if (ExtractExistingFile != 0)
			{
				zipEntry.ExtractExistingFile = ExtractExistingFile;
			}
			zipEntry.Password = _Password;
			zipEntry.Extract();
		}

		[Obsolete("Please use method ZipEntry.Extract(string)")]
		public void Extract(string entryName, string directoryName)
		{
			ZipEntry zipEntry = this[entryName];
			if (ExtractExistingFile != 0)
			{
				zipEntry.ExtractExistingFile = ExtractExistingFile;
			}
			zipEntry.Password = _Password;
			zipEntry.Extract(directoryName);
		}

		[Obsolete("Please use method ZipEntry.Extract(ExtractExistingFileAction)")]
		public void Extract(string entryName, bool wantOverwrite)
		{
			ZipEntry zipEntry = this[entryName];
			zipEntry.ExtractExistingFile = (wantOverwrite ? ExtractExistingFileAction.OverwriteSilently : ExtractExistingFileAction.Throw);
			zipEntry.Password = _Password;
			zipEntry.Extract(Directory.GetCurrentDirectory());
		}

		[Obsolete("Please use method ZipEntry.Extract(ExtractExistingFileAction)")]
		public void Extract(string entryName, ExtractExistingFileAction extractExistingFile)
		{
			ZipEntry zipEntry = this[entryName];
			zipEntry.ExtractExistingFile = extractExistingFile;
			zipEntry.Password = _Password;
			zipEntry.Extract(Directory.GetCurrentDirectory());
		}

		[Obsolete("Please use method ZipEntry.Extract(String,ExtractExistingFileAction)")]
		public void Extract(string entryName, string directoryName, bool wantOverwrite)
		{
			ZipEntry zipEntry = this[entryName];
			zipEntry.Password = _Password;
			zipEntry.ExtractExistingFile = (wantOverwrite ? ExtractExistingFileAction.OverwriteSilently : ExtractExistingFileAction.Throw);
			zipEntry.Extract(directoryName);
		}

		[Obsolete("Please use method ZipEntry.Extract(string, ExtractExistingFileAction)")]
		public void Extract(string entryName, string directoryName, ExtractExistingFileAction extractExistingFile)
		{
			ZipEntry zipEntry = this[entryName];
			zipEntry.ExtractExistingFile = extractExistingFile;
			zipEntry.Password = _Password;
			zipEntry.Extract(directoryName);
		}

		[Obsolete("Please use method ZipEntry.Extract(Stream)")]
		public void Extract(string entryName, Stream outputStream)
		{
			if (outputStream == null || !outputStream.CanWrite)
			{
				throw new ZipException("Cannot extract.", new ArgumentException("The OutputStream must be a writable stream.", "outputStream"));
			}
			if (string.IsNullOrEmpty(entryName))
			{
				throw new ZipException("Cannot extract.", new ArgumentException("The file name must be neither null nor empty.", "entryName"));
			}
			ZipEntry zipEntry = this[entryName];
			zipEntry.Password = _Password;
			zipEntry.Extract(outputStream);
		}

		public static ZipFile Read(string fileName)
		{
			return Read(fileName, null, DefaultEncoding);
		}

		public static ZipFile Read(string fileName, EventHandler<ReadProgressEventArgs> readProgress)
		{
			return Read(fileName, null, DefaultEncoding, readProgress);
		}

		public static ZipFile Read(string fileName, TextWriter statusMessageWriter)
		{
			return Read(fileName, statusMessageWriter, DefaultEncoding);
		}

		public static ZipFile Read(string fileName, TextWriter statusMessageWriter, EventHandler<ReadProgressEventArgs> readProgress)
		{
			return Read(fileName, statusMessageWriter, DefaultEncoding, readProgress);
		}

		public static ZipFile Read(string fileName, Encoding encoding)
		{
			return Read(fileName, null, encoding);
		}

		public static ZipFile Read(string fileName, Encoding encoding, EventHandler<ReadProgressEventArgs> readProgress)
		{
			return Read(fileName, null, encoding, readProgress);
		}

		public static ZipFile Read(string fileName, TextWriter statusMessageWriter, Encoding encoding)
		{
			return Read(fileName, statusMessageWriter, encoding, null);
		}

		public static ZipFile Read(string fileName, TextWriter statusMessageWriter, Encoding encoding, EventHandler<ReadProgressEventArgs> readProgress)
		{
			ZipFile zipFile = new ZipFile();
			zipFile.ProvisionalAlternateEncoding = encoding;
			zipFile._StatusMessageTextWriter = statusMessageWriter;
			zipFile._name = fileName;
			if (readProgress != null)
			{
				zipFile.ReadProgress = readProgress;
			}
			if (zipFile.Verbose)
			{
				zipFile._StatusMessageTextWriter.WriteLine("reading from {0}...", fileName);
			}
			try
			{
				ReadIntoInstance(zipFile);
				zipFile._fileAlreadyExists = true;
			}
			catch (Exception innerException)
			{
				throw new ZipException($"{fileName} could not be read", innerException);
			}
			return zipFile;
		}

		public static ZipFile Read(Stream zipStream)
		{
			return Read(zipStream, null, DefaultEncoding);
		}

		public static ZipFile Read(Stream zipStream, EventHandler<ReadProgressEventArgs> readProgress)
		{
			return Read(zipStream, null, DefaultEncoding, readProgress);
		}

		public static ZipFile Read(Stream zipStream, TextWriter statusMessageWriter)
		{
			return Read(zipStream, statusMessageWriter, DefaultEncoding);
		}

		public static ZipFile Read(Stream zipStream, TextWriter statusMessageWriter, EventHandler<ReadProgressEventArgs> readProgress)
		{
			return Read(zipStream, statusMessageWriter, DefaultEncoding, readProgress);
		}

		public static ZipFile Read(Stream zipStream, Encoding encoding)
		{
			return Read(zipStream, null, encoding);
		}

		public static ZipFile Read(Stream zipStream, Encoding encoding, EventHandler<ReadProgressEventArgs> readProgress)
		{
			return Read(zipStream, null, encoding, readProgress);
		}

		public static ZipFile Read(Stream zipStream, TextWriter statusMessageWriter, Encoding encoding)
		{
			return Read(zipStream, statusMessageWriter, encoding, null);
		}

		public static ZipFile Read(Stream zipStream, TextWriter statusMessageWriter, Encoding encoding, EventHandler<ReadProgressEventArgs> readProgress)
		{
			if (zipStream == null)
			{
				throw new ZipException("Cannot read.", new ArgumentException("The stream must be non-null", "zipStream"));
			}
			ZipFile zipFile = new ZipFile();
			zipFile._provisionalAlternateEncoding = encoding;
			if (readProgress != null)
			{
				zipFile.ReadProgress += readProgress;
			}
			zipFile._StatusMessageTextWriter = statusMessageWriter;
			zipFile._readstream = zipStream;
			zipFile._ReadStreamIsOurs = false;
			if (zipFile.Verbose)
			{
				zipFile._StatusMessageTextWriter.WriteLine("reading from stream...");
			}
			ReadIntoInstance(zipFile);
			return zipFile;
		}

		public static ZipFile Read(byte[] buffer)
		{
			return Read(buffer, null, DefaultEncoding);
		}

		public static ZipFile Read(byte[] buffer, TextWriter statusMessageWriter)
		{
			return Read(buffer, statusMessageWriter, DefaultEncoding);
		}

		public static ZipFile Read(byte[] buffer, TextWriter statusMessageWriter, Encoding encoding)
		{
			ZipFile zipFile = new ZipFile();
			zipFile._StatusMessageTextWriter = statusMessageWriter;
			zipFile._provisionalAlternateEncoding = encoding;
			zipFile._readstream = new MemoryStream(buffer);
			zipFile._ReadStreamIsOurs = true;
			if (zipFile.Verbose)
			{
				zipFile._StatusMessageTextWriter.WriteLine("reading from byte[]...");
			}
			ReadIntoInstance(zipFile);
			return zipFile;
		}

		private static void ReadIntoInstance(ZipFile zf)
		{
			Stream readStream = zf.ReadStream;
			try
			{
				if (!readStream.CanSeek)
				{
					ReadIntoInstance_Orig(zf);
					return;
				}
				zf.OnReadStarted();
				zf._originPosition = readStream.Position;
				uint num = VerifyBeginningOfZipFile(readStream);
				if (num == 101010256)
				{
					return;
				}
				int num2 = 0;
				bool flag = false;
				long num3 = readStream.Length - 64;
				long num4 = Math.Max(readStream.Length - 16384, 10L);
				do
				{
					readStream.Seek(num3, SeekOrigin.Begin);
					long num5 = SharedUtilities.FindSignature(readStream, 101010256);
					if (num5 != -1)
					{
						flag = true;
						continue;
					}
					num2++;
					num3 -= 32 * (num2 + 1) * num2;
					if (num3 < 0)
					{
						num3 = 0L;
					}
				}
				while (!flag && num3 > num4);
				if (flag)
				{
					zf._locEndOfCDS = readStream.Position - 4;
					byte[] array = new byte[16];
					zf.ReadStream.Read(array, 0, array.Length);
					int num6 = 12;
					uint num7 = (uint)(array[num6++] + array[num6++] * 256 + array[num6++] * 256 * 256 + array[num6++] * 256 * 256 * 256);
					if (num7 == uint.MaxValue)
					{
						Zip64SeekToCentralDirectory(zf);
					}
					else
					{
						zf.SeekFromOrigin(num7);
					}
					ReadCentralDirectory(zf);
				}
				else
				{
					readStream.Seek(zf._originPosition, SeekOrigin.Begin);
					ReadIntoInstance_Orig(zf);
				}
			}
			catch
			{
				if (zf._ReadStreamIsOurs && zf._readstream != null)
				{
					try
					{
						zf._readstream.Close();
						zf._readstream.Dispose();
						zf._readstream = null;
					}
					finally
					{
					}
				}
				throw;
			}
			zf._contentsChanged = false;
		}

		private static void Zip64SeekToCentralDirectory(ZipFile zf)
		{
			Stream readStream = zf.ReadStream;
			byte[] array = new byte[16];
			readStream.Seek(-40L, SeekOrigin.Current);
			readStream.Read(array, 0, 16);
			long position = BitConverter.ToInt64(array, 8);
			zf.SeekFromOrigin(position);
			uint num = (uint)SharedUtilities.ReadInt(readStream);
			if (num != 101075792)
			{
				throw new BadReadException($"  ZipFile::Read(): Bad signature (0x{num:X8}) looking for ZIP64 EoCD Record at position 0x{readStream.Position:X8}");
			}
			readStream.Read(array, 0, 8);
			long num2 = BitConverter.ToInt64(array, 0);
			array = new byte[num2];
			readStream.Read(array, 0, array.Length);
			position = BitConverter.ToInt64(array, 36);
			zf.SeekFromOrigin(position);
		}

		private static uint VerifyBeginningOfZipFile(Stream s)
		{
			return (uint)SharedUtilities.ReadInt(s);
		}

		private static void ReadCentralDirectory(ZipFile zf)
		{
			ZipEntry zipEntry;
			while ((zipEntry = ZipEntry.ReadDirEntry(zf)) != null)
			{
				zipEntry.ResetDirEntry();
				zf.OnReadEntry(before: true, null);
				if (zf.Verbose)
				{
					zf.StatusMessageTextWriter.WriteLine("entry {0}", zipEntry.FileName);
				}
				zf._entries.Add(zipEntry);
			}
			if (zf._locEndOfCDS > 0)
			{
				zf.SeekFromOrigin(zf._locEndOfCDS);
			}
			ReadCentralDirectoryFooter(zf);
			if (zf.Verbose && !string.IsNullOrEmpty(zf.Comment))
			{
				zf.StatusMessageTextWriter.WriteLine("Zip file Comment: {0}", zf.Comment);
			}
			if (zf.Verbose)
			{
				zf.StatusMessageTextWriter.WriteLine("read in {0} entries.", zf._entries.Count);
			}
			zf.OnReadCompleted();
		}

		private static void ReadIntoInstance_Orig(ZipFile zf)
		{
			zf.OnReadStarted();
			zf._entries = new List<ZipEntry>();
			if (zf.Verbose)
			{
				if (zf.Name == null)
				{
					zf.StatusMessageTextWriter.WriteLine("Reading zip from stream...");
				}
				else
				{
					zf.StatusMessageTextWriter.WriteLine("Reading zip {0}...", zf.Name);
				}
			}
			bool first = true;
			ZipEntry zipEntry;
			while ((zipEntry = ZipEntry.Read(zf, first)) != null)
			{
				if (zf.Verbose)
				{
					zf.StatusMessageTextWriter.WriteLine("  {0}", zipEntry.FileName);
				}
				zf._entries.Add(zipEntry);
				first = false;
			}
			ZipEntry zipEntry2;
			while ((zipEntry2 = ZipEntry.ReadDirEntry(zf)) != null)
			{
				foreach (ZipEntry entry in zf._entries)
				{
					if (entry.FileName == zipEntry2.FileName)
					{
						entry._Comment = zipEntry2.Comment;
						if (zipEntry2.AttributesIndicateDirectory)
						{
							entry.MarkAsDirectory();
						}
						break;
					}
				}
			}
			if (zf._locEndOfCDS > 0)
			{
				zf.SeekFromOrigin(zf._locEndOfCDS);
			}
			ReadCentralDirectoryFooter(zf);
			if (zf.Verbose && !string.IsNullOrEmpty(zf.Comment))
			{
				zf.StatusMessageTextWriter.WriteLine("Zip file Comment: {0}", zf.Comment);
			}
			zf.OnReadCompleted();
		}

		private static void ReadCentralDirectoryFooter(ZipFile zf)
		{
			Stream readStream = zf.ReadStream;
			int num = SharedUtilities.ReadSignature(readStream);
			byte[] array = null;
			if ((long)num == 101075792)
			{
				array = new byte[52];
				readStream.Read(array, 0, array.Length);
				long num2 = BitConverter.ToInt64(array, 0);
				if (num2 < 44)
				{
					throw new ZipException("Bad DataSize in the ZIP64 Central Directory.");
				}
				array = new byte[num2 - 44];
				readStream.Read(array, 0, array.Length);
				num = SharedUtilities.ReadSignature(readStream);
				if ((long)num != 117853008)
				{
					throw new ZipException("Inconsistent metadata in the ZIP64 Central Directory.");
				}
				array = new byte[16];
				readStream.Read(array, 0, array.Length);
				num = SharedUtilities.ReadSignature(readStream);
			}
			if ((long)num != 101010256)
			{
				readStream.Seek(-4L, SeekOrigin.Current);
				throw new BadReadException($"  ZipFile::Read(): Bad signature ({num:X8}) at position 0x{readStream.Position:X8}");
			}
			array = new byte[16];
			zf.ReadStream.Read(array, 0, array.Length);
			ReadZipFileComment(zf);
		}

		private static void ReadZipFileComment(ZipFile zf)
		{
			byte[] array = new byte[2];
			zf.ReadStream.Read(array, 0, array.Length);
			short num = (short)(array[0] + array[1] * 256);
			if (num > 0)
			{
				array = new byte[num];
				zf.ReadStream.Read(array, 0, array.Length);
				string @string = DefaultEncoding.GetString(array, 0, array.Length);
				byte[] bytes = DefaultEncoding.GetBytes(@string);
				if (BlocksAreEqual(array, bytes))
				{
					zf.Comment = @string;
					return;
				}
				Encoding encoding = (zf._provisionalAlternateEncoding.CodePage == 437) ? Encoding.UTF8 : zf._provisionalAlternateEncoding;
				zf.Comment = encoding.GetString(array, 0, array.Length);
			}
		}

		private static bool BlocksAreEqual(byte[] a, byte[] b)
		{
			if (a.Length != b.Length)
			{
				return false;
			}
			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] != b[i])
				{
					return false;
				}
			}
			return true;
		}

		internal void SeekFromOrigin(long position)
		{
			ReadStream.Seek(position + _originPosition, SeekOrigin.Begin);
		}

		public static bool IsZipFile(string fileName)
		{
			return IsZipFile(fileName, testExtract: false);
		}

		public static bool IsZipFile(string fileName, bool testExtract)
		{
			bool result = false;
			try
			{
				if (!File.Exists(fileName))
				{
					return false;
				}
				using FileStream stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				result = IsZipFile(stream, testExtract);
			}
			catch
			{
			}
			return result;
		}

		public static bool IsZipFile(Stream stream, bool testExtract)
		{
			bool result = false;
			try
			{
				if (!stream.CanRead)
				{
					return false;
				}
				Stream @null = Stream.Null;
				using (ZipFile zipFile = Read(stream, null, Encoding.GetEncoding("IBM437")))
				{
					if (testExtract)
					{
						foreach (ZipEntry item in zipFile)
						{
							if (!item.IsDirectory)
							{
								item.Extract(@null);
							}
						}
					}
				}
				result = true;
			}
			catch
			{
			}
			return result;
		}

		public void Save()
		{
			try
			{
				bool flag = false;
				_saveOperationCanceled = false;
				OnSaveStarted();
				if (WriteStream == null)
				{
					throw new BadStateException("You haven't specified where to save the zip.");
				}
				if (_name != null && _name.EndsWith(".exe") && !_SavingSfx)
				{
					throw new BadStateException("You specified an EXE for a plain zip file.");
				}
				if (!_contentsChanged)
				{
					return;
				}
				if (Verbose)
				{
					StatusMessageTextWriter.WriteLine("saving....");
				}
				if (_entries.Count >= 65535 && _zip64 == Zip64Option.Default)
				{
					throw new ZipException("The number of entries is 65535 or greater. Consider setting the UseZip64WhenSaving property on the ZipFile instance.");
				}
				int num = 0;
				foreach (ZipEntry entry in _entries)
				{
					OnSaveEntry(num, entry, before: true);
					entry.Write(WriteStream);
					if (_saveOperationCanceled)
					{
						break;
					}
					entry._zipfile = this;
					num++;
					OnSaveEntry(num, entry, before: false);
					if (_saveOperationCanceled)
					{
						break;
					}
					if (entry.IncludedInMostRecentSave)
					{
						flag |= entry.OutputUsedZip64.Value;
					}
				}
				if (_saveOperationCanceled)
				{
					return;
				}
				WriteCentralDirectoryStructure(WriteStream);
				OnSaveEvent(ZipProgressEventType.Saving_AfterSaveTempArchive);
				_hasBeenSaved = true;
				_contentsChanged = false;
				flag |= _NeedZip64CentralDirectory;
				_OutputUsesZip64 = flag;
				if (_temporaryFileName == null || _name == null)
				{
					goto IL_028c;
				}
				WriteStream.Close();
				WriteStream.Dispose();
				WriteStream = null;
				if (_saveOperationCanceled)
				{
					return;
				}
				if (_fileAlreadyExists && _readstream != null)
				{
					_readstream.Close();
					_readstream = null;
				}
				if (_fileAlreadyExists)
				{
					File.Delete(_name);
					OnSaveEvent(ZipProgressEventType.Saving_BeforeRenameTempArchive);
					File.Move(_temporaryFileName, _name);
					OnSaveEvent(ZipProgressEventType.Saving_AfterRenameTempArchive);
				}
				else
				{
					File.Move(_temporaryFileName, _name);
				}
				_fileAlreadyExists = true;
				goto IL_028c;
				IL_028c:
				OnSaveCompleted();
				_JustSaved = true;
			}
			finally
			{
				CleanupAfterSaveOperation();
			}
		}

		private void RemoveTempFile()
		{
			try
			{
				if (File.Exists(_temporaryFileName))
				{
					File.Delete(_temporaryFileName);
				}
			}
			catch (Exception ex)
			{
				if (Verbose)
				{
					StatusMessageTextWriter.WriteLine("ZipFile::Save: could not delete temp file: {0}.", ex.Message);
				}
			}
		}

		private void CleanupAfterSaveOperation()
		{
			if (_temporaryFileName == null || _name == null)
			{
				return;
			}
			if (_writestream != null)
			{
				try
				{
					_writestream.Close();
				}
				catch
				{
				}
				try
				{
					_writestream.Dispose();
				}
				catch
				{
				}
			}
			_writestream = null;
			RemoveTempFile();
			_temporaryFileName = null;
		}

		public void Save(string fileName)
		{
			if (_name == null)
			{
				_writestream = null;
			}
			_name = fileName;
			if (Directory.Exists(_name))
			{
				throw new ZipException("Bad Directory", new ArgumentException("That name specifies an existing directory. Please specify a filename.", "fileName"));
			}
			_contentsChanged = true;
			_fileAlreadyExists = File.Exists(_name);
			Save();
		}

		public void Save(Stream outputStream)
		{
			if (!outputStream.CanWrite)
			{
				throw new ArgumentException("The outputStream must be a writable stream.");
			}
			_name = null;
			_writestream = new CountingStream(outputStream);
			_contentsChanged = true;
			_fileAlreadyExists = false;
			Save();
		}

		private void WriteCentralDirectoryStructure(Stream s)
		{
			CountingStream countingStream = s as CountingStream;
			long num = countingStream?.BytesWritten ?? s.Position;
			foreach (ZipEntry entry in _entries)
			{
				if (entry.IncludedInMostRecentSave)
				{
					entry.WriteCentralDirectoryEntry(s);
				}
			}
			long num2 = countingStream?.BytesWritten ?? s.Position;
			long num3 = num2 - num;
			_NeedZip64CentralDirectory = (_zip64 == Zip64Option.Always || CountEntries() >= 65535 || num3 > uint.MaxValue || num > uint.MaxValue);
			if (_NeedZip64CentralDirectory)
			{
				if (_zip64 == Zip64Option.Default)
				{
					throw new ZipException("The archive requires a ZIP64 Central Directory. Consider setting the UseZip64WhenSaving property.");
				}
				WriteZip64EndOfCentralDirectory(s, num, num2);
			}
			WriteCentralDirectoryFooter(s, num, num2);
		}

		private int CountEntries()
		{
			int num = 0;
			foreach (ZipEntry entry in _entries)
			{
				if (entry.IncludedInMostRecentSave)
				{
					num++;
				}
			}
			return num;
		}

		private void WriteZip64EndOfCentralDirectory(Stream s, long StartOfCentralDirectory, long EndOfCentralDirectory)
		{
			byte[] array = new byte[76];
			int num = 0;
			byte[] bytes = BitConverter.GetBytes(101075792u);
			Array.Copy(bytes, 0, array, num, 4);
			num += 4;
			long value = 44L;
			Array.Copy(BitConverter.GetBytes(value), 0, array, num, 8);
			num += 8;
			array[num++] = 45;
			array[num++] = 0;
			array[num++] = 45;
			array[num++] = 0;
			for (int i = 0; i < 8; i++)
			{
				array[num++] = 0;
			}
			long value2 = CountEntries();
			Array.Copy(BitConverter.GetBytes(value2), 0, array, num, 8);
			num += 8;
			Array.Copy(BitConverter.GetBytes(value2), 0, array, num, 8);
			num += 8;
			long value3 = EndOfCentralDirectory - StartOfCentralDirectory;
			Array.Copy(BitConverter.GetBytes(value3), 0, array, num, 8);
			num += 8;
			Array.Copy(BitConverter.GetBytes(StartOfCentralDirectory), 0, array, num, 8);
			num += 8;
			bytes = BitConverter.GetBytes(117853008u);
			Array.Copy(bytes, 0, array, num, 4);
			num += 4;
			array[num++] = 0;
			array[num++] = 0;
			array[num++] = 0;
			array[num++] = 0;
			Array.Copy(BitConverter.GetBytes(EndOfCentralDirectory), 0, array, num, 8);
			num += 8;
			array[num++] = 1;
			array[num++] = 0;
			array[num++] = 0;
			array[num++] = 0;
			s.Write(array, 0, num);
		}

		private void WriteCentralDirectoryFooter(Stream s, long StartOfCentralDirectory, long EndOfCentralDirectory)
		{
			int num = 0;
			int num2 = 24;
			byte[] array = null;
			short num3 = 0;
			if (Comment != null && Comment.Length != 0)
			{
				array = ProvisionalAlternateEncoding.GetBytes(Comment);
				num3 = (short)array.Length;
			}
			num2 += num3;
			byte[] array2 = new byte[num2];
			int num4 = 0;
			byte[] bytes = BitConverter.GetBytes(101010256u);
			Array.Copy(bytes, 0, array2, num4, 4);
			num4 += 4;
			array2[num4++] = 0;
			array2[num4++] = 0;
			array2[num4++] = 0;
			array2[num4++] = 0;
			if (CountEntries() >= 65535 || _zip64 == Zip64Option.Always)
			{
				for (num = 0; num < 4; num++)
				{
					array2[num4++] = byte.MaxValue;
				}
			}
			else
			{
				int num5 = CountEntries();
				array2[num4++] = (byte)(num5 & 0xFF);
				array2[num4++] = (byte)((num5 & 0xFF00) >> 8);
				array2[num4++] = (byte)(num5 & 0xFF);
				array2[num4++] = (byte)((num5 & 0xFF00) >> 8);
			}
			long num6 = EndOfCentralDirectory - StartOfCentralDirectory;
			if (num6 >= uint.MaxValue || StartOfCentralDirectory >= uint.MaxValue)
			{
				for (num = 0; num < 8; num++)
				{
					array2[num4++] = byte.MaxValue;
				}
			}
			else
			{
				array2[num4++] = (byte)(num6 & 0xFF);
				array2[num4++] = (byte)((num6 & 0xFF00) >> 8);
				array2[num4++] = (byte)((num6 & 0xFF0000) >> 16);
				array2[num4++] = (byte)((num6 & 4278190080u) >> 24);
				array2[num4++] = (byte)(StartOfCentralDirectory & 0xFF);
				array2[num4++] = (byte)((StartOfCentralDirectory & 0xFF00) >> 8);
				array2[num4++] = (byte)((StartOfCentralDirectory & 0xFF0000) >> 16);
				array2[num4++] = (byte)((StartOfCentralDirectory & 4278190080u) >> 24);
			}
			if (Comment == null || Comment.Length == 0)
			{
				array2[num4++] = 0;
				array2[num4++] = 0;
			}
			else
			{
				if (num3 + num4 + 2 > array2.Length)
				{
					num3 = (short)(array2.Length - num4 - 2);
				}
				array2[num4++] = (byte)(num3 & 0xFF);
				array2[num4++] = (byte)((num3 & 0xFF00) >> 8);
				if (num3 != 0)
				{
					for (num = 0; num < num3 && num4 + num < array2.Length; num++)
					{
						array2[num4 + num] = array[num];
					}
					num4 += num;
				}
			}
			s.Write(array2, 0, num4);
		}

		public void SaveSelfExtractor(string exeToGenerate, SelfExtractorFlavor flavor)
		{
			SaveSelfExtractor(exeToGenerate, flavor, null, null, null);
		}

		public void SaveSelfExtractor(string exeToGenerate, SelfExtractorFlavor flavor, string defaultExtractDirectory)
		{
			SaveSelfExtractor(exeToGenerate, flavor, defaultExtractDirectory, null, null);
		}

		public void SaveSelfExtractor(string exeToGenerate, SelfExtractorFlavor flavor, string defaultExtractDirectory, string postExtractCommandToExecute)
		{
			SaveSelfExtractor(exeToGenerate, flavor, defaultExtractDirectory, postExtractCommandToExecute, null);
		}

		public void SaveSelfExtractor(string exeToGenerate, SelfExtractorFlavor flavor, string defaultExtractDirectory, string postExtractCommandToExecute, string iconFile)
		{
			if (_name == null)
			{
				_writestream = null;
			}
			_SavingSfx = true;
			_name = exeToGenerate;
			if (Directory.Exists(_name))
			{
				throw new ZipException("Bad Directory", new ArgumentException("That name specifies an existing directory. Please specify a filename.", "exeToGenerate"));
			}
			_contentsChanged = true;
			_fileAlreadyExists = File.Exists(_name);
			_SaveSfxStub(exeToGenerate, flavor, defaultExtractDirectory, postExtractCommandToExecute, iconFile);
			Save();
			_SavingSfx = false;
		}

		private void ExtractResourceToFile(Assembly a, string resourceName, string filename)
		{
			int num = 0;
			byte[] array = new byte[1024];
			using Stream stream = a.GetManifestResourceStream(resourceName);
			if (stream == null)
			{
				throw new ZipException($"missing resource '{resourceName}'");
			}
			using FileStream fileStream = File.OpenWrite(filename);
			do
			{
				num = stream.Read(array, 0, array.Length);
				fileStream.Write(array, 0, num);
			}
			while (num > 0);
		}

		private void _SaveSfxStub(string exeToGenerate, SelfExtractorFlavor flavor, string defaultExtractLocation, string postExtractCmdLine, string nameOfIconFile)
		{
			bool flag = false;
			string text = null;
			string text2 = null;
			try
			{
				if (File.Exists(exeToGenerate) && Verbose)
				{
					StatusMessageTextWriter.WriteLine("The existing file ({0}) will be overwritten.", exeToGenerate);
				}
				if (!exeToGenerate.EndsWith(".exe") && Verbose)
				{
					StatusMessageTextWriter.WriteLine("Warning: The generated self-extracting file will not have an .exe extension.");
				}
				text = GenerateTempPathname("exe", null);
				Assembly assembly = typeof(ZipFile).Assembly;
				CSharpCodeProvider cSharpCodeProvider = new CSharpCodeProvider();
				ExtractorSettings extractorSettings = null;
				ExtractorSettings[] settingsList = SettingsList;
				foreach (ExtractorSettings extractorSettings2 in settingsList)
				{
					if (extractorSettings2.Flavor == flavor)
					{
						extractorSettings = extractorSettings2;
						break;
					}
				}
				if (extractorSettings == null)
				{
					throw new BadStateException($"While saving a Self-Extracting Zip, Cannot find that flavor ({flavor})?");
				}
				CompilerParameters compilerParameters = new CompilerParameters();
				compilerParameters.ReferencedAssemblies.Add(assembly.Location);
				if (extractorSettings.ReferencedAssemblies != null)
				{
					foreach (string referencedAssembly in extractorSettings.ReferencedAssemblies)
					{
						compilerParameters.ReferencedAssemblies.Add(referencedAssembly);
					}
				}
				compilerParameters.GenerateInMemory = false;
				compilerParameters.GenerateExecutable = true;
				compilerParameters.IncludeDebugInformation = false;
				compilerParameters.CompilerOptions = "";
				Assembly executingAssembly = Assembly.GetExecutingAssembly();
				if (nameOfIconFile == null)
				{
					flag = true;
					nameOfIconFile = GenerateTempPathname("ico", null);
					ExtractResourceToFile(executingAssembly, "Ionic.Zip.Resources.zippedFile.ico", nameOfIconFile);
					compilerParameters.CompilerOptions += $"/win32icon:\"{nameOfIconFile}\"";
				}
				else if (nameOfIconFile != "")
				{
					compilerParameters.CompilerOptions += $"/win32icon:\"{nameOfIconFile}\"";
				}
				compilerParameters.OutputAssembly = text;
				if (flavor == SelfExtractorFlavor.WinFormsApplication)
				{
					compilerParameters.CompilerOptions += " /target:winexe";
				}
				if (compilerParameters.CompilerOptions == "")
				{
					compilerParameters.CompilerOptions = null;
				}
				text2 = GenerateTempPathname("tmp", null);
				if (extractorSettings.CopyThroughResources != null && extractorSettings.CopyThroughResources.Count != 0)
				{
					Directory.CreateDirectory(text2);
					foreach (string copyThroughResource in extractorSettings.CopyThroughResources)
					{
						string text3 = Path.Combine(text2, copyThroughResource);
						ExtractResourceToFile(executingAssembly, copyThroughResource, text3);
						compilerParameters.EmbeddedResources.Add(text3);
					}
				}
				compilerParameters.EmbeddedResources.Add(assembly.Location);
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("[assembly: System.Reflection.AssemblyTitle(\"DotNetZip SFX Archive\")]\n");
				stringBuilder.Append("[assembly: System.Reflection.AssemblyProduct(\"ZipLibrary\")]\n");
				stringBuilder.Append("[assembly: System.Reflection.AssemblyCopyright(\"Copyright © Dino Chiesa 2008, 2009\")]\n");
				stringBuilder.Append($"[assembly: System.Reflection.AssemblyVersion(\"{LibraryVersion.ToString()}\")]\n\n");
				bool flag2 = defaultExtractLocation != null;
				if (flag2)
				{
					defaultExtractLocation = defaultExtractLocation.Replace("\"", "").Replace("\\", "\\\\");
				}
				foreach (string item in extractorSettings.ResourcesToCompile)
				{
					Stream manifestResourceStream = executingAssembly.GetManifestResourceStream(item);
					if (manifestResourceStream == null)
					{
						throw new ZipException($"missing resource '{item}'");
					}
					using (StreamReader streamReader = new StreamReader(manifestResourceStream))
					{
						while (streamReader.Peek() >= 0)
						{
							string text4 = streamReader.ReadLine();
							if (flag2)
							{
								text4 = text4.Replace("@@EXTRACTLOCATION", defaultExtractLocation);
							}
							if (postExtractCmdLine != null)
							{
								text4 = text4.Replace("@@POST_UNPACK_CMD_LINE", postExtractCmdLine.Replace("\\", "\\\\"));
							}
							stringBuilder.Append(text4).Append("\n");
						}
					}
					stringBuilder.Append("\n\n");
				}
				string text5 = stringBuilder.ToString();
				CompilerResults compilerResults = cSharpCodeProvider.CompileAssemblyFromSource(compilerParameters, text5);
				if (compilerResults == null)
				{
					throw new SfxGenerationException("Cannot compile the extraction logic!");
				}
				if (Verbose)
				{
					StringEnumerator enumerator4 = compilerResults.Output.GetEnumerator();
					try
					{
						while (enumerator4.MoveNext())
						{
							string current4 = enumerator4.Current;
							StatusMessageTextWriter.WriteLine(current4);
						}
					}
					finally
					{
						(enumerator4 as IDisposable)?.Dispose();
					}
				}
				if (compilerResults.Errors.Count != 0)
				{
					string text6 = GenerateTempPathname("cs", null);
					using (TextWriter textWriter = new StreamWriter(text6))
					{
						textWriter.Write(text5);
					}
					throw new SfxGenerationException($"Errors compiling the extraction logic!  {text6}");
				}
				OnSaveEvent(ZipProgressEventType.Saving_AfterCompileSelfExtractor);
				using (Stream stream = File.OpenRead(text))
				{
					byte[] array = new byte[4000];
					int num = 1;
					while (num != 0)
					{
						num = stream.Read(array, 0, array.Length);
						if (num != 0)
						{
							WriteStream.Write(array, 0, num);
						}
					}
				}
				OnSaveEvent(ZipProgressEventType.Saving_AfterSaveTempArchive);
			}
			finally
			{
				try
				{
					if (Directory.Exists(text2))
					{
						try
						{
							Directory.Delete(text2, recursive: true);
						}
						catch
						{
						}
					}
					if (File.Exists(text))
					{
						try
						{
							File.Delete(text);
						}
						catch
						{
						}
					}
					if (flag && File.Exists(nameOfIconFile))
					{
						try
						{
							File.Delete(nameOfIconFile);
						}
						catch
						{
						}
					}
				}
				catch
				{
				}
			}
		}

		internal static string GenerateTempPathname(string extension, string ContainingDirectory)
		{
			string text = null;
			string name = Assembly.GetExecutingAssembly().GetName().Name;
			string path = (ContainingDirectory == null) ? Path.GetTempPath() : ContainingDirectory;
			int num = 0;
			do
			{
				num++;
				string path2 = string.Format("{0}-{1}-{2}.{3}", name, DateTime.Now.ToString("yyyyMMMdd-HHmmss"), num, extension);
				text = Path.Combine(path, path2);
			}
			while (File.Exists(text) || Directory.Exists(text));
			return text;
		}

		public void AddSelectedFiles(string selectionCriteria)
		{
			AddSelectedFiles(selectionCriteria, ".", null, recurseDirectories: false);
		}

		public void AddSelectedFiles(string selectionCriteria, bool recurseDirectories)
		{
			AddSelectedFiles(selectionCriteria, ".", null, recurseDirectories);
		}

		public void AddSelectedFiles(string selectionCriteria, string directoryOnDisk)
		{
			AddSelectedFiles(selectionCriteria, directoryOnDisk, null, recurseDirectories: false);
		}

		public void AddSelectedFiles(string selectionCriteria, string directoryOnDisk, bool recurseDirectories)
		{
			AddSelectedFiles(selectionCriteria, directoryOnDisk, null, recurseDirectories);
		}

		public void AddSelectedFiles(string selectionCriteria, string directoryOnDisk, string directoryPathInArchive)
		{
			AddSelectedFiles(selectionCriteria, directoryOnDisk, directoryPathInArchive, recurseDirectories: false);
		}

		public void AddSelectedFiles(string selectionCriteria, string directoryOnDisk, string directoryPathInArchive, bool recurseDirectories)
		{
			_AddOrUpdateSelectedFiles(selectionCriteria, directoryOnDisk, directoryPathInArchive, recurseDirectories, wantUpdate: false);
		}

		public void UpdateSelectedFiles(string selectionCriteria, string directoryOnDisk, string directoryPathInArchive, bool recurseDirectories)
		{
			_AddOrUpdateSelectedFiles(selectionCriteria, directoryOnDisk, directoryPathInArchive, recurseDirectories, wantUpdate: true);
		}

		private void _AddOrUpdateSelectedFiles(string selectionCriteria, string directoryOnDisk, string directoryPathInArchive, bool recurseDirectories, bool wantUpdate)
		{
			if (directoryOnDisk == null && Directory.Exists(selectionCriteria))
			{
				directoryOnDisk = selectionCriteria;
				selectionCriteria = "*.*";
			}
			else if (string.IsNullOrEmpty(directoryOnDisk))
			{
				directoryOnDisk = ".";
			}
			if (Verbose)
			{
				StatusMessageTextWriter.WriteLine("adding selection '{0}' from dir '{1}'...", selectionCriteria, directoryOnDisk);
			}
			FileSelector fileSelector = new FileSelector(selectionCriteria);
			ReadOnlyCollection<string> readOnlyCollection = fileSelector.SelectFiles(directoryOnDisk, recurseDirectories);
			if (Verbose)
			{
				StatusMessageTextWriter.WriteLine("found {0} files...", readOnlyCollection.Count);
			}
			OnAddStarted();
			foreach (string item in readOnlyCollection)
			{
				if (directoryPathInArchive != null)
				{
					string directoryPathInArchive2 = Path.GetDirectoryName(item).Replace(directoryOnDisk, directoryPathInArchive);
					if (wantUpdate)
					{
						UpdateFile(item, directoryPathInArchive2);
					}
					else
					{
						AddFile(item, directoryPathInArchive2);
					}
				}
				else if (wantUpdate)
				{
					UpdateFile(item, null);
				}
				else
				{
					AddFile(item, null);
				}
			}
			OnAddCompleted();
		}

		public ICollection<ZipEntry> SelectEntries(string selectionCriteria)
		{
			FileSelector fileSelector = new FileSelector(selectionCriteria);
			return fileSelector.SelectEntries(this);
		}

		public ICollection<ZipEntry> SelectEntries(string selectionCriteria, string directoryPathInArchive)
		{
			FileSelector fileSelector = new FileSelector(selectionCriteria);
			return fileSelector.SelectEntries(this, directoryPathInArchive);
		}

		public int RemoveSelectedEntries(string selectionCriteria)
		{
			ICollection<ZipEntry> collection = SelectEntries(selectionCriteria);
			RemoveEntries(collection);
			return collection.Count;
		}

		public int RemoveSelectedEntries(string selectionCriteria, string directoryPathInArchive)
		{
			ICollection<ZipEntry> collection = SelectEntries(selectionCriteria, directoryPathInArchive);
			RemoveEntries(collection);
			return collection.Count;
		}

		public void ExtractSelectedEntries(string selectionCriteria)
		{
			foreach (ZipEntry item in SelectEntries(selectionCriteria))
			{
				item.Password = _Password;
				item.Extract();
			}
		}

		public void ExtractSelectedEntries(string selectionCriteria, ExtractExistingFileAction extractExistingFile)
		{
			foreach (ZipEntry item in SelectEntries(selectionCriteria))
			{
				item.Password = _Password;
				item.Extract(extractExistingFile);
			}
		}

		public void ExtractSelectedEntries(string selectionCriteria, string directoryPathInArchive)
		{
			foreach (ZipEntry item in SelectEntries(selectionCriteria, directoryPathInArchive))
			{
				item.Password = _Password;
				item.Extract();
			}
		}

		public void ExtractSelectedEntries(string selectionCriteria, string directoryInArchive, string extractDirectory)
		{
			foreach (ZipEntry item in SelectEntries(selectionCriteria, directoryInArchive))
			{
				item.Password = _Password;
				item.Extract(extractDirectory);
			}
		}

		public void ExtractSelectedEntries(string selectionCriteria, string directoryPathInArchive, string extractDirectory, ExtractExistingFileAction extractExistingFile)
		{
			foreach (ZipEntry item in SelectEntries(selectionCriteria, directoryPathInArchive))
			{
				item.Password = _Password;
				item.Extract(extractDirectory, extractExistingFile);
			}
		}

		public IEnumerator<ZipEntry> GetEnumerator()
		{
			foreach (ZipEntry entry in _entries)
			{
				yield return entry;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		[DispId(-4)]
		public IEnumerator GetNewEnum()
		{
			return GetEnumerator();
		}
	}
}
