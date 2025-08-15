using System;

namespace Updater.Localization
{
	public class LocStringTwoParams : LocString
	{
		public string Param1
		{
			get;
			set;
		}

		private string Param2
		{
			get;
			set;
		}

		public override string GetLocStr => LangInfo.Lang switch
		{
			Languages.Rus => string.Format(_rusStr, Param1, Param2), 
			Languages.Eng => string.Format(_engStr, Param1, Param2), 
			_ => throw new ArgumentOutOfRangeException(), 
		};

		public LocStringTwoParams(string rusStr, string engStr)
			: base(rusStr, engStr)
		{
		}
	}
}
