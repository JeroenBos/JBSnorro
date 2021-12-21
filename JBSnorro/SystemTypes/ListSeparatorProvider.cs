using System;
using System.Collections.Generic;
using System.Text;

namespace JBSnorro.SystemTypes
{
	internal static class ListSeparatorProvider
	{
		public static string GetCurrentCultureListSeparator()
		{
			return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
		}
	}
}
