using System;
using System.Runtime.InteropServices;

namespace Updater.HashZip.ZIPLib
{
	[Guid("ebc25cf6-9120-4283-b972-0e5520d0000E")]
	public class ZlibException : Exception
	{
		public ZlibException()
		{
		}

		public ZlibException(string s)
			: base(s)
		{
		}
	}
}
