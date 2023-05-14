using Newtonsoft.Json;
using States;

public class UpdateStateDto
{
	[JsonProperty("type")]
	public StateType StateType { get; private set; }

	[JsonProperty("status")]
	public StateStatus Status { get; private set; }

	[JsonProperty("duration")]
	public float Duration { get; private set; }

	public UpdateStateDto(StateType stateType, StateStatus status, float duration)
	{
		StateType = stateType;
		Status = status;
		Duration = duration;
	}
}