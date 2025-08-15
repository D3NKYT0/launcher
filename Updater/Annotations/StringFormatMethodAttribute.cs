using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Delegate)]
	public sealed class StringFormatMethodAttribute : Attribute
	{
		[NotNull]
		public string FormatParameterName
		{
			get;
			private set;
		}

		public StringFormatMethodAttribute([NotNull] string formatParameterName)
		{
			FormatParameterName = formatParameterName;
		}
	}
}
