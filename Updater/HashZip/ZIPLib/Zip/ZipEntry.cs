using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Updater.HashZip.ZIPLib.Zip
{
	[Guid("ebc25cf6-9120-4283-b972-0e5520d00004")]
	[ComVisible(true)]
	[ClassInterface(ClassInterfaceType.AutoDispatch)]
	public class ZipEntry
	{
		private short _VersionMadeBy;

		private short _InternalFileAttrs;

		private int _ExternalFileAttrs;

		private short _filenameLength;

		private short _extraFieldLength;

		private short _commentLength;

		internal ZipCrypto _zipCrypto;

		internal DateTime _LastModified;

		private DateTime _Mtime;

		private DateTime _Atime;

		private DateTime _Ctime;

		private bool _ntfsTimesAreSet;

		private bool _emitNtfsTimes = true;

		private bool _emitUnixTimes;

		private bool _TrimVolumeFromFullyQualifiedPaths = true;

		private bool _ForceNoCompression;

		internal string _LocalFileName;

		private string _FileNameInArchive;

		internal short _VersionNeeded;

		internal short _BitField;

		internal short _CompressionMethod;

		internal string _Comment;

		private bool _IsDirectory;

		private byte[] _CommentBytes;

		internal long _CompressedSize;

		internal long _CompressedFileDataSize;

		internal long _UncompressedSize;

		internal int _TimeBlob;

		private bool _crcCalculated;

		internal int _Crc32;

		internal byte[] _Extra;

		private bool _metadataChanged;

		private bool _restreamRequiredOnSave;

		private bool _sourceIsEncrypted;

		private bool _skippedDuringSave;

		private static Encoding ibm437 = Encoding.GetEncoding("IBM437");

		private Encoding _provisionalAlternateEncoding = Encoding.GetEncoding("IBM437");

		private Encoding _actualEncoding;

		internal ZipFile _zipfile;

		internal long __FileDataPosition = -1L;

		private byte[] _EntryHeader;

		internal long _RelativeOffsetOfLocalHeader;

		private long _TotalEntrySize;

		internal int _LengthOfHeader;

		internal int _LengthOfTrailer;

		private bool _InputUsesZip64;

		private uint _UnsupportedAlgorithmId;

		internal string _Password;

		internal ZipEntrySource _Source = ZipEntrySource.None;

		internal EncryptionAlgorithm _Encryption = EncryptionAlgorithm.None;

		internal byte[] _WeakEncryptionHeader;

		internal Stream _archiveStream;

		private Stream _sourceStream;

		private long? _sourceStreamOriginalPosition;

		private bool _sourceWasJitProvided;

		private bool _ioOperationCanceled;

		private bool _presumeZip64;

		private bool? _entryRequiresZip64;

		private bool? _OutputUsesZip64;

		private bool _IsText;

		private ZipEntryTimestamp _timestamp;

		private static DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private int _readExtraDepth;

		private static Regex _IncompressibleRegex = new Regex("(?i)^(.+)\\.(mp3|png|docx|xlsx|pptx|jpg|zip)$");

		internal bool AttributesIndicateDirectory => _InternalFileAttrs == 0 && (_ExternalFileAttrs & 0x10) == 16;

		public DateTime LastModified
		{
			get
			{
				return _LastModified.ToLocalTime();
			}
			set
			{
				_LastModified = ((value.Kind == DateTimeKind.Unspecified) ? DateTime.SpecifyKind(value, DateTimeKind.Local) : value);
				_Mtime = _LastModified.ToUniversalTime();
				_metadataChanged = true;
			}
		}

		private int BufferSize => _zipfile.BufferSize;

		public DateTime ModifiedTime => _Mtime;

		public DateTime AccessedTime => _Atime;

		public DateTime CreationTime => _Ctime;

		public bool EmitTimesInWindowsFormatWhenSaving
		{
			get
			{
				return _emitNtfsTimes;
			}
			set
			{
				_emitNtfsTimes = value;
				_metadataChanged = true;
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
				_metadataChanged = true;
			}
		}

		public ZipEntryTimestamp Timestamp => _timestamp;

		public FileAttributes Attributes
		{
			get
			{
				return (FileAttributes)_ExternalFileAttrs;
			}
			set
			{
				_ExternalFileAttrs = (int)value;
				_VersionMadeBy = 45;
				_metadataChanged = true;
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
				if (value != _ForceNoCompression)
				{
					_ForceNoCompression = value;
					if (_ForceNoCompression)
					{
						CompressionMethod = 0;
					}
				}
			}
		}

		internal string LocalFileName => _LocalFileName;

		public string FileName
		{
			get
			{
				return _FileNameInArchive;
			}
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					throw new ZipException("The FileName must be non empty and non-null.");
				}
				string text = NameInArchive(value, null);
				if (!(_FileNameInArchive == text))
				{
					if (_zipfile.EntryFileNames.Contains(text))
					{
						throw new ZipException($"Cannot rename {_FileNameInArchive} to {text}; an entry by that name already exists in the archive.");
					}
					_FileNameInArchive = text;
					if (_zipfile != null)
					{
						_zipfile.NotifyEntryChanged();
					}
					_metadataChanged = true;
				}
			}
		}

		public Stream InputStream
		{
			get
			{
				return _sourceStream;
			}
			set
			{
				if (_Source != ZipEntrySource.Stream)
				{
					throw new ZipException("You must not set the input stream for this ZipEntry.");
				}
				_sourceWasJitProvided = true;
				_sourceStream = value;
			}
		}

		public bool InputStreamWasJitProvided => _sourceWasJitProvided;

		public ZipEntrySource Source => _Source;

		public short VersionNeeded => _VersionNeeded;

		public string Comment
		{
			get
			{
				return _Comment;
			}
			set
			{
				_Comment = value;
				_metadataChanged = true;
			}
		}

		public bool? RequiresZip64 => _entryRequiresZip64;

		public bool? OutputUsedZip64 => _OutputUsesZip64;

		public short BitField => _BitField;

		public short CompressionMethod
		{
			get
			{
				return _CompressionMethod;
			}
			set
			{
				if (value != _CompressionMethod)
				{
					if (value != 0 && value != 8)
					{
						throw new InvalidOperationException("Unsupported compression method. Specify 8 or 0.");
					}
					if (_Source == ZipEntrySource.ZipFile && _sourceIsEncrypted)
					{
						throw new InvalidOperationException("Cannot change compression method on encrypted entries read from archives.");
					}
					_CompressionMethod = value;
					_ForceNoCompression = (_CompressionMethod == 0);
					_restreamRequiredOnSave = true;
				}
			}
		}

		public long CompressedSize => _CompressedSize;

		public long UncompressedSize => _UncompressedSize;

		public double CompressionRatio
		{
			get
			{
				if (UncompressedSize == 0)
				{
					return 0.0;
				}
				return 100.0 * (1.0 - 1.0 * (double)CompressedSize / (1.0 * (double)UncompressedSize));
			}
		}

		public int Crc => _Crc32;

		public bool IsDirectory => _IsDirectory;

		public bool UsesEncryption => Encryption != EncryptionAlgorithm.None;

		public EncryptionAlgorithm Encryption
		{
			get
			{
				return _Encryption;
			}
			set
			{
				if (value != _Encryption)
				{
					if (value == EncryptionAlgorithm.Unsupported)
					{
						throw new InvalidOperationException("You may not set Encryption to that value.");
					}
					if (_Source == ZipEntrySource.ZipFile && _sourceIsEncrypted)
					{
						throw new InvalidOperationException("You cannot change the encryption method on encrypted entries read from archives.");
					}
					_Encryption = value;
					_restreamRequiredOnSave = true;
				}
			}
		}

		public string Password
		{
			set
			{
				_Password = value;
				if (_Password == null)
				{
					_Encryption = EncryptionAlgorithm.None;
					return;
				}
				if (_Source == ZipEntrySource.ZipFile && !_sourceIsEncrypted)
				{
					_restreamRequiredOnSave = true;
				}
				if (Encryption == EncryptionAlgorithm.None)
				{
					_Encryption = EncryptionAlgorithm.PkzipWeak;
				}
			}
		}

		[Obsolete("Please use property ExtractExistingFile")]
		public bool OverwriteOnExtract
		{
			get
			{
				return ExtractExistingFile == ExtractExistingFileAction.OverwriteSilently;
			}
			set
			{
				ExtractExistingFile = (value ? ExtractExistingFileAction.OverwriteSilently : ExtractExistingFileAction.Throw);
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

		public bool IncludedInMostRecentSave => !_skippedDuringSave;

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

		public bool UseUnicodeAsNecessary
		{
			get
			{
				return _provisionalAlternateEncoding == Encoding.GetEncoding("UTF-8");
			}
			set
			{
				_provisionalAlternateEncoding = (value ? Encoding.GetEncoding("UTF-8") : ZipFile.DefaultEncoding);
			}
		}

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

		public Encoding ActualEncoding => _actualEncoding;

		public bool IsText
		{
			get
			{
				return _IsText;
			}
			set
			{
				_IsText = value;
			}
		}

		internal int LengthOfCryptoHeaderBytes
		{
			get
			{
				if ((_BitField & 1) != 1)
				{
					return 0;
				}
				if (Encryption == EncryptionAlgorithm.PkzipWeak)
				{
					return 12;
				}
				throw new ZipException("internal error");
			}
		}

		internal long FileDataPosition
		{
			get
			{
				if (__FileDataPosition == -1)
				{
					SetFdpLoh();
				}
				return __FileDataPosition;
			}
		}

		private int LengthOfHeader
		{
			get
			{
				if (_LengthOfHeader == 0)
				{
					SetFdpLoh();
				}
				return _LengthOfHeader;
			}
		}

		internal Stream ArchiveStream
		{
			get
			{
				if (_archiveStream == null && _zipfile != null)
				{
					_zipfile.Reset();
					_archiveStream = _zipfile.ReadStream;
				}
				return _archiveStream;
			}
		}

		private string UnsupportedAlgorithm
		{
			get
			{
				string empty = string.Empty;
				return _UnsupportedAlgorithmId switch
				{
					0u => "--", 
					26113u => "DES", 
					26114u => "RC2", 
					26115u => "3DES-168", 
					26121u => "3DES-112", 
					26126u => "PKWare AES128", 
					26127u => "PKWare AES192", 
					26128u => "PKWare AES256", 
					26370u => "RC2", 
					26400u => "Blowfish", 
					26401u => "Twofish", 
					26625u => "RC4", 
					_ => $"Unknown (0x{_UnsupportedAlgorithmId:X4})", 
				};
			}
		}

		private string UnsupportedCompressionMethod
		{
			get
			{
				string empty = string.Empty;
				return _CompressionMethod switch
				{
					0 => "Store", 
					1 => "Shrink", 
					8 => "DEFLATE", 
					9 => "Deflate64", 
					14 => "LZMA", 
					19 => "LZ77", 
					98 => "PPMd", 
					_ => $"Unknown (0x{_CompressionMethod:X4})", 
				};
			}
		}

		internal void ResetDirEntry()
		{
			__FileDataPosition = -1L;
			_LengthOfHeader = 0;
		}

		internal static ZipEntry ReadDirEntry(ZipFile zf)
		{
			Stream readStream = zf.ReadStream;
			Encoding provisionalAlternateEncoding = zf.ProvisionalAlternateEncoding;
			int num = SharedUtilities.ReadSignature(readStream);
			if (IsNotValidZipDirEntrySig(num))
			{
				readStream.Seek(-4L, SeekOrigin.Current);
				if ((long)num != 101010256 && (long)num != 101075792 && num != 67324752)
				{
					throw new BadReadException($"  ZipEntry::ReadDirEntry(): Bad signature (0x{num:X8}) at position 0x{readStream.Position:X8}");
				}
				return null;
			}
			int num2 = 46;
			byte[] array = new byte[42];
			int num3 = readStream.Read(array, 0, array.Length);
			if (num3 != array.Length)
			{
				return null;
			}
			int num4 = 0;
			ZipEntry zipEntry = new ZipEntry();
			zipEntry._Source = ZipEntrySource.ZipFile;
			zipEntry._archiveStream = readStream;
			zipEntry._zipfile = zf;
			zipEntry._VersionMadeBy = (short)(array[num4++] + array[num4++] * 256);
			zipEntry._VersionNeeded = (short)(array[num4++] + array[num4++] * 256);
			zipEntry._BitField = (short)(array[num4++] + array[num4++] * 256);
			zipEntry._CompressionMethod = (short)(array[num4++] + array[num4++] * 256);
			zipEntry._TimeBlob = array[num4++] + array[num4++] * 256 + array[num4++] * 256 * 256 + array[num4++] * 256 * 256 * 256;
			zipEntry._LastModified = SharedUtilities.PackedToDateTime(zipEntry._TimeBlob);
			zipEntry._timestamp |= ZipEntryTimestamp.DOS;
			zipEntry._Crc32 = array[num4++] + array[num4++] * 256 + array[num4++] * 256 * 256 + array[num4++] * 256 * 256 * 256;
			zipEntry._CompressedSize = (uint)(array[num4++] + array[num4++] * 256 + array[num4++] * 256 * 256 + array[num4++] * 256 * 256 * 256);
			zipEntry._UncompressedSize = (uint)(array[num4++] + array[num4++] * 256 + array[num4++] * 256 * 256 + array[num4++] * 256 * 256 * 256);
			zipEntry._filenameLength = (short)(array[num4++] + array[num4++] * 256);
			zipEntry._extraFieldLength = (short)(array[num4++] + array[num4++] * 256);
			zipEntry._commentLength = (short)(array[num4++] + array[num4++] * 256);
			num4 += 2;
			zipEntry._InternalFileAttrs = (short)(array[num4++] + array[num4++] * 256);
			zipEntry._ExternalFileAttrs = array[num4++] + array[num4++] * 256 + array[num4++] * 256 * 256 + array[num4++] * 256 * 256 * 256;
			zipEntry._RelativeOffsetOfLocalHeader = (uint)(array[num4++] + array[num4++] * 256 + array[num4++] * 256 * 256 + array[num4++] * 256 * 256 * 256);
			zipEntry.IsText = ((zipEntry._InternalFileAttrs & 1) == 1);
			array = new byte[zipEntry._filenameLength];
			num3 = readStream.Read(array, 0, array.Length);
			num2 += num3;
			if ((zipEntry._BitField & 0x800) == 2048)
			{
				zipEntry._LocalFileName = SharedUtilities.Utf8StringFromBuffer(array);
			}
			else
			{
				zipEntry._LocalFileName = SharedUtilities.StringFromBuffer(array, provisionalAlternateEncoding);
			}
			zipEntry._FileNameInArchive = zipEntry._LocalFileName;
			if (zipEntry.AttributesIndicateDirectory)
			{
				zipEntry.MarkAsDirectory();
			}
			if (zipEntry._LocalFileName.EndsWith("/"))
			{
				zipEntry.MarkAsDirectory();
			}
			zipEntry._CompressedFileDataSize = zipEntry._CompressedSize;
			if ((zipEntry._BitField & 1) == 1)
			{
				zipEntry._Encryption = EncryptionAlgorithm.PkzipWeak;
				zipEntry._sourceIsEncrypted = true;
			}
			if (zipEntry._extraFieldLength > 0)
			{
				zipEntry._InputUsesZip64 = (zipEntry._CompressedSize == uint.MaxValue || zipEntry._UncompressedSize == uint.MaxValue || zipEntry._RelativeOffsetOfLocalHeader == uint.MaxValue);
				num2 += zipEntry.ProcessExtraField(zipEntry._extraFieldLength);
				zipEntry._CompressedFileDataSize = zipEntry._CompressedSize;
			}
			if (zipEntry._Encryption == EncryptionAlgorithm.PkzipWeak)
			{
				zipEntry._CompressedFileDataSize -= 12L;
			}
			if ((zipEntry._BitField & 8) == 8)
			{
				if (zipEntry._InputUsesZip64)
				{
					zipEntry._LengthOfTrailer += 24;
				}
				else
				{
					zipEntry._LengthOfTrailer += 16;
				}
			}
			if (zipEntry._commentLength > 0)
			{
				array = new byte[zipEntry._commentLength];
				num3 = readStream.Read(array, 0, array.Length);
				num2 += num3;
				if ((zipEntry._BitField & 0x800) == 2048)
				{
					zipEntry._Comment = SharedUtilities.Utf8StringFromBuffer(array);
				}
				else
				{
					zipEntry._Comment = SharedUtilities.StringFromBuffer(array, provisionalAlternateEncoding);
				}
			}
			return zipEntry;
		}

		internal static bool IsNotValidZipDirEntrySig(int signature)
		{
			return signature != 33639248;
		}

		public void SetEntryTimes(DateTime created, DateTime accessed, DateTime modified)
		{
			_ntfsTimesAreSet = true;
			_Ctime = created.ToUniversalTime();
			_Atime = accessed.ToUniversalTime();
			_Mtime = modified.ToUniversalTime();
			_LastModified = _Mtime;
			_emitNtfsTimes = true;
			_metadataChanged = true;
		}

		[Obsolete("Please use method SetEntryTimes(DateTime,DateTime,DateTime)")]
		public void SetNtfsTimes(DateTime created, DateTime accessed, DateTime modified)
		{
			SetEntryTimes(created, accessed, modified);
		}

		internal static string NameInArchive(string filename, string directoryPathInArchive)
		{
			string text = null;
			text = ((directoryPathInArchive == null) ? filename : ((!string.IsNullOrEmpty(directoryPathInArchive)) ? Path.Combine(directoryPathInArchive, Path.GetFileName(filename)) : Path.GetFileName(filename)));
			text = SharedUtilities.TrimVolumeAndSwapSlashes(text);
			text = SharedUtilities.NormalizeFwdSlashPath(text);
			while (text.StartsWith("/"))
			{
				text = text.Substring(1);
			}
			return text;
		}

		internal static ZipEntry Create(string filename, string nameInArchive)
		{
			return Create(filename, nameInArchive, isStream: false, null);
		}

		internal static ZipEntry Create(string filename, string nameInArchive, bool isStream, Stream stream)
		{
			if (string.IsNullOrEmpty(filename))
			{
				throw new ZipException("The entry name must be non-null and non-empty.");
			}
			ZipEntry zipEntry = new ZipEntry();
			zipEntry._VersionMadeBy = 45;
			if (isStream)
			{
				zipEntry._Source = ZipEntrySource.Stream;
				zipEntry._sourceStream = stream;
				zipEntry._Mtime = (zipEntry._Atime = (zipEntry._Ctime = DateTime.UtcNow));
			}
			else
			{
				zipEntry._Source = ZipEntrySource.FileSystem;
				zipEntry._Mtime = File.GetLastWriteTimeUtc(filename);
				zipEntry._Ctime = File.GetCreationTimeUtc(filename);
				zipEntry._Atime = File.GetLastAccessTimeUtc(filename);
				if (File.Exists(filename) || Directory.Exists(filename))
				{
					zipEntry._ExternalFileAttrs = (int)File.GetAttributes(filename);
				}
				zipEntry._ntfsTimesAreSet = true;
			}
			zipEntry._LastModified = zipEntry._Mtime;
			zipEntry._LocalFileName = filename;
			zipEntry._FileNameInArchive = nameInArchive.Replace('\\', '/');
			return zipEntry;
		}

		internal void MarkAsDirectory()
		{
			_IsDirectory = true;
			if (!_FileNameInArchive.EndsWith("/"))
			{
				_FileNameInArchive += "/";
			}
		}

		public override string ToString()
		{
			return $"ZipEntry/{FileName}";
		}

		private void SetFdpLoh()
		{
			long position = ArchiveStream.Position;
			try
			{
				_zipfile.SeekFromOrigin(_RelativeOffsetOfLocalHeader);
			}
			catch (IOException innerException)
			{
				string message = $"Exception seeking  entry({FileName}) offset(0x{_RelativeOffsetOfLocalHeader:X8}) len(0x{ArchiveStream.Length:X8})";
				throw new BadStateException(message, innerException);
			}
			byte[] array = new byte[30];
			ArchiveStream.Read(array, 0, array.Length);
			short num = (short)(array[26] + array[27] * 256);
			short num2 = (short)(array[28] + array[29] * 256);
			ArchiveStream.Seek(num + num2, SeekOrigin.Current);
			_LengthOfHeader = 30 + num2 + num + LengthOfCryptoHeaderBytes;
			__FileDataPosition = _RelativeOffsetOfLocalHeader + _LengthOfHeader;
			ArchiveStream.Seek(position, SeekOrigin.Begin);
		}

		public void Extract()
		{
			InternalExtract(".", null, null);
		}

		[Obsolete("Please use method Extract(ExtractExistingFileAction)")]
		public void Extract(bool overwrite)
		{
			OverwriteOnExtract = overwrite;
			InternalExtract(".", null, null);
		}

		public void Extract(ExtractExistingFileAction extractExistingFile)
		{
			ExtractExistingFile = extractExistingFile;
			InternalExtract(".", null, null);
		}

		public void Extract(Stream stream)
		{
			InternalExtract(null, stream, null);
		}

		public void Extract(string baseDirectory)
		{
			InternalExtract(baseDirectory, null, null);
		}

		[Obsolete("Please use method Extract(String,ExtractExistingFileAction)")]
		public void Extract(string baseDirectory, bool overwrite)
		{
			OverwriteOnExtract = overwrite;
			InternalExtract(baseDirectory, null, null);
		}

		public void Extract(string baseDirectory, ExtractExistingFileAction extractExistingFile)
		{
			ExtractExistingFile = extractExistingFile;
			InternalExtract(baseDirectory, null, null);
		}

		public void ExtractWithPassword(string password)
		{
			InternalExtract(".", null, password);
		}

		public void ExtractWithPassword(string baseDirectory, string password)
		{
			InternalExtract(baseDirectory, null, password);
		}

		[Obsolete("Please use method ExtractWithPassword(ExtractExistingFileAction,String)")]
		public void ExtractWithPassword(bool overwrite, string password)
		{
			OverwriteOnExtract = overwrite;
			InternalExtract(".", null, password);
		}

		public void ExtractWithPassword(ExtractExistingFileAction extractExistingFile, string password)
		{
			ExtractExistingFile = extractExistingFile;
			InternalExtract(".", null, password);
		}

		[Obsolete("Please use method ExtractWithPassword(String,ExtractExistingFileAction,String)")]
		public void ExtractWithPassword(string baseDirectory, bool overwrite, string password)
		{
			OverwriteOnExtract = overwrite;
			InternalExtract(baseDirectory, null, password);
		}

		public void ExtractWithPassword(string baseDirectory, ExtractExistingFileAction extractExistingFile, string password)
		{
			ExtractExistingFile = extractExistingFile;
			InternalExtract(baseDirectory, null, password);
		}

		public void ExtractWithPassword(Stream stream, string password)
		{
			InternalExtract(null, stream, password);
		}

		public CrcCalculatorStream OpenReader()
		{
			return InternalOpenReader(_Password ?? _zipfile._Password);
		}

		public CrcCalculatorStream OpenReader(string password)
		{
			return InternalOpenReader(password);
		}

		private CrcCalculatorStream InternalOpenReader(string password)
		{
			ValidateCompression();
			ValidateEncryption();
			SetupCrypto(password);
			if (_Source != ZipEntrySource.ZipFile)
			{
				throw new BadStateException("You must call ZipFile.Save before calling OpenReader.");
			}
			Stream archiveStream = ArchiveStream;
			_zipfile.SeekFromOrigin(FileDataPosition);
			Stream stream = archiveStream;
			if (Encryption == EncryptionAlgorithm.PkzipWeak)
			{
				stream = new ZipCipherStream(archiveStream, _zipCrypto, CryptoMode.Decrypt);
			}
			return new CrcCalculatorStream((CompressionMethod == 8) ? new DeflateStream(stream, CompressionMode.Decompress, leaveOpen: true) : stream, _UncompressedSize);
		}

		private void OnExtractProgress(long bytesWritten, long totalBytesToWrite)
		{
			_ioOperationCanceled = _zipfile.OnExtractBlock(this, bytesWritten, totalBytesToWrite);
		}

		private void OnBeforeExtract(string path)
		{
			if (!_zipfile._inExtractAll)
			{
				_ioOperationCanceled = _zipfile.OnSingleEntryExtract(this, path, before: true);
			}
		}

		private void OnAfterExtract(string path)
		{
			if (!_zipfile._inExtractAll)
			{
				_zipfile.OnSingleEntryExtract(this, path, before: false);
			}
		}

		private void OnExtractExisting(string path)
		{
			_ioOperationCanceled = _zipfile.OnExtractExisting(this, path);
		}

		private void OnWriteBlock(long bytesXferred, long totalBytesToXfer)
		{
			_ioOperationCanceled = _zipfile.OnSaveBlock(this, bytesXferred, totalBytesToXfer);
		}

		private static void ReallyDelete(string fileName)
		{
			if ((File.GetAttributes(fileName) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
			{
				File.SetAttributes(fileName, FileAttributes.Normal);
			}
			File.Delete(fileName);
		}

		private void InternalExtract(string baseDir, Stream outstream, string password)
		{
			if (_zipfile == null)
			{
				throw new BadStateException("This ZipEntry is an orphan.");
			}
			_zipfile.Reset();
			if (_Source != ZipEntrySource.ZipFile)
			{
				throw new BadStateException("You must call ZipFile.Save before calling any Extract method.");
			}
			OnBeforeExtract(baseDir);
			_ioOperationCanceled = false;
			string OutputFile = null;
			Stream stream = null;
			bool flag = false;
			bool flag2 = false;
			try
			{
				ValidateCompression();
				ValidateEncryption();
				if (ValidateOutput(baseDir, outstream, out OutputFile))
				{
					if (_zipfile.Verbose)
					{
						_zipfile.StatusMessageTextWriter.WriteLine("extract dir {0}...", OutputFile);
					}
					OnAfterExtract(baseDir);
					return;
				}
				string text = password ?? _Password ?? _zipfile._Password;
				if (UsesEncryption)
				{
					if (text == null)
					{
						throw new BadPasswordException();
					}
					SetupCrypto(text);
				}
				if (OutputFile != null)
				{
					if (_zipfile.Verbose)
					{
						_zipfile.StatusMessageTextWriter.WriteLine("extract file {0}...", OutputFile);
					}
					if (!Directory.Exists(Path.GetDirectoryName(OutputFile)))
					{
						Directory.CreateDirectory(Path.GetDirectoryName(OutputFile));
					}
					else
					{
						flag2 = _zipfile._inExtractAll;
					}
					if (!File.Exists(OutputFile))
					{
						goto IL_0194;
					}
					flag = true;
					int num = CheckExtractExistingFile(baseDir, OutputFile);
					if (num != 2 && num != 1)
					{
						goto IL_0194;
					}
					return;
				}
				if (_zipfile.Verbose)
				{
					_zipfile.StatusMessageTextWriter.WriteLine("extract entry {0} to stream...", FileName);
				}
				stream = outstream;
				goto IL_01d0;
				IL_01d0:
				if (_ioOperationCanceled)
				{
					return;
				}
				int num2 = _ExtractOne(stream);
				if (_ioOperationCanceled)
				{
					return;
				}
				if (num2 != _Crc32)
				{
					throw new BadCrcException("CRC error: the file being extracted appears to be corrupted. " + $"Expected 0x{_Crc32:X8}, Actual 0x{num2:X8}");
				}
				if (OutputFile != null)
				{
					stream.Close();
					stream = null;
					_SetTimes(OutputFile, isFile: true);
					if (flag2 && FileName.IndexOf('/') != -1)
					{
						string directoryName = Path.GetDirectoryName(FileName);
						if (_zipfile[directoryName] == null)
						{
							_SetTimes(Path.GetDirectoryName(OutputFile), isFile: false);
						}
					}
					if ((_VersionMadeBy & 0xFF00) == 2560 || (_VersionMadeBy & 0xFF00) == 0)
					{
						File.SetAttributes(OutputFile, (FileAttributes)_ExternalFileAttrs);
					}
				}
				OnAfterExtract(baseDir);
				goto end_IL_005b;
				IL_0194:
				stream = new FileStream(OutputFile, FileMode.CreateNew);
				goto IL_01d0;
				end_IL_005b:;
			}
			catch (Exception ex)
			{
				_ioOperationCanceled = true;
				if (!(ex is ZipException))
				{
					throw new ZipException("Cannot extract", ex);
				}
				throw;
			}
			finally
			{
				if (_ioOperationCanceled && OutputFile != null)
				{
					try
					{
						stream?.Close();
						if (File.Exists(OutputFile) && (!flag || ExtractExistingFile == ExtractExistingFileAction.OverwriteSilently))
						{
							File.Delete(OutputFile);
						}
					}
					finally
					{
					}
				}
			}
		}

		private int CheckExtractExistingFile(string baseDir, string TargetFile)
		{
			int num = 0;
			while (true)
			{
				switch (ExtractExistingFile)
				{
				case ExtractExistingFileAction.OverwriteSilently:
					if (_zipfile.Verbose)
					{
						_zipfile.StatusMessageTextWriter.WriteLine("the file {0} exists; deleting it...", TargetFile);
					}
					ReallyDelete(TargetFile);
					return 0;
				case ExtractExistingFileAction.DoNotOverwrite:
					if (_zipfile.Verbose)
					{
						_zipfile.StatusMessageTextWriter.WriteLine("the file {0} exists; not extracting entry...", FileName);
					}
					OnAfterExtract(baseDir);
					return 1;
				case ExtractExistingFileAction.InvokeExtractProgressEvent:
					if (num > 0)
					{
						throw new ZipException($"The file {TargetFile} already exists.");
					}
					OnExtractExisting(baseDir);
					if (_ioOperationCanceled)
					{
						return 2;
					}
					break;
				default:
					throw new ZipException($"The file {TargetFile} already exists.");
				}
				num++;
				bool flag = true;
			}
		}

		private void _CheckRead(int nbytes)
		{
			if (nbytes == 0)
			{
				throw new BadReadException($"bad read of entry {FileName} from compressed archive.");
			}
		}

		private int _ExtractOne(Stream output)
		{
			Stream archiveStream = ArchiveStream;
			_zipfile.SeekFromOrigin(FileDataPosition);
			int result = 0;
			byte[] array = new byte[BufferSize];
			long num = (CompressionMethod == 8) ? UncompressedSize : _CompressedFileDataSize;
			Stream stream = null;
			stream = ((Encryption != EncryptionAlgorithm.PkzipWeak) ? archiveStream : new ZipCipherStream(archiveStream, _zipCrypto, CryptoMode.Decrypt));
			Stream stream2 = (CompressionMethod == 8) ? new DeflateStream(stream, CompressionMode.Decompress, leaveOpen: true) : stream;
			long num2 = 0L;
			using (CrcCalculatorStream crcCalculatorStream = new CrcCalculatorStream(stream2))
			{
				while (num > 0)
				{
					int count = (int)((num > array.Length) ? array.Length : num);
					int num3 = crcCalculatorStream.Read(array, 0, count);
					_CheckRead(num3);
					output.Write(array, 0, num3);
					num -= num3;
					num2 += num3;
					OnExtractProgress(num2, UncompressedSize);
					if (_ioOperationCanceled)
					{
						break;
					}
				}
				result = crcCalculatorStream.Crc;
			}
			return result;
		}

		internal void _SetTimes(string fileOrDirectory, bool isFile)
		{
			if (_ntfsTimesAreSet)
			{
				if (isFile)
				{
					if (File.Exists(fileOrDirectory))
					{
						File.SetCreationTimeUtc(fileOrDirectory, _Ctime);
						File.SetLastAccessTimeUtc(fileOrDirectory, _Atime);
						File.SetLastWriteTimeUtc(fileOrDirectory, _Mtime);
					}
				}
				else if (Directory.Exists(fileOrDirectory))
				{
					Directory.SetCreationTimeUtc(fileOrDirectory, _Ctime);
					Directory.SetLastAccessTimeUtc(fileOrDirectory, _Atime);
					Directory.SetLastWriteTimeUtc(fileOrDirectory, _Mtime);
				}
			}
			else
			{
				DateTime lastWriteTime = SharedUtilities.AdjustTime_DotNetToWin32(LastModified);
				if (isFile)
				{
					File.SetLastWriteTime(fileOrDirectory, lastWriteTime);
				}
				else
				{
					Directory.SetLastWriteTime(fileOrDirectory, lastWriteTime);
				}
			}
		}

		private void ValidateEncryption()
		{
			if (Encryption != EncryptionAlgorithm.PkzipWeak && Encryption != 0)
			{
				if (_UnsupportedAlgorithmId != 0)
				{
					throw new ZipException($"Cannot extract: Entry {FileName} is encrypted with an algorithm not supported by DotNetZip: {UnsupportedAlgorithm}");
				}
				throw new ZipException($"Cannot extract: Entry {FileName} uses an unsupported encryption algorithm ({(int)Encryption:X2})");
			}
		}

		private void ValidateCompression()
		{
			if (CompressionMethod != 0 && CompressionMethod != 8)
			{
				throw new ZipException($"Entry {FileName} uses an unsupported compression method (0x{CompressionMethod:X2}, {UnsupportedCompressionMethod})");
			}
		}

		private void SetupCrypto(string password)
		{
			if (password != null && Encryption == EncryptionAlgorithm.PkzipWeak)
			{
				_zipfile.SeekFromOrigin(FileDataPosition - 12);
				_zipCrypto = ZipCrypto.ForRead(password, this);
			}
		}

		private bool ValidateOutput(string basedir, Stream outstream, out string OutputFile)
		{
			if (basedir != null)
			{
				string text = FileName;
				if (text.StartsWith("/"))
				{
					text = FileName.Substring(1);
				}
				if (_zipfile.FlattenFoldersOnExtract)
				{
					OutputFile = Path.Combine(basedir, (text.IndexOf('/') != -1) ? Path.GetFileName(text) : text);
				}
				else
				{
					OutputFile = Path.Combine(basedir, text);
				}
				if (IsDirectory || FileName.EndsWith("/"))
				{
					if (!Directory.Exists(OutputFile))
					{
						Directory.CreateDirectory(OutputFile);
						_SetTimes(OutputFile, isFile: false);
					}
					else if (ExtractExistingFile == ExtractExistingFileAction.OverwriteSilently)
					{
						_SetTimes(OutputFile, isFile: false);
					}
					return true;
				}
				return false;
			}
			if (outstream != null)
			{
				OutputFile = null;
				if (IsDirectory || FileName.EndsWith("/"))
				{
					return true;
				}
				return false;
			}
			throw new ZipException("Cannot extract.", new ArgumentException("Invalid input.", "outstream"));
		}

		private void ReadExtraField()
		{
			_readExtraDepth++;
			long position = ArchiveStream.Position;
			_zipfile.SeekFromOrigin(_RelativeOffsetOfLocalHeader);
			byte[] array = new byte[30];
			ArchiveStream.Read(array, 0, array.Length);
			int num = 26;
			short num2 = (short)(array[num++] + array[num++] * 256);
			short extraFieldLength = (short)(array[num++] + array[num++] * 256);
			ArchiveStream.Seek(num2, SeekOrigin.Current);
			ProcessExtraField(extraFieldLength);
			ArchiveStream.Seek(position, SeekOrigin.Begin);
			_readExtraDepth--;
		}

		private static bool ReadHeader(ZipEntry ze, Encoding defaultEncoding)
		{
			int num = 0;
			ze._RelativeOffsetOfLocalHeader = ze._zipfile.RelativeOffset;
			int num2 = SharedUtilities.ReadSignature(ze.ArchiveStream);
			num += 4;
			if (IsNotValidSig(num2))
			{
				ze.ArchiveStream.Seek(-4L, SeekOrigin.Current);
				if (IsNotValidZipDirEntrySig(num2) && (long)num2 != 101010256)
				{
					throw new BadReadException($"  ZipEntry::ReadHeader(): Bad signature (0x{num2:X8}) at position  0x{ze.ArchiveStream.Position:X8}");
				}
				return false;
			}
			byte[] array = new byte[26];
			int num3 = ze.ArchiveStream.Read(array, 0, array.Length);
			if (num3 != array.Length)
			{
				return false;
			}
			num += num3;
			int num4 = 0;
			ze._VersionNeeded = (short)(array[num4++] + array[num4++] * 256);
			ze._BitField = (short)(array[num4++] + array[num4++] * 256);
			ze._CompressionMethod = (short)(array[num4++] + array[num4++] * 256);
			ze._TimeBlob = array[num4++] + array[num4++] * 256 + array[num4++] * 256 * 256 + array[num4++] * 256 * 256 * 256;
			ze._LastModified = SharedUtilities.PackedToDateTime(ze._TimeBlob);
			ze._timestamp |= ZipEntryTimestamp.DOS;
			ze._Crc32 = array[num4++] + array[num4++] * 256 + array[num4++] * 256 * 256 + array[num4++] * 256 * 256 * 256;
			ze._CompressedSize = (uint)(array[num4++] + array[num4++] * 256 + array[num4++] * 256 * 256 + array[num4++] * 256 * 256 * 256);
			ze._UncompressedSize = (uint)(array[num4++] + array[num4++] * 256 + array[num4++] * 256 * 256 + array[num4++] * 256 * 256 * 256);
			if ((int)ze._CompressedSize == -1 || (int)ze._UncompressedSize == -1)
			{
				ze._InputUsesZip64 = true;
			}
			short num5 = (short)(array[num4++] + array[num4++] * 256);
			short extraFieldLength = (short)(array[num4++] + array[num4++] * 256);
			array = new byte[num5];
			num3 = ze.ArchiveStream.Read(array, 0, array.Length);
			num += num3;
			ze._actualEncoding = (((ze._BitField & 0x800) == 2048) ? Encoding.UTF8 : defaultEncoding);
			ze._FileNameInArchive = ze._actualEncoding.GetString(array, 0, array.Length);
			ze._LocalFileName = ze._FileNameInArchive;
			if (ze._LocalFileName.EndsWith("/"))
			{
				ze.MarkAsDirectory();
			}
			num += ze.ProcessExtraField(extraFieldLength);
			ze._LengthOfTrailer = 0;
			if (!ze._LocalFileName.EndsWith("/") && (ze._BitField & 8) == 8)
			{
				long position = ze.ArchiveStream.Position;
				bool flag = true;
				long num6 = 0L;
				int num7 = 0;
				while (flag)
				{
					num7++;
					ze._zipfile.OnReadBytes(ze);
					long num8 = SharedUtilities.FindSignature(ze.ArchiveStream, 134695760);
					if (num8 == -1)
					{
						return false;
					}
					num6 += num8;
					if (ze._InputUsesZip64)
					{
						array = new byte[20];
						num3 = ze.ArchiveStream.Read(array, 0, array.Length);
						if (num3 != 20)
						{
							return false;
						}
						num4 = 0;
						ze._Crc32 = array[num4++] + array[num4++] * 256 + array[num4++] * 256 * 256 + array[num4++] * 256 * 256 * 256;
						ze._CompressedSize = BitConverter.ToInt64(array, num4);
						num4 += 8;
						ze._UncompressedSize = BitConverter.ToInt64(array, num4);
						num4 += 8;
						ze._LengthOfTrailer += 24;
					}
					else
					{
						array = new byte[12];
						num3 = ze.ArchiveStream.Read(array, 0, array.Length);
						if (num3 != 12)
						{
							return false;
						}
						num4 = 0;
						ze._Crc32 = array[num4++] + array[num4++] * 256 + array[num4++] * 256 * 256 + array[num4++] * 256 * 256 * 256;
						ze._CompressedSize = (uint)(array[num4++] + array[num4++] * 256 + array[num4++] * 256 * 256 + array[num4++] * 256 * 256 * 256);
						ze._UncompressedSize = (uint)(array[num4++] + array[num4++] * 256 + array[num4++] * 256 * 256 + array[num4++] * 256 * 256 * 256);
						ze._LengthOfTrailer += 16;
					}
					flag = (num6 != ze._CompressedSize);
					if (flag)
					{
						ze.ArchiveStream.Seek(-12L, SeekOrigin.Current);
						num6 += 4;
					}
				}
				ze.ArchiveStream.Seek(position, SeekOrigin.Begin);
			}
			ze._CompressedFileDataSize = ze._CompressedSize;
			if ((ze._BitField & 1) == 1)
			{
				ze._WeakEncryptionHeader = new byte[12];
				num += ReadWeakEncryptionHeader(ze._archiveStream, ze._WeakEncryptionHeader);
				ze._CompressedFileDataSize -= 12L;
			}
			ze._LengthOfHeader = num;
			ze._TotalEntrySize = ze._LengthOfHeader + ze._CompressedFileDataSize + ze._LengthOfTrailer;
			return true;
		}

		internal static int ReadWeakEncryptionHeader(Stream s, byte[] buffer)
		{
			int num = s.Read(buffer, 0, 12);
			if (num != 12)
			{
				throw new ZipException($"Unexpected end of data at position 0x{s.Position:X8}");
			}
			return num;
		}

		private static bool IsNotValidSig(int signature)
		{
			return signature != 67324752;
		}

		internal static ZipEntry Read(ZipFile zf, bool first)
		{
			Stream readStream = zf.ReadStream;
			Encoding provisionalAlternateEncoding = zf.ProvisionalAlternateEncoding;
			ZipEntry zipEntry = new ZipEntry();
			zipEntry._Source = ZipEntrySource.ZipFile;
			zipEntry._zipfile = zf;
			zipEntry._archiveStream = readStream;
			zf.OnReadEntry(before: true, null);
			if (first)
			{
				HandlePK00Prefix(readStream);
			}
			if (!ReadHeader(zipEntry, provisionalAlternateEncoding))
			{
				return null;
			}
			zipEntry.__FileDataPosition = zipEntry._zipfile.RelativeOffset;
			readStream.Seek(zipEntry._CompressedFileDataSize + zipEntry._LengthOfTrailer, SeekOrigin.Current);
			HandleUnexpectedDataDescriptor(zipEntry);
			zf.OnReadBytes(zipEntry);
			zf.OnReadEntry(before: false, zipEntry);
			return zipEntry;
		}

		internal static void HandlePK00Prefix(Stream s)
		{
			uint num = (uint)SharedUtilities.ReadInt(s);
			if (num != 808471376)
			{
				s.Seek(-4L, SeekOrigin.Current);
			}
		}

		private static void HandleUnexpectedDataDescriptor(ZipEntry entry)
		{
			Stream archiveStream = entry.ArchiveStream;
			uint num = (uint)SharedUtilities.ReadInt(archiveStream);
			if (num == entry._Crc32)
			{
				int num2 = SharedUtilities.ReadInt(archiveStream);
				if (num2 == entry._CompressedSize)
				{
					num2 = SharedUtilities.ReadInt(archiveStream);
					if (num2 != entry._UncompressedSize)
					{
						archiveStream.Seek(-12L, SeekOrigin.Current);
					}
				}
				else
				{
					archiveStream.Seek(-8L, SeekOrigin.Current);
				}
			}
			else
			{
				archiveStream.Seek(-4L, SeekOrigin.Current);
			}
		}

		internal int ProcessExtraField(short extraFieldLength)
		{
			int num = 0;
			Stream archiveStream = ArchiveStream;
			if (extraFieldLength > 0)
			{
				byte[] array = _Extra = new byte[extraFieldLength];
				num = archiveStream.Read(array, 0, array.Length);
				long posn = archiveStream.Position - num;
				int num2 = 0;
				while (num2 < array.Length)
				{
					int num3 = num2;
					ushort num4 = (ushort)(array[num2] + array[num2 + 1] * 256);
					short num5 = (short)(array[num2 + 2] + array[num2 + 3] * 256);
					num2 += 4;
					switch (num4)
					{
					case 10:
						num2 = ProcessExtraFieldWindowsTimes(array, num2, num5, posn);
						break;
					case 21589:
						num2 = ProcessExtraFieldUnixTimes(array, num2, num5, posn);
						break;
					case 22613:
						num2 = ProcessExtraFieldInfoZipTimes(array, num2, num5, posn);
						break;
					case 1:
						num2 = ProcessExtraFieldZip64(array, num2, num5, posn);
						break;
					case 23:
						num2 = ProcessExtraFieldPkwareStrongEncryption(array, num2);
						break;
					}
					num2 = num3 + num5 + 4;
				}
			}
			return num;
		}

		private int ProcessExtraFieldPkwareStrongEncryption(byte[] Buffer, int j)
		{
			j += 2;
			_UnsupportedAlgorithmId = (ushort)(Buffer[j] + Buffer[j + 1] * 256);
			j += 2;
			_Encryption = EncryptionAlgorithm.Unsupported;
			return j;
		}

		private int ProcessExtraFieldZip64(byte[] Buffer, int j, short DataSize, long posn)
		{
			_InputUsesZip64 = true;
			if (DataSize > 28)
			{
				throw new BadReadException($"  Inconsistent datasize (0x{DataSize:X4}) for ZIP64 extra field at position 0x{posn:X16}");
			}
			int num = DataSize;
			if (_UncompressedSize == uint.MaxValue)
			{
				if (num < 8)
				{
					throw new BadReadException(string.Format("  Missing data for ZIP64 extra field (Uncompressed Size) at position 0x{1:X16}", posn));
				}
				_UncompressedSize = BitConverter.ToInt64(Buffer, j);
				j += 8;
				num -= 8;
			}
			if (_CompressedSize == uint.MaxValue)
			{
				if (num < 8)
				{
					throw new BadReadException(string.Format("  Missing data for ZIP64 extra field (Compressed Size) at position 0x{1:X16}", posn));
				}
				_CompressedSize = BitConverter.ToInt64(Buffer, j);
				j += 8;
				num -= 8;
			}
			if (_RelativeOffsetOfLocalHeader == uint.MaxValue)
			{
				if (num < 8)
				{
					throw new BadReadException(string.Format("  Missing data for ZIP64 extra field (Relative Offset) at position 0x{1:X16}", posn));
				}
				_RelativeOffsetOfLocalHeader = BitConverter.ToInt64(Buffer, j);
				j += 8;
				num -= 8;
			}
			return j;
		}

		private int ProcessExtraFieldInfoZipTimes(byte[] Buffer, int j, short DataSize, long posn)
		{
			if (DataSize != 12 && DataSize != 8)
			{
				throw new BadReadException($"  Unexpected datasize (0x{DataSize:X4}) for InfoZip v1 extra field at position 0x{posn:X16}");
			}
			int num = BitConverter.ToInt32(Buffer, j);
			_Mtime = _unixEpoch.AddSeconds(num);
			j += 4;
			num = BitConverter.ToInt32(Buffer, j);
			_Atime = _unixEpoch.AddSeconds(num);
			j += 4;
			_Ctime = DateTime.UtcNow;
			_ntfsTimesAreSet = true;
			_timestamp |= ZipEntryTimestamp.InfoZip1;
			return j;
		}

		private int ProcessExtraFieldUnixTimes(byte[] Buffer, int j, short DataSize, long posn)
		{
			if (DataSize != 13 && DataSize != 9 && DataSize != 5)
			{
				throw new BadReadException($"  Unexpected datasize (0x{DataSize:X4}) for Extended Timestamp extra field at position 0x{posn:X16}");
			}
			int num = DataSize;
			if (DataSize == 13 || _readExtraDepth > 1)
			{
				byte b = Buffer[j++];
				num--;
				if ((b & 1) != 0 && num >= 4)
				{
					int num2 = BitConverter.ToInt32(Buffer, j);
					_Mtime = _unixEpoch.AddSeconds(num2);
					j += 4;
					num -= 4;
				}
				if ((b & 2) != 0 && num >= 4)
				{
					int num3 = BitConverter.ToInt32(Buffer, j);
					_Atime = _unixEpoch.AddSeconds(num3);
					j += 4;
					num -= 4;
				}
				else
				{
					_Atime = DateTime.UtcNow;
				}
				if ((b & 4) != 0 && num >= 4)
				{
					int num4 = BitConverter.ToInt32(Buffer, j);
					_Ctime = _unixEpoch.AddSeconds(num4);
					j += 4;
					num -= 4;
				}
				else
				{
					_Ctime = DateTime.UtcNow;
				}
				_timestamp |= ZipEntryTimestamp.Unix;
				_ntfsTimesAreSet = true;
				_emitUnixTimes = true;
			}
			else
			{
				ReadExtraField();
			}
			return j;
		}

		private int ProcessExtraFieldWindowsTimes(byte[] Buffer, int j, short DataSize, long posn)
		{
			if (DataSize != 32)
			{
				throw new BadReadException($"  Unexpected datasize (0x{DataSize:X4}) for NTFS times extra field at position 0x{posn:X16}");
			}
			j += 4;
			short num = (short)(Buffer[j] + Buffer[j + 1] * 256);
			short num2 = (short)(Buffer[j + 2] + Buffer[j + 3] * 256);
			j += 4;
			if (num == 1 && num2 == 24)
			{
				long fileTime = BitConverter.ToInt64(Buffer, j);
				_Mtime = DateTime.FromFileTimeUtc(fileTime);
				j += 8;
				fileTime = BitConverter.ToInt64(Buffer, j);
				_Atime = DateTime.FromFileTimeUtc(fileTime);
				j += 8;
				fileTime = BitConverter.ToInt64(Buffer, j);
				_Ctime = DateTime.FromFileTimeUtc(fileTime);
				j += 8;
				_ntfsTimesAreSet = true;
				_timestamp |= ZipEntryTimestamp.Windows;
				_emitNtfsTimes = true;
			}
			return j;
		}

		internal void WriteCentralDirectoryEntry(Stream s)
		{
			_ConsAndWriteCentralDirectoryEntry(s);
		}

		private void _ConsAndWriteCentralDirectoryEntry(Stream s)
		{
			byte[] array = new byte[4096];
			int num = 0;
			array[num++] = 80;
			array[num++] = 75;
			array[num++] = 1;
			array[num++] = 2;
			array[num++] = (byte)(_VersionMadeBy & 0xFF);
			array[num++] = (byte)((_VersionMadeBy & 0xFF00) >> 8);
			short num2 = (short)(_OutputUsesZip64.Value ? 45 : 20);
			array[num++] = (byte)(num2 & 0xFF);
			array[num++] = (byte)((num2 & 0xFF00) >> 8);
			array[num++] = (byte)(_BitField & 0xFF);
			array[num++] = (byte)((_BitField & 0xFF00) >> 8);
			array[num++] = (byte)(CompressionMethod & 0xFF);
			array[num++] = (byte)((CompressionMethod & 0xFF00) >> 8);
			array[num++] = (byte)(_TimeBlob & 0xFF);
			array[num++] = (byte)((_TimeBlob & 0xFF00) >> 8);
			array[num++] = (byte)((_TimeBlob & 0xFF0000) >> 16);
			array[num++] = (byte)((_TimeBlob & 4278190080u) >> 24);
			array[num++] = (byte)(_Crc32 & 0xFF);
			array[num++] = (byte)((_Crc32 & 0xFF00) >> 8);
			array[num++] = (byte)((_Crc32 & 0xFF0000) >> 16);
			array[num++] = (byte)((_Crc32 & 4278190080u) >> 24);
			int num3 = 0;
			if (_OutputUsesZip64.Value)
			{
				for (num3 = 0; num3 < 8; num3++)
				{
					array[num++] = byte.MaxValue;
				}
			}
			else
			{
				array[num++] = (byte)(_CompressedSize & 0xFF);
				array[num++] = (byte)((_CompressedSize & 0xFF00) >> 8);
				array[num++] = (byte)((_CompressedSize & 0xFF0000) >> 16);
				array[num++] = (byte)((_CompressedSize & 4278190080u) >> 24);
				array[num++] = (byte)(_UncompressedSize & 0xFF);
				array[num++] = (byte)((_UncompressedSize & 0xFF00) >> 8);
				array[num++] = (byte)((_UncompressedSize & 0xFF0000) >> 16);
				array[num++] = (byte)((_UncompressedSize & 4278190080u) >> 24);
			}
			byte[] array2 = _GetEncodedFileNameBytes();
			short num4 = (short)array2.Length;
			array[num++] = (byte)(num4 & 0xFF);
			array[num++] = (byte)((num4 & 0xFF00) >> 8);
			_presumeZip64 = _OutputUsesZip64.Value;
			_Extra = ConsExtraField(forCentralDirectory: true);
			short num5 = (short)((_Extra != null) ? _Extra.Length : 0);
			array[num++] = (byte)(num5 & 0xFF);
			array[num++] = (byte)((num5 & 0xFF00) >> 8);
			int num6 = (_CommentBytes != null) ? _CommentBytes.Length : 0;
			if (num6 + num > array.Length)
			{
				num6 = array.Length - num;
			}
			array[num++] = (byte)(num6 & 0xFF);
			array[num++] = (byte)((num6 & 0xFF00) >> 8);
			array[num++] = 0;
			array[num++] = 0;
			array[num++] = (byte)(_IsText ? 1u : 0u);
			array[num++] = 0;
			array[num++] = (byte)(_ExternalFileAttrs & 0xFF);
			array[num++] = (byte)((_ExternalFileAttrs & 0xFF00) >> 8);
			array[num++] = (byte)((_ExternalFileAttrs & 0xFF0000) >> 16);
			array[num++] = (byte)((_ExternalFileAttrs & 4278190080u) >> 24);
			if (_OutputUsesZip64.Value)
			{
				for (num3 = 0; num3 < 4; num3++)
				{
					array[num++] = byte.MaxValue;
				}
			}
			else
			{
				array[num++] = (byte)(_RelativeOffsetOfLocalHeader & 0xFF);
				array[num++] = (byte)((_RelativeOffsetOfLocalHeader & 0xFF00) >> 8);
				array[num++] = (byte)((_RelativeOffsetOfLocalHeader & 0xFF0000) >> 16);
				array[num++] = (byte)((_RelativeOffsetOfLocalHeader & 4278190080u) >> 24);
			}
			for (num3 = 0; num3 < num4; num3++)
			{
				array[num + num3] = array2[num3];
			}
			num += num3;
			if (_Extra != null)
			{
				for (num3 = 0; num3 < num5; num3++)
				{
					array[num + num3] = _Extra[num3];
				}
				num += num3;
			}
			if (num6 != 0)
			{
				for (num3 = 0; num3 < num6 && num + num3 < array.Length; num3++)
				{
					array[num + num3] = _CommentBytes[num3];
				}
				num += num3;
			}
			s.Write(array, 0, num);
		}

		private byte[] ConsExtraField(bool forCentralDirectory)
		{
			List<byte[]> list = new List<byte[]>();
			if (_zipfile._zip64 != 0)
			{
				int num = 4 + (forCentralDirectory ? 28 : 16);
				byte[] array = new byte[num];
				int num2 = 0;
				if (_presumeZip64)
				{
					array[num2++] = 1;
					array[num2++] = 0;
				}
				else
				{
					array[num2++] = 153;
					array[num2++] = 153;
				}
				array[num2++] = (byte)(num - 4);
				array[num2++] = 0;
				Array.Copy(BitConverter.GetBytes(_UncompressedSize), 0, array, num2, 8);
				num2 += 8;
				Array.Copy(BitConverter.GetBytes(_CompressedSize), 0, array, num2, 8);
				if (forCentralDirectory)
				{
					num2 += 8;
					Array.Copy(BitConverter.GetBytes(_RelativeOffsetOfLocalHeader), 0, array, num2, 8);
					num2 += 8;
					Array.Copy(BitConverter.GetBytes(0), 0, array, num2, 4);
				}
				list.Add(array);
			}
			if (_ntfsTimesAreSet && _emitNtfsTimes)
			{
				byte[] array = new byte[36];
				int num3 = 0;
				array[num3++] = 10;
				array[num3++] = 0;
				array[num3++] = 32;
				array[num3++] = 0;
				num3 += 4;
				array[num3++] = 1;
				array[num3++] = 0;
				array[num3++] = 24;
				array[num3++] = 0;
				long value = _Mtime.ToFileTime();
				Array.Copy(BitConverter.GetBytes(value), 0, array, num3, 8);
				num3 += 8;
				value = _Atime.ToFileTime();
				Array.Copy(BitConverter.GetBytes(value), 0, array, num3, 8);
				num3 += 8;
				value = _Ctime.ToFileTime();
				Array.Copy(BitConverter.GetBytes(value), 0, array, num3, 8);
				num3 += 8;
				list.Add(array);
			}
			if (_ntfsTimesAreSet && _emitUnixTimes)
			{
				int num4 = 9;
				if (!forCentralDirectory)
				{
					num4 += 8;
				}
				byte[] array = new byte[num4];
				int num5 = 0;
				array[num5++] = 85;
				array[num5++] = 84;
				array[num5++] = (byte)(num4 - 4);
				array[num5++] = 0;
				array[num5++] = 7;
				int value2 = (int)(_Mtime - _unixEpoch).TotalSeconds;
				Array.Copy(BitConverter.GetBytes(value2), 0, array, num5, 4);
				num5 += 4;
				if (!forCentralDirectory)
				{
					value2 = (int)(_Atime - _unixEpoch).TotalSeconds;
					Array.Copy(BitConverter.GetBytes(value2), 0, array, num5, 4);
					num5 += 4;
					value2 = (int)(_Ctime - _unixEpoch).TotalSeconds;
					Array.Copy(BitConverter.GetBytes(value2), 0, array, num5, 4);
					num5 += 4;
				}
				list.Add(array);
			}
			byte[] array2 = null;
			if (list.Count > 0)
			{
				int num6 = 0;
				int num7 = 0;
				for (int i = 0; i < list.Count; i++)
				{
					num6 += list[i].Length;
				}
				array2 = new byte[num6];
				for (int i = 0; i < list.Count; i++)
				{
					Array.Copy(list[i], 0, array2, num7, list[i].Length);
					num7 += list[i].Length;
				}
			}
			return array2;
		}

		private Encoding GenerateCommentBytes()
		{
			_CommentBytes = ibm437.GetBytes(_Comment);
			string @string = ibm437.GetString(_CommentBytes, 0, _CommentBytes.Length);
			if (@string == _Comment)
			{
				return ibm437;
			}
			_CommentBytes = _provisionalAlternateEncoding.GetBytes(_Comment);
			return _provisionalAlternateEncoding;
		}

		private byte[] _GetEncodedFileNameBytes()
		{
			string text = FileName.Replace("\\", "/");
			string text2 = null;
			if (_TrimVolumeFromFullyQualifiedPaths && FileName.Length >= 3 && FileName[1] == ':' && text[2] == '/')
			{
				text2 = text.Substring(3);
			}
			else if (FileName.Length < 4 || text[0] != '/' || text[1] != '/')
			{
				text2 = ((FileName.Length < 3 || text[0] != '.' || text[1] != '/') ? text : text.Substring(2));
			}
			else
			{
				int num = text.IndexOf('/', 2);
				if (num == -1)
				{
					throw new ArgumentException("The path for that entry appears to be badly formatted");
				}
				text2 = text.Substring(num + 1);
			}
			byte[] bytes = ibm437.GetBytes(text2);
			string @string = ibm437.GetString(bytes, 0, bytes.Length);
			_CommentBytes = null;
			if (@string == text2)
			{
				if (_Comment == null || _Comment.Length == 0)
				{
					_actualEncoding = ibm437;
					return bytes;
				}
				Encoding encoding = GenerateCommentBytes();
				if (encoding.CodePage == 437)
				{
					_actualEncoding = ibm437;
					return bytes;
				}
				_actualEncoding = encoding;
				return encoding.GetBytes(text2);
			}
			bytes = _provisionalAlternateEncoding.GetBytes(text2);
			if (_Comment != null && _Comment.Length != 0)
			{
				_CommentBytes = _provisionalAlternateEncoding.GetBytes(_Comment);
			}
			_actualEncoding = _provisionalAlternateEncoding;
			return bytes;
		}

		private bool WantReadAgain()
		{
			if (_UncompressedSize < 16)
			{
				return false;
			}
			if (_CompressionMethod == 0)
			{
				return false;
			}
			if (_CompressedSize < _UncompressedSize)
			{
				return false;
			}
			if (ForceNoCompression)
			{
				return false;
			}
			if (_Source == ZipEntrySource.Stream && !_sourceStream.CanSeek)
			{
				return false;
			}
			if (_zipCrypto != null && CompressedSize - 12 <= UncompressedSize)
			{
				return false;
			}
			if (WillReadTwiceOnInflation != null)
			{
				return WillReadTwiceOnInflation(_UncompressedSize, _CompressedSize, FileName);
			}
			return true;
		}

		private static bool SeemsCompressible(string filename)
		{
			return !_IncompressibleRegex.IsMatch(filename);
		}

		private bool DefaultWantCompression()
		{
			if (_LocalFileName != null)
			{
				return SeemsCompressible(_LocalFileName);
			}
			if (_FileNameInArchive != null)
			{
				return SeemsCompressible(_FileNameInArchive);
			}
			return true;
		}

		private void FigureCompressionMethodForWriting(int cycle)
		{
			if (cycle > 1)
			{
				_CompressionMethod = 0;
			}
			else if (IsDirectory)
			{
				_CompressionMethod = 0;
			}
			else
			{
				if (__FileDataPosition != -1)
				{
					return;
				}
				if (_Source == ZipEntrySource.Stream)
				{
					if (_sourceStream != null && _sourceStream.CanSeek)
					{
						long length = _sourceStream.Length;
						if (length == 0)
						{
							_CompressionMethod = 0;
							return;
						}
					}
				}
				else
				{
					FileInfo fileInfo = new FileInfo(LocalFileName);
					long length2 = fileInfo.Length;
					if (length2 == 0)
					{
						_CompressionMethod = 0;
						return;
					}
				}
				if (_ForceNoCompression)
				{
					_CompressionMethod = 0;
				}
				else if (WantCompression != null)
				{
					_CompressionMethod = (short)(WantCompression(LocalFileName, _FileNameInArchive) ? 8 : 0);
				}
				else
				{
					_CompressionMethod = (short)(DefaultWantCompression() ? 8 : 0);
				}
			}
		}

		private void WriteHeader(Stream s, int cycle)
		{
			int num = 0;
			_RelativeOffsetOfLocalHeader = ((s as CountingStream)?.BytesWritten ?? s.Position);
			byte[] array = new byte[512];
			int num2 = 0;
			array[num2++] = 80;
			array[num2++] = 75;
			array[num2++] = 3;
			array[num2++] = 4;
			if (_zipfile._zip64 == Zip64Option.Default && (uint)_RelativeOffsetOfLocalHeader >= uint.MaxValue)
			{
				throw new ZipException("Offset within the zip archive exceeds 0xFFFFFFFF. Consider setting the UseZip64WhenSaving property on the ZipFile instance.");
			}
			_presumeZip64 = (_zipfile._zip64 == Zip64Option.Always || (_zipfile._zip64 == Zip64Option.AsNecessary && !s.CanSeek));
			short num3 = (short)(_presumeZip64 ? 45 : 20);
			array[num2++] = (byte)(num3 & 0xFF);
			array[num2++] = (byte)((num3 & 0xFF00) >> 8);
			byte[] array2 = _GetEncodedFileNameBytes();
			short num4 = (short)array2.Length;
			_BitField = (short)(UsesEncryption ? 1 : 0);
			if (ActualEncoding.CodePage == Encoding.UTF8.CodePage)
			{
				_BitField |= 2048;
			}
			if (!s.CanSeek)
			{
				_BitField |= 8;
			}
			array[num2++] = (byte)(_BitField & 0xFF);
			array[num2++] = (byte)((_BitField & 0xFF00) >> 8);
			if (__FileDataPosition == -1)
			{
				_UncompressedSize = 0L;
				_CompressedSize = 0L;
				_Crc32 = 0;
				_crcCalculated = false;
			}
			FigureCompressionMethodForWriting(cycle);
			array[num2++] = (byte)(CompressionMethod & 0xFF);
			array[num2++] = (byte)((CompressionMethod & 0xFF00) >> 8);
			_TimeBlob = SharedUtilities.DateTimeToPacked(LastModified);
			array[num2++] = (byte)(_TimeBlob & 0xFF);
			array[num2++] = (byte)((_TimeBlob & 0xFF00) >> 8);
			array[num2++] = (byte)((_TimeBlob & 0xFF0000) >> 16);
			array[num2++] = (byte)((_TimeBlob & 4278190080u) >> 24);
			array[num2++] = (byte)(_Crc32 & 0xFF);
			array[num2++] = (byte)((_Crc32 & 0xFF00) >> 8);
			array[num2++] = (byte)((_Crc32 & 0xFF0000) >> 16);
			array[num2++] = (byte)((_Crc32 & 4278190080u) >> 24);
			if (_presumeZip64)
			{
				for (num = 0; num < 8; num++)
				{
					array[num2++] = byte.MaxValue;
				}
			}
			else
			{
				array[num2++] = (byte)(_CompressedSize & 0xFF);
				array[num2++] = (byte)((_CompressedSize & 0xFF00) >> 8);
				array[num2++] = (byte)((_CompressedSize & 0xFF0000) >> 16);
				array[num2++] = (byte)((_CompressedSize & 4278190080u) >> 24);
				array[num2++] = (byte)(_UncompressedSize & 0xFF);
				array[num2++] = (byte)((_UncompressedSize & 0xFF00) >> 8);
				array[num2++] = (byte)((_UncompressedSize & 0xFF0000) >> 16);
				array[num2++] = (byte)((_UncompressedSize & 4278190080u) >> 24);
			}
			array[num2++] = (byte)(num4 & 0xFF);
			array[num2++] = (byte)((num4 & 0xFF00) >> 8);
			_Extra = ConsExtraField(forCentralDirectory: false);
			short num5 = (short)((_Extra != null) ? _Extra.Length : 0);
			array[num2++] = (byte)(num5 & 0xFF);
			array[num2++] = (byte)((num5 & 0xFF00) >> 8);
			for (num = 0; num < array2.Length && num2 + num < array.Length; num++)
			{
				array[num2 + num] = array2[num];
			}
			num2 += num;
			if (_Extra != null)
			{
				for (num = 0; num < _Extra.Length; num++)
				{
					array[num2 + num] = _Extra[num];
				}
				num2 += num;
			}
			_LengthOfHeader = num2;
			s.Write(array, 0, num2);
			_EntryHeader = new byte[num2];
			for (num = 0; num < num2; num++)
			{
				_EntryHeader[num] = array[num];
			}
		}

		private int FigureCrc32()
		{
			if (!_crcCalculated)
			{
				Stream stream = null;
				if (_Source == ZipEntrySource.Stream)
				{
					PrepSourceStream();
					stream = _sourceStream;
				}
				else
				{
					stream = File.Open(LocalFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				}
				CRC32 cRC = new CRC32();
				_Crc32 = cRC.GetCrc32(stream);
				if (_sourceStream == null)
				{
					stream.Close();
					stream.Dispose();
				}
				_crcCalculated = true;
			}
			return _Crc32;
		}

		private void PrepSourceStream()
		{
			if (_sourceStream == null)
			{
				throw new ZipException($"The input stream is null for entry '{FileName}'.");
			}
			if (_sourceStreamOriginalPosition.HasValue)
			{
				_sourceStream.Position = _sourceStreamOriginalPosition.Value;
			}
			else if (_sourceStream.CanSeek)
			{
				_sourceStreamOriginalPosition = _sourceStream.Position;
			}
			else if (Encryption == EncryptionAlgorithm.PkzipWeak)
			{
				throw new ZipException("It is not possible to use PKZIP encryption on a non-seekable stream");
			}
		}

		internal void CopyMetaData(ZipEntry source)
		{
			__FileDataPosition = source.__FileDataPosition;
			CompressionMethod = source.CompressionMethod;
			_CompressedFileDataSize = source._CompressedFileDataSize;
			_UncompressedSize = source._UncompressedSize;
			_BitField = source._BitField;
			_Source = source._Source;
			_LastModified = source._LastModified;
			_Mtime = source._Mtime;
			_Atime = source._Atime;
			_Ctime = source._Ctime;
			_ntfsTimesAreSet = source._ntfsTimesAreSet;
			_emitUnixTimes = source._emitUnixTimes;
			_emitNtfsTimes = source._emitNtfsTimes;
		}

		private void _WriteFileData(Stream s)
		{
			Stream stream = null;
			CrcCalculatorStream crcCalculatorStream = null;
			CountingStream countingStream = null;
			try
			{
				__FileDataPosition = s.Position;
			}
			catch
			{
			}
			try
			{
				long totalBytesToXfer = 0L;
				if (_Source == ZipEntrySource.Stream)
				{
					PrepSourceStream();
					stream = _sourceStream;
					try
					{
						totalBytesToXfer = _sourceStream.Length;
					}
					catch (NotSupportedException)
					{
					}
				}
				else
				{
					FileShare fileShare = FileShare.ReadWrite;
					fileShare |= FileShare.Delete;
					FileInfo fileInfo = new FileInfo(LocalFileName);
					totalBytesToXfer = fileInfo.Length;
					stream = File.Open(LocalFileName, FileMode.Open, FileAccess.Read, fileShare);
				}
				crcCalculatorStream = new CrcCalculatorStream(stream);
				countingStream = new CountingStream(s);
				Stream stream2 = countingStream;
				if (Encryption == EncryptionAlgorithm.PkzipWeak)
				{
					stream2 = new ZipCipherStream(countingStream, _zipCrypto, CryptoMode.Encrypt);
				}
				Stream stream3 = null;
				bool flag = false;
				if (CompressionMethod == 8)
				{
					DeflateStream deflateStream = new DeflateStream(stream2, CompressionMode.Compress, _zipfile.CompressionLevel, leaveOpen: true);
					if (_zipfile.CodecBufferSize > 0)
					{
						deflateStream.BufferSize = _zipfile.CodecBufferSize;
					}
					deflateStream.Strategy = _zipfile.Strategy;
					flag = true;
					stream3 = deflateStream;
				}
				else
				{
					stream3 = stream2;
				}
				byte[] array = new byte[BufferSize];
				int count;
				while ((count = SharedUtilities.ReadWithRetry(crcCalculatorStream, array, 0, array.Length, FileName)) != 0)
				{
					stream3.Write(array, 0, count);
					OnWriteBlock(crcCalculatorStream.TotalBytesSlurped, totalBytesToXfer);
					if (_ioOperationCanceled)
					{
						break;
					}
				}
				if (flag)
				{
					stream3.Close();
				}
				stream2.Flush();
				stream2.Close();
				_LengthOfTrailer = 0;
			}
			finally
			{
				if (_Source != ZipEntrySource.Stream && stream != null)
				{
					stream.Close();
					stream.Dispose();
				}
			}
			if (_ioOperationCanceled)
			{
				return;
			}
			_UncompressedSize = crcCalculatorStream.TotalBytesSlurped;
			_CompressedFileDataSize = countingStream.BytesWritten;
			_CompressedSize = _CompressedFileDataSize;
			_Crc32 = crcCalculatorStream.Crc;
			if (_Password != null && Encryption == EncryptionAlgorithm.PkzipWeak)
			{
				_CompressedSize += 12L;
			}
			int num = 8;
			_EntryHeader[num++] = (byte)(CompressionMethod & 0xFF);
			_EntryHeader[num++] = (byte)((CompressionMethod & 0xFF00) >> 8);
			num = 14;
			_EntryHeader[num++] = (byte)(_Crc32 & 0xFF);
			_EntryHeader[num++] = (byte)((_Crc32 & 0xFF00) >> 8);
			_EntryHeader[num++] = (byte)((_Crc32 & 0xFF0000) >> 16);
			_EntryHeader[num++] = (byte)((_Crc32 & 4278190080u) >> 24);
			_entryRequiresZip64 = (_CompressedSize >= uint.MaxValue || _UncompressedSize >= uint.MaxValue || _RelativeOffsetOfLocalHeader >= uint.MaxValue);
			if (_zipfile._zip64 == Zip64Option.Default && _entryRequiresZip64.Value)
			{
				throw new ZipException("Compressed or Uncompressed size, or offset exceeds the maximum value. Consider setting the UseZip64WhenSaving property on the ZipFile instance.");
			}
			_OutputUsesZip64 = (_zipfile._zip64 == Zip64Option.Always || _entryRequiresZip64.Value);
			short num2 = (short)(_EntryHeader[26] + _EntryHeader[27] * 256);
			short num3 = (short)(_EntryHeader[28] + _EntryHeader[29] * 256);
			if (_OutputUsesZip64.Value)
			{
				_EntryHeader[4] = 45;
				_EntryHeader[5] = 0;
				for (int i = 0; i < 8; i++)
				{
					_EntryHeader[num++] = byte.MaxValue;
				}
				num = 30 + num2;
				_EntryHeader[num++] = 1;
				_EntryHeader[num++] = 0;
				num += 2;
				Array.Copy(BitConverter.GetBytes(_UncompressedSize), 0, _EntryHeader, num, 8);
				num += 8;
				Array.Copy(BitConverter.GetBytes(_CompressedSize), 0, _EntryHeader, num, 8);
			}
			else
			{
				_EntryHeader[4] = 20;
				_EntryHeader[5] = 0;
				num = 18;
				_EntryHeader[num++] = (byte)(_CompressedSize & 0xFF);
				_EntryHeader[num++] = (byte)((_CompressedSize & 0xFF00) >> 8);
				_EntryHeader[num++] = (byte)((_CompressedSize & 0xFF0000) >> 16);
				_EntryHeader[num++] = (byte)((_CompressedSize & 4278190080u) >> 24);
				_EntryHeader[num++] = (byte)(_UncompressedSize & 0xFF);
				_EntryHeader[num++] = (byte)((_UncompressedSize & 0xFF00) >> 8);
				_EntryHeader[num++] = (byte)((_UncompressedSize & 0xFF0000) >> 16);
				_EntryHeader[num++] = (byte)((_UncompressedSize & 4278190080u) >> 24);
				if (num3 != 0)
				{
					num = 30 + num2;
					short num4 = (short)(_EntryHeader[num + 2] + _EntryHeader[num + 3] * 256);
					if (num4 == 16)
					{
						_EntryHeader[num++] = 153;
						_EntryHeader[num++] = 153;
					}
				}
			}
			if ((_BitField & 8) != 8)
			{
				s.Seek(_RelativeOffsetOfLocalHeader, SeekOrigin.Begin);
				s.Write(_EntryHeader, 0, _EntryHeader.Length);
				(s as CountingStream)?.Adjust(_EntryHeader.Length);
				s.Seek(_CompressedSize, SeekOrigin.Current);
				return;
			}
			byte[] array2 = new byte[16 + (_OutputUsesZip64.Value ? 8 : 0)];
			num = 0;
			Array.Copy(BitConverter.GetBytes(134695760), 0, array2, num, 4);
			num += 4;
			Array.Copy(BitConverter.GetBytes(_Crc32), 0, array2, num, 4);
			num += 4;
			if (_OutputUsesZip64.Value)
			{
				Array.Copy(BitConverter.GetBytes(_CompressedSize), 0, array2, num, 8);
				num += 8;
				Array.Copy(BitConverter.GetBytes(_UncompressedSize), 0, array2, num, 8);
				num += 8;
			}
			else
			{
				array2[num++] = (byte)(_CompressedSize & 0xFF);
				array2[num++] = (byte)((_CompressedSize & 0xFF00) >> 8);
				array2[num++] = (byte)((_CompressedSize & 0xFF0000) >> 16);
				array2[num++] = (byte)((_CompressedSize & 4278190080u) >> 24);
				array2[num++] = (byte)(_UncompressedSize & 0xFF);
				array2[num++] = (byte)((_UncompressedSize & 0xFF00) >> 8);
				array2[num++] = (byte)((_UncompressedSize & 0xFF0000) >> 16);
				array2[num++] = (byte)((_UncompressedSize & 4278190080u) >> 24);
			}
			s.Write(array2, 0, array2.Length);
			_LengthOfTrailer += array2.Length;
		}

		private void OnZipErrorWhileSaving(Exception e)
		{
			_ioOperationCanceled = _zipfile.OnZipErrorSaving(this, e);
		}

		internal void Write(Stream s)
		{
			bool flag = false;
			do
			{
				if (_Source == ZipEntrySource.ZipFile && !_restreamRequiredOnSave)
				{
					CopyThroughOneEntry(s);
					break;
				}
				try
				{
					if (IsDirectory)
					{
						WriteHeader(s, 1);
						_entryRequiresZip64 = (_RelativeOffsetOfLocalHeader >= uint.MaxValue);
						_OutputUsesZip64 = (_zipfile._zip64 == Zip64Option.Always || _entryRequiresZip64.Value);
						return;
					}
					bool flag2 = true;
					int num = 0;
					do
					{
						num++;
						WriteHeader(s, num);
						_EmitOne(s);
						flag2 = (num <= 1 && s.CanSeek && WantReadAgain());
						if (flag2)
						{
							s.Seek(_RelativeOffsetOfLocalHeader, SeekOrigin.Begin);
							s.SetLength(s.Position);
							(s as CountingStream)?.Adjust(_TotalEntrySize);
						}
					}
					while (flag2);
					_skippedDuringSave = false;
					flag = true;
				}
				catch (Exception ex)
				{
					ZipErrorAction zipErrorAction = ZipErrorAction;
					int num2 = 0;
					while (true)
					{
						if (ZipErrorAction == ZipErrorAction.Throw)
						{
							throw;
						}
						if (ZipErrorAction == ZipErrorAction.Skip || ZipErrorAction == ZipErrorAction.Retry)
						{
							if (!s.CanSeek)
							{
								throw;
							}
							long position = s.Position;
							s.Seek(_RelativeOffsetOfLocalHeader, SeekOrigin.Begin);
							long position2 = s.Position;
							s.SetLength(s.Position);
							(s as CountingStream)?.Adjust(position - position2);
							if (ZipErrorAction == ZipErrorAction.Skip)
							{
								if (_zipfile.StatusMessageTextWriter != null)
								{
									_zipfile.StatusMessageTextWriter.WriteLine("Skipping file {0} (exception: {1})", LocalFileName, ex.ToString());
								}
								_skippedDuringSave = true;
								flag = true;
							}
							else
							{
								ZipErrorAction = zipErrorAction;
							}
							break;
						}
						if (num2 > 0)
						{
							throw;
						}
						if (ZipErrorAction == ZipErrorAction.InvokeErrorEvent)
						{
							OnZipErrorWhileSaving(ex);
							if (_ioOperationCanceled)
							{
								flag = true;
								break;
							}
						}
						num2++;
						bool flag3 = true;
					}
				}
			}
			while (!flag);
		}

		private void _EmitOne(Stream outstream)
		{
			_WriteSecurityMetadata(outstream);
			_WriteFileData(outstream);
			_TotalEntrySize = _LengthOfHeader + _CompressedFileDataSize + _LengthOfTrailer;
		}

		private void _WriteSecurityMetadata(Stream outstream)
		{
			if (_Password != null && Encryption == EncryptionAlgorithm.PkzipWeak)
			{
				_zipCrypto = ZipCrypto.ForWrite(_Password);
				Random random = new Random();
				byte[] array = new byte[12];
				random.NextBytes(array);
				if ((_BitField & 8) == 8)
				{
					_TimeBlob = SharedUtilities.DateTimeToPacked(LastModified);
					array[11] = (byte)((_TimeBlob >> 8) & 0xFF);
				}
				else
				{
					FigureCrc32();
					array[11] = (byte)((_Crc32 >> 24) & 0xFF);
				}
				byte[] array2 = _zipCrypto.EncryptMessage(array, array.Length);
				outstream.Write(array2, 0, array2.Length);
				_LengthOfHeader += array2.Length;
			}
		}

		private void CopyThroughOneEntry(Stream outstream)
		{
			if (LengthOfHeader == 0)
			{
				throw new BadStateException("Bad header length.");
			}
			CrcCalculatorStream input = new CrcCalculatorStream(ArchiveStream);
			if (_metadataChanged || (_InputUsesZip64 && _zipfile.UseZip64WhenSaving == Zip64Option.Default) || (!_InputUsesZip64 && _zipfile.UseZip64WhenSaving == Zip64Option.Always))
			{
				CopyThroughWithRecompute(outstream, input);
			}
			else
			{
				CopyThroughWithNoChange(outstream, input);
			}
			_entryRequiresZip64 = (_CompressedSize >= uint.MaxValue || _UncompressedSize >= uint.MaxValue || _RelativeOffsetOfLocalHeader >= uint.MaxValue);
			_OutputUsesZip64 = (_zipfile._zip64 == Zip64Option.Always || _entryRequiresZip64.Value);
		}

		private void CopyThroughWithRecompute(Stream outstream, CrcCalculatorStream input1)
		{
			byte[] array = new byte[BufferSize];
			Stream archiveStream = ArchiveStream;
			long relativeOffsetOfLocalHeader = _RelativeOffsetOfLocalHeader;
			int lengthOfHeader = LengthOfHeader;
			WriteHeader(outstream, 0);
			if (!FileName.EndsWith("/"))
			{
				long num = relativeOffsetOfLocalHeader + lengthOfHeader;
				num -= LengthOfCryptoHeaderBytes;
				_LengthOfHeader += LengthOfCryptoHeaderBytes;
				_zipfile.SeekFromOrigin(num);
				long num2 = _CompressedSize;
				while (num2 > 0)
				{
					int count = (int)((num2 > array.Length) ? array.Length : num2);
					int num3 = input1.Read(array, 0, count);
					outstream.Write(array, 0, num3);
					num2 -= num3;
					OnWriteBlock(input1.TotalBytesSlurped, _CompressedSize);
					if (_ioOperationCanceled)
					{
						break;
					}
				}
				if ((_BitField & 8) == 8)
				{
					int num4 = 16;
					if (_InputUsesZip64)
					{
						num4 += 8;
					}
					byte[] buffer = new byte[num4];
					archiveStream.Read(buffer, 0, num4);
					if (_InputUsesZip64 && _zipfile.UseZip64WhenSaving == Zip64Option.Default)
					{
						outstream.Write(buffer, 0, 8);
						if (_CompressedSize > uint.MaxValue)
						{
							throw new InvalidOperationException("ZIP64 is required");
						}
						outstream.Write(buffer, 8, 4);
						if (_UncompressedSize > uint.MaxValue)
						{
							throw new InvalidOperationException("ZIP64 is required");
						}
						outstream.Write(buffer, 16, 4);
						_LengthOfTrailer -= 8;
					}
					else if (!_InputUsesZip64 && _zipfile.UseZip64WhenSaving == Zip64Option.Always)
					{
						byte[] buffer2 = new byte[4];
						outstream.Write(buffer, 0, 8);
						outstream.Write(buffer, 8, 4);
						outstream.Write(buffer2, 0, 4);
						outstream.Write(buffer, 12, 4);
						outstream.Write(buffer2, 0, 4);
						_LengthOfTrailer += 8;
					}
					else
					{
						outstream.Write(buffer, 0, num4);
					}
				}
			}
			_TotalEntrySize = _LengthOfHeader + _CompressedFileDataSize + _LengthOfTrailer;
		}

		private void CopyThroughWithNoChange(Stream outstream, CrcCalculatorStream input1)
		{
			byte[] array = new byte[BufferSize];
			_zipfile.SeekFromOrigin(_RelativeOffsetOfLocalHeader);
			if (_TotalEntrySize == 0)
			{
				_TotalEntrySize = _LengthOfHeader + _CompressedFileDataSize + _LengthOfTrailer;
			}
			_RelativeOffsetOfLocalHeader = ((outstream as CountingStream)?.BytesWritten ?? outstream.Position);
			long num = _TotalEntrySize;
			while (num > 0)
			{
				int count = (int)((num > array.Length) ? array.Length : num);
				int num2 = input1.Read(array, 0, count);
				outstream.Write(array, 0, num2);
				num -= num2;
				OnWriteBlock(input1.TotalBytesSlurped, _TotalEntrySize);
				if (_ioOperationCanceled)
				{
					break;
				}
			}
		}
	}
}
