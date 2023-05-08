using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OPCClientInterface;
using System;
using Opc.Ua.Client;
using Opc.Ua;

public class ConnectPlc : MonoBehaviour
{
	public Button _connectBtn;
	public Button _backBtn;
	public Transform _goview;
	public Transform _opcView;
	public InputField _urlInput;
	private bool _isConnect;
	private ClientLogIn _clientLogIn;
	private OpcuaClient _opcuaClient;
	private OPCUAClientEX _opcuaClientEx;

	private bool _isChange;

	private Dictionary<string, GameObject> _dicOpcItem = new();
	private Dictionary<string, (string, object)> _dicOpcData = new();
	private Dictionary<string, GameObject> _dicGo = new();

	private void SubCallBack(string arg1, MonitoredItem arg2, MonitoredItemNotificationEventArgs arg3)
	{
		MonitoredItemNotification notification = arg3.NotificationValue as MonitoredItemNotification;
		if (notification != null)
		{
			var bValue = notification.Value.WrappedValue.Value;
			if (_dicOpcData.ContainsKey(arg2.DisplayName))
			{
				_dicOpcData[arg2.DisplayName] = (notification.Value.WrappedValue.TypeInfo.ToString(), bValue);
			}
			else
			{
				_dicOpcData.Add(arg2.DisplayName, (notification.Value.WrappedValue.TypeInfo.ToString(), bValue));
			}
		}

		_isChange = true;
	}

	private void Start()
	{
		UIEventBind();
		LoadGoEvent();
	}

	private void Update()
	{
		if (_isChange)
		{
			foreach (var key in _dicOpcItem.Keys)
			{
				if (!_dicOpcData.ContainsKey(key)) continue;

				var type = _dicOpcData[key].Item1;
				var value = _dicOpcData[key].Item2;

				var opcItem = _dicOpcItem[key];
				if (value != null)
					opcItem.transform.GetChild(2).GetComponent<Text>().text = value.ToString();
			}
		}

		foreach (var key in _dicGo.Keys)
		{
			var jointeach = _opcuaClientEx.GetData(key);

			var drive = _dicGo[key].GetComponent<ArticulationBody>().xDrive;

			if (jointeach.Value.ToString() == "False")
			{
				drive.target = drive.lowerLimit;
				_dicGo[key].GetComponent<ArticulationBody>().xDrive = drive;
				continue;
			}

			if (jointeach.Value.ToString() == "True")
			{
				drive.target = drive.upperLimit;
				_dicGo[key].GetComponent<ArticulationBody>().xDrive = drive;
				continue;
			}

			var x = Convert.ToSingle(jointeach.Value);
			drive.target = x;
			_dicGo[key].GetComponent<ArticulationBody>().xDrive = drive;
		}
	}

	private void LoadGoEvent()
	{
		var models = GameObject.FindGameObjectsWithTag("Model");

		foreach (var go in models)
		{
			Transform[] father = go.GetComponentsInChildren<Transform>();
			for (int j = 1; j < father.Length; j++)
			{
				var goChild = father[j];
				var goBtn = (GameObject) Instantiate(Resources.Load("Prefabs/GoBtnItem"), _goview.transform, false);
				goBtn.GetComponentInChildren<Text>().text = goChild.name;
			}
		}
	}

	private void OPCUAClient_ConnectComplete(object sender, string[] e)
	{
		if (_opcuaClient.Connected == false) return;
		_opcuaClientEx = new OPCUAClientEX(_opcuaClient);
		var opcList = _opcuaClientEx.GetBranch(SourceID.ObjectsFolder, true);
	}

	private void OPCUAClient_OpcStatusChange(object sender, OpcUaStatusEventArgs e)
	{
		if (e.Text.Contains("Disconnected"))
		{
			_isConnect = false;
			_opcuaClientEx = null;
		}
		else
		{
			_isConnect = true;
		}
	}

	private async void OnConnectBtnClick()
	{
		var connectText = _connectBtn.GetComponentInChildren<Text>();
		if (!_isConnect)
		{
			var url = _urlInput.text;
			if (string.IsNullOrEmpty(url))
			{
				return;
			}

			_opcuaClient = new OpcuaClient();
			_opcuaClient.ConnectComplete += OPCUAClient_ConnectComplete;
			_opcuaClient.OpcStatusChange += OPCUAClient_OpcStatusChange;
			_clientLogIn = new ClientLogIn(_opcuaClient);
			bool result = _clientLogIn.GuestLogin();
			bool isConnected = await _opcuaClient.ConnectServer(url);
			if (!result)
			{
				return;
			}

			if (!_opcuaClient.Connected || !isConnected)
			{
				return;
			}

			_isConnect = true;
			connectText.text = "�Ͽ�";
		}
		else
		{
			_isConnect = false;
			connectText.text = "����";
		}
	}

	private void OnBackBtnClick()
	{
		GameObject.Find("ConnectWindow").transform.position += transform.up * 10;
		BindOpc();
	}

	private void BindOpc()
	{
		var items = _opcView.GetComponentsInChildren<OPCItem>();
		foreach (var item in items)
		{
			var bindName = item.BindBtn.GetComponentInChildren<Text>().text;
			if (string.IsNullOrEmpty(bindName))
			{
				if (_dicGo.ContainsKey(item.IDTxt.text))
					_dicGo.Remove(item.IDTxt.text);
				continue;
			}

			var go = GameObject.Find(bindName);
			_dicGo[item.IDTxt.text] = go;
		}
	}

	private void UIEventBind()
	{
		_connectBtn.onClick.AddListener(OnConnectBtnClick);
		_backBtn.onClick.AddListener(OnBackBtnClick);
	}
}