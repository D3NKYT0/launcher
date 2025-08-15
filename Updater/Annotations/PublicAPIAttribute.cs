using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.All)]
	public sealed class PublicAPIAttribute : Attribute
	{
		[CanBeNull]
		public string? Comment
		{
			get;
			set;
		}
	}
}
