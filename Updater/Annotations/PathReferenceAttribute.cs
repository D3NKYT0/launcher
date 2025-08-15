using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class PathReferenceAttribute : Attribute
	{
		[CanBeNull]
		public string BasePath
		{
			get;
			private set;
		}

		public PathReferenceAttribute()
		{
		}

		public PathReferenceAttribute([NotNull][PathReference] string basePath)
		{
			BasePath = basePath;
		}
	}
}
