using System;

namespace States.Implementation
{
	public class OpenState : IState
	{
		public Guid StateId { get; }
		public Action<bool> OnComplete { get; set; }
		public StateType Type => StateType.Open;
		public StateStatus Status { get; set; }
		public float Duration { get; }

		public OpenState(Guid stateId)
		{
			StateId = stateId;
		}

		public void Start()
		{
			SteamGeneratorActor.Instance.SetOpenState(true);
			SteamGeneratorActor.Instance.SetTemperature(0);
		}

		public void Execute(float deltaTime)
		{
			OnComplete?.Invoke(true);
		}
	}
}