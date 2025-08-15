using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public sealed class AspMvcSuppressViewErrorAttribute : Attribute
	{
	}
}
