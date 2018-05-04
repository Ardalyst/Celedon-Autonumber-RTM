# Celedon Partners Dynamics CRM AutoNumber
> Spec Status: *Current*<br />

Provides auto-numbering to Dynamics CRM.

## Instructions for Runtime Parameters
The Prefix and Suffix fields support runtime parameters.  You can enter the name of an attribute into those fields, and the value of that attribute will be inserted into the prefix or suffix when the autonumber is generated.

* Parameters are specified using curly braces, eg: {new_attributename}
* You can inter-mix parameters with hardcoded values: {new_att}id
* You can enter multiple parameters, or the same parameter multiple times: {new_att1}-{new_att2}

### Advanced/Optional Runtime Parameters
If any optional parameters are used, then the attribute name must always be the first parameter specified.

#### Parent Record Values (Optional)
It's possible to retrieve values from a parent record to use in a child autonumber.
Enter the name of the parent record lookup followed by the attribute from the parent record, separated by a dot .
The format for specifying a parent field is: {lookupAttributeName.parentAttributeName}
Example: {originatingleadid.industrycode}

* All other optional parameters are still supported when using a parent record value

#### Default Value (Optional)
You can specify a default value after the attributeName using a | character.  The default value will be used in the autonumber if the attribute is null or empty.
The default value, if used, must be the first parameter following the attribute name.

Example: {new_name|NONAME}
If the new_name attribute contains data, then that value will be used in the autonumber.  But if new_name is empty, then the value NONAME will be used in the autonumber.

#### Formatting String (Optional)
If the parameter attribute is a number or a date field, you can specify custom formatting using a colon, eg: {new_numberfield:D2} or {new_datefield:yyyy}
The formatting parameter, if used, must be the first parameter following the attribute name or default value.

Example: To insert the 4 digit year into an autonumber: {createdon:yyyy} or 2 digit: {createdon:yy}

* Full details on supported formatting options for numbers here: https://msdn.microsoft.com/en-us/library/dwhawy9k%28v=vs.110%29.aspx
* Full details on supported formatting options for datetime here: https://msdn.microsoft.com/en-us/library/8kb3ddd4(v=vs.110).aspx
* Use of the time separator : when specifying custom date/time formats is NOT supported

#### Conditional Operator (Optional)
You can specify conditional parameters using the : ? | characters.
The conditional parameter must always be the last parameter specified.

Example: {new_attributeName:matchValue?trueValue|falseValue}
If the value of new_attribute equals the matchValue, then the autonumber will use the trueValue, otherwise it will use the falseValue.

* The conditional operator supports all attribute types.
* If a conditional is used on an OptionSet, then it will match on the numeric value, not the text value.
* If a conditional is used on a Lookup, then it will match on the GUID value, not the text value.
* If a conditional is used on a Boolean, it will ignore the specified matchValue, and the bool value will be used directly to select the result.  NOTE: matchValue is still required to be entered, it's just not used for anything.
* If BOTH a Formatting String AND a Conditional Operator are used on a DateTime, then the conditional will match using the formatted value, not the original value.
* If BOTH a Formatting String AND a Conditional Operator are used on a Numeric, then the conditional will match using the original value, not the formatted value.
* Numeric and DateTime attributes support GreaterThan and LessThan operations.  This is done by prefixing the match value with either a '>' or '<' character. eg: {attributeName:<100?low|high}
* If a Numeric or DateTime value is used as a matchValue, the value entered must be parsable to the appropriate type

#### Nested Conditional Operators (Optional)
Conditional parameters can be infinitely nested within each other.  This basically allows an "If(), Else If(), Else If(), Else()" logic to occur.

Example: {attributeName:match1?result2|match2?result2|match3?result3|elseResult}
In this example, if the attribute value equals "match1", then the number will use "result1", if attributeValue equals match2, then it will use result2.  If attributeValue equals none of the specified match values, then it will use the "elseResult" value.

#### Sample Valid Parameter Strings:

* attribute only: **{attributeName}**
* parent attribute only: **{parentLookup.parentAttribute}**
* attribute with default: **{attributeName|defaultValue}**
* parent attribute with default: **{parentLookup.parentAttribute|defaultValue}**
* attribute and format: **{attributeName:formatString}**
* attribute and condition: **{attributeName:matchValue?trueValue|falseValue}**
* attribute with multiple conditions: **{attributeName:match1?result2|match2?result2|match3?result3|elseResult}**
* attribute and format and condition: **{attributeName:formatString:matchValue?trueValue|falseValue}**
* parent attribute with format and condition: **{parentLookup.parentAttribute:formatString:matchValue?trueValue|falseValue}**
* attribute with default and condition: **{attributeName|defaultValue:matchValue?trueValue|falseValue}**
* attribute with default, format and condition: **{attributeName|defaultValue:formatString:matchValue?trueValue|falseValue}**

## FAQ

**Q**: Does the current version work with Lookups?  **A: No. This has been logged as a future enhancement**

**Q**: Are auto numbering on integer fields supported? **A: Not in this release**

**Q**: Is this solution FIPS compliant/compatible? **A: No. The code has not been through the review process for FIPS compliance**

**Q**: Are values from grandparent or child entities supported? **A: No, not part of this release. A workaround for the current version: Add a calculated or auto number field on Opportunity that gets the value from the grandparent.**

**Considerations:**
* Once you get to grandparent, it could introduce performance issues, because multiple retrieve calls will be needed to get the final value

* There is a possibility of a circular reference. For example Account/Contact: You can have Parent Account on Contact, and you can have Primary Contact on Account, and in some instances that would be an infinite loop.

