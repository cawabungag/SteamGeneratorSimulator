using States;

namespace DefaultNamespace.StateMachine
{
	public interface IStateMachine
	{
		void AddState(IState state);
		void Execute(float deltaTime);
	}
}