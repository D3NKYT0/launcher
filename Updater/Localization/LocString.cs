using System;

namespace Updater.Localization
{
	public class LocString
	{
		protected string _rusStr;

		protected string _engStr;

		public virtual string GetLocStr => LangInfo.Lang switch
		{
			Languages.Rus => _rusStr, 
			Languages.Eng => _engStr, 
			_ => throw new ArgumentOutOfRangeException(), 
		};

		public string UpperString => GetLocStr.ToUpperInvariant();

		public LocString(string rusStr, string engStr)
		{
			_rusStr = rusStr;
			_engStr = engStr;
		}

		public override string ToString()
		{
			return GetLocStr;
		}
	}
}
