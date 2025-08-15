using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class RazorPageBaseTypeAttribute : Attribute
	{
		[NotNull]
		public string BaseType
		{
			get;
			private set;
		}

		[CanBeNull]
		public string? PageName
		{
			get;
			set;
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
