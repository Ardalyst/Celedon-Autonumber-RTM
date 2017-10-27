using System;
using Microsoft.Xrm.Sdk;
using NUnit.Framework;

namespace Celedon
{
	[TestFixture]
	public class AutoNumberUnitTest
	{
		[Test]
		public void RuntimeParameterParseTest1()
		{
			RuntimeParameter rp = RuntimeParameter.Parse("{attributeName}");

			// Stuff that should be populated
			Assert.AreEqual(rp.AttributeName, "attributeName");

			// Stuff that should not be populated
			Assert.AreEqual(rp.Conditional, new RuntimeParameter.ConditionalFormatter());
			Assert.AreEqual(rp.DefaultValue, String.Empty);
			Assert.AreEqual(rp.ParentLookupName, String.Empty);
			Assert.AreEqual(rp.StringFormatter, String.Empty);
		}

		[Test]
		public void RuntimeParameterParseTest2()
		{
			RuntimeParameter rp = RuntimeParameter.Parse("{parentLookup.parentAttribute}");

			// Stuff that should be populated
			Assert.AreEqual(rp.AttributeName, "parentAttribute");
			Assert.AreEqual(rp.ParentLookupName, "parentLookup");

			// Stuff that should not be populated
			Assert.AreEqual(rp.Conditional, new RuntimeParameter.ConditionalFormatter());
			Assert.AreEqual(rp.DefaultValue, String.Empty);
			Assert.AreEqual(rp.StringFormatter, String.Empty);
		}

		[Test]
		public void RuntimeParameterParseTest3()
		{
			RuntimeParameter rp = RuntimeParameter.Parse("{attributeName|defaultValue}");

			// Stuff that should be populated
			Assert.AreEqual(rp.AttributeName, "attributeName");
			Assert.AreEqual(rp.DefaultValue, "defaultValue");

			// Stuff that should not be populated
			Assert.AreEqual(rp.Conditional, new RuntimeParameter.ConditionalFormatter());
			Assert.AreEqual(rp.ParentLookupName, String.Empty);
			Assert.AreEqual(rp.StringFormatter, String.Empty);
		}

		[Test]
		public void RuntimeParameterParseTest4()
		{
			RuntimeParameter rp = RuntimeParameter.Parse("{attributeName:formatString}");

			// Stuff that should be populated
			Assert.AreEqual(rp.AttributeName, "attributeName");
			Assert.AreEqual(rp.StringFormatter, "formatString");

			// Stuff that should not be populated
			Assert.AreEqual(rp.Conditional, new RuntimeParameter.ConditionalFormatter());
			Assert.AreEqual(rp.DefaultValue, String.Empty);
			Assert.AreEqual(rp.ParentLookupName, String.Empty);
		}

		[Test]
		public void RuntimeParameterParseTest5()
		{
			RuntimeParameter rp = RuntimeParameter.Parse("{attributeName:matchValue?trueValue|falseValue}");

			// Stuff that should be populated
			Assert.AreEqual(rp.AttributeName, "attributeName");
			Assert.AreEqual(rp.Conditional.MatchValue, "matchValue");

			// Stuff that should not be populated
			Assert.AreEqual(rp.DefaultValue, String.Empty);
			Assert.AreEqual(rp.ParentLookupName, String.Empty);
			Assert.AreEqual(rp.StringFormatter, String.Empty);

			// Conditional test cases
			Assert.AreEqual(rp.Conditional.GetResult("matchValue"), "trueValue");
			Assert.AreEqual(rp.Conditional.GetResult("other"), "falseValue");
		}

		[Test]
		public void RuntimeParameterParseTest6()
		{
			RuntimeParameter rp = RuntimeParameter.Parse("{attributeName:match1?result1|match2?result2|match3?result3|elseResult}");

			// Stuff that should be populated
			Assert.AreEqual(rp.AttributeName, "attributeName");

			// Stuff that should not be populated
			Assert.AreEqual(rp.DefaultValue, String.Empty);
			Assert.AreEqual(rp.ParentLookupName, String.Empty);
			Assert.AreEqual(rp.StringFormatter, String.Empty);

			// Conditional test cases
			Assert.AreEqual(rp.Conditional.GetResult("match1"), "result1");
			Assert.AreEqual(rp.Conditional.GetResult("match2"), "result2");
			Assert.AreEqual(rp.Conditional.GetResult("match3"), "result3");
			Assert.AreEqual(rp.Conditional.GetResult("other"), "elseResult");
		}

		[Test]
		public void RuntimeParameterParseTest7()
		{
			RuntimeParameter rp = RuntimeParameter.Parse("{attributeName:formatString:matchValue?trueValue|falseValue}");

			// Stuff that should be populated
			Assert.AreEqual(rp.AttributeName, "attributeName");
			Assert.AreEqual(rp.StringFormatter, "formatString");

			// Stuff that should not be populated
			Assert.AreEqual(rp.DefaultValue, String.Empty);
			Assert.AreEqual(rp.ParentLookupName, String.Empty);

			// Conditional test cases
			Assert.AreEqual(rp.Conditional.GetResult("matchValue"), "trueValue");
			Assert.AreEqual(rp.Conditional.GetResult("other"), "falseValue");

		}

		[Test]
		public void RuntimeParameterParseTest8()
		{
			RuntimeParameter rp = RuntimeParameter.Parse("{parentLookup.parentAttribute:formatString:matchValue?trueValue|falseValue}");

			// Stuff that should be populated
			Assert.AreEqual(rp.AttributeName, "parentAttribute");
			Assert.AreEqual(rp.ParentLookupName, "parentLookup");
			Assert.AreEqual(rp.StringFormatter, "formatString");

			// Stuff that should not be populated
			Assert.AreEqual(rp.DefaultValue, String.Empty);

			// Conditional test cases
			Assert.AreEqual(rp.Conditional.GetResult("matchValue"), "trueValue");
			Assert.AreEqual(rp.Conditional.GetResult("other"), "falseValue");
		}

		[Test]
		public void RuntimeParameterParseTest9()
		{
			RuntimeParameter rp = RuntimeParameter.Parse("{attributeName|defaultValue:formatString:matchValue?trueValue|falseValue}");

			// Stuff that should be populated
			Assert.AreEqual(rp.AttributeName, "attributeName");
			Assert.AreEqual(rp.DefaultValue, "defaultValue");
			Assert.AreEqual(rp.StringFormatter, "formatString");

			// Stuff that should not be populated
			Assert.AreEqual(rp.ParentLookupName, String.Empty);

			// Conditional test cases
			Assert.AreEqual(rp.Conditional.GetResult("matchValue"), "trueValue");
			Assert.AreEqual(rp.Conditional.GetResult("other"), "falseValue");
		}

		[Test]
		public void RuntimeParameterParseTest10()
		{
			RuntimeParameter rp = RuntimeParameter.Parse("{attributeName:>100?trueValue|falseValue}");

			// Stuff that should be populated
			Assert.AreEqual(rp.AttributeName, "attributeName");

			// Stuff that should not be populated
			Assert.AreEqual(rp.DefaultValue, String.Empty);
			Assert.AreEqual(rp.ParentLookupName, String.Empty);
			Assert.AreEqual(rp.StringFormatter, String.Empty);

			// Conditional test cases
			Assert.AreEqual(rp.Conditional.GetResult(101), "trueValue");
			Assert.AreEqual(rp.Conditional.GetResult(99), "falseValue");
		}

		[Test]
		public void RuntimeParameterParseTest11()
		{
			RuntimeParameter rp = RuntimeParameter.Parse("{attributeName:<2015-1-1?trueValue|falseValue}");

			// Stuff that should be populated
			Assert.AreEqual(rp.AttributeName, "attributeName");

			// Stuff that should not be populated
			Assert.AreEqual(rp.DefaultValue, String.Empty);
			Assert.AreEqual(rp.ParentLookupName, String.Empty);
			Assert.AreEqual(rp.StringFormatter, String.Empty);

			// Conditional test cases
			Assert.AreEqual(rp.Conditional.GetResult(new DateTime(2014,1,1)), "trueValue");
			Assert.AreEqual(rp.Conditional.GetResult(new DateTime(2016,1,1)), "falseValue");
		}

		[Test]
		public void RuntimeParameterParseTest12()
		{
			RuntimeParameter rp = RuntimeParameter.Parse("{attributeName:yyyy:2015?trueValue|falseValue}");

			// Stuff that should be populated
			Assert.AreEqual(rp.AttributeName, "attributeName");
			Assert.AreEqual(rp.StringFormatter, "yyyy");

			// Stuff that should not be populated
			Assert.AreEqual(rp.DefaultValue, String.Empty);
			Assert.AreEqual(rp.ParentLookupName, String.Empty);

			// Conditional test cases
			Entity test = new Entity();
			test["attributeName"] = new DateTime(2015,1,1);
			Assert.AreEqual(rp.GetParameterValue(test), "trueValue");
			test["attributeName"] = new DateTime(2016, 1, 1);
			Assert.AreEqual(rp.GetParameterValue(test), "falseValue");
		}
	}
}
