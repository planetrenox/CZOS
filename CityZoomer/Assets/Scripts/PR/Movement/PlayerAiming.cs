using System;
using UnityEngine;

public class PlayerAiming : MonoBehaviour
{
	[Header("References")]
	public Transform bodyTransform;
	public static bool isSmoothingMouse = true;

	[Header("Restrictions")]
	public float minYRotation = -90f;
	public float maxYRotation = 90f;

	//The real rotation of the camera without recoil
	private Vector3 realRotation;

	[Header("Aimpunch")]
	[Tooltip("bigger number makes the response more damped, smaller is less damped, currently the system will overshoot, with larger damping values it won't")]
	public float punchDamping = 9.0f;

	[Tooltip("bigger number increases the speed at which the view corrects")]
	public float punchSpringConstant = 65.0f;

	[HideInInspector]
	public Vector2 punchAngle;

	[HideInInspector]
	public Vector2 punchAngleVel;

	[HideInInspector] public float xMovement, yMovement;

	private AnimLegFirstPersonNanaScript animateLegs;
	
	

	// PR Addition for skybox rotation
	private static readonly int Rotation = Shader.PropertyToID("_Rotation");
	[Space]
	public float skyboxRotationSpeed = 1f;
	


	private void Start()
	{
		animateLegs = GameObject.Find("SelfLegsNana").GetComponent<AnimLegFirstPersonNanaScript>();
	}

	private void Update()
	{
		// Fix pausing
		if (Mathf.Abs(Time.timeScale) <= 0)
			return;
		
		// Rotate skybox - pr addition
		RenderSettings.skybox.SetFloat(Rotation, Time.time * skyboxRotationSpeed);
		
		
		DecayPunchAngle();

		if (isSmoothingMouse)
		{
			xMovement = Input.GetAxis("Mouse X") *  PR.UI.InputField_Sensitivity_READ;
			yMovement = -Input.GetAxis("Mouse Y") *  PR.UI.InputField_Sensitivity_READ;
		}
		else
		{
			xMovement = Input.GetAxisRaw("Mouse X") *  PR.UI.InputField_Sensitivity_READ;
			yMovement = -Input.GetAxisRaw("Mouse Y") *  PR.UI.InputField_Sensitivity_READ;
		}
		
		

		

		// Calculate real rotation from input
		realRotation   = new Vector3(Mathf.Clamp(realRotation.x + yMovement, minYRotation, maxYRotation), realRotation.y + xMovement, realRotation.z);

		realRotation.z = Mathf.Lerp(realRotation.z, 0f, Time.deltaTime * 3f);

		//Apply real rotation to body
		bodyTransform.eulerAngles = Vector3.Scale(realRotation, new Vector3(0f, 1f, 0f));

		//Apply rotation and recoil
		Vector3 cameraEulerPunchApplied = realRotation;
		cameraEulerPunchApplied.x += punchAngle.x;
		cameraEulerPunchApplied.y += punchAngle.y;

		transform.eulerAngles = cameraEulerPunchApplied;


		animateLegs.updateLegAnimations(xMovement);
	}
	
	
	public void ViewPunch(Vector2 punchAmount)
	{
		//Remove previous recoil
		punchAngle = Vector2.zero;

		//Recoil go up
		punchAngleVel -= punchAmount * 20;
	}

	private void DecayPunchAngle()
	{
		if (punchAngle.sqrMagnitude > 0.001 || punchAngleVel.sqrMagnitude > 0.001)
		{
			punchAngle += punchAngleVel * Time.deltaTime;
			float damping = 1 - (punchDamping * Time.deltaTime);

			if (damping < 0)
				damping = 0;

			punchAngleVel *= damping;

			float springForceMagnitude = punchSpringConstant * Time.deltaTime;
			punchAngleVel -= punchAngle * springForceMagnitude;
		}
		else
		{
			punchAngle    = Vector2.zero;
			punchAngleVel = Vector2.zero;
		}
	}
}
