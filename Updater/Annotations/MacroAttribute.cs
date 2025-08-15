using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property)]
	public sealed class MacroAttribute : Attribute
	{
		[CanBeNull]
		public string? Expression
		{
			get;
			set;
		}

		public int Editable
		{
			get;
			set;
		}

		[CanBeNull]
		public string? Target
		{
			get;
			set;
		}
	}
}
