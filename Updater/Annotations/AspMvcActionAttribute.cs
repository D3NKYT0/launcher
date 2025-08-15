using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
	public sealed class AspMvcActionAttribute : Attribute
	{
		[CanBeNull]
		public string AnonymousProperty
		{
			get;
			private set;
		}

		public AspMvcActionAttribute()
		{
		}

		public AspMvcActionAttribute([NotNull] string anonymousProperty)
		{
			AnonymousProperty = anonymousProperty;
		}
	}
}
