using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class AspMvcAreaAttribute : Attribute
	{
		[CanBeNull]
		public string AnonymousProperty
		{
			get;
			private set;
		}

		public AspMvcAreaAttribute()
		{
		}

		public AspMvcAreaAttribute([NotNull] string anonymousProperty)
		{
			AnonymousProperty = anonymousProperty;
		}
	}
}
