using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class HtmlElementAttributesAttribute : Attribute
	{
		[CanBeNull]
		public string? Name
		{
			get;
			set;
		}
	}
}
