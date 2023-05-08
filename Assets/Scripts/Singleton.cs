using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Component
{
	public static T Instance => _instance;
	private static T _instance;

	protected virtual void Awake()
	{
		if (_instance == null)
		{
			_instance = this as T;
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			Destroy(gameObject);
		}
	}
}