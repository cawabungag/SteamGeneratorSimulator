using System;
using System.Collections.Generic;
using States;
using UnityEngine;

namespace DefaultNamespace.UI
{
	public class UIStatesController : Singleton<UIStatesController>, IUIStatesController
	{
		[SerializeField]
		private UICurrentStateController _originalState;

		[SerializeField]
		private RectTransform _targetStates;

		[SerializeField]
		private Color _heatingColor;

		[SerializeField]
		private Color _coolingColor;

		[SerializeField]
		private Color _maintanceColor;

		[SerializeField]
		private Color _queuedColor;

		[SerializeField]
		private Color _inProgressColor;

		[SerializeField]
		private Color _finishedColor;

		private Dictionary<Guid, UICurrentStateController> _states = new();

		public void AddState(IState state)
		{
			var newState = Instantiate(_originalState, _targetStates);
			UpdateState(state, newState);
			_states.Add(state.StateId, newState);
		}

		public void UpdateState(IState state)
		{
			if (state == null)
				return;

			if (!_states.TryGetValue(state.StateId, out var stateController))
				return;

			UpdateState(state, stateController);
		}

		public void DeleteState(Guid stateId)
		{
			if (!_states.TryGetValue(stateId, out var stateController))
				return;

			Destroy(stateController.gameObject, 2f);
			_states.Remove(stateId);
		}

		private void UpdateState(IState state, UICurrentStateController newState)
		{
			var stateType = state.Type;
			var stateStatus = state.Status;
			newState.Initialize(state.Duration.ToString("0.00"), GetTextByStateType(stateType),
				GetColorByStateType(stateType),
				GetTextByStateStatus(stateStatus), GetColorByStateStatus(stateStatus), state.StateId.ToString(),
				state.OnComplete);
		}

		private Color GetColorByStateStatus(StateStatus status)
		{
			var stateColor = Color.red;
			switch (status)
			{
				case StateStatus.Queued:
					stateColor = _queuedColor;
					break;
				case StateStatus.InProgress:
					stateColor = _inProgressColor;
					break;
				case StateStatus.Finished:
					stateColor = _finishedColor;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(status), status, null);
			}

			return stateColor;
		}

		private string GetTextByStateStatus(StateStatus status)
		{
			var statusText = "";
			switch (status)
			{
				case StateStatus.Queued:
					statusText = "Запланировано";
					break;
				case StateStatus.InProgress:
					statusText = "В Процессе";
					break;
				case StateStatus.Finished:
					statusText = "Завершено";
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(status), status, null);
			}

			return statusText;
		}

		private string GetTextByStateType(StateType stateType)
		{
			var stateText = "";
			switch (stateType)
			{
				case StateType.Heating:
					stateText = "Нагревание";
					break;
				case StateType.Maintenance:
					stateText = "Поддерживание";
					break;
				case StateType.Open:
					stateText = "Открытие";
					break;
				case StateType.Close:
					stateText = "Закрытие";
					break;
				case StateType.PluggingIn:
					stateText = "Включение";
					break;
				case StateType.PluggingOut:
					stateText = "Выключение";
					break;
			}

			return stateText;
		}

		private Color GetColorByStateType(StateType stateType)
		{
			var stateColor = Color.red;
			switch (stateType)
			{
				case StateType.Heating:
					stateColor = _heatingColor;
					break;
				case StateType.Maintenance:
					stateColor = _maintanceColor;
					break;
				default:
					stateColor = _maintanceColor;
					break;
			}

			return stateColor;
		}
	}
}