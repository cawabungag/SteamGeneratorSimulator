using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OPCClientInterface
{
	public class OpcuaClient
	{
		private ApplicationConfiguration _mConfiguration;
		private Session _mSession;
		private bool _mIsConnected;
		private int _mReconnectPeriod = 10;
		private bool _mUseSecurity;
		private EndpointDescription _endpointDescription;
		private SessionReconnectHandler _mReconnectHandler;
		private EventHandler _mReconnectComplete;
		private EventHandler _mReconnectStarting;
		private EventHandler _mKeepAliveComplete;
		private EventHandler<string[]> _mConnectComplete;
		private EventHandler<OpcUaStatusEventArgs> _mOpcStatusChange;

		private Dictionary<string, Subscription> _dicSubscriptions;

		public string OpcUaName { get; set; } = "Robot Opc Ua";


		public bool UseSecurity
		{
			get => _mUseSecurity;
			set => _mUseSecurity = value;
		}

		public IUserIdentity UserIdentity { get; set; }


		public Session Session => _mSession;


		public bool Connected => _mIsConnected;

		public event EventHandler<string[]> ConnectComplete
		{
			add => _mConnectComplete += value;
			remove => _mConnectComplete -= value;
		}

		public event EventHandler<OpcUaStatusEventArgs> OpcStatusChange
		{
			add => _mOpcStatusChange += value;
			remove => _mOpcStatusChange -= value;
		}

		public ApplicationConfiguration AppConfig => _mConfiguration;


		public OpcuaClient()
		{
			_dicSubscriptions = new Dictionary<string, Subscription>();

			var certificateValidator = new CertificateValidator();
			certificateValidator.CertificateValidation += (sender, eventArgs) =>
			{
				if (ServiceResult.IsGood(eventArgs.Error))
					eventArgs.Accept = true;
				else if (eventArgs.Error.StatusCode.Code == StatusCodes.BadCertificateUntrusted)
					eventArgs.Accept = true;
				else
					throw new Exception(string.Format("Failed to validate certificate with error code {0}: {1}",
						eventArgs.Error.Code, eventArgs.Error.AdditionalInfo));
			};

			var securityConfigurationcv = new SecurityConfiguration
			{
				AutoAcceptUntrustedCertificates = true,
				RejectSHA1SignedCertificates = false,
				MinimumCertificateKeySize = 1024,
			};
			certificateValidator.Update(securityConfigurationcv);

			// Build the application configuration
			var application = new ApplicationInstance
			{
				ApplicationType = ApplicationType.Client,
				ConfigSectionName = OpcUaName,
				ApplicationConfiguration = new ApplicationConfiguration
				{
					ApplicationName = OpcUaName,
					ApplicationType = ApplicationType.Client,
					CertificateValidator = certificateValidator,
					ServerConfiguration = new ServerConfiguration
					{
						MaxSubscriptionCount = 100000,
						MaxMessageQueueSize = 1000000,
						MaxNotificationQueueSize = 1000000,
						MaxPublishRequestCount = 10000000,
					},

					SecurityConfiguration = new SecurityConfiguration
					{
						AutoAcceptUntrustedCertificates = true,
						RejectSHA1SignedCertificates = false,
						MinimumCertificateKeySize = 1024,
					},

					TransportQuotas = new TransportQuotas
					{
						OperationTimeout = 6000,
						MaxStringLength = int.MaxValue,
						MaxByteStringLength = int.MaxValue,
						MaxArrayLength = 65535,
						MaxMessageSize = 419430400,
						MaxBufferSize = 65535,
						ChannelLifetime = -1,
						SecurityTokenLifetime = -1
					},
					ClientConfiguration = new ClientConfiguration
					{
						DefaultSessionTimeout = -1,
						MinSubscriptionLifetime = -1,
					},
					DisableHiResClock = true
				}
			};
			_mConfiguration = application.ApplicationConfiguration;
		}

		public async Task<bool> ConnectServer(string serverUrl)
		{
			try
			{
				_mSession = await Connect(serverUrl);
			}
			catch (Exception ex)
			{
				throw new Exception(ex + "����ʧ�ܣ�������ַ�Ƿ���ȷ��");
			}

			if (_mSession == null)
			{
				throw new Exception("����ʧ�ܣ�������ַ�Ƿ���ȷ��");
			}

			return true;
		}

		private async Task<Session> Connect(string serverUrl)
		{
			// disconnect from existing session.
			Disconnect();

			if (_mConfiguration == null)
			{
				throw new ArgumentNullException("m_configuration");
			}

			_endpointDescription = CoreClientUtils.SelectEndpoint(serverUrl, UseSecurity);

			var endpointConfiguration = EndpointConfiguration.Create(_mConfiguration);
			var endpoint = new ConfiguredEndpoint(null, _endpointDescription, endpointConfiguration);

			_mSession = await Session.Create(
				_mConfiguration,
				endpoint,
				false,
				false,
				(string.IsNullOrEmpty(OpcUaName)) ? _mConfiguration.ApplicationName : OpcUaName,
				60000,
				UserIdentity,
				new string[] { });

			_mSession.KeepAlive += new KeepAliveEventHandler(Session_KeepAlive);

			_mIsConnected = true;
			DoConnectComplete(null, new string[]
			{
				_mSession.ConfiguredEndpoint.Description.Server.ApplicationName.ToString(),
				_mSession.ConfiguredEndpoint.Description.SecurityMode.ToString(),
				_mSession.ConfiguredEndpoint.Description.SecurityPolicyUri,
				_mSession.ConfiguredEndpoint.Description.EndpointUrl
			});
			// return the new session.
			return _mSession;
		}

		public void Disconnect()
		{
			UpdateStatus(false, DateTime.UtcNow, "Disconnected");

			if (_mReconnectHandler != null)
			{
				_mReconnectHandler.Dispose();
				_mReconnectHandler = null;
			}

			if (_mSession != null)
			{
				_mSession.Close(10000);
				_mSession = null;
			}

			_mIsConnected = false;

			DoConnectComplete(null, null);
		}

		private void UpdateStatus(bool error, DateTime time, string status, params object[] args)
		{
			_mOpcStatusChange?.Invoke(this, new OpcUaStatusEventArgs()
			{
				Error = error,
				Time = time.ToLocalTime(),
				Text = String.Format(status, args),
			});
		}

		/// <summary>
		/// Handles a keep alive event from a session.
		/// </summary>
		private void Session_KeepAlive(Session session, KeepAliveEventArgs e)
		{
			try
			{
				// check for events from discarded sessions.
				if (!Object.ReferenceEquals(session, _mSession))
				{
					return;
				}

				// start reconnect sequence on communication error.
				if (ServiceResult.IsBad(e.Status))
				{
					if (_mReconnectPeriod <= 0)
					{
						UpdateStatus(true, e.CurrentTime, "Communication Error ({0})", e.Status);
						return;
					}

					UpdateStatus(true, e.CurrentTime, "Reconnecting in {0}s", _mReconnectPeriod);

					if (_mReconnectHandler == null)
					{
						_mReconnectStarting?.Invoke(this, e);

						_mReconnectHandler = new SessionReconnectHandler();
						_mReconnectHandler.BeginReconnect(_mSession, _mReconnectPeriod * 1000,
							Server_ReconnectComplete);
					}

					return;
				}

				// update status.
				UpdateStatus(false, e.CurrentTime, "Connected [{0}]", session.Endpoint.EndpointUrl);

				// raise any additional notifications.
				_mKeepAliveComplete?.Invoke(this, e);
			}
			catch (Exception exception)
			{
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
                ClientUtils.HandleException(OpcUaName, exception);
#else
				throw;
#endif
			}
		}

		/// <summary>
		/// Handles a reconnect event complete from the reconnect handler.
		/// </summary>
		private void Server_ReconnectComplete(object sender, EventArgs e)
		{
			try
			{
				// ignore callbacks from discarded objects.
				if (!Object.ReferenceEquals(sender, _mReconnectHandler))
				{
					return;
				}

				_mSession = _mReconnectHandler.Session;
				_mReconnectHandler.Dispose();
				_mReconnectHandler = null;

				// raise any additional notifications.
				_mReconnectComplete?.Invoke(this, e);
			}
			catch (Exception exception)
			{
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
                ClientUtils.HandleException(OpcUaName, exception);
#else
				throw;
#endif
			}
		}

		public void SetLogPathName(string filePath, bool deleteExisting)
		{
			Utils.SetTraceLog(filePath, deleteExisting);
			Utils.SetTraceMask(515);
		}

		internal DataValue ReadNode(NodeId nodeId)
		{
			var nodesToRead = new ReadValueIdCollection
			{
				new ReadValueId()
				{
					NodeId = nodeId,
					AttributeId = Attributes.Value
				}
			};

			// read the current value
			_mSession.Read(
				null,
				0,
				TimestampsToReturn.Neither,
				nodesToRead,
				out var results,
				out var diagnosticInfos);

			ClientBase.ValidateResponse(results, nodesToRead);
			ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

			return results[0];
		}

		internal T ReadNode<T>(string tag)
		{
			var dataValue = ReadNode(new NodeId(tag));
			return (T) dataValue.Value;
		}

		internal Task<T> ReadNodeAsync<T>(string tag)
		{
			var nodesToRead = new ReadValueIdCollection
			{
				new ReadValueId()
				{
					NodeId = new NodeId(tag),
					AttributeId = Attributes.Value
				}
			};

			// Wrap the ReadAsync logic in a TaskCompletionSource, so we can use C# async/await syntax to call it:
			var taskCompletionSource = new TaskCompletionSource<T>();
			_mSession.BeginRead(
				requestHeader: null,
				maxAge: 0,
				timestampsToReturn: TimestampsToReturn.Neither,
				nodesToRead: nodesToRead,
				callback: ar =>
				{
					DataValueCollection results;
					DiagnosticInfoCollection diag;
					var response = _mSession.EndRead(
						result: ar,
						results: out results,
						diagnosticInfos: out diag);

					try
					{
						CheckReturnValue(response.ServiceResult);
						CheckReturnValue(results[0].StatusCode);
						var val = results[0];
						taskCompletionSource.TrySetResult((T) val.Value);
					}
					catch (Exception ex)
					{
						taskCompletionSource.TrySetException(ex);
					}
				},
				asyncState: null);

			return taskCompletionSource.Task;
		}

		internal List<DataValue> ReadNodes(NodeId[] nodeIds)
		{
			var nodesToRead = new ReadValueIdCollection();
			for (var i = 0; i < nodeIds.Length; i++)
			{
				nodesToRead.Add(new ReadValueId()
				{
					NodeId = nodeIds[i],
					AttributeId = Attributes.Value
				});
			}

			_mSession.Read(
				null,
				0,
				TimestampsToReturn.Neither,
				nodesToRead,
				out var results,
				out var diagnosticInfos);

			ClientBase.ValidateResponse(results, nodesToRead);
			ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

			return results.ToList();
		}

		internal Task<List<DataValue>> ReadNodesAsync(NodeId[] nodeIds)
		{
			var nodesToRead = new ReadValueIdCollection();
			for (var i = 0; i < nodeIds.Length; i++)
			{
				nodesToRead.Add(new ReadValueId()
				{
					NodeId = nodeIds[i],
					AttributeId = Attributes.Value
				});
			}

			var taskCompletionSource = new TaskCompletionSource<List<DataValue>>();
			_mSession.BeginRead(
				null,
				0,
				TimestampsToReturn.Neither,
				nodesToRead,
				callback: ar =>
				{
					DataValueCollection results;
					DiagnosticInfoCollection diag;
					var response = _mSession.EndRead(
						result: ar,
						results: out results,
						diagnosticInfos: out diag);

					try
					{
						CheckReturnValue(response.ServiceResult);
						taskCompletionSource.TrySetResult(results.ToList());
					}
					catch (Exception ex)
					{
						taskCompletionSource.TrySetException(ex);
					}
				},
				asyncState: null);

			return taskCompletionSource.Task;
		}

		internal List<T> ReadNodes<T>(string[] tags)
		{
			var result = new List<T>();
			var nodesToRead = new ReadValueIdCollection();
			for (var i = 0; i < tags.Length; i++)
			{
				nodesToRead.Add(new ReadValueId()
				{
					NodeId = new NodeId(tags[i]),
					AttributeId = Attributes.Value
				});
			}

			_mSession.Read(
				null,
				0,
				TimestampsToReturn.Neither,
				nodesToRead,
				out var results,
				out var diagnosticInfos);

			ClientBase.ValidateResponse(results, nodesToRead);
			ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

			foreach (var item in results)
			{
				result.Add((T) item.Value);
			}

			return result;
		}

		internal Task<List<T>> ReadNodesAsync<T>(string[] tags)
		{
			var nodesToRead = new ReadValueIdCollection();
			for (var i = 0; i < tags.Length; i++)
			{
				nodesToRead.Add(new ReadValueId()
				{
					NodeId = new NodeId(tags[i]),
					AttributeId = Attributes.Value
				});
			}

			var taskCompletionSource = new TaskCompletionSource<List<T>>();
			_mSession.BeginRead(
				null,
				0,
				TimestampsToReturn.Neither,
				nodesToRead,
				callback: ar =>
				{
					DataValueCollection results;
					DiagnosticInfoCollection diag;
					var response = _mSession.EndRead(
						result: ar,
						results: out results,
						diagnosticInfos: out diag);

					try
					{
						CheckReturnValue(response.ServiceResult);
						var result = new List<T>();
						foreach (var item in results)
						{
							result.Add((T) item.Value);
						}

						taskCompletionSource.TrySetResult(result);
					}
					catch (Exception ex)
					{
						taskCompletionSource.TrySetException(ex);
					}
				},
				asyncState: null);

			return taskCompletionSource.Task;
		}

		internal bool WriteNode<T>(string tag, T value)
		{
			var valueToWrite = new WriteValue()
			{
				NodeId = new NodeId(tag),
				AttributeId = Attributes.Value
			};
			valueToWrite.Value.Value = value;
			valueToWrite.Value.StatusCode = StatusCodes.Good;
			valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
			valueToWrite.Value.SourceTimestamp = DateTime.MinValue;

			var valuesToWrite = new WriteValueCollection
			{
				valueToWrite
			};

			var asd = _mSession.Write(
				null,
				valuesToWrite,
				out var results,
				out var diagnosticInfos);

			ClientBase.ValidateResponse(results, valuesToWrite);
			ClientBase.ValidateDiagnosticInfos(diagnosticInfos, valuesToWrite);

			if (StatusCode.IsBad(results[0]))
			{
				throw new ServiceResultException(results[0]);
			}

			return !StatusCode.IsBad(results[0]);
		}

		internal Task<bool> WriteNodeAsync<T>(string tag, T value)
		{
			var valueToWrite = new WriteValue()
			{
				NodeId = new NodeId(tag),
				AttributeId = Attributes.Value,
			};
			valueToWrite.Value.Value = value;
			valueToWrite.Value.StatusCode = StatusCodes.Good;
			valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
			valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
			var valuesToWrite = new WriteValueCollection
			{
				valueToWrite
			};

			// Wrap the WriteAsync logic in a TaskCompletionSource, so we can use C# async/await syntax to call it:
			var taskCompletionSource = new TaskCompletionSource<bool>();
			_mSession.BeginWrite(
				requestHeader: null,
				nodesToWrite: valuesToWrite,
				callback: ar =>
				{
					var response = _mSession.EndWrite(
						result: ar,
						results: out var results,
						diagnosticInfos: out var diag);

					try
					{
						ClientBase.ValidateResponse(results, valuesToWrite);
						ClientBase.ValidateDiagnosticInfos(diag, valuesToWrite);
						taskCompletionSource.SetResult(StatusCode.IsGood(results[0]));
					}
					catch (Exception ex)
					{
						taskCompletionSource.TrySetException(ex);
					}
				},
				asyncState: null);
			return taskCompletionSource.Task;
		}

		internal bool WriteNodes(string[] tags, object[] values)
		{
			var valuesToWrite = new WriteValueCollection();

			for (var i = 0; i < tags.Length; i++)
			{
				if (i < values.Length)
				{
					var valueToWrite = new WriteValue()
					{
						NodeId = new NodeId(tags[i]),
						AttributeId = Attributes.Value
					};
					valueToWrite.Value.Value = values[i];
					valueToWrite.Value.StatusCode = StatusCodes.Good;
					valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
					valueToWrite.Value.SourceTimestamp = DateTime.MinValue;
					valuesToWrite.Add(valueToWrite);
				}
			}

			_mSession.Write(
				null,
				valuesToWrite,
				out var results,
				out var diagnosticInfos);

			ClientBase.ValidateResponse(results, valuesToWrite);
			ClientBase.ValidateDiagnosticInfos(diagnosticInfos, valuesToWrite);

			var result = true;
			foreach (var r in results)
			{
				if (StatusCode.IsBad(r))
				{
					result = false;
					break;
				}
			}

			return result;
		}


		internal bool DeleteExsistNode(string tag)
		{
			var waitDelete = new DeleteNodesItemCollection();

			var nodesItem = new DeleteNodesItem()
			{
				NodeId = new NodeId(tag),
			};

			_mSession.DeleteNodes(
				null,
				waitDelete,
				out var results,
				out var diagnosticInfos);

			ClientBase.ValidateResponse(results, waitDelete);
			ClientBase.ValidateDiagnosticInfos(diagnosticInfos, waitDelete);

			return !StatusCode.IsBad(results[0]);
		}

		[Obsolete("��δ�������ԣ��޷�ʹ��")]
		internal void AddNewNode(NodeId parent)
		{
			// Create a Variable node.
			var node2 = new AddNodesItem();
			node2.ParentNodeId = new NodeId(parent);
			node2.ReferenceTypeId = ReferenceTypes.HasComponent;
			node2.RequestedNewNodeId = null;
			node2.BrowseName = new QualifiedName("DataVariable1");
			node2.NodeClass = NodeClass.Variable;
			node2.NodeAttributes = null;
			node2.TypeDefinition = VariableTypeIds.BaseDataVariableType;

			//specify node attributes.
			var node2Attribtues = new VariableAttributes();
			node2Attribtues.DisplayName = "DataVariable1";
			node2Attribtues.Description = "DataVariable1 Description";
			node2Attribtues.Value = new Variant(123);
			node2Attribtues.DataType = (uint) BuiltInType.Int32;
			node2Attribtues.ValueRank = ValueRanks.Scalar;
			node2Attribtues.ArrayDimensions = new UInt32Collection();
			node2Attribtues.AccessLevel = AccessLevels.CurrentReadOrWrite;
			node2Attribtues.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
			node2Attribtues.MinimumSamplingInterval = 0;
			node2Attribtues.Historizing = false;
			node2Attribtues.WriteMask = (uint) AttributeWriteMask.None;
			node2Attribtues.UserWriteMask = (uint) AttributeWriteMask.None;
			node2Attribtues.SpecifiedAttributes = (uint) NodeAttributesMask.All;

			node2.NodeAttributes = new ExtensionObject(node2Attribtues);


			var nodesToAdd = new AddNodesItemCollection
			{
				node2
			};

			_mSession.AddNodes(
				null,
				nodesToAdd,
				out var results,
				out var diagnosticInfos);

			ClientBase.ValidateResponse(results, nodesToAdd);
			ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToAdd);
		}

		internal void AddSubscription(string key,
			string tag,
			Action<string, MonitoredItem, MonitoredItemNotificationEventArgs> callback)
		{
			AddSubscription(key, new string[] {tag}, callback);
		}

		internal void AddSubscription(string key,
			string[] tags,
			Action<string, MonitoredItem, MonitoredItemNotificationEventArgs> callback)
		{
			var mSubscription = new Subscription(_mSession.DefaultSubscription);

			mSubscription.PublishingEnabled = true;
			mSubscription.PublishingInterval = 0;
			mSubscription.KeepAliveCount = uint.MaxValue;
			mSubscription.LifetimeCount = uint.MaxValue;
			mSubscription.MaxNotificationsPerPublish = uint.MaxValue;
			mSubscription.Priority = 100;
			mSubscription.DisplayName = key;


			for (var i = 0; i < tags.Length; i++)
			{
				var item = new MonitoredItem
				{
					StartNodeId = new NodeId(tags[i]),
					AttributeId = Attributes.Value,
					DisplayName = tags[i],
					SamplingInterval = 50,
				};
				item.Notification += (MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs args) =>
				{
					callback?.Invoke(key, monitoredItem, args);
				};
				mSubscription.AddItem(item);
			}


			_mSession.AddSubscription(mSubscription);
			mSubscription.Create();

			lock (_dicSubscriptions)
			{
				if (_dicSubscriptions.ContainsKey(key))
				{
					// remove 
					_dicSubscriptions[key].Delete(true);
					_mSession.RemoveSubscription(_dicSubscriptions[key]);
					_dicSubscriptions[key].Dispose();
					_dicSubscriptions[key] = mSubscription;
				}
				else
				{
					_dicSubscriptions.Add(key, mSubscription);
				}
			}
		}

		internal void RemoveSubscription(string key)
		{
			lock (_dicSubscriptions)
			{
				if (_dicSubscriptions.ContainsKey(key))
				{
					// remove 
					_dicSubscriptions[key].Delete(true);
					_mSession.RemoveSubscription(_dicSubscriptions[key]);
					_dicSubscriptions[key].Dispose();
					_dicSubscriptions.Remove(key);
				}
			}
		}

		internal void RemoveAllSubscription()
		{
			lock (_dicSubscriptions)
			{
				foreach (var item in _dicSubscriptions)
				{
					item.Value.Delete(true);
					_mSession.RemoveSubscription(item.Value);
					item.Value.Dispose();
				}

				_dicSubscriptions.Clear();
			}
		}

		internal IEnumerable<DataValue> ReadHistoryRawDataValues(string tag,
			DateTime start,
			DateTime end,
			uint count = 1,
			bool containBound = false)
		{
			var mNodeToContinue = new HistoryReadValueId()
			{
				NodeId = new NodeId(tag),
			};

			var mDetails = new ReadRawModifiedDetails
			{
				StartTime = start,
				EndTime = end,
				NumValuesPerNode = count,
				IsReadModified = false,
				ReturnBounds = containBound
			};

			var nodesToRead = new HistoryReadValueIdCollection();
			nodesToRead.Add(mNodeToContinue);


			_mSession.HistoryRead(
				null,
				new ExtensionObject(mDetails),
				TimestampsToReturn.Both,
				false,
				nodesToRead,
				out var results,
				out var diagnosticInfos);

			ClientBase.ValidateResponse(results, nodesToRead);
			ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

			if (StatusCode.IsBad(results[0].StatusCode))
			{
				throw new ServiceResultException(results[0].StatusCode);
			}

			var values = ExtensionObject.ToEncodeable(results[0].HistoryData) as HistoryData;
			foreach (var value in values.DataValues)
			{
				yield return value;
			}
		}

		internal IEnumerable<T> ReadHistoryRawDataValues<T>(string tag,
			DateTime start,
			DateTime end,
			uint count = 1,
			bool containBound = false)
		{
			var mNodeToContinue = new HistoryReadValueId()
			{
				NodeId = new NodeId(tag),
			};

			var mDetails = new ReadRawModifiedDetails
			{
				StartTime = start.ToUniversalTime(),
				EndTime = end.ToUniversalTime(),
				NumValuesPerNode = count,
				IsReadModified = false,
				ReturnBounds = containBound
			};

			var nodesToRead = new HistoryReadValueIdCollection();
			nodesToRead.Add(mNodeToContinue);


			_mSession.HistoryRead(
				null,
				new ExtensionObject(mDetails),
				TimestampsToReturn.Both,
				false,
				nodesToRead,
				out var results,
				out var diagnosticInfos);

			ClientBase.ValidateResponse(results, nodesToRead);
			ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

			if (StatusCode.IsBad(results[0].StatusCode))
			{
				throw new ServiceResultException(results[0].StatusCode);
			}

			var values = ExtensionObject.ToEncodeable(results[0].HistoryData) as HistoryData;
			foreach (var value in values.DataValues)
			{
				yield return (T) value.Value;
			}
		}

		internal ReferenceDescription[] BrowseNodeReference(string tag)
		{
			var sourceId = new NodeId(tag);
			var nodeToBrowse1 = new BrowseDescription();

			nodeToBrowse1.NodeId = sourceId;
			nodeToBrowse1.BrowseDirection = BrowseDirection.Forward;
			nodeToBrowse1.ReferenceTypeId = ReferenceTypeIds.Aggregates;
			nodeToBrowse1.IncludeSubtypes = true;
			nodeToBrowse1.NodeClassMask = (uint) (NodeClass.Object | NodeClass.Variable | NodeClass.Method);
			nodeToBrowse1.ResultMask = (uint) BrowseResultMask.All;

			var nodeToBrowse2 = new BrowseDescription();

			nodeToBrowse2.NodeId = sourceId;
			nodeToBrowse2.BrowseDirection = BrowseDirection.Forward;
			nodeToBrowse2.ReferenceTypeId = ReferenceTypeIds.Organizes;
			nodeToBrowse2.IncludeSubtypes = true;
			nodeToBrowse2.NodeClassMask = (uint) (NodeClass.Object | NodeClass.Variable);
			nodeToBrowse2.ResultMask = (uint) BrowseResultMask.All;

			var nodesToBrowse = new BrowseDescriptionCollection();
			nodesToBrowse.Add(nodeToBrowse1);
			nodesToBrowse.Add(nodeToBrowse2);

			// fetch references from the server.
			var references = FormUtils.Browse(_mSession, nodesToBrowse, false);

			return references.ToArray();
		}

		internal OpcNodeAttribute[] ReadNoteAttributes(string tag)
		{
			var sourceId = new NodeId(tag);
			var nodesToRead = new ReadValueIdCollection();

			// attempt to read all possible attributes.
			// ������ȥ��ȡ���п��ܵ�����
			for (var ii = Attributes.NodeClass; ii <= Attributes.UserExecutable; ii++)
			{
				var nodeToRead = new ReadValueId();
				nodeToRead.NodeId = sourceId;
				nodeToRead.AttributeId = ii;
				nodesToRead.Add(nodeToRead);
			}

			var startOfProperties = nodesToRead.Count;

			// find all of the pror of the node.
			var nodeToBrowse1 = new BrowseDescription();

			nodeToBrowse1.NodeId = sourceId;
			nodeToBrowse1.BrowseDirection = BrowseDirection.Forward;
			nodeToBrowse1.ReferenceTypeId = ReferenceTypeIds.HasProperty;
			nodeToBrowse1.IncludeSubtypes = true;
			nodeToBrowse1.NodeClassMask = 0;
			nodeToBrowse1.ResultMask = (uint) BrowseResultMask.All;

			var nodesToBrowse = new BrowseDescriptionCollection();
			nodesToBrowse.Add(nodeToBrowse1);

			// fetch property references from the server.
			var references = FormUtils.Browse(_mSession, nodesToBrowse, false);

			if (references == null)
			{
				return new OpcNodeAttribute[0];
			}

			for (var ii = 0; ii < references.Count; ii++)
			{
				// ignore external references.
				if (references[ii].NodeId.IsAbsolute)
				{
					continue;
				}

				var nodeToRead = new ReadValueId();
				nodeToRead.NodeId = (NodeId) references[ii].NodeId;
				nodeToRead.AttributeId = Attributes.Value;
				nodesToRead.Add(nodeToRead);
			}

			// read all values.
			DataValueCollection results = null;
			DiagnosticInfoCollection diagnosticInfos = null;

			_mSession.Read(
				null,
				0,
				TimestampsToReturn.Neither,
				nodesToRead,
				out results,
				out diagnosticInfos);

			ClientBase.ValidateResponse(results, nodesToRead);
			ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

			// process results.


			var nodeAttribute = new List<OpcNodeAttribute>();
			for (var ii = 0; ii < results.Count; ii++)
			{
				var item = new OpcNodeAttribute();

				// process attribute value.
				if (ii < startOfProperties)
				{
					// ignore attributes which are invalid for the node.
					if (results[ii].StatusCode == StatusCodes.BadAttributeIdInvalid)
					{
						continue;
					}

					// get the name of the attribute.
					item.Name = Attributes.GetBrowseName(nodesToRead[ii].AttributeId);

					// display any unexpected error.
					if (StatusCode.IsBad(results[ii].StatusCode))
					{
						item.Type = Utils.Format("{0}", Attributes.GetDataTypeId(nodesToRead[ii].AttributeId));
						item.Value = Utils.Format("{0}", results[ii].StatusCode);
					}

					// display the value.
					else
					{
						var typeInfo = TypeInfo.Construct(results[ii].Value);

						item.Type = typeInfo.BuiltInType.ToString();

						if (typeInfo.ValueRank >= ValueRanks.OneOrMoreDimensions)
						{
							item.Type += "[]";
						}

						item.Value = results[ii].Value; //Utils.Format("{0}", results[ii].Value);
					}
				}

				// process property value.
				else
				{
					// ignore properties which are invalid for the node.
					if (results[ii].StatusCode == StatusCodes.BadNodeIdUnknown)
					{
						continue;
					}

					// get the name of the property.
					item.Name = Utils.Format("{0}", references[ii - startOfProperties]);

					// display any unexpected error.
					if (StatusCode.IsBad(results[ii].StatusCode))
					{
						item.Type = String.Empty;
						item.Value = Utils.Format("{0}", results[ii].StatusCode);
					}

					// display the value.
					else
					{
						var typeInfo = TypeInfo.Construct(results[ii].Value);

						item.Type = typeInfo.BuiltInType.ToString();

						if (typeInfo.ValueRank >= ValueRanks.OneOrMoreDimensions)
						{
							item.Type += "[]";
						}

						item.Value = results[ii].Value; //Utils.Format("{0}", results[ii].Value);
					}
				}

				nodeAttribute.Add(item);
			}

			return nodeAttribute.ToArray();
		}

		internal DataValue[] ReadNoteDataValueAttributes(string tag)
		{
			var sourceId = new NodeId(tag);
			var nodesToRead = new ReadValueIdCollection();

			// attempt to read all possible attributes.
			// ������ȥ��ȡ���п��ܵ�����
			for (var ii = Attributes.NodeId; ii <= Attributes.UserExecutable; ii++)
			{
				var nodeToRead = new ReadValueId();
				nodeToRead.NodeId = sourceId;
				nodeToRead.AttributeId = ii;
				nodesToRead.Add(nodeToRead);
			}

			var startOfProperties = nodesToRead.Count;

			// find all of the pror of the node.
			var nodeToBrowse1 = new BrowseDescription();

			nodeToBrowse1.NodeId = sourceId;
			nodeToBrowse1.BrowseDirection = BrowseDirection.Forward;
			nodeToBrowse1.ReferenceTypeId = ReferenceTypeIds.HasProperty;
			nodeToBrowse1.IncludeSubtypes = true;
			nodeToBrowse1.NodeClassMask = 0;
			nodeToBrowse1.ResultMask = (uint) BrowseResultMask.All;

			var nodesToBrowse = new BrowseDescriptionCollection();
			nodesToBrowse.Add(nodeToBrowse1);

			// fetch property references from the server.
			var references = FormUtils.Browse(_mSession, nodesToBrowse, false);

			if (references == null)
			{
				return new DataValue[0];
			}

			for (var ii = 0; ii < references.Count; ii++)
			{
				// ignore external references.
				if (references[ii].NodeId.IsAbsolute)
				{
					continue;
				}

				var nodeToRead = new ReadValueId();
				nodeToRead.NodeId = (NodeId) references[ii].NodeId;
				nodeToRead.AttributeId = Attributes.Value;
				nodesToRead.Add(nodeToRead);
			}

			// read all values.
			DataValueCollection results = null;
			DiagnosticInfoCollection diagnosticInfos = null;

			_mSession.Read(
				null,
				0,
				TimestampsToReturn.Neither,
				nodesToRead,
				out results,
				out diagnosticInfos);

			ClientBase.ValidateResponse(results, nodesToRead);
			ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

			return results.ToArray();
		}

		internal object[] CallMethodByNodeId(string tagParent, string tag, params object[] args)
		{
			if (_mSession == null)
			{
				return null;
			}

			var outputArguments = _mSession.Call(
				new NodeId(tagParent),
				new NodeId(tag),
				args);

			return outputArguments.ToArray();
		}

		private void DoConnectComplete(object state, string[] e)
		{
			_mConnectComplete?.Invoke(this, e);
		}

		private void CheckReturnValue(StatusCode status)
		{
			if (!StatusCode.IsGood(status))
				throw new Exception(string.Format("Invalid response from the server. (Response Status: {0})", status));
		}
	}
}