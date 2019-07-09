using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JarPack
{
	public static class ExtensionMethods
	{
		public static string ComputeHash<T>(this string instance) where T : HashAlgorithm
		{
			var bytes = new UTF8Encoding().GetBytes(instance);

			var createMethod = typeof(T).GetMethod(@"Create",
				new Type[]
				{
				});
			using (var algorithm = (T)createMethod.Invoke(null, null))
			{
				return BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace(@"-", string.Empty);
			}
		}

		public static string ComputeHash<T>(this Stream instance) where T : HashAlgorithm
		{
			var createMethod = typeof(T).GetMethod(@"Create",
				new Type[]
				{
				});
			using (var algorithm = (T)createMethod.Invoke(null, null))
			{
				return BitConverter.ToString(algorithm.ComputeHash(instance)).Replace(@"-", string.Empty);
			}
		}

		public static bool Contains(this IEnumerable<string> instance, string value, StringComparison stringComparison)
		{
			return instance.Any(item => item.Equals(value, stringComparison));
		}

		public static string Convert(this string instance)
		{
			return Path.DirectorySeparatorChar == '\\'
				? instance.Replace('/', Path.DirectorySeparatorChar)
				: instance;
		}

		public static Task ForEachAsync<T>(this IEnumerable<T> instance, Func<T, Task> action)
		{
			return Task.WhenAll(
				from item in instance
				select Task.Run(() => action(item)));
		}

		public static DateTime ToTwoSecondPrecision(this DateTime instance)
		{
			var second = (int)Math.Round(instance.Second / 2d, MidpointRounding.AwayFromZero) * 2;
			return new DateTime(instance.Year, instance.Month, instance.Day, instance.Hour, instance.Minute, second);
		}
	}
}
