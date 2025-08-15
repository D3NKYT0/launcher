using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class AspTypePropertyAttribute : Attribute
	{
		public bool CreateConstructorReferences
		{
			get;
			private set;
		}

		public AspTypePropertyAttribute(bool createConstructorReferences)
		{
			CreateConstructorReferences = createConstructorReferences;
		}
	}
}
