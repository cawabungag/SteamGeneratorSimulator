using System.Collections;
using TMPro;
using UnityEngine;

namespace DefaultNamespace.UI
{
	public class UIIndicatorsController : Singleton<UIIndicatorsController>
	{
		[SerializeField]
		private TextMeshProUGUI _temperatureText;

		[SerializeField]
		private TextMeshProUGUI _preasureText;

		public void SetTemperature(string temperature)
			=> _temperatureText.text = temperature;

		public void SetPreasure(string preasure)
			=> _preasureText.text = preasure;

		
	}
}