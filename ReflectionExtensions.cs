using System;
using System.Collections.Generic;
using System.Reflection;

namespace MiniRealisticAirways;

public static class ReflectionExtensions
{
	private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> FieldCache = new Dictionary<Type, Dictionary<string, FieldInfo>>();

	public static T GetFieldValue<T>(this object obj, string name)
	{
		FieldInfo field = FindField(obj, name);
		if (field == null)
		{
			return default(T);
		}
		object value = field.GetValue(obj);
		return value is T typedValue ? typedValue : default(T);
	}

	public static void SetFieldValue<T>(this object obj, string name, T value)
	{
		FieldInfo field = FindField(obj, name);
		if (field != null && !field.IsInitOnly)
		{
			field.SetValue(obj, value);
		}
	}

	private static FieldInfo FindField(object obj, string name)
	{
		if (obj == null || string.IsNullOrEmpty(name))
		{
			return null;
		}
		Type objectType = obj.GetType();
		if (!FieldCache.TryGetValue(objectType, out Dictionary<string, FieldInfo> fields))
		{
			fields = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
			FieldCache[objectType] = fields;
		}
		if (fields.TryGetValue(name, out FieldInfo cachedField))
		{
			return cachedField;
		}

		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;
		FieldInfo field = null;
		for (Type type = objectType; type != null; type = type.BaseType)
		{
			field = type.GetField(name, bindingAttr);
			if (field != null)
			{
				break;
			}
		}
		fields[name] = field;
		return field;
	}
}
