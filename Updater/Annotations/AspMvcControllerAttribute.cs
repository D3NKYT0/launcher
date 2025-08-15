using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class AspMvcControllerAttribute : Attribute
	{
		[CanBeNull]
		public string? AnonymousProperty
		{
			get;
			set;
		}
	}
}
