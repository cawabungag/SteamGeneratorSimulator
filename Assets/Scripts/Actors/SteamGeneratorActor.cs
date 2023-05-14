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
	private MeshRenderer[] _cullingRenderers;

	[SerializeField]
	private Gradient _gradient;
	
	private float _currentTempetaruture = Utils.MIN_TEMPERATURE;
	private float _curentPreasure;
	private bool _isOpened;
	private bool _isPlugin;

	private void Start() => SetTemperature(0);

	public void Culling(bool isShow)
	{
		foreach (var meshRenderer in _cullingRenderers)
			meshRenderer.gameObject.SetActive(isShow);
	}

	public void SetTemperature(float deltaTemperature)
	{
		if (!_isPlugin)
			return;

		var newTemperature = Math.Clamp(_currentTempetaruture + deltaTemperature, Utils.MIN_TEMPERATURE,
			Utils.MAX_TEMPERATURE);
		_currentTempetaruture = newTemperature;

		var colorFactor = Mathf.Clamp01((newTemperature - Utils.MIN_TEMPERATURE)
										/ (Utils.MAX_TEMPERATURE - Utils.MIN_TEMPERATURE));

		if (newTemperature > 100f)
			_curentPreasure = Utils.MAX_PREASURE * colorFactor;

		if (Math.Abs(_curentPreasure - Utils.MAX_PREASURE) < 0.001f)
		{
			_isOpened = true;
		}

		SetColor(colorFactor);
		UIIndicatorsController.Instance.SetTemperature($"Текущая температура: {newTemperature:F1}°C");
		UIIndicatorsController.Instance.SetPreasure($"Текущее рабочее давление: {_curentPreasure:F}МПа");
	}

	public void SetOpenState(bool state)
	{
		_isOpened = state;
		UIIndicatorsController.Instance.SetOpen(_isOpened);
	}

	public void SetPlugin(bool state)
	{
		_isPlugin = state;
		UIIndicatorsController.Instance.SetPlugin(_isPlugin);
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