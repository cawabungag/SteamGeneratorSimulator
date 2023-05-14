using System;

namespace States.Implementation
{
	public abstract class BaseState : IState
	{
		public abstract Guid StateId { get; }
		public abstract Action<bool> OnComplete { get; set; }
		public abstract StateType Type { get; }
		public abstract StateStatus Status { get; set; }
		public abstract float Duration { get; }
		public abstract void Execute(float deltaTime);
		public void Start()
		{
			throw new NotImplementedException();
		}

	}
}