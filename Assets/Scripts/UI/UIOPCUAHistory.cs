using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
	public class UIOPCUAHistory : Singleton<UIOPCUAHistory>
	{
		[SerializeField]
		private RectTransform _targetTransform;

		[SerializeField]
		private UIOPCHistoryItem _original;

		[SerializeField]
		private Button _clearButton;

		private readonly List<UIOPCHistoryItem> _history = new();

		protected override void Awake()
		{
			base.Awake();
			_clearButton.onClick.AddListener(OnClear);
		}

		private void OnClear()
		{
			foreach (var historyItem in _history)
				Destroy(historyItem.gameObject);

			_history.Clear();
		}

		public void AddHistory(string title, string description)
		{
			var historyItem = Instantiate(_original, _targetTransform);
			historyItem.SetText(title, description);
			_history.Add(historyItem);
		}
	}
}