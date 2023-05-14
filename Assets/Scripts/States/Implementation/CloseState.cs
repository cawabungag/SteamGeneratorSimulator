using System;

namespace States.Implementation
{
	public class CloseState : IState
	{
		public Guid StateId { get; }
		public Action<bool> OnComplete { get; set; }
		public StateType Type => StateType.Close;
		public StateStatus Status { get; set; }
		public float Duration { get; }

		public CloseState(Guid stateId)
		{
			StateId = stateId;
		}

		public void Start()
		{
			SteamGeneratorActor.Instance.SetOpenState(false);
		}

		public void Execute(float deltaTime)
		{
			OnComplete?.Invoke(true);
		}
	}
}