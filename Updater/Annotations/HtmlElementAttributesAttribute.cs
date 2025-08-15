using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
	public sealed class HtmlElementAttributesAttribute : Attribute
	{
		[CanBeNull]
		public string Name
		{
			get;
			private set;
		}

		public HtmlElementAttributesAttribute()
		{
		}

		public HtmlElementAttributesAttribute([NotNull] string name)
		{
			Name = name;
		}
	}
}
