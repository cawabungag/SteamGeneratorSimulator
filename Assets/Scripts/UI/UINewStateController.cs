using System.Collections;
using States;
using States.Implementation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
	public class UINewStateController : Singleton<UINewStateController>
	{
		[SerializeField]
		private Toggle _heatingToggle;

		[SerializeField]
		private Toggle _coolingToggle;

		[SerializeField]
		private Toggle _maintanceToggle;

		[SerializeField]
		private TMP_InputField _inputField;

		[SerializeField]
		private Button _addTaskToggle;

		[SerializeField]
		private Toggle _toggleSimulationAutomatic;

		private int _delayBetweenStates = 5;
		private bool _isInitialSimulation = true;
		private Coroutine _coroutine;

		private void Update()
		{
			if (!_toggleSimulationAutomatic.isOn)
				_isInitialSimulation = true;

			if (_toggleSimulationAutomatic.isOn && _coroutine == null)
			{
				if (_isInitialSimulation)
					AddRandomState();

				_isInitialSimulation = false;
				_coroutine = StartCoroutine(Wait(_delayBetweenStates));
			}
		}

		private IEnumerator Wait(int waitSecond)
		{
			yield return new WaitForSeconds(waitSecond);

			AddRandomState();
			StopAllCoroutines();
			_coroutine = null;
		}

		private void AddRandomState()
		{
			var randomState = Random.Range(1, 4);
			var randomDuration = Random.Range(2, 11);

			switch (randomState)
			{
				case 1:
					AddHeatingState(randomDuration);
					break;

				case 2:
					AddColingState(randomDuration);
					break;

				case 3:
					AddMaintenanceState(randomDuration);
					break;
			}
		}

		protected override void Awake()
		{
			_addTaskToggle.onClick.AddListener(OnClickAddTask);
		}

		private void OnClickAddTask()
		{
			if (_toggleSimulationAutomatic.isOn)
				return;

			if (!int.TryParse(_inputField.text, out var duration))
				return;

			if (_heatingToggle.isOn)
			{
				AddHeatingState(duration);
				return;
			}

			if (_coolingToggle.isOn)
			{
				AddColingState(duration);
				return;
			}

			if (_maintanceToggle.isOn)
			{
				AddMaintenanceState(duration);
				return;
			}
		}

		private void AddHeatingState(int duration)
		{
			var heatingState = new HeatingState(new StateConfiguration(duration));
			Bootstrap.Instance.StateMachine.AddState(heatingState);
		}

		private void AddColingState(int duration)
		{
			var heatingState = new СoolingState(new StateConfiguration(duration));
			Bootstrap.Instance.StateMachine.AddState(heatingState);
		}

		private void AddMaintenanceState(int duration)
		{
			var heatingState = new MaintenanceState(new StateConfiguration(duration));
			Bootstrap.Instance.StateMachine.AddState(heatingState);
		}
	}
}