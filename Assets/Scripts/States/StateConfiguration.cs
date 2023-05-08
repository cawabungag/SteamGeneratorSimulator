namespace States
{
	public class StateConfiguration : IStateConfiguration
	{
		public float Duration { get; }
		public float Temperature { get; }
		public bool IsNeedReachTemperature { get; }

		public StateConfiguration(float duration, float temperature = -1, bool isNeedReachTemperature = false)
		{
			Duration = duration;
			Temperature = temperature;
			IsNeedReachTemperature = isNeedReachTemperature;
		}
	}
}