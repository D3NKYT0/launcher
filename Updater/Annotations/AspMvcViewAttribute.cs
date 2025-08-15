using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
	public sealed class AspMvcViewAttribute : Attribute
	{
	}
}
