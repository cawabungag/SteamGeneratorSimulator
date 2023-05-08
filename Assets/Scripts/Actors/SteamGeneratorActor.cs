using System;
using System.Collections;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

public class SteamGeneratorActor : Singleton<SteamGeneratorActor>
{
	[SerializeField]
	private MeshRenderer[] _renderers;

	[SerializeField]
	private Gradient _gradient;

	private float _currentTempetaruture = Utils.MIN_TEMPERATURE;
	private float _curentPreasure = 0f;

	private void Start() => SetTemperature(0);

	public void SetTemperature(float deltaTemperature)
	{
		var newTemperature = Math.Clamp(_currentTempetaruture + deltaTemperature, Utils.MIN_TEMPERATURE,
			Utils.MAX_TEMPERATURE);
		_currentTempetaruture = newTemperature;

		var colorFactor = Mathf.Clamp01((newTemperature - Utils.MIN_TEMPERATURE)
										/ (Utils.MAX_TEMPERATURE - Utils.MIN_TEMPERATURE));
		_curentPreasure = 0.8f * colorFactor;
		SetColor(colorFactor);

		UIIndicatorsController.Instance.SetTemperature($"Текущая температура: {newTemperature:F1}°C");
		UIIndicatorsController.Instance.SetPreasure($"Текущее рабочее давление: {_curentPreasure:F}МПа");
	}

	private Coroutine _coroutine;

	private void Update()
	{
		if (_coroutine != null)
			return;

		_coroutine = StartCoroutine(Wait(1));
	}

	private IEnumerator Wait(int waitSecond)
	{
		yield return new WaitForSeconds(waitSecond);
		Bootstrap.Instance.LoginOpc.WriteNode("temperature", $"{_currentTempetaruture}");
		Bootstrap.Instance.LoginOpc.WriteNode("preasure", $"{_curentPreasure}");
		StopAllCoroutines();
		_coroutine = null;
	}

	private void SetColor(float colorFactor)
	{
		foreach (var meshRenderer in _renderers)
		{
			var color = _gradient.Evaluate(colorFactor);
			meshRenderer.material.color = color;
		}
	}
}