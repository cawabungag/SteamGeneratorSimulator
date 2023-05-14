using System;

namespace States.Implementation
{
	public class PluginOut : IState
	{
		public Guid StateId { get; }
		public Action<bool> OnComplete { get; set; }
		public StateType Type => StateType.PluggingOut;
		public StateStatus Status { get; set; }
		public float Duration { get; }

		public PluginOut(Guid stateId)
		{
			StateId = stateId;
		}

		public void Start()
		{
			SteamGeneratorActor.Instance.SetPlugin(false);
			SteamGeneratorActor.Instance.SetTemperature(0);
		}

		public void Execute(float deltaTime)
		{
			OnComplete?.Invoke(true);
		}
	}
}