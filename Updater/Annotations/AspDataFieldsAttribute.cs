using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
	public sealed class AspDataFieldsAttribute : Attribute
	{
	}
}
