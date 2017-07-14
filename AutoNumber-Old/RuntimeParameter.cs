// Author: Matt Barnes (matt.barnes@celedonpartners.com)
/*The MIT License (MIT)

Copyright (c) 2015 Celedon Partners 

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

// Class for storing and processing Runtime Parameters in the Autonumber configuration

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk;

namespace Celedon
{
	public class RuntimeParameter
	{
		public string ParameterText { get; private set; }
		public string AttributeName { get; private set; }
		public string ParentLookupName { get; private set; }
		public string DefaultValue { get; private set; }
		public string StringFormatter { get; private set; }
		public ConditionalFormatter Conditional { get; private set; }

		private RuntimeParameter() : this("", "", "", "", "", new ConditionalFormatter()) { }
		public RuntimeParameter(string paramText, string attributeText, string parentLookup, string defaultValue, string stringFormat, ConditionalFormatter condition)
		{
			ParameterText = paramText;
			AttributeName = attributeText;
			ParentLookupName = parentLookup;
			DefaultValue = defaultValue;
			StringFormatter = stringFormat;
			Conditional = condition;
		}

		public static RuntimeParameter Parse(string input)
		{
			RuntimeParameter rp = new RuntimeParameter();
			rp.ParameterText = input;
			rp.AttributeName = input.Trim('{', '}');

			if (rp.AttributeName.Contains(':'))
			{
				string[] paramList = rp.AttributeName.Split(':');
				rp.AttributeName = paramList[0];

				if (rp.AttributeName == "rand")
				{
					rp.StringFormatter = paramList[1];
				}
				else
				{
					if (paramList[1].Contains('?'))
					{
						rp.Conditional = ConditionalFormatter.Parse(paramList[1]);
					}
					else if (paramList.Length > 2)
					{
						rp.StringFormatter = paramList[1];
						rp.Conditional = ConditionalFormatter.Parse(paramList[2]);
					}
					else
					{
						rp.StringFormatter = paramList[1];
					}
				}
			}

			if (rp.AttributeName.Contains('|'))
			{
				rp.DefaultValue = rp.AttributeName.Split('|')[1];
				rp.AttributeName = rp.AttributeName.Split('|')[0];
			}

			if (rp.AttributeName.Contains('.'))
			{
				rp.ParentLookupName = rp.AttributeName.Split('.')[0];
				rp.AttributeName = rp.AttributeName.Split('.')[1];
			}

			return rp;
		}

		public static IEnumerable<RuntimeParameter> GetParametersFromString(string text)
		{
			foreach (string p in Regex.Matches(text, @"{(.*?)}").OfType<Match>().Select(m => m.Groups[0].Value).Distinct())
			{
				yield return Parse(p);
			}
		}

		public string GetParameterValue(Entity Target)
		{
			if (Target.Contains(AttributeName))
			{
				if (Target[AttributeName] is EntityReference)
				{
					// Lookup condition is based on GUID
					return Conditional.HasCondition ? Conditional.GetResult(Target.GetAttributeValue<EntityReference>(AttributeName).Id) : Target.GetAttributeValue<EntityReference>(AttributeName).Name;
				}
				else if (Target[AttributeName] is OptionSetValue)
				{
					// Conditional OptionSetValue is based on the integer value
					return Conditional.HasCondition ? Conditional.GetResult(Target.GetAttributeValue<OptionSetValue>(AttributeName).Value.ToString()) : Target.FormattedValues[AttributeName];
				}
				else if (Target[AttributeName] is bool)
				{
					// Note: Boolean values ignore the match value, they just use the attribute value itself as the condition
					return Conditional.HasCondition ? Conditional.GetResult(Target.GetAttributeValue<bool>(AttributeName)) : Target.FormattedValues[AttributeName];
				}
				else if (Target[AttributeName] is DateTime)
				{
					// If there is a format AND a condition, apply formatting first, then evaluate condition as a string
					// If there is a condition without any format, evaluate condition as DateTime
					return String.IsNullOrEmpty(StringFormatter) ? Conditional.GetResult(Target.GetAttributeValue<DateTime>(AttributeName)) : Conditional.GetResult(Target.GetAttributeValue<DateTime>(AttributeName).ToString(StringFormatter));
				}
				else if (Target[AttributeName] is Money)
				{
					return Conditional.HasCondition ? Conditional.GetResult(Target.GetAttributeValue<Money>(AttributeName).Value) : Target.GetAttributeValue<Money>(AttributeName).Value.ToString(StringFormatter);
				}
				else if (Target[AttributeName] is int)
				{
					return Conditional.HasCondition ? Conditional.GetResult(Target.GetAttributeValue<double>(AttributeName)) : Target.GetAttributeValue<double>(AttributeName).ToString(StringFormatter);
				}
				else if (Target[AttributeName] is decimal)
				{
					return Conditional.HasCondition ? Conditional.GetResult(Target.GetAttributeValue<decimal>(AttributeName)) : Target.GetAttributeValue<decimal>(AttributeName).ToString(StringFormatter);
				}
				else if (Target[AttributeName] is double)
				{
					return Conditional.HasCondition ? Conditional.GetResult(Target.GetAttributeValue<double>(AttributeName)) : Target.GetAttributeValue<double>(AttributeName).ToString(StringFormatter);
				}
				else if (Target[AttributeName] is string)
				{
					return Conditional.GetResult(Target[AttributeName].ToString());
				}
			}
			else if (AttributeName.Equals("rand"))
			{
				string length = "";
				string stringStyle = "upper";
				int stringLength = 5;  // Seems like reasonable default

				if (StringFormatter.Contains('?'))
				{
					length = StringFormatter.Split('?')[0];
					stringStyle = StringFormatter.Split('?')[1].ToLower();
				}
				else
				{
					length = StringFormatter;
				}

				if (!Int32.TryParse(length, out stringLength))
				{
					stringLength = 5;
				}

				string stringValues = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
				if (stringStyle == "mix")
				{
					stringValues = stringValues + stringValues.ToLower();
				}
				else if (stringStyle == "lower")
				{
					stringValues = stringValues.ToLower();
				}

				Random rnd = new Random();
				return String.Join("", Enumerable.Range(0, stringLength).Select(n => stringValues[rnd.Next(stringValues.Length)]));
			}

			return DefaultValue;
		}

		public bool IsParentParameter()
		{
			return !String.IsNullOrEmpty(ParentLookupName);
		}

		public bool IsRandomParameter()
		{
			return AttributeName.Equals("rand");
		}

		public class ConditionalFormatter
		{
			private char Operator;
			private ConditionalFormatter FalseCondition = null;
			public string MatchValue { get; private set; }
			public string TrueValue { get; private set; }
			public string FalseValue { get; private set; }

			public ConditionalFormatter() : this("", "", "") { }
			public ConditionalFormatter(string matchValue, string trueValue, string falseValue)
			{
				Operator = matchValue.StartsWith(">") || matchValue.StartsWith("<") ? matchValue[0] : '=';
				MatchValue = matchValue.TrimStart('>', '<');
				TrueValue = trueValue;
				FalseValue = falseValue;

				if (falseValue.Contains('?'))  // if any nested conditions
				{
					string[] condition1 = falseValue.Split(new char[] { '?' }, 2);
					string[] condition2 = condition1[1].Split(new char[] { '|' }, 2);
					FalseCondition = new ConditionalFormatter(condition1[0], condition2[0], condition2[1]);
				}
			}

			public static ConditionalFormatter Parse(string conditionalString)
			{
				string[] condition1 = conditionalString.Split(new char[] { '?' }, 2);
				string[] condition2 = condition1[1].Split(new char[] { '|' }, 2);

				return new ConditionalFormatter(condition1[0], condition2[0], condition2[1]);
			}

			public bool HasCondition
			{
				get { return !String.IsNullOrEmpty(MatchValue); }
			}

			public bool IsRecursive
			{
				get { return FalseCondition != null; }
			}

			public string GetResult(string inputText)
			{
				if (!this.HasCondition)
				{
					return inputText;
				}

				if (this.IsRecursive)
				{
					return inputText == MatchValue ? TrueValue : FalseCondition.GetResult(inputText);
				}
				else
				{
					return inputText == MatchValue ? TrueValue : FalseValue;
				}
			}

			public string GetResult(Guid inputGuid)
			{
				if (!this.HasCondition)
				{
					return inputGuid.ToString();
				}

				if (this.IsRecursive)
				{
					return CompareGuid(inputGuid) ? TrueValue : FalseCondition.GetResult(inputGuid);
				}
				else
				{
					return CompareGuid(inputGuid) ? TrueValue : FalseValue;
				}
			}

			public string GetResult(int inputInt)
			{
				if (!this.HasCondition)
				{
					return inputInt.ToString();
				}

				if (this.IsRecursive)
				{
					return CompareNumeric(inputInt) ? TrueValue : FalseCondition.GetResult(inputInt);
				}
				else
				{
					return CompareNumeric(inputInt) ? TrueValue : FalseValue;
				}
			}

			public string GetResult(double inputDouble)
			{
				if (!this.HasCondition)
				{
					return inputDouble.ToString();
				}

				if (this.IsRecursive)
				{
					return CompareNumeric((decimal)inputDouble) ? TrueValue : FalseCondition.GetResult(inputDouble);
				}
				else
				{
					return CompareNumeric((decimal)inputDouble) ? TrueValue : FalseValue;
				}
			}

			public string GetResult(decimal inputDecimal)
			{
				if (!this.HasCondition)
				{
					return inputDecimal.ToString();
				}

				if (this.IsRecursive)
				{
					return CompareNumeric((decimal)inputDecimal) ? TrueValue : FalseCondition.GetResult(inputDecimal);
				}
				else
				{
					return CompareNumeric((decimal)inputDecimal) ? TrueValue : FalseValue;
				}
			}

			public string GetResult(DateTime inputDate)
			{
				if (!this.HasCondition)
				{
					return inputDate.ToString();
				}

				if (this.IsRecursive)
				{
					return CompareDateTime(inputDate) ? TrueValue : FalseCondition.GetResult(inputDate);
				}
				else
				{
					return CompareDateTime(inputDate) ? TrueValue : FalseValue;
				}
			}

			private bool CompareGuid(Guid id)
			{
				Guid matchGuid;
				if (Guid.TryParse(MatchValue, out matchGuid))
				{
					return id.Equals(matchGuid);
				}
				else
				{
					return id.ToString().Equals(MatchValue);
				}
			}

			private bool CompareDateTime(DateTime date)
			{
				DateTime matchDate;
				if (DateTime.TryParse(MatchValue, out matchDate))
				{
					switch (Operator)
					{
						case '>':
							return date > matchDate;
						case '<':
							return date < matchDate;
						default:
							return date == matchDate;
					}
				}
				else
				{
					return date.ToShortDateString() == MatchValue;
				}
			}

			private bool CompareNumeric(decimal number)
			{
				decimal matchNumber;
				if (Decimal.TryParse(MatchValue, out matchNumber))
				{
					switch (Operator)
					{
						case '>':
							return number > matchNumber;
						case '<':
							return number < matchNumber;
						default:
							return number == matchNumber;
					}
				}
				else
				{
					return number.ToString() == MatchValue;
				}
			}

			public string GetResult(bool value)
			{
				// Boolean only has 2 possible values, so it doesn't support recursive conditions
				return value ? TrueValue : FalseValue;
			}

			public override bool Equals(Object obj)
			{
				return obj is ConditionalFormatter && this == (ConditionalFormatter)obj;
			}
			public override int GetHashCode()
			{
				return MatchValue.GetHashCode() ^ TrueValue.GetHashCode() ^ FalseValue.GetHashCode();
			}
			public static bool operator ==(ConditionalFormatter x, ConditionalFormatter y)
			{
				if (Object.ReferenceEquals(null, x) && Object.ReferenceEquals(null, y))
				{
					return true;
				}
				else if (Object.ReferenceEquals(null, x) || Object.ReferenceEquals(null, y))
				{
					return false;
				}
				return x.MatchValue == y.MatchValue && x.TrueValue == y.TrueValue && x.FalseValue == y.FalseValue && x.FalseCondition == y.FalseCondition;
			}
			public static bool operator !=(ConditionalFormatter x, ConditionalFormatter y)
			{
				return !(x == y);
			}
		}
	}
}
