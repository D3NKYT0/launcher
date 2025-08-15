using System;

namespace Updater.HashZip.ZIPLib.Zip
{
	[Flags]
	public enum ZipEntryTimestamp
	{
		None = 0x0,
		DOS = 0x1,
		Windows = 0x2,
		Unix = 0x4,
		InfoZip1 = 0x8
	}
}
