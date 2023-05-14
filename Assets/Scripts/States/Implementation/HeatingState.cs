using System;
using DefaultNamespace;

namespace States.Implementation
{
	public class HeatingState : IState
	{
		public Guid StateId { get; }
		public Action<bool> OnComplete { get; set; }
		public StateType Type => StateType.Heating;
		public StateStatus Status { get; set; }

		public float Duration { get; private set; }

		private double _startTime;
		private double _finishTime;
		private IStateConfiguration _configuration;

		public HeatingState(IStateConfiguration configuration, Guid stateId)
		{
			_configuration = configuration;
			StateId = stateId;
			Duration = configuration.Duration;
		}

		public void Start()
		{
			_startTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			_finishTime = _startTime + _configuration.Duration;
		}

		public void Execute(float deltaTime)
		{
			var unixTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			Duration = (float) (_finishTime - unixTimeSeconds);

			if (unixTimeSeconds >= _finishTime)
			{
				OnComplete?.Invoke(true);
				return;
			}

			var deltaTemperature = deltaTime * Utils.TEMPERATURE_HEATING_TO_100_IN_SECOND;
			SteamGeneratorActor.Instance.SetTemperature(deltaTemperature);
		}
	}
}