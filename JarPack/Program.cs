using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;

namespace JarPack
{
	class Program
	{
		private static EnumerationOptions EnumerationOptions => new EnumerationOptions
		{
			RecurseSubdirectories = true
		};

		private static List<string> Extensions =>
			new List<string>
			{
				@".bmp",
				@".css",
				@".gif",
				@".ico",
				@".jar",
				@".jpeg",
				@".jpg",
				@".js",
				@".png",
				@".svg",
				@".tif",
				@".tiff"
			};

		//private const string broadleafDir = @"C:\Broadleaf\DemoSite-broadleaf-6.0.4-GA";
		private const string broadleafDir = @"\\192.168.18.130\ryan\.m2\repository\org\broadleafcommerce";
		private const string jarExtension = @".jar";

		private static string ComputeHash<T>(string filename) where T : HashAlgorithm
		{
			using (var stream = File.OpenRead(filename))
				return stream.ComputeHash<T>();
		}

		private static void Main(string[] args)
		{
			Unpack();
			// Pack();
		}

		private static void Pack()
		{
			var jars = Directory.EnumerateFiles(broadleafDir, @"*.*", EnumerationOptions)
				.Where(item => item.EndsWith(@".jar", StringComparison.OrdinalIgnoreCase))
				.Reverse();

			foreach (var jar in jars)
			{
				var dirName = string.Concat(jar, @".unpack");
				if (!Directory.Exists(dirName))
					continue;

				var files = Directory.EnumerateFiles(dirName, @"*.*", EnumerationOptions)
					.Select(item => item.Replace($"{dirName}{Path.DirectorySeparatorChar}", string.Empty))
					.Where(item => !item.Contains($@".unpack{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
					.ToList();

				if (files.Count == 0)
					continue;

				var dirty = new List<string>();
				using (var zipArchive = new ZipArchive(File.OpenRead(jar)))
				{
					foreach (var file in files)
					{
						var entry = zipArchive.Entries.FirstOrDefault(item =>
						{
							var fullName = item.FullName.Convert();
							return fullName.Equals(file, StringComparison.OrdinalIgnoreCase);
						});

						if (entry == null)
							throw new Exception($@"File: {file} does not exist in Archive: {jar}");

						var path = Path.Combine(dirName, entry.FullName.Convert());

						var fileInfo = new FileInfo(path);
						if (entry.Length == fileInfo.Length &&
							entry.LastWriteTime.DateTime == fileInfo.LastWriteTime.ToTwoSecondPrecision())
							continue;

						var fileHash = ComputeHash<SHA1>(path);
						using (var stream = entry.Open())
						{
							if (stream.ComputeHash<SHA1>() == fileHash)
								continue;
						}

						dirty.Add(file);
					}
				}

				if (dirty.Count == 0)
					continue;

				using (var jarStream = new FileStream(jar, FileMode.Open))
				using (var zipArchive = new ZipArchive(jarStream, ZipArchiveMode.Update))
				{
					foreach (var file in dirty)
					{
						var entry = zipArchive.Entries.FirstOrDefault(item =>
						{
							var fullName = item.FullName.Convert();
							return fullName.Equals(file, StringComparison.OrdinalIgnoreCase);
						});

						if (entry == null)
							throw new Exception($@"File: {file} does not exist in Archive: {jar}");

						Console.WriteLine($@"Updating: {entry.FullName}");
						entry.Delete();

						var newEntry = zipArchive.CreateEntry(entry.FullName, CompressionLevel.Optimal);

						var path = Path.Combine(dirName, entry.FullName.Convert());
						var fileInfo = new FileInfo(path);

						using (var stream = newEntry.Open())
						using (var fileStream = File.OpenRead(path))
						{
							fileStream.CopyTo(stream);
							stream.Flush();
						}

						newEntry.LastWriteTime = fileInfo.LastWriteTime.ToTwoSecondPrecision();
					}
				}
				
			}
		}

		private static void Unpack()
		{
			var queue = Directory.EnumerateFiles(broadleafDir, @"*.*", new EnumerationOptions
				{
					RecurseSubdirectories = true
				})
				.Where(item => item.EndsWith(@".jar", StringComparison.OrdinalIgnoreCase))
				.Where(item => !item.Contains($@".unpack{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
				.ToList();
			
			while (queue.Count > 0)
			{
				var jar = queue[0];
				queue.RemoveAt(0);

				// Console.WriteLine($@"Unzipping: {jar}");

				var dirName = string.Concat(jar, @".unpack");
				// Directory.CreateDirectory(dirName);

				var zipArchive = ZipFile.OpenRead(jar);
				foreach (var entry in zipArchive.Entries)
				{
					var fullName = entry.FullName.Convert();

					if (fullName.EndsWith(@"\"))
						continue;

					//var extension = Path.GetExtension(fullName);
					//if (!Extensions.Contains(extension, StringComparison.OrdinalIgnoreCase))
					//	continue;

					var path = Path.Combine(dirName, fullName);

					if (File.Exists(path))
					{
						var fileInfo = new FileInfo(path);
						if (fileInfo.Length == entry.Length &&
							entry.LastWriteTime.DateTime == fileInfo.LastWriteTime.ToTwoSecondPrecision())
							continue;
					}

					if (path.EndsWith(jarExtension, StringComparison.OrdinalIgnoreCase))
					{
						queue.RemoveAll(item => item.Equals(path));
						queue.Add(path);
					}

					Console.WriteLine($@"Inflating: {entry.FullName}");
					var dstDir = Path.GetDirectoryName(path);
					Directory.CreateDirectory(dstDir);
					entry.ExtractToFile(path, true);
				}
			}
		}
	}
}
