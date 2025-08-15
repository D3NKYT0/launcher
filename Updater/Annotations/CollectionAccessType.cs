using System;

namespace Updater.Annotations
{
	[Flags]
	public enum CollectionAccessType
	{
		None = 0x0,
		Read = 0x1,
		ModifyExistingContent = 0x2,
		UpdatedContent = 0x6
	}
}
