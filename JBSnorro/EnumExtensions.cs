using JBSnorro.Diagnostics;
using System;
using System.IO;

namespace JBSnorro
{
	public static class EnumExtensions
	{
		/// <summary> Writes the specified enum to the binary writer. </summary>
		/// <typeparam name="TEnum"> An enum type. </typeparam>
		/// <param name="writer"> The binary writer to write the enum to. </param>
		/// <param name="value"> The enum value to write. </param>
		public static void Write<TEnum>(this BinaryWriter writer, TEnum value) where TEnum : struct//UnconstrainedMelody.IEnumConstraint
		{
			Contract.Requires(writer != null);
			Contract.Requires(typeof(TEnum).IsEnum);

			//currently, the enums are written as longs, but that may be changed to the underlying primitive type of TEnum
			writer.Write((long)(object)value);
		}

		/// <summary> Reads an enum value from the specified reader. </summary>
		/// <typeparam name="TEnum"> The type of the enum to read. </typeparam>
		/// <param name="reader"> The reader to read the enum value from. </param>
		public static TEnum Read<TEnum>(this BinaryReader reader) where TEnum : struct//UnconstrainedMelody.IEnumConstraint
		{
			Contract.Requires(reader != null);
			Contract.Requires(typeof(TEnum).IsEnum);

			//currently, the enums are written as longs, but that may be changed to the underlying primitive type of TEnum
			long result = reader.ReadInt64();
			return (TEnum)(object)result;
		}

		/// <summary> Returns whether the specified value of the enum is defined. </summary>
		public static bool IsDefined<TEnum>(this TEnum enumValue) where TEnum : struct
		{
			return Enum.IsDefined(typeof(TEnum), enumValue);
		}
	}
}
