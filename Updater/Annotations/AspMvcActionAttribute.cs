using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class AspMvcActionAttribute : Attribute
	{
		[CanBeNull]
		public string? AnonymousProperty
		{
			get;
			set;
		}
	}
}
