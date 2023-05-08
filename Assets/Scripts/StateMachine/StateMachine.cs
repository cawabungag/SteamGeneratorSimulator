using System.Collections.Generic;
using DefaultNamespace.UI;
using States;

namespace DefaultNamespace.StateMachine
{
	public class StateMachine : IStateMachine
	{
		private static Queue<IState> _states = new();
		private IState _currentState;

		public void AddState(IState state)
		{
			state.Status = StateStatus.Queued;
			UIStatesController.Instance.AddState(state);
			Bootstrap.Instance.LoginOpc.WriteNode("state_machine", $"{state.Status}_{state.Type}_{state.StateId}");
			_states.Enqueue(state);
		}

		public void Execute(float deltaTime)
		{
			if (_currentState == null && _states.Count == 0)
				return;

			if (_currentState == null)
				StartNewState();

			if (_currentState == null)
				return;

			_currentState.Status = StateStatus.InProgress;
			_currentState.Execute(deltaTime);
			UIStatesController.Instance.UpdateState(_currentState);
		}

		private void StartNewState()
		{
			if (_states.Count == 0)
				return;

			_currentState = _states.Dequeue();
			_currentState.Start();
			Bootstrap.Instance.LoginOpc.WriteNode("state_machine", $"{StateStatus.InProgress}_{_currentState.Type}_{_currentState.StateId}");
			_currentState.OnComplete += _ => FinishCurentState();
		}

		private void FinishCurentState()
		{
			_currentState.Status = StateStatus.Finished;
			UIStatesController.Instance.UpdateState(_currentState);
			UIStatesController.Instance.DeleteState(_currentState.StateId);
			Bootstrap.Instance.LoginOpc.WriteNode("state_machine", $"{_currentState.Status}_{_currentState.Type}_{_currentState.StateId}");
			_currentState.OnComplete = null;
			_currentState = null;
			StartNewState();
		}
	}
}