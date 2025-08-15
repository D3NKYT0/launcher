using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;

namespace Updater.HashZip.ZIPLib.Zip
{
	public class FileSelector
	{
		private enum ParseState
		{
			Start,
			OpenParen,
			CriterionDone,
			ConjunctionPending,
			Whitespace
		}

		internal SelectionCriterion _Criterion;

		public string SelectionCriteria
		{
			get
			{
				if (_Criterion == null)
				{
					return null;
				}
				return _Criterion.ToString();
			}
			set
			{
				if (value == null)
				{
					_Criterion = null;
				}
				else if (value.Trim() == "")
				{
					_Criterion = null;
				}
				else
				{
					_Criterion = _ParseCriterion(value);
				}
			}
		}

		public FileSelector(string selectionCriteria)
		{
			if (!string.IsNullOrEmpty(selectionCriteria))
			{
				_Criterion = _ParseCriterion(selectionCriteria);
			}
		}

		private static SelectionCriterion _ParseCriterion(string s)
		{
			if (s == null)
			{
				return null;
			}
			if (s.IndexOf(" ") == -1)
			{
				s = "name = " + s;
			}
			string[] array = new string[4]
			{
				"\\((\\S)",
				"( $1",
				"(\\S)\\)",
				"$1 )"
			};
			for (int i = 0; i + 1 < array.Length; i += 2)
			{
				Regex regex = new Regex(array[i]);
				s = regex.Replace(s, array[i + 1]);
			}
			string[] array2 = s.Trim().Split(' ', '\t');
			if (array2.Length < 3)
			{
				throw new ArgumentException(s);
			}
			SelectionCriterion selectionCriterion = null;
			LogicalConjunction logicalConjunction = LogicalConjunction.NONE;
			Stack<ParseState> stack = new Stack<ParseState>();
			Stack<SelectionCriterion> stack2 = new Stack<SelectionCriterion>();
			stack.Push(ParseState.Start);
			for (int j = 0; j < array2.Length; j++)
			{
				ParseState parseState;
				switch (array2[j].ToLower())
				{
				case "and":
				case "xor":
				case "or":
					parseState = stack.Peek();
					if (parseState != ParseState.CriterionDone)
					{
						throw new ArgumentException(string.Join(" ", array2, j, array2.Length - j));
					}
					if (array2.Length <= j + 3)
					{
						throw new ArgumentException(string.Join(" ", array2, j, array2.Length - j));
					}
					logicalConjunction = (LogicalConjunction)Enum.Parse(typeof(LogicalConjunction), array2[j].ToUpper());
					selectionCriterion = new CompoundCriterion
					{
						Left = selectionCriterion,
						Right = null,
						Conjunction = logicalConjunction
					};
					stack.Push(parseState);
					stack.Push(ParseState.ConjunctionPending);
					stack2.Push(selectionCriterion);
					break;
				case "(":
					parseState = stack.Peek();
					if (parseState != 0 && parseState != ParseState.ConjunctionPending && parseState != ParseState.OpenParen)
					{
						throw new ArgumentException(string.Join(" ", array2, j, array2.Length - j));
					}
					if (array2.Length <= j + 4)
					{
						throw new ArgumentException(string.Join(" ", array2, j, array2.Length - j));
					}
					stack.Push(ParseState.OpenParen);
					break;
				case ")":
					parseState = stack.Pop();
					if (stack.Peek() != ParseState.OpenParen)
					{
						throw new ArgumentException(string.Join(" ", array2, j, array2.Length - j));
					}
					stack.Pop();
					stack.Push(ParseState.CriterionDone);
					break;
				case "atime":
				case "ctime":
				case "mtime":
				{
					if (array2.Length <= j + 2)
					{
						throw new ArgumentException(string.Join(" ", array2, j, array2.Length - j));
					}
					DateTime value;
					try
					{
						value = DateTime.ParseExact(array2[j + 2], "yyyy-MM-dd-HH:mm:ss", null);
					}
					catch (FormatException)
					{
						value = DateTime.ParseExact(array2[j + 2], "yyyy-MM-dd", null);
					}
					value = DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();
					TimeCriterion timeCriterion = new TimeCriterion();
					timeCriterion.Which = (WhichTime)Enum.Parse(typeof(WhichTime), array2[j]);
					timeCriterion.Operator = (ComparisonOperator)EnumUtil.Parse(typeof(ComparisonOperator), array2[j + 1]);
					timeCriterion.Time = value;
					selectionCriterion = timeCriterion;
					j += 2;
					stack.Push(ParseState.CriterionDone);
					break;
				}
				case "length":
				case "size":
				{
					if (array2.Length <= j + 2)
					{
						throw new ArgumentException(string.Join(" ", array2, j, array2.Length - j));
					}
					long num2 = 0L;
					string text2 = array2[j + 2];
					num2 = (text2.ToUpper().EndsWith("K") ? (long.Parse(text2.Substring(0, text2.Length - 1)) * 1024) : (text2.ToUpper().EndsWith("KB") ? (long.Parse(text2.Substring(0, text2.Length - 2)) * 1024) : (text2.ToUpper().EndsWith("M") ? (long.Parse(text2.Substring(0, text2.Length - 1)) * 1024 * 1024) : (text2.ToUpper().EndsWith("MB") ? (long.Parse(text2.Substring(0, text2.Length - 2)) * 1024 * 1024) : (text2.ToUpper().EndsWith("G") ? (long.Parse(text2.Substring(0, text2.Length - 1)) * 1024 * 1024 * 1024) : ((!text2.ToUpper().EndsWith("GB")) ? long.Parse(array2[j + 2]) : (long.Parse(text2.Substring(0, text2.Length - 2)) * 1024 * 1024 * 1024)))))));
					SizeCriterion sizeCriterion = new SizeCriterion();
					sizeCriterion.Size = num2;
					sizeCriterion.Operator = (ComparisonOperator)EnumUtil.Parse(typeof(ComparisonOperator), array2[j + 1]);
					selectionCriterion = sizeCriterion;
					j += 2;
					stack.Push(ParseState.CriterionDone);
					break;
				}
				case "filename":
				case "name":
				{
					if (array2.Length <= j + 2)
					{
						throw new ArgumentException(string.Join(" ", array2, j, array2.Length - j));
					}
					ComparisonOperator comparisonOperator2 = (ComparisonOperator)EnumUtil.Parse(typeof(ComparisonOperator), array2[j + 1]);
					if (comparisonOperator2 != ComparisonOperator.NotEqualTo && comparisonOperator2 != ComparisonOperator.EqualTo)
					{
						throw new ArgumentException(string.Join(" ", array2, j, array2.Length - j));
					}
					string text = array2[j + 2];
					if (text.StartsWith("'"))
					{
						int num = j;
						if (!text.EndsWith("'"))
						{
							do
							{
								j++;
								if (array2.Length <= j + 2)
								{
									throw new ArgumentException(string.Join(" ", array2, num, array2.Length - num));
								}
								text = text + " " + array2[j + 2];
							}
							while (!array2[j + 2].EndsWith("'"));
						}
						text = text.Substring(1, text.Length - 2);
					}
					selectionCriterion = new NameCriterion
					{
						MatchingFileSpec = text,
						Operator = comparisonOperator2
					};
					j += 2;
					stack.Push(ParseState.CriterionDone);
					break;
				}
				case "attributes":
				{
					if (array2.Length <= j + 2)
					{
						throw new ArgumentException(string.Join(" ", array2, j, array2.Length - j));
					}
					ComparisonOperator comparisonOperator = (ComparisonOperator)EnumUtil.Parse(typeof(ComparisonOperator), array2[j + 1]);
					if (comparisonOperator != ComparisonOperator.NotEqualTo && comparisonOperator != ComparisonOperator.EqualTo)
					{
						throw new ArgumentException(string.Join(" ", array2, j, array2.Length - j));
					}
					selectionCriterion = new AttributesCriterion
					{
						AttributeString = array2[j + 2],
						Operator = comparisonOperator
					};
					j += 2;
					stack.Push(ParseState.CriterionDone);
					break;
				}
				case "":
					stack.Push(ParseState.Whitespace);
					break;
				default:
					throw new ArgumentException("'" + array2[j] + "'");
				}
				parseState = stack.Peek();
				if (parseState == ParseState.CriterionDone)
				{
					stack.Pop();
					if (stack.Peek() == ParseState.ConjunctionPending)
					{
						while (stack.Peek() == ParseState.ConjunctionPending)
						{
							CompoundCriterion compoundCriterion = stack2.Pop() as CompoundCriterion;
							compoundCriterion.Right = selectionCriterion;
							selectionCriterion = compoundCriterion;
							stack.Pop();
							parseState = stack.Pop();
							if (parseState != ParseState.CriterionDone)
							{
								throw new ArgumentException("??");
							}
						}
					}
					else
					{
						stack.Push(ParseState.CriterionDone);
					}
				}
				if (parseState == ParseState.Whitespace)
				{
					stack.Pop();
				}
			}
			return selectionCriterion;
		}

		public override string ToString()
		{
			return "FileSelector(" + _Criterion.ToString() + ")";
		}

		private bool Evaluate(string filename)
		{
			return _Criterion.Evaluate(filename);
		}

		public ICollection<string> SelectFiles(string directory)
		{
			return SelectFiles(directory, recurseDirectories: false);
		}

		public ReadOnlyCollection<string> SelectFiles(string directory, bool recurseDirectories)
		{
			if (_Criterion == null)
			{
				throw new ArgumentException("SelectionCriteria has not been set");
			}
			List<string> list = new List<string>();
			try
			{
				if (Directory.Exists(directory))
				{
					string[] files = Directory.GetFiles(directory);
					string[] array = files;
					foreach (string text in array)
					{
						if (Evaluate(text))
						{
							list.Add(text);
						}
					}
					if (recurseDirectories)
					{
						string[] directories = Directory.GetDirectories(directory);
						string[] array2 = directories;
						foreach (string directory2 in array2)
						{
							list.AddRange(SelectFiles(directory2, recurseDirectories));
						}
					}
				}
			}
			catch (UnauthorizedAccessException)
			{
			}
			catch (IOException)
			{
			}
			return list.AsReadOnly();
		}

		private bool Evaluate(ZipEntry entry)
		{
			return _Criterion.Evaluate(entry);
		}

		public ICollection<ZipEntry> SelectEntries(ZipFile zip)
		{
			List<ZipEntry> list = new List<ZipEntry>();
			foreach (ZipEntry item in zip)
			{
				if (Evaluate(item))
				{
					list.Add(item);
				}
			}
			return list;
		}

		public ICollection<ZipEntry> SelectEntries(ZipFile zip, string directoryPathInArchive)
		{
			List<ZipEntry> list = new List<ZipEntry>();
			string b = directoryPathInArchive?.Replace("/", "\\");
			foreach (ZipEntry item in zip)
			{
				if ((directoryPathInArchive == null || Path.GetDirectoryName(item.FileName) == directoryPathInArchive || Path.GetDirectoryName(item.FileName) == b) && Evaluate(item))
				{
					list.Add(item);
				}
			}
			return list.AsReadOnly();
		}
	}
}
