using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DefaultNamespace;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace States
{
	public class ServerStatesController : Singleton<ServerStatesController>
	{
		private const float DELAY_BETWEEN_REQUESTS = 0.5f;
		private const string GET_STATES_URL = "https://localhost:7146/states";
		private readonly List<State> _currentStates = new();
		private Coroutine _statesRequest;
		private Coroutine _stateDeleteRequest;
		private Coroutine _statePutRequeste;
		private Coroutine _deleteAllStates;

		private void Start()
		{
			_statesRequest = StartCoroutine(RequestNewStates(DELAY_BETWEEN_REQUESTS));
			Bootstrap.Instance.StateMachine.OnStateFinished += OnStateFinished;
		}

		private void OnStateFinished(Guid stateId)
		{
			_stateDeleteRequest = StartCoroutine(RequestDeleteState(stateId));
		}

		private IEnumerator RequestDeleteState(Guid stateId)
		{
			var delete = UnityWebRequest.Delete($"{GET_STATES_URL}/{stateId}");
			yield return delete.SendWebRequest();
			Debug.Log($"Delete state: {delete.result}");
			StopCoroutine(_stateDeleteRequest);
		}

		private IEnumerator RequestPutState(Guid stateId, StateStatus status, StateType type, float duration)
		{
			var newUpdateStateDto = new UpdateStateDto(type, status, duration);
			var json = JsonConvert.SerializeObject(newUpdateStateDto);
			var put = new UnityWebRequest($"{GET_STATES_URL}/{stateId}", "PUT");
			var bodyRaw = Encoding.UTF8.GetBytes(json);
			put.uploadHandler = new UploadHandlerRaw(bodyRaw);
			put.SetRequestHeader("Content-Type", "application/json");
			yield return put.SendWebRequest();
			Debug.Log($"Put state: {put.result} Json: {json}");
			StopCoroutine(_statePutRequeste);
		}

		private IEnumerator RequestNewStates(float delayBetweenRequests)
		{
			yield return new WaitForSeconds(delayBetweenRequests);

			var unityWebRequest = UnityWebRequest.Get(GET_STATES_URL);
			yield return unityWebRequest.SendWebRequest();

			if (unityWebRequest.result == UnityWebRequest.Result.Success)
			{
				var statesJson = unityWebRequest.downloadHandler.text;
				Debug.Log($"Get states: {statesJson}");
				var states = JsonConvert.DeserializeObject<State[]>(statesJson);

				foreach (var state in states)
				{
					var find = _currentStates.Find(x => x.Id == state.Id);
					if (find == null || find.Status != state.Status)
					{
						_currentStates.Add(state);
						switch (state.Status)
						{
							case StateStatus.Queued:
								Bootstrap.Instance.StateMachine.AddState(state.ToClientState());
								_statePutRequeste = StartCoroutine(RequestPutState(state.Id, StateStatus.InProgress,
									state.Type, state.Duration));
								break;
							case StateStatus.Finished:
								Bootstrap.Instance.StateMachine.FinishCurentState();
								break;
						}
					}
				}
			}

			StopCoroutine(_statesRequest);
			_statesRequest = StartCoroutine(RequestNewStates(DELAY_BETWEEN_REQUESTS));
		}

		public void DeleteAll()
		{
			_deleteAllStates = StartCoroutine(DeleteStates());
		}

		private IEnumerator DeleteStates()
		{
			Debug.LogError("Delete all states");
			var unityWebRequest = UnityWebRequest.Get(GET_STATES_URL);
			yield return unityWebRequest.SendWebRequest();

			if (unityWebRequest.result != UnityWebRequest.Result.Success)
				yield break;

			var statesJson = unityWebRequest.downloadHandler.text;
			var states = JsonConvert.DeserializeObject<State[]>(statesJson);
			foreach (var state in states)
			{
				var delete = UnityWebRequest.Delete($"{GET_STATES_URL}/{state.Id}");
				yield return delete.SendWebRequest();
				Debug.Log($"Delete state: {delete.result}");
			}

			Bootstrap.Instance.StateMachine.FinishAllState();
			StopCoroutine(_deleteAllStates);
		}
	}
}