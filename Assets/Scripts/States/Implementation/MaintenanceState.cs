using System;

namespace States.Implementation
{
	public class MaintenanceState : IState
	{
		public Guid StateId { get; }
		public Action<bool> OnComplete { get; set; }
		public StateType Type => StateType.Maintenance;
		public StateStatus Status { get; set; }
		public float Duration { get; private set; }

		private double _startTime;
		private double _finishTime;
		private IStateConfiguration _configuration;

		public MaintenanceState(IStateConfiguration configuration)
		{
			_configuration = configuration;
			StateId = Guid.NewGuid();
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

			if (!(unixTimeSeconds >= _finishTime))
				return;

			OnComplete?.Invoke(true);
		}
	}
}