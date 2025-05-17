using System.Text.Json.Serialization;

namespace JBSnorro.Text.Json;

	/// <summary> Indicates that the implementing json converter introduces/requires other json converter. </summary>
public interface IJsonConverterIntroducer
{
	/// <summary> Get all converts introduced/required by the current converter. </summary>
	IEnumerable<JsonConverter> IntroducedConverters { get; }
}
