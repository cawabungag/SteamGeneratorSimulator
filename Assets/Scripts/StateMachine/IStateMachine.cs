using System;
using States;

namespace DefaultNamespace.StateMachine
{
	public interface IStateMachine
	{
		void AddState(IState state);
		void Execute(float deltaTime);
		void FinishCurentState();
		void FinishState(Guid guid);
		Action<Guid> OnStateFinished { get; set; }
	}
}