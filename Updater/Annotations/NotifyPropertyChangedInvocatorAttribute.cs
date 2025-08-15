using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class NotifyPropertyChangedInvocatorAttribute : Attribute
	{
		[CanBeNull]
		public string? ParameterName
		{
			get;
			set;
		}

		public NotifyPropertyChangedInvocatorAttribute()
		{
		}

		public NotifyPropertyChangedInvocatorAttribute([NotNull] string parameterName)
		{
			ParameterName = parameterName;
		}
	}
}
