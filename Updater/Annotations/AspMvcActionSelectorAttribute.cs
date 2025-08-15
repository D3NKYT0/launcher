using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
	public sealed class AspMvcActionSelectorAttribute : Attribute
	{
	}
}
