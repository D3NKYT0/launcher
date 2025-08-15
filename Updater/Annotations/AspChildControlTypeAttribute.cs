using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public sealed class AspChildControlTypeAttribute : Attribute
	{
		[NotNull]
		public string TagName
		{
			get;
			private set;
		}

		[NotNull]
		public Type ControlType
		{
			get;
			private set;
		}

		public AspChildControlTypeAttribute([NotNull] string tagName, [NotNull] Type controlType)
		{
			TagName = tagName;
			ControlType = controlType;
		}
	}
}
