using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;

// StringExtensions
public static class StringExtensions
{
    // 1. ToTitleCase
    public static string ToTitleCase(this string str)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());
    }

    // 2. Truncate
    public static string Truncate(this string str, int maxLength)
    {
        return str.Length <= maxLength ? str : str.Substring(0, maxLength) + "...";
    }

    // 3. Split
    public static string[] Split(this string source, string delimiter, StringSplitOptions options = StringSplitOptions.None)
    {
        return source.Split(new[] { delimiter }, options);
    }

    // 4. ToEnum
    public static TEnum ToEnum<TEnum>(this string value) where TEnum : struct
    {
        return (TEnum)Enum.Parse(typeof(TEnum), value, true);
    }

    // 5. ToJson
    public static string ToJson(this object obj)
    {
        return JsonConvert.SerializeObject(obj);
    }

    // 6. FromJson
    public static T FromJson<T>(this string json)
    {
        return JsonConvert.DeserializeObject<T>(json);
    }
}

// CollectionExtensions
public static class CollectionExtensions
{
    // 1. RemoveAll
    public static void RemoveAll<T>(this ICollection<T> collection, Func<T, bool> predicate)
    {
        var itemsToRemove = collection.Where(predicate).ToList();
        foreach (var item in itemsToRemove)
        {
            collection.Remove(item);
        }
    }

    // 2. IsEmpty
    public static bool IsEmpty<T>(this IEnumerable<T> source)
    {
        return !source.Any();
    }

    // 3. ChunkBy
    public static IEnumerable<IEnumerable<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
    {
        return source
            .Select((x, i) => new { Index = i, Value = x })
            .GroupBy(x => x.Index / chunkSize)
            .Select(x => x.Select(v => v.Value));
    }

    // 4. AddRange
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    // 5. DistinctBy
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
        this IEnumerable<TSource> source, 
        Func<TSource, TKey> keySelector)
    {
        var seenKeys = new HashSet<TKey>();
        foreach (var element in source)
        {
            if (seenKeys.Add(keySelector(element)))
            {
                yield return element;
            }
        }
    }

    // 6. Shuffle
    private static readonly Random rng = new Random();
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    // 7. Flatten
    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> source)
    {
        return source.SelectMany(x => x);
    }

    // 8. NotNull
    public static IEnumerable<T> NotNull<T>(this IEnumerable<T> source) where T : class
    {
        return source.Where(x => x != null);
    }

    // 9. MinBy
    public static TSource MinBy<TSource, TKey>(
        this IEnumerable<TSource> source, 
        Func<TSource, TKey> selector) where TKey : IComparable<TKey>
    {
        return source.Aggregate((min, x) => selector(x).CompareTo(selector(min)) < 0 ? x : min);
    }

    // 10. MaxBy
    public static TSource MaxBy<TSource, TKey>(
        this IEnumerable<TSource> source, 
        Func<TSource, TKey> selector) where TKey : IComparable<TKey>
    {
        return source.Aggregate((max, x) => selector(x).CompareTo(selector(max)) > 0 ? x : max);
    }
}

// DateTimeExtensions
public static class DateTimeExtensions
{
    // 1. StartOfDay
    public static DateTime StartOfDay(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    // 2. EndOfDay
    public static DateTime EndOfDay(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(1).AddTicks(-1);
    }

    // 3. Age
    public static int Age(this DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}

// NumericExtensions
public static class NumericExtensions
{
    // 1. IsEven
    public static bool IsEven(this int number)
    {
        return number % 2 == 0;
    }

    // 2. IsOdd
    public static bool IsOdd(this int number)
    {
        return number % 2 != 0;
    }
}

// ObjectExtensions
public static class ObjectExtensions
{
    // 1. IfNotNull
    public static void IfNotNull<T>(this T obj, Action<T> action) where T : class
    {
        if (obj != null)
        {
            action(obj);
        }
    }

    // 2. With
    public static T With<T>(this T obj, Action<T> action)
    {
        action(obj);
        return obj;
    }

    // 3. ToMaybe
    public static Maybe<T> ToMaybe<T>(this T? value) where T : struct
    {
        return value.HasValue ? value.Value : Maybe<T>.None;
    }

    public static Maybe<T> ToMaybe<T>(this T value) where T : class
    {
        return value != null ? value : Maybe<T>.None;
    }

    // 4. In
    public static bool In<T>(this T source, params T[] list)
    {
        return list.Contains(source);
    }

    // 5. Pipe
    public static TResult Pipe<T, TResult>(this T input, Func<T, TResult> func)
    {
        return func(input);
    }
}

// ConcurrentDictionaryExtensions
public static class ConcurrentDictionaryExtensions
{
    // 1. GetOrAdd
    public static TValue GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory)
    {
        return dictionary.GetOrAdd(key, valueFactory);
    }
}

// Usage examples
public class Program
{
    public static void Main()
    {
        // StringExtensions usage
        string title = "hello world".ToTitleCase();
        string truncated = "This is a very long string that needs to be shortened".Truncate(20);
        
        Console.WriteLine(title); // Hello World
        Console.WriteLine(truncated); // This is a very lon...

        // CollectionExtensions usage
        var numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        numbers.RemoveAll(n => n % 2 == 0); // Remove all even numbers
        bool isEmpty = numbers.IsEmpty();
        var chunks = numbers.ChunkBy(3);
        
        Console.WriteLine(isEmpty); // False
        foreach (var chunk in chunks)
        {
            Console.WriteLine(string.Join(", ", chunk)); // 1, 3, 5, 7, 9 (each in separate lines)
        }

        // DateTimeExtensions usage
        DateTime now = DateTime.Now;
        DateTime startOfDay = now.StartOfDay();
        DateTime endOfDay = now.EndOfDay();
        int age = new DateTime(1990, 5, 28).Age();
        
        Console.WriteLine(startOfDay); // Current day's start time
        Console.WriteLine(endOfDay); // Current day's end time
        Console.WriteLine(age); // Age calculated from birthdate

        // NumericExtensions usage
        int number = 5;
        bool isEven = number.IsEven();
        bool isOdd = number.IsOdd();
        
        Console.WriteLine(isEven); // False
        Console.WriteLine(isOdd); // True

        // ObjectExtensions usage
        string message = "Hello, World!";
        message.IfNotNull(m => Console.WriteLine(m)); // Prints "Hello, World!"

        var person = new Person { Name = "Alice" }
            .With(p => p.Age = 30)
            .With(p => p.Address = "123 Main St");
        // person will have Name = "Alice", Age = 30, and Address = "123 Main St"

        var json = person.ToJson();
        var deserializedPerson = json.FromJson<Person>();

        int? nullableNumber = 5;
        var maybeNumber = nullableNumber.ToMaybe(); // Maybe<int> with value 5

        int value = 2;
        bool isInList = value.In(1, 2, 3); // true

        var result = "hello"
            .Pipe(str => str.ToUpper())
            .Pipe(str => str + " WORLD");
        // result is "HELLO WORLD"

        // ConcurrentDictionaryExtensions usage
        var dictionary = new ConcurrentDictionary<int, string>();
        string dictValue = dictionary.GetOrAdd(1, key => "Value for " + key); // "Value for 1"
    }
}

public struct Maybe<T>
{
    public static readonly Maybe<T> None = new Maybe<T>();

    public T Value { get; }
    public bool HasValue { get; }

    private Maybe(T value)
    {
        Value = value;
        HasValue = true;
    }

    public static implicit operator Maybe<T>(T value) => new Maybe<T>(value);
}

public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Address { get; set; }
}
