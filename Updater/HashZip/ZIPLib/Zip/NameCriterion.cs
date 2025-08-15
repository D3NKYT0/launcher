using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Updater.HashZip.ZIPLib.Zip
{
	internal class NameCriterion : SelectionCriterion
	{
		private Regex _re;

		private string _regexString;

		internal ComparisonOperator Operator;

		private string _MatchingFileSpec;

		internal virtual string MatchingFileSpec
		{
			set
			{
				if (Directory.Exists(value))
				{
					_MatchingFileSpec = value + "\\*.*";
				}
				else
				{
					_MatchingFileSpec = value;
				}
				_regexString = "^" + Regex.Escape(_MatchingFileSpec).Replace("\\*\\.\\*", "([^\\.]+|.*\\.[^\\\\\\.]*)").Replace("\\.\\*", "\\.[^\\\\\\.]*")
					.Replace("\\*", ".*")
					.Replace("\\?", "[^\\\\\\.]") + "$";
				_re = new Regex(_regexString, RegexOptions.IgnoreCase);
			}
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("name = ").Append(_MatchingFileSpec);
			return stringBuilder.ToString();
		}

		internal override bool Evaluate(string filename)
		{
			return _Evaluate(filename);
		}

		private bool _Evaluate(string fullpath)
		{
			string input = (_MatchingFileSpec.IndexOf('\\') == -1) ? Path.GetFileName(fullpath) : fullpath;
			bool flag = _re.IsMatch(input);
			if (Operator != ComparisonOperator.EqualTo)
			{
				flag = !flag;
			}
			return flag;
		}

		internal override bool Evaluate(ZipEntry entry)
		{
			string fullpath = entry.FileName.Replace("/", "\\");
			return _Evaluate(fullpath);
		}
	}
}
