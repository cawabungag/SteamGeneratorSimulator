using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace.UI;
using States;

namespace DefaultNamespace.StateMachine
{
	public class StateMachine : IStateMachine
	{
		public Action<Guid> OnStateFinished { get; set; }
		private static Queue<IState> _states = new();
		private IState _currentState;

		public void AddState(IState state)
		{
			state.Status = StateStatus.Queued;
			UIStatesController.Instance.AddState(state);
			Bootstrap.Instance.LoginOpc.WriteNode("state_machine", $"{state.Status}_{state.Type}_{state.StateId}");
			_states.Enqueue(state);
		}

		public void FinishState(Guid guid)
		{
			if (_currentState.StateId == guid)
			{
				FinishCurentState();
				return;
			}

			UIStatesController.Instance.DeleteState(guid);
			_states = new Queue<IState>(_states.Where(x => x.StateId != guid));
		}

		public void FinishAllState()
		{
			var states = _states.ToArray();
			
			foreach (var state in states) 
				UIStatesController.Instance.DeleteState(state.StateId);

			_states = new Queue<IState>();
			FinishCurentState();
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
			Bootstrap.Instance.LoginOpc.WriteNode("state_machine",
				$"{StateStatus.InProgress}_{_currentState.Type}_{_currentState.StateId}");
			_currentState.OnComplete += _ => FinishCurentState();
		}

		public void FinishCurentState()
		{
			OnStateFinished?.Invoke(_currentState.StateId);
			_currentState.Status = StateStatus.Finished;
			UIStatesController.Instance.UpdateState(_currentState);
			UIStatesController.Instance.DeleteState(_currentState.StateId);
			Bootstrap.Instance.LoginOpc.WriteNode("state_machine",
				$"{_currentState.Status}_{_currentState.Type}_{_currentState.StateId}");
			_currentState.OnComplete = null;
			_currentState = null;
			StartNewState();
		}
	}
}