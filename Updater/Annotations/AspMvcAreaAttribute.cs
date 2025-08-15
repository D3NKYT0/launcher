using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class AspMvcAreaAttribute : Attribute
	{
		[CanBeNull]
		public string? AnonymousProperty
		{
			get;
			set;
		}
	}
}
