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

// Collection of useful extension methods

using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Celedon
{
	// Helpful extentions 
	// Use these and add to these to make our plugins a little shorter/cleaner/leaner
	public static class Extensions
	{
	    public static string ReplaceParameters(this IOrganizationService service, Entity target, string text)
	    {
	        if (string.IsNullOrWhiteSpace(text))
	        {
	            return "";
	        }

	        foreach (var param in RuntimeParameter.GetParametersFromString(text))
	        {
	            if (!param.IsParentParameter())
	            {
	                text = text.Replace(param.ParameterText, param.GetParameterValue(target));
	            }
	            else
	            {
	                if (target.Contains(param.ParentLookupName))
	                {
	                    var parentRecord = service.Retrieve(target.GetAttributeValue<EntityReference>(param.ParentLookupName).LogicalName, target.GetAttributeValue<EntityReference>(param.ParentLookupName).Id, new ColumnSet(param.AttributeName));
	                    text = text.Replace(param.ParameterText, param.GetParameterValue(parentRecord));
	                }
	                else  // target record has no parent, so use default value
	                {
	                    text = text.Replace(param.ParameterText, param.DefaultValue);
	                }
	            }
	        }

	        return text;
	    }

        // Used to get values from context inputparameters and outputparameters, of a specific type rather than handling the Object type in our code.
        public static bool TryGetValue<T>(this ParameterCollection parameterCollection, string key, out T value)
		{
		    if (parameterCollection.TryGetValue(key, out var valueObj))
			{
				try
				{
					value = (T)valueObj;
					return true;
				}
				catch (InvalidCastException) { }  // Key exists, but cast failed.  Let this fall through to the default return value.
			}

			value = default(T);
			return false;
		}

		// NotNull because Plugins attached to custom Actions recieve ALL input parameters, even if they were not included in the original Action context
		public static bool TryGetValueNotNull<T>(this ParameterCollection parameterCollection, string key, out T value)
		{
		    if (parameterCollection.TryGetValue(key, out var valueObj))
			{
				if (valueObj != null)
				{
					try
					{
						value = (T)valueObj;
						return true;
					}
					catch (InvalidCastException) { }  // Key exists, but cast failed.  Let this fall through to the default return value.
				}
			}

			value = default(T);
			return false;
		}

		// NotNull because Plugins attached to custom Actions recieve ALL input parameters, even if they were not included in the original Action context
		public static bool ContainsNotNull(this ParameterCollection parameterCollection, string key)
		{
			return parameterCollection.Contains(key) && parameterCollection[key] != null;
		}

		// Parse JSON string to object - CRM Online compatible
		public static T ParseJson<T>(this string jsonString)
		{
			try
			{
				var jsonDeserializer = new DataContractJsonSerializer(typeof(T));
				using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
				{
					return (T)jsonDeserializer.ReadObject(stream);
				}
			}
			catch
			{
				throw new InvalidDataContractException("JSON string is invalid, or could not be serialized to the specified type.");
			}
		}

		// Try Parse JSON string to object - CRM Online compatible
		public static bool TryParseJson<T>(this string jsonString, out T obj)
		{
			try
			{
				obj = jsonString.ParseJson<T>();
				return true;
			}
			catch
			{
				obj = default(T);
				return false;
			}
		}

		// Convert object to JSON string - CRM Online compatible
		public static string ToJson(this object obj, bool useSimpleDictionaryFormat = true)
		{
			var jsonSerializer = new DataContractJsonSerializer(obj.GetType());
			using (var stream = new MemoryStream())
			{
				jsonSerializer.WriteObject(stream, obj);
				return Encoding.UTF8.GetString(stream.ToArray());
			}
		}

		// Easily convert Guid to EntityReference
		public static EntityReference ToEntityReference(this Guid id, string entityType)
		{
			return new EntityReference(entityType, id);
		}

		// Easily convert integer to OptionSetValue
		public static OptionSetValue ToOptionSetValue(this int value)
		{
			return new OptionSetValue(value);
		}

		// This is how the OOB GetService method should have been...
		public static T GetService<T>(this IServiceProvider serviceProvider)
		{
			return (T)serviceProvider.GetService(typeof(T));
		}
	}
}
