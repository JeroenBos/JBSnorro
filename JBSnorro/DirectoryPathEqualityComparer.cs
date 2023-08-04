using JBSnorro.Diagnostics;

namespace JBSnorro;

/// <summary>
/// Compares two directory paths for equality, dismissing any final directory separator.
/// </summary>
public sealed class DirectoryPathEqualityComparer : IEqualityComparer<string>
{
	/// <summary>
	/// Gets the singleton instance.
	/// </summary>
	public static readonly IEqualityComparer<string> Instance = new DirectoryPathEqualityComparer();
	private static readonly string DirectorySeparator = Path.DirectorySeparatorChar.ToString();
	/// <summary>
	/// Gest whether two directory paths are equal, dismissing any final directory separator.
	/// </summary>
	/// <param name="x"> An absolute directory path to compare to y. </param>
	/// <param name="y"> An absolute directory path to compare to x. </param>
	public static bool Equals(string x, string y) => Instance.Equals(x, y);

	bool IEqualityComparer<string>.Equals(string? x, string? y)
	{
		if (x == null && y == null) return true;
		if (x == null || y == null) return false;

		Contract.Requires(Uri.TryCreate(x, UriKind.Absolute, out _));
		Contract.Requires(Uri.TryCreate(y, UriKind.Absolute, out _));

		if (x.EndsWith(DirectorySeparator) == y.EndsWith(DirectorySeparator))
		{
			return x == y;
		}
		else if (x.EndsWith(DirectorySeparator))
		{
			return x == y + DirectorySeparator;
		}
		else
		{
			return x + DirectorySeparator == y;
		}
	}

	int IEqualityComparer<string>.GetHashCode(string obj)
	{
		Contract.Requires(obj != null);
		Contract.Requires(!obj.EndsWith(DirectorySeparator + DirectorySeparator));

		if (obj.EndsWith(DirectorySeparator))
			return obj.Substring(0, obj.Length - 1).GetHashCode();
		else
			return obj.GetHashCode();
	}
}
