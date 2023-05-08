using System;
using DefaultNamespace.UI;
using Opc.Ua;
using OPCClientInterface;
using UnityEngine;

namespace DefaultNamespace
{
	public class OPCProvider
	{
		private OpcuaClient _opcuaClient;
		private ClientLogIn _clientLogIn;
		private object _mSession;

		public void Login()
		{
			_opcuaClient = new OpcuaClient();
			_opcuaClient.ConnectComplete += OPCUAClient_ConnectComplete;
			_opcuaClient.OpcStatusChange += OPCUAClient_OpcStatusChange;
			_clientLogIn = new ClientLogIn(_opcuaClient);
		}

		public void WriteNode<T>(string tag, T value)
		{
			var valueToWrite = new WriteValue
			{
				NodeId = new NodeId(tag),
				AttributeId = Attributes.Value
			};
			valueToWrite.Value.Value = value;
			valueToWrite.Value.StatusCode = StatusCodes.Good;
			valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
			valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
			UIOPCUAHistory.Instance.AddHistory($"s={valueToWrite.NodeId.Identifier}", value.ToString());
			_clientLogIn.SendNode(valueToWrite);
		}

		private void OPCUAClient_ConnectComplete(object sender, string[] e)
		{
			if (e == null || e.Length == 0)
				return;

			var join = string.Join(",", e);
			Debug.Log($"{join}");
		}

		private void OPCUAClient_OpcStatusChange(object sender, OpcUaStatusEventArgs e)
		{
			Debug.Log($"{e.Error}");
		}
	}
}