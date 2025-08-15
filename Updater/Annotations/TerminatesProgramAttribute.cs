using System;

namespace Updater.Annotations
{
	[Obsolete("Use [ContractAnnotation('=> halt')] instead")]
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class TerminatesProgramAttribute : Attribute
	{
	}
}
