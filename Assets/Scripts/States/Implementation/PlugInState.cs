using System;

namespace States.Implementation
{
	public class PlugInState : IState
	{
		public Guid StateId { get; }
		public Action<bool> OnComplete { get; set; }
		public StateType Type => StateType.PluggingIn;
		public StateStatus Status { get; set; }
		public float Duration { get; }

		public PlugInState(Guid stateId)
		{
			StateId = stateId;
		}

		public void Start()
		{
			SteamGeneratorActor.Instance.SetPlugin(true);
		}

		public void Execute(float deltaTime)
		{
			OnComplete?.Invoke(true);
		}
	}
}