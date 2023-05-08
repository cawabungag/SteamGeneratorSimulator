using UnityEngine;

public class CameraController : MonoBehaviour
{
	public Transform _target;
	public float _rotationSpeed = 5f;
	public float _zoomSpeed = 5f;
	public float _minZoomDistance = 2f;
	public float _maxZoomDistance = 10f;
	private float _currentZoomDistance = 10f;

	private void Update()
	{
		if (!Input.GetKey(KeyCode.Z))
			return;
		
		// Camera rotation
		if (Input.GetMouseButton(0))
		{
			var rotationX = Input.GetAxis("Mouse X") * _rotationSpeed;
			var rotationY = Input.GetAxis("Mouse Y") * _rotationSpeed;

			transform.RotateAround(_target.position, Vector3.up, rotationX);
			transform.RotateAround(_target.position, transform.right, -rotationY);
		}

		// Camera zoom
		var zoomInput = Input.GetAxis("Mouse ScrollWheel");
		_currentZoomDistance -= zoomInput * _zoomSpeed;
		_currentZoomDistance = Mathf.Clamp(_currentZoomDistance, _minZoomDistance, _maxZoomDistance);
		var zoomVector = -transform.forward * _currentZoomDistance;
		transform.position = _target.position + zoomVector;
	}
}