using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Net.WebRequestMethods;

namespace JBSnorro
{
	public static class IOExtensions
	{
		/// <summary> Reads a dictionary from the stream. </summary>
		public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(this BinaryReader reader, Func<BinaryReader, TKey> readKey, Func<BinaryReader, TValue> readValue)
		{
			Contract.Requires(reader != null);
			Contract.Requires(readKey != null);
			Contract.Requires(readValue != null);

			var result = new Dictionary<TKey, TValue>();
			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				TKey key = readKey(reader);
				TValue value = readValue(reader);
				result.Add(key, value);
			}

			return result;
		}
		/// <summary> Writes the specified dictionary to the stream. </summary>
		public static void Write<TKey, TValue>(this BinaryWriter writer, Dictionary<TKey, TValue> dictionary, Action<BinaryWriter, TKey> keyWriter, Action<BinaryWriter, TValue> valueWriter)
		{
			Contract.Requires(writer != null);
			Contract.Requires(dictionary != null);
			Contract.Requires(keyWriter != null);
			Contract.Requires(valueWriter != null);

			writer.Write(dictionary.Count);
			foreach (var v in dictionary)
			{
				keyWriter(writer, v.Key);
				valueWriter(writer, v.Value);
			}
		}
		/// <summary> Gets the entire current contents of the stream as span. </summary>
		public static Span<byte> AsSpan(this MemoryStream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (stream.Length > int.MaxValue)
				throw new OverflowException();

			return new Span<byte>(stream.GetBuffer()).Slice(0, (int)stream.Length);
		}
		/// <summary> Gets a new temporary directory. </summary>
		public static string CreateTempDirectory()
		{
			string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempDirectory);
			return tempDirectory.Replace('\\', '/');
		}
		/// <summary> Gets a new temporary directory, and deletes it on disposal. </summary>
		public static Disposable<string> CreateTemporaryDirectory()
		{
			string tempDirectory = CreateTempDirectory();
			return new Disposable<string>(tempDirectory, [DebuggerHidden] () => Directory.Delete(tempDirectory));
		}
#nullable enable
		/// <summary>
		/// Normalizes the path. On case-insensitive file systems, equality should still be compared case-insensitively.
		/// </summary>
		/// <seealso href="https://stackoverflow.com/a/21058121/308451"/>
		public static string NormalizePath(this string path)
		{
			return Path.GetFullPath(new Uri(path).LocalPath)
					   .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}
		/// <summary>
		/// Gets whether the specified dir is a subfolder (recursively) of the specified directory info.
		/// </summary>
		public static bool Contains(this DirectoryInfo @this, string dir)
		{
			var normalizedThis = @this.ToString().NormalizePath();
			var normalizedDir = dir.NormalizePath();

			return normalizedThis.StartsWith(normalizedDir);
		}
		/// <summary>
		/// Copies a directory and its content.
		/// </summary>
		/// <seealso href="https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories"/>
		public static void CopyDirectory(string sourceDirName, string destDirName, bool recursively = true)
		{
			CopyDirectory(sourceDirName, destDirName, recursively ? GlobPatternCollection.Empty : GlobPatternCollection.SubdirectoriesPattern);
		}
		public static void CopyDirectory(string sourceDirName, string destDirName, string ignoreFile)
		{
			CopyDirectory(sourceDirName, destDirName, GlobPatternCollection.FromFile(ignoreFile));
		}
		public static void CopyDirectory(string sourceDirName, string destDirName, GlobPatternCollection ignorePatterns)
		{
			// TODO: copy argument checking from below
			var source = new DirectoryInfo(sourceDirName);
			if (!source.Exists)
				throw new DirectoryNotFoundException($"Source root does not exist or could not be found: '{sourceDirName}'");
			var destDir = new DirectoryInfo(destDirName);
			if (destDir.Contains(sourceDirName))
				throw new ArgumentException("The destination directory cannot contain the source");
			if (source.Contains(destDirName))
				throw new ArgumentException("The source directory cannot contain the destination.");


			destDir.Create();

			foreach (FileInfo file in source.EnumerateFiles())
			{
				if (!ignorePatterns.Matches(file.Name))
				{
					string dst = Path.Combine(destDirName, file.Name);
					file.CopyTo(dst, overwrite: true);
				}
			}


			foreach (DirectoryInfo subdir in source.EnumerateDirectories())
			{
				if (!ignorePatterns.MatchesSubdirectory(subdir.Name))
				{
					var subdirIgnorePatterns = ignorePatterns.ForSubdirectory(subdir.Name);
					string destSubdir = Path.Combine(destDirName, subdir.Name);
					CopyDirectory(subdir.FullName, destSubdir, subdirIgnorePatterns);
				}
			}
		}
   
		public static string CloneDirectoryTemporarily(string dir, string? ignoreFile = null) => TemporarilyDuplicate(dir, ignoreFile);
		/// <summary>
		/// Duplicates a directory to a temporary location.
		/// </summary>
		public static string TemporarilyDuplicate(string dir, string? ignoreFile = null)
		{
			var dstDir = CreateTemporaryDirectory();
			CopyDirectory(dir, dstDir, ignoreFile == null ? GlobPatternCollection.Empty : GlobPatternCollection.FromFile(ignoreFile));
			return dstDir;
		}
#nullable restore
		/// <summary> Gets whether the path is a full path in the current OS. </summary>
		/// <see href="https://stackoverflow.com/a/35046453/308451" />
		public static bool IsFullPath(string path)
		{
			if (OperatingSystem.IsWindows())
				return IsFullPathInWindows(path);
			else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
				return IsFullPathInUnix(path);

			throw new NotImplementedException("IsFullPath not implemented yet for current OS");
		}
		/// <summary> Gets whether the path is a full path. </summary>
		/// <see href="https://stackoverflow.com/a/2202096/308451"/>
		public static bool IsFullPathInUnix(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return false;

			// check if valid linux path:
			if (path.Contains((char)0))
				return false;

			// char 47 is '/', so we can skip checking it
			if (path.StartsWith("/"))
				return true;

			return false;
		}
		/// <summary> Gets whether the path is a full path. </summary>
		/// <see href="https://stackoverflow.com/a/35046453/308451" />
		public static bool IsFullPathInWindows(string path)
		{
			if (!OperatingSystem.IsWindows()) throw new NotImplementedException(nameof(IsFullPathInWindows) + " is only implemented on Windows");

			return !string.IsNullOrWhiteSpace(path)
				&& path.IndexOfAny(Path.GetInvalidPathChars().ToArray()) == -1
				&& Path.IsPathRooted(path)
				&& !(Path.GetPathRoot(path)?.Equals("\\", StringComparison.Ordinal) ?? false);
		}
	}
}
