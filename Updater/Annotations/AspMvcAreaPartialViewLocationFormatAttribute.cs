using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
	public sealed class AspMvcAreaPartialViewLocationFormatAttribute : Attribute
	{
		[NotNull]
		public string Format
		{
			get;
			private set;
		}

		public AspMvcAreaPartialViewLocationFormatAttribute([NotNull] string format)
		{
			Format = format;
		}
	}
}
