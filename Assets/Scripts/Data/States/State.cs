using System;
using Newtonsoft.Json;
using States;

public class State
{
	[JsonProperty("id")]
	public Guid Id { get; private set; }

	[JsonProperty("type")]
	public StateType Type { get; private set; }

	[JsonProperty("status")]
	public StateStatus Status { get; private set; }

	[JsonProperty("duration")]
	public float Duration { get; private set; }

	[JsonProperty("createdDate")]
	public DateTimeOffset CreateDate { get; private set; }
}