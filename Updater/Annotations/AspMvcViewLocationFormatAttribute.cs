using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
	public sealed class AspMvcViewLocationFormatAttribute : Attribute
	{
		[NotNull]
		public string Format
		{
			get;
			private set;
		}

		public AspMvcViewLocationFormatAttribute([NotNull] string format)
		{
			Format = format;
		}
	}
}
