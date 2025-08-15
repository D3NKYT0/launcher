using System;

namespace Updater.Annotations
{
	[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
	public sealed class PublicAPIAttribute : Attribute
	{
		[CanBeNull]
		public string Comment
		{
			get;
			private set;
		}

		public PublicAPIAttribute()
		{
		}

		public PublicAPIAttribute([NotNull] string comment)
		{
			Comment = comment;
		}
	}
}
