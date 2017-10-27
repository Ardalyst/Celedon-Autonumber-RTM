/*The MIT License (MIT)

Copyright (c) 2017 Celedon Partners 

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
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk;

namespace Celedon
{
	public class RuntimeParameter
	{
	    private static readonly Random Random = new Random();

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
		    var rp = new RuntimeParameter
		    {
		        ParameterText = input,
		        AttributeName = input.Trim('{', '}')
		    };

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
		    return Regex.Matches(text, @"{(.*?)}").OfType<Match>().Select(m => m.Groups[0].Value).Distinct().Select(Parse);
		}

		public string GetParameterValue(Entity target)
		{
			if (target.Contains(AttributeName))
			{
			    switch (target[AttributeName])
			    {
			        case EntityReference _:
			            // Lookup condition is based on GUID
			            return Conditional.HasCondition ? Conditional.GetResult(target.GetAttributeValue<EntityReference>(AttributeName).Id) : target.GetAttributeValue<EntityReference>(AttributeName).Name;
			        case OptionSetValue _:
			            // Conditional OptionSetValue is based on the integer value
			            return Conditional.HasCondition ? Conditional.GetResult(target.GetAttributeValue<OptionSetValue>(AttributeName).Value.ToString()) : target.FormattedValues[AttributeName];
			        case bool _:
			            // Note: Boolean values ignore the match value, they just use the attribute value itself as the condition
			            return Conditional.HasCondition ? Conditional.GetResult(target.GetAttributeValue<bool>(AttributeName)) : target.FormattedValues[AttributeName];
			        case DateTime _:
			            // If there is a format AND a condition, apply formatting first, then evaluate condition as a string
			            // If there is a condition without any format, evaluate condition as DateTime
			            return string.IsNullOrEmpty(StringFormatter) ? Conditional.GetResult(target.GetAttributeValue<DateTime>(AttributeName)) : Conditional.GetResult(target.GetAttributeValue<DateTime>(AttributeName).ToString(StringFormatter));
			        case Money _:
			            return Conditional.HasCondition ? Conditional.GetResult(target.GetAttributeValue<Money>(AttributeName).Value) : target.GetAttributeValue<Money>(AttributeName).Value.ToString(StringFormatter);
			        case int _:
			            return Conditional.HasCondition ? Conditional.GetResult(target.GetAttributeValue<double>(AttributeName)) : target.GetAttributeValue<double>(AttributeName).ToString(StringFormatter);
			        case decimal _:
			            return Conditional.HasCondition ? Conditional.GetResult(target.GetAttributeValue<decimal>(AttributeName)) : target.GetAttributeValue<decimal>(AttributeName).ToString(StringFormatter);
			        case double _:
			            return Conditional.HasCondition ? Conditional.GetResult(target.GetAttributeValue<double>(AttributeName)) : target.GetAttributeValue<double>(AttributeName).ToString(StringFormatter);
			        case string _:
			            return Conditional.GetResult(target[AttributeName].ToString());
			    }
			}
			else if (AttributeName.Equals("rand"))
			{
				string length;
				var stringStyle = "upper";

			    if (StringFormatter.Contains('?'))
				{
					length = StringFormatter.Split('?')[0];
					stringStyle = StringFormatter.Split('?')[1].ToLower();
				}
				else
				{
					length = StringFormatter;
				}

				if (!int.TryParse(length, out var stringLength))
				{
					stringLength = 5;
				}

				var stringValues = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

			    if (stringStyle == "mix")
			        stringValues = stringValues + stringValues.ToLower();
			    else if (stringStyle == "lower")
			    {
			        stringValues = stringValues.ToLower();
			    }

				return string.Join("", Enumerable.Range(0, stringLength).Select(n => stringValues[Random.Next(stringValues.Length)]));
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
			private readonly char _operator;
			private readonly ConditionalFormatter _falseCondition;

			public string MatchValue { get; }
			public string TrueValue { get; }
			public string FalseValue { get; }

			public ConditionalFormatter() : this("", "", "") { }
			public ConditionalFormatter(string matchValue, string trueValue, string falseValue)
			{
				_operator = matchValue.StartsWith(">") || matchValue.StartsWith("<") ? matchValue[0] : '=';
				MatchValue = matchValue.TrimStart('>', '<');
				TrueValue = trueValue;
				FalseValue = falseValue;

			    if (!falseValue.Contains('?'))
			    {
			        return;
			    }

			    var condition1 = falseValue.Split(new [] { '?' }, 2);
			    var condition2 = condition1[1].Split(new [] { '|' }, 2);
			    _falseCondition = new ConditionalFormatter(condition1[0], condition2[0], condition2[1]);
			}

		    // ReSharper disable once MemberHidesStaticFromOuterClass
			public static ConditionalFormatter Parse(string conditionalString)
			{
				var condition1 = conditionalString.Split(new [] { '?' }, 2);
				var condition2 = condition1[1].Split(new [] { '|' }, 2);

				return new ConditionalFormatter(condition1[0], condition2[0], condition2[1]);
			}

			public bool HasCondition => !string.IsNullOrEmpty(MatchValue);

		    public bool IsRecursive => _falseCondition != null;

		    public string GetResult(string inputText)
			{
				if (!HasCondition)
				{
					return inputText;
				}

				if (IsRecursive)
				{
					return inputText == MatchValue ? TrueValue : _falseCondition.GetResult(inputText);
				}

			    return inputText == MatchValue ? TrueValue : FalseValue;
			}

			public string GetResult(Guid inputGuid)
			{
				if (!HasCondition)
				{
					return inputGuid.ToString();
				}

				if (IsRecursive)
				{
					return CompareGuid(inputGuid) ? TrueValue : _falseCondition.GetResult(inputGuid);
				}

			    return CompareGuid(inputGuid) ? TrueValue : FalseValue;
            }

			public string GetResult(int inputInt)
			{
				if (!HasCondition)
				{
					return inputInt.ToString();
				}

				if (IsRecursive)
				{
					return CompareNumeric(inputInt) ? TrueValue : _falseCondition.GetResult(inputInt);
				}

			    return CompareNumeric(inputInt) ? TrueValue : FalseValue;
            }

			public string GetResult(double inputDouble)
			{
				if (!HasCondition)
				{
					return inputDouble.ToString(CultureInfo.InvariantCulture);
				}

				if (IsRecursive)
				{
					return CompareNumeric(inputDouble) ? TrueValue : _falseCondition.GetResult(inputDouble);
				}

			    return CompareNumeric(inputDouble) ? TrueValue : FalseValue;
			}

			public string GetResult(decimal inputDecimal)
			{
				if (!HasCondition)
				{
					return inputDecimal.ToString(CultureInfo.InvariantCulture);
				}

				if (IsRecursive)
				{
					return CompareNumeric(inputDecimal) ? TrueValue : _falseCondition.GetResult(inputDecimal);
				}

			    return CompareNumeric(inputDecimal) ? TrueValue : FalseValue;
			}

			public string GetResult(DateTime inputDate)
			{
				if (!HasCondition)
				{
					return inputDate.ToString(CultureInfo.InvariantCulture);
				}

				if (IsRecursive)
				{
					return CompareDateTime(inputDate) ? TrueValue : _falseCondition.GetResult(inputDate);
				}

			    return CompareDateTime(inputDate) ? TrueValue : FalseValue;
			}

			private bool CompareGuid(Guid id)
			{
			    if (Guid.TryParse(MatchValue, out var matchGuid))
				{
					return id.Equals(matchGuid);
				}

			    return id.ToString().Equals(MatchValue);
			}

			private bool CompareDateTime(DateTime date)
			{
			    if (DateTime.TryParse(MatchValue, out var matchDate))
				{
					switch (_operator)
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

		    private bool CompareNumeric(int number)
		    {
		        return CompareNumeric((decimal)number);
		    }

            private bool CompareNumeric(double number)
		    {
		        return CompareNumeric((decimal) number);
		    }

            private bool CompareNumeric(decimal number)
			{
			    if (!decimal.TryParse(MatchValue, out var matchNumber))
			    {
			        return number.ToString(CultureInfo.InvariantCulture) == MatchValue;
			    }

			    switch (_operator)
			    {
			        case '>':
			            return number > matchNumber;
			        case '<':
			            return number < matchNumber;
			        default:
			            return number == matchNumber;
			    }
			}

			public string GetResult(bool value)
			{
				// Boolean only has 2 possible values, so it doesn't support recursive conditions
				return value ? TrueValue : FalseValue;
			}

			public override bool Equals(object obj)
			{
				return obj is ConditionalFormatter formatter && this == formatter;
			}
			public override int GetHashCode()
			{
				return MatchValue.GetHashCode() ^ TrueValue.GetHashCode() ^ FalseValue.GetHashCode();
			}
			public static bool operator ==(ConditionalFormatter x, ConditionalFormatter y)
			{
				if (ReferenceEquals(null, x) && ReferenceEquals(null, y))
				{
					return true;
				}

			    if (ReferenceEquals(null, x) || ReferenceEquals(null, y))
			    {
			        return false;
			    }

			    return x.MatchValue == y.MatchValue && x.TrueValue == y.TrueValue && x.FalseValue == y.FalseValue && x._falseCondition == y._falseCondition;
			}
			public static bool operator !=(ConditionalFormatter x, ConditionalFormatter y)
			{
				return !(x == y);
			}
		}
	}
}
