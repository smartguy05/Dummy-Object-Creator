using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DummyCreator
{
  public static class DummyCreator
  {
    /// <summary>
    /// Creates an IEnumerable of the specified type full of dummy values
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj">new instance of type you want dummy values for</param>
    /// <param name="numberOfItems">number of items to create for the list</param>
    /// <returns></returns>
    public static IEnumerable<T> PopulateListOfObjects<T>(IEnumerable<T> obj, int numberOfItems) where T : new()
    {
        for (var i = 0; i < numberOfItems; i++)
        {
            var newObj = Activator.CreateInstance(typeof(T));
            yield return (T)PopulateObject(newObj);
        }
     }

     /// <summary>
     /// Creats an object of the specified type full of random dummy values
     /// </summary>
     /// <typeparam name="T"></typeparam>
     /// <param name="obj">new instanc eof the type you want dummy values for</param>
     /// <returns></returns>
     public static T PopulateObject<T>(T obj) where T: new()
     {
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

            if (type.Namespace == "System.Collections.Generic" && type != typeof(string))
            {
                var thisType = type.GenericTypeArguments[0].UnderlyingSystemType;
                var listType = typeof(List<>).MakeGenericType(thisType);
                var newList = Activator.CreateInstance(listType);
                    
                for (var i = 0; i < 3; i++)
                {
                    var newObj = Activator.CreateInstance(thisType);
                    newObj = PopulateObject(newObj);
                    ((IList) newList).Add(newObj);
                }

                property.SetValue(obj, newList);
            }
            else if (value != null && value.IsNumericType())
            {
                var rdm = new Random(DateTime.Now.Millisecond);
                    
                if (type != typeof(int) && type != typeof(short) && type != typeof(long))
                {
                    var val = rdm.Next(1000) + rdm.NextDouble();
                    var returnVal = Convert.ChangeType(val, type);
                    property.SetValue(obj, returnVal);
                }
                else
                {
                    property.SetValue(obj, rdm.Next(1000));   
                }
            }
            else if (type == typeof(string))
            {
                property.SetValue(obj, GetRandomString());
            }
            else if (type == typeof(char))
            {
                property.SetValue(obj, GetRandomString()[0]);
            }
            else if (type == typeof(DateTime))
            {
                property.SetValue(obj, GetRandomDatetime());
            }
            else if (type == typeof(Guid))
            {
                property.SetValue(obj, new Guid());
            }
            else if (!type.IsPrimitive && !type.IsEnum && type.IsClass)
            {
                var newObj = Activator.CreateInstance(type);
                newObj = PopulateObject(newObj);
                property.SetValue(obj, newObj);
            }
        }
         return obj;
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

    private static string GetRandomString()
    {
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static DateTime GetRandomDatetime()
    {
        var gen = new Random();
        var start = new DateTime(1995, 1, 1);
        var range = (DateTime.Today - start).Days;
        return start.AddDays(gen.Next(range));
    }
  }
}
