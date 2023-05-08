using TMPro;
using UnityEngine;

namespace DefaultNamespace.UI
{
	public class UIOPCHistoryItem : MonoBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI _textTitle;

		[SerializeField]
		private TextMeshProUGUI _textDescription;

		public void SetText(string title, string desription)
		{
			_textTitle.text = title;
			_textDescription.text = desription;
		}
	}
}