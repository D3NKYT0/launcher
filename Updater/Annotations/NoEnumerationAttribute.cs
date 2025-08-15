using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class NoEnumerationAttribute : Attribute
	{
	}
}
