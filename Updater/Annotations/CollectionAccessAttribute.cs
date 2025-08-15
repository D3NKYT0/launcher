using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property)]
	public sealed class CollectionAccessAttribute : Attribute
	{
		public CollectionAccessType CollectionAccessType
		{
			get;
			private set;
		}

		public CollectionAccessAttribute(CollectionAccessType collectionAccessType)
		{
			CollectionAccessType = collectionAccessType;
		}
	}
}
