//Unitychan
//AutoBlink.cs
//オート目パチスクリプト
//2014/06/23 N.Kobayashi
//

using System;
using UnityEngine;
using System.Collections;
using System.Security;
using Random = UnityEngine.Random;

namespace UnityChan
{
	public class AutoBlink : MonoBehaviour
	{
        public int blendShapeIndex;
		public bool isActive = true;				
		public SkinnedMeshRenderer face;	
		
		public float ratio_Close = 85.0f;			
		public float ratio_HalfClose = 20.0f;		
		[HideInInspector]
		public float
			ratio_Open = 0.0f;
		private bool timerStarted = false;			
		private bool isBlink = false;				

		public float timeBlink = 0.4f;				
		private float timeRemining = 0.0f;			

		public float threshold = 0.3f;				
		public float interval = 3.0f;				



		enum Status
		{
			Close,
			HalfClose,
			Open	
		}
		
		

		private Status eyeStatus;	

		// Use this for initialization
		void Start ()
		{
			ResetTimer ();
			
			StartCoroutine ("RandomChange");
		}

		
		void ResetTimer ()
		{
			timeRemining = timeBlink;
			timerStarted = false;
		}

		// Update is called once per frame
		void Update ()
		{
			if (!timerStarted) {
				eyeStatus = Status.Close;
				timerStarted = true;
			}
			if (timerStarted) {
				timeRemining -= Time.deltaTime;
				if (timeRemining <= 0.0f) {
					eyeStatus = Status.Open;
					ResetTimer ();
				} else if (timeRemining <= timeBlink * 0.3f) {
					eyeStatus = Status.HalfClose;
				}
			}
		}

		void LateUpdate ()
		{
			if (isActive) {
				if (isBlink) {
					switch (eyeStatus) {
					case Status.Close:
						SetCloseEyes ();
						break;
					case Status.HalfClose:
						SetHalfCloseEyes ();
						break;
					case Status.Open:
						SetOpenEyes ();
						isBlink = false;
						break;
					}
					
				}
			}
		}

		void SetCloseEyes ()
		{
			
			face.SetBlendShapeWeight (blendShapeIndex, ratio_Close);
		}

		void SetHalfCloseEyes ()
		{
			face.SetBlendShapeWeight (blendShapeIndex, ratio_HalfClose);
		}

		void SetOpenEyes ()
		{
			face.SetBlendShapeWeight (blendShapeIndex, ratio_Open);
		}
		
		
		IEnumerator RandomChange ()
		{
			
			while (true) {
				
				float _seed = Random.Range (0.0f, 1.0f);
				if (!isBlink) {
					if (_seed > threshold) {
						isBlink = true;
					}
				}
				
				yield return new WaitForSeconds (interval);
			}
		}
	}
}