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
		// Used to get values from context inputparameters and outputparameters, of a specific type rather than handling the Object type in our code.
		public static bool TryGetValue<T>(this ParameterCollection parameterCollection, string key, out T value)
		{
			object valueObj;
			if (parameterCollection.TryGetValue(key, out valueObj))
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
			object valueObj;
			if (parameterCollection.TryGetValue(key, out valueObj))
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
		public static T ParseJSON<T>(this string jsonString, bool useSimpleDictionaryFormat = true)
		{
			try
			{
				DataContractJsonSerializer JsonDeserializer = new DataContractJsonSerializer(typeof(T));
				using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
				{
					return (T)JsonDeserializer.ReadObject(stream);
				}
			}
			catch
			{
				throw new InvalidDataContractException("JSON string is invalid, or could not be serialized to the specified type.");
			}
		}

		// Try Parse JSON string to object - CRM Online compatible
		public static bool TryParseJSON<T>(this string jsonString, out T obj, bool useSimpleDictionaryFormat = true)
		{
			try
			{
				obj = jsonString.ParseJSON<T>(useSimpleDictionaryFormat);
				return true;
			}
			catch
			{
				obj = default(T);
				return false;
			}
		}

		// Convert object to JSON string - CRM Online compatible
		public static string ToJSON(this object obj, bool useSimpleDictionaryFormat = true)
		{
			DataContractJsonSerializer JsonSerializer = new DataContractJsonSerializer(obj.GetType());
			using (MemoryStream stream = new MemoryStream())
			{
				JsonSerializer.WriteObject(stream, obj);
				return Encoding.UTF8.GetString(stream.ToArray());
			}
		}

		// A slightly easier way to retreive all columns
		public static Entity Retrieve(this IOrganizationService service, string entityName, Guid entityId, bool allColumns)
		{
			return service.Retrieve(entityName, entityId, new ColumnSet(allColumns));
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
