using Newtonsoft.Json;

public class CreateMeasureDto
{
	[JsonProperty("title")]
	public string Title { get; }

	[JsonProperty("valve")]
	public float Value { get; }

	public CreateMeasureDto(string title, float value)
	{
		Title = title;
		Value = value;
	}
}