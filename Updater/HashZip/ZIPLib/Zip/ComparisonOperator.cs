using System.ComponentModel;

namespace Updater.HashZip.ZIPLib.Zip
{
	internal enum ComparisonOperator
	{
		[Description(">")]
		GreaterThan,
		[Description(">=")]
		GreaterThanOrEqualTo,
		[Description("<")]
		LesserThan,
		[Description("<=")]
		LesserThanOrEqualTo,
		[Description("=")]
		EqualTo,
		[Description("!=")]
		NotEqualTo
	}
}
