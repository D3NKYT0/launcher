using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
	public sealed class AspMvcPartialViewLocationFormatAttribute : Attribute
	{
		[NotNull]
		public string Format
		{
			get;
			private set;
		}

		public AspMvcPartialViewLocationFormatAttribute([NotNull] string format)
		{
			Format = format;
		}
	}
}
