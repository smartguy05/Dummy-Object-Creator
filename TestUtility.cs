using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Portal.Common.Utility
{
    public static class TestUtility
    {
        private static Random random = new Random();

        /// <summary>
        /// Compares two objects of any type based on field names. If objects are lists the objects must be sorted 1:1, in order. Only compares counts for embedded IEnumberable objects
        /// </summary>
        /// <param name="first">First object to compare</param>
        /// <param name="second">Second object to compare</param>
        /// <param name="notTested">Tuple of field name and value not tested because a match could not be found on the second object.
        /// Only objects that have been checked until loop fails</param>
        /// <returns></returns>
        public static bool CompareObjects(object first, object second, out List<Tuple<string, dynamic>> notTested)
        {
            if (!(first is string) && first is IEnumerable)
            {
                var firstList = (IList)first;
                var secondList = (IList)second;
                var pass = true;
                notTested = new List<Tuple<string, dynamic>>();
                for (var i = 0; i < firstList.Count; i++)
                {
                    var notTestedLoop = new List<Tuple<string, dynamic>>();
                    var objectsEqual = CompareObjectsWork(firstList[i], secondList[i], out notTestedLoop);
                    notTested.AddRange(notTestedLoop);
                    if (!objectsEqual)
                    {
                        pass = false;
                        break;
                    }
                }

                return pass;
            }
            else
            {
                return CompareObjectsWork(first, second, out notTested);
            }
        }

        public static T DeepCopy<T>(this T objectToCopy) where T : new()
        {
            var returnValue = Activator.CreateInstance<T>();
            var props = returnValue.GetType().GetProperties().ToList();

            foreach (var property in props)
            {
                if (!property.CanWrite)
                {
                    continue;
                }

                var value = property.GetValue(objectToCopy, null);
                property.SetValue(returnValue, value);
            }

            return returnValue;
        }

        /// <summary>
        /// Creates an IEnumerable of the specified type full of dummy values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">new instance of type you want dummy values for</param>
        /// <param name="numberOfItems">number of items to create for the list</param>
        /// <returns></returns>
        public static IEnumerable<T> PopulateListOfObjects<T>(int numberOfItems)
        {
            for (var i = 0; i < numberOfItems; i++)
            {
                yield return PopulateObject<T>();
            }
        }

        /// <summary>
        /// Creates an object of the specified type full of random dummy values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">new instanc eof the type you want dummy values for</param>
        /// <returns></returns>
        public static T PopulateObject<T>()
        {
            try
            {
                var obj = Activator.CreateInstance(typeof(T));
                var props = obj.GetType().GetProperties()
                    .ToList();

                foreach (var property in props)
                {
                    if (!property.CanWrite)
                    {
                        continue;
                    }

                    var value = property.GetValue(obj, null);
                    var type = property.PropertyType;

                    var newObj = SetSimpleObjectValue(type, value);

                    property.SetValue(obj, newObj);
                }

                return (T)obj;
            }
            catch (Exception)
            {
                return (T)SetSimpleObjectValue(typeof(T), null);
            }
        }

        private static bool CompareObjectsWork(object first, object second, out List<Tuple<string, dynamic>> notTested)
        {
            notTested = new List<Tuple<string, dynamic>>();

            if (first is string)
            {
                return first.Equals(second);
            }

            if (first == null)
            {
                return first == second;
            }

            var firstProps = first.GetType().GetProperties().ToList();
            var secondProps = second.GetType().GetProperties().ToList();

            var pass = true;

            if (firstProps.Count == 0)
            {
                pass = first.Equals(second);
            }

            foreach (var prop in firstProps)
            {
                var value1 = prop.GetValue(first, null);
                var val1Type = value1?.GetType();
                var prop2 = secondProps.FirstOrDefault(s => s.Name == prop.Name);

                if (prop2 == null)
                {
                    notTested.Add(new Tuple<string, dynamic>(prop.Name, value1));
                    continue;
                }

                var value2 = prop2.GetValue(second, null);
                if (value1 == null)
                {
                    if (value2 == null)
                    {
                        continue;
                    }

                    pass = false;
                    break;
                }

                if (!(value1 is string) && value1 is IEnumerable)
                {
                    var notTested2 = new List<Tuple<string, dynamic>>();
                    var embeddedListPass = CompareObjects(value1, value2, out notTested2);
                    notTested.AddRange(notTested2);

                    if (!embeddedListPass)
                    {
                        pass = false;
                        break;
                    }
                }
                else if (!val1Type.IsPrimitive && !val1Type.IsEnum && val1Type.IsClass && !(value1 is string))
                {
                    var notTested2 = new List<Tuple<string, dynamic>>();
                    var embeddedObjectPass = CompareObjectsWork(value1, value2, out notTested2);
                    notTested.AddRange(notTested2);

                    if (!embeddedObjectPass)
                    {
                        pass = false;
                        break;
                    }
                }
                else if (val1Type == typeof(DateTime) || val1Type == typeof(DateTime?))
                {
                    if (value1?.ToString() != value2.ToString())
                    {
                        pass = false;
                        break;
                    }
                }
                else if (!value1.Equals(value2))
                {
                    pass = false;
                    break;
                }
            }

            return pass;
        }

        private static DateTime GetRandomDatetime()
        {
            var start = new DateTime(1995, 1, 1);
            var range = (DateTime.Today - start).Days;
            return start.AddDays(random.Next(range));
        }

        private static string GetRandomString()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static bool IsNumericType(this object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;

                default:
                    return false;
            }
        }

        private static object PopulateObjectOfUnknownType(Type type)
        {
            try
            {
                var method = typeof(TestUtility).GetMethod("PopulateObject");
                var methodRef = method.MakeGenericMethod(type);
                return methodRef.Invoke(null, null);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static object SetSimpleObjectValue(Type type, object value)
        {
            if ((type.Namespace == "System.Collections.Generic" && type != typeof(string)))
            {
                var thisType = type.GenericTypeArguments.FirstOrDefault()?.UnderlyingSystemType ?? type.GetElementType();
                var listType = typeof(List<>).MakeGenericType(thisType);
                var newList = Activator.CreateInstance(listType);

                try
                {
                    for (var i = 0; i < 3; i++)
                    {
                        if (thisType.IsValueType)
                        {
                            value = Activator.CreateInstance(thisType);
                        }

                        var newObj = !thisType.IsPrimitive && !thisType.IsEnum && thisType.IsClass && thisType != typeof(string)
                            ? PopulateObjectOfUnknownType(thisType)
                            : SetSimpleObjectValue(thisType, value);
                        ((IList)newList).Add(newObj);
                    }

                    return newList;
                }
                catch (MissingMethodException)
                {
                    return null;
                }
            }

            if (type.IsArray)
            {
                var thisType = type.GetElementType();
                var newList = Array.CreateInstance(thisType, 3);

                try
                {
                    for (var i = 0; i < 3; i++)
                    {
                        if (thisType.IsValueType)
                        {
                            value = Activator.CreateInstance(thisType);
                        }

                        var newObj = !thisType.IsPrimitive && !thisType.IsEnum && thisType.IsClass && thisType != typeof(string)
                            ? PopulateObjectOfUnknownType(thisType)
                            : SetSimpleObjectValue(thisType, value);
                        newList.SetValue(newObj, i);
                    }

                    return newList;
                }
                catch (MissingMethodException)
                {
                    return null;
                }
            }

            if (type == typeof(bool))
            {
                var trueFalse = random.Next() % 2 == 0;

                return trueFalse;
            }

            if (value is Enum)
            {
                var options = Enum.GetNames(type).Length - 1;
                var rdm = random.Next(0, options);
                return Enum.Parse(type, rdm.ToString());
            }

            if (value != null && value.IsNumericType())
            {
                if (type != typeof(int) && type != typeof(short) && type != typeof(long))
                {
                    var val = random.Next(1000) + random.NextDouble();
                    var returnVal = Convert.ChangeType(val, type);
                    return returnVal;
                }
                else
                {
                    var val = random.Next(1000);
                    var returnVal = Convert.ChangeType(val, type);
                    return returnVal;
                }
            }

            if (type == typeof(string))
            {
                return GetRandomString();
            }

            if (type == typeof(char))
            {
                return GetRandomString()[0];
            }

            if (type == typeof(DateTime))
            {
                return GetRandomDatetime();
            }

            if (type == typeof(Guid))
            {
                return Guid.NewGuid();
            }

            if (!type.IsPrimitive && !type.IsEnum && type.IsClass)
            {
                try
                {
                    var newObj = PopulateObjectOfUnknownType(type);
                    return newObj;
                }
                catch (MissingMethodException)
                {
                    return null;
                }
            }

            if (type.IsValueType && value == null)
            {
                var underlyingType = Nullable.GetUnderlyingType(type);
                var trueFalse = random.Next() % 2 == 0;

                if (underlyingType == typeof(DateTime))
                {
                    var newVal = GetRandomDatetime();
                    return trueFalse ? newVal : value;
                }
                else
                {
                    var newVal = Convert.ChangeType(random.Next(1000), Nullable.GetUnderlyingType(type));
                    return trueFalse ? newVal : value;
                }
            }

            return null;
        }
    }
}
