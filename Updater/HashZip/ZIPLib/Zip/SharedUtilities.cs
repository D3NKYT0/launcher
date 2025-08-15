using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Updater.HashZip.ZIPLib.Zip
{
	internal class SharedUtilities
	{
		private static Encoding ibm437 = Encoding.GetEncoding("IBM437");

		private static Encoding utf8 = Encoding.GetEncoding("UTF-8");

		private static Random _rnd = new Random();

		private SharedUtilities()
		{
		}

		internal static string NormalizePath(string path)
		{
			if (path.StartsWith(".\\"))
			{
				path = path.Substring(2);
			}
			path = path.Replace("\\.\\", "\\");
			Regex regex = new Regex("^(.*\\\\)?([^\\\\\\.]+\\\\\\.\\.\\\\)(.+)$");
			path = regex.Replace(path, "$1$3");
			return path;
		}

		internal static string NormalizeFwdSlashPath(string path)
		{
			if (path.StartsWith("./"))
			{
				path = path.Substring(2);
			}
			path = path.Replace("/./", "/");
			Regex regex = new Regex("^(.*/)b?([^/\\\\.]+/\\\\.\\\\./)(.+)$");
			path = regex.Replace(path, "$1$3");
			return path;
		}

		public static string TrimVolumeAndSwapSlashes(string pathName)
		{
			if (string.IsNullOrEmpty(pathName))
			{
				return pathName;
			}
			if (pathName.Length < 2)
			{
				return pathName.Replace('\\', '/');
			}
			return ((pathName[1] == ':' && pathName[2] == '\\') ? pathName.Substring(3) : pathName).Replace('\\', '/');
		}

		internal static byte[] StringToByteArray(string value, Encoding encoding)
		{
			return encoding.GetBytes(value);
		}

		internal static byte[] StringToByteArray(string value)
		{
			return StringToByteArray(value, ibm437);
		}

		internal static string Utf8StringFromBuffer(byte[] buf)
		{
			return StringFromBuffer(buf, utf8);
		}

		internal static string StringFromBuffer(byte[] buf, Encoding encoding)
		{
			return encoding.GetString(buf, 0, buf.Length);
		}

		internal static int ReadSignature(Stream s)
		{
			int result = 0;
			try
			{
				result = _ReadFourBytes(s, "nul");
			}
			catch (BadReadException)
			{
			}
			return result;
		}

		internal static int ReadInt(Stream s)
		{
			return _ReadFourBytes(s, "Could not read block - no data!  (position 0x{0:X8})");
		}

		private static int _ReadFourBytes(Stream s, string message)
		{
			int num = 0;
			byte[] array = new byte[4];
			num = s.Read(array, 0, array.Length);
			if (num != array.Length)
			{
				throw new BadReadException(string.Format(message, s.Position));
			}
			return ((array[3] * 256 + array[2]) * 256 + array[1]) * 256 + array[0];
		}

		protected internal static long FindSignature(Stream stream, int SignatureToFind)
		{
			long position = stream.Position;
			int num = 65536;
			byte[] array = new byte[4]
			{
				(byte)(SignatureToFind >> 24),
				(byte)((SignatureToFind & 0xFF0000) >> 16),
				(byte)((SignatureToFind & 0xFF00) >> 8),
				(byte)(SignatureToFind & 0xFF)
			};
			byte[] array2 = new byte[num];
			int num2 = 0;
			bool flag = false;
			while (true)
			{
				num2 = stream.Read(array2, 0, array2.Length);
				if (num2 == 0)
				{
					break;
				}
				for (int i = 0; i < num2; i++)
				{
					if (array2[i] == array[3])
					{
						long position2 = stream.Position;
						stream.Seek(i - num2, SeekOrigin.Current);
						int num3 = ReadSignature(stream);
						flag = (num3 == SignatureToFind);
						if (flag)
						{
							break;
						}
						stream.Seek(position2, SeekOrigin.Begin);
					}
				}
				if (flag)
				{
					break;
				}
				bool flag2 = true;
			}
			if (!flag)
			{
				stream.Seek(position, SeekOrigin.Begin);
				return -1L;
			}
			return stream.Position - position - 4;
		}

		internal static DateTime AdjustTime_DotNetToWin32(DateTime time)
		{
			if (time.Kind == DateTimeKind.Utc)
			{
				return time;
			}
			DateTime result = time;
			if (DateTime.Now.IsDaylightSavingTime() && !time.IsDaylightSavingTime())
			{
				result = time - new TimeSpan(1, 0, 0);
			}
			else if (!DateTime.Now.IsDaylightSavingTime() && time.IsDaylightSavingTime())
			{
				result = time + new TimeSpan(1, 0, 0);
			}
			return result;
		}

		internal static DateTime AdjustTime_Win32ToDotNet(DateTime time)
		{
			if (time.Kind == DateTimeKind.Utc)
			{
				return time;
			}
			DateTime result = time;
			if (DateTime.Now.IsDaylightSavingTime() && !time.IsDaylightSavingTime())
			{
				result = time + new TimeSpan(1, 0, 0);
			}
			else if (!DateTime.Now.IsDaylightSavingTime() && time.IsDaylightSavingTime())
			{
				result = time - new TimeSpan(1, 0, 0);
			}
			return result;
		}

		internal static DateTime PackedToDateTime(int packedDateTime)
		{
			if (packedDateTime == 65535 || packedDateTime == 0)
			{
				return new DateTime(1995, 1, 1, 0, 0, 0, 0);
			}
			short num = (short)(packedDateTime & 0xFFFF);
			short num2 = (short)((packedDateTime & 4294901760u) >> 16);
			int num3 = 1980 + ((num2 & 0xFE00) >> 9);
			int num4 = (num2 & 0x1E0) >> 5;
			int num5 = num2 & 0x1F;
			int num6 = (num & 0xF800) >> 11;
			int num7 = (num & 0x7E0) >> 5;
			int num8 = (num & 0x1F) * 2;
			if (num8 >= 60)
			{
				num7++;
				num8 = 0;
			}
			if (num7 >= 60)
			{
				num6++;
				num7 = 0;
			}
			if (num6 >= 24)
			{
				num5++;
				num6 = 0;
			}
			DateTime value = DateTime.Now;
			bool flag = false;
			try
			{
				value = new DateTime(num3, num4, num5, num6, num7, num8, 0);
				flag = true;
			}
			catch (ArgumentOutOfRangeException)
			{
				if (num3 == 1980 && num4 == 0 && num5 == 0)
				{
					try
					{
						value = new DateTime(1980, 1, 1, num6, num7, num8, 0);
						flag = true;
					}
					catch (ArgumentOutOfRangeException)
					{
						try
						{
							value = new DateTime(1980, 1, 1, 0, 0, 0, 0);
							flag = true;
						}
						catch (ArgumentOutOfRangeException)
						{
						}
					}
				}
			}
			if (!flag)
			{
				string arg = $"y({num3}) m({num4}) d({num5}) h({num6}) m({num7}) s({num8})";
				throw new ZipException($"Bad date/time format in the zip file. ({arg})");
			}
			return DateTime.SpecifyKind(value, DateTimeKind.Local);
		}

		internal static int DateTimeToPacked(DateTime time)
		{
			time = time.ToLocalTime();
			time = AdjustTime_Win32ToDotNet(time);
			ushort num = (ushort)((time.Day & 0x1F) | ((time.Month << 5) & 0x1E0) | ((time.Year - 1980 << 9) & 0xFE00));
			ushort num2 = (ushort)(((time.Second / 2) & 0x1F) | ((time.Minute << 5) & 0x7E0) | ((time.Hour << 11) & 0xF800));
			return (num << 16) | num2;
		}

		public static string GetTempFilename()
		{
			string text = null;
			do
			{
				text = "DotNetZip-" + GenerateRandomStringImpl(8, 97) + ".tmp";
			}
			while (File.Exists(text));
			return text;
		}

		private static string GenerateRandomStringImpl(int length, int delta)
		{
			bool flag = delta == 0;
			string text = "";
			char[] array = new char[length];
			for (int i = 0; i < length; i++)
			{
				if (flag)
				{
					delta = ((_rnd.Next(2) == 0) ? 65 : 97);
				}
				array[i] = GetOneRandomChar(delta);
			}
			return new string(array);
		}

		private static char GetOneRandomChar(int delta)
		{
			return (char)(_rnd.Next(26) + delta);
		}

		internal static int ReadWithRetry(Stream s, byte[] buffer, int offset, int count, string FileName)
		{
			int result = 0;
			bool flag = false;
			int num = 0;
			do
			{
				try
				{
					result = s.Read(buffer, offset, count);
					flag = true;
				}
				catch (IOException ex)
				{
					SecurityPermission securityPermission = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);
					if (securityPermission.IsUnrestricted())
					{
						uint num2 = _HRForException(ex);
						if (num2 != 2147942433u)
						{
							throw new IOException($"Cannot read file {FileName}", ex);
						}
						num++;
						if (num > 10)
						{
							throw new IOException($"Cannot read file {FileName}, at offset 0x{offset:X8} after 10 retries", ex);
						}
						Thread.Sleep(250 + num * 550);
						continue;
					}
					throw;
				}
			}
			while (!flag);
			return result;
		}

		private static uint _HRForException(Exception ex1)
		{
			return (uint)Marshal.GetHRForException(ex1);
		}
	}
}
