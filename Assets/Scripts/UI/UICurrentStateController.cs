using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
	public class UICurrentStateController : MonoBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI _durationText;

		[SerializeField]
		private TextMeshProUGUI _statusText;

		[SerializeField]
		private TextMeshProUGUI _stateTypeText;

		[SerializeField]
		private TextMeshProUGUI _guidText;

		[SerializeField]
		private Button _buttonComplete;

		[SerializeField]
		private Image _backgroundImage;

		private Action<bool> _currentState;

		private void Awake() => _buttonComplete.onClick.AddListener(OnComplete);

		private void OnDestroy() => _buttonComplete.onClick.RemoveAllListeners();

		private void OnComplete() => _currentState?.Invoke(true);

		public void Initialize(string duration,
			string state,
			Color backGroudColor,
			string status,
			Color color,
			string guid,
			Action<bool> currentState)
		{
			_currentState = currentState;
			SetDuration(duration);
			SetStateTypeText(state, backGroudColor);
			SetStatus(status, color);
			_guidText.text = guid;
		}

		public void SetDuration(string duration)
			=> _durationText.text = duration;

		public void SetStateTypeText(string state, Color backGroudColor)
		{
			_stateTypeText.text = state;
			// _backgroundImage.color = backGroudColor;
		}

		public void SetStatus(string status, Color color)
		{
			_statusText.text = status;
			_statusText.color = color;
		}
	}
}