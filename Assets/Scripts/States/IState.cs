using System;

namespace States
{
	public interface IState
	{
		Guid StateId { get; }
		Action<bool> OnComplete { get; set; }
		StateType Type { get; }
		StateStatus Status { get; set; }
		float Duration { get; }
		void Start();
		void Execute(float deltaTime);
	}
}