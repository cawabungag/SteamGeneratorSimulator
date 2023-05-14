using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
	public class UIIndicatorsController : Singleton<UIIndicatorsController>
	{
		[SerializeField]
		private TextMeshProUGUI _temperatureText;

		[SerializeField]
		private TextMeshProUGUI _preasureText;
		
		[SerializeField]
		private Toggle _togglePlugin;

		[SerializeField]
		private Toggle _toggleOpenState;

		public void SetTemperature(string temperature)
			=> _temperatureText.text = temperature;

		public void SetPreasure(string preasure)
			=> _preasureText.text = preasure;
		
		public void SetPlugin(bool state)
			=> _togglePlugin.isOn = state;

		public void SetOpen(bool state)
			=> _toggleOpenState.isOn = state;
	}
}