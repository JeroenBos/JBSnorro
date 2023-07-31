using System.Text;

namespace JBSnorro.Text;

/// <summary>
/// Subclasses the built-in <see cref="StringBuilder"/> to make the EoL string configurable.
/// </summary>
public sealed class ConfigurableStringBuilder : StringBuilderAdapter
{
	public string Newline { get; }
	public ConfigurableStringBuilder(StringBuilder? stringBuilder = null,
									 string newline = "\n")
		: base(stringBuilder ?? new())
	{
		this.Newline = newline;
	}

	public override StringBuilder AppendLine()
	{
		return base.Append(this.Newline);
	}
	public override StringBuilder AppendLine(string value)
	{
		return base.Append(value).Append(this.Newline);
	}
}
