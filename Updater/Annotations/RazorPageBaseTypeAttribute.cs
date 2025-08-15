using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public sealed class RazorPageBaseTypeAttribute : Attribute
	{
		[NotNull]
		public string BaseType
		{
			get;
			private set;
		}

		[CanBeNull]
		public string PageName
		{
			get;
			private set;
		}

		public RazorPageBaseTypeAttribute([NotNull] string baseType)
		{
			BaseType = baseType;
		}

		public RazorPageBaseTypeAttribute([NotNull] string baseType, string pageName)
		{
			BaseType = baseType;
			PageName = pageName;
		}
	}
}
