using System;

namespace Updater.Annotations
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public sealed class ContractAnnotationAttribute : Attribute
	{
		[NotNull]
		public string Contract
		{
			get;
			private set;
		}

		public bool ForceFullStates
		{
			get;
			private set;
		}

		public ContractAnnotationAttribute([NotNull] string contract)
			: this(contract, forceFullStates: false)
		{
		}

		public ContractAnnotationAttribute([NotNull] string contract, bool forceFullStates)
		{
			Contract = contract;
			ForceFullStates = forceFullStates;
		}
	}
}
