namespace TypeADBSetupUtility
{
	using System.Collections;
	using System.Collections.Generic;
	using System;
	using UnityEngine;

	[Serializable]
	public class TA_DBProp
	{
		[SerializeField] private string _root;
		[SerializeField] private float _UpdateRate;
		[SerializeField] private DynamicBone.UpdateMode _UpdateMode;
		[SerializeField] private float _Damping;
		[SerializeField] private AnimationCurve _DampingDistrib;
		[SerializeField] private float _Elasticity;
		[SerializeField] private AnimationCurve _ElasticityDistrib;
		[SerializeField] private float _Stiffness;
		[SerializeField] private AnimationCurve _StiffnessDistrib;
		[SerializeField] private float _Inert;
		[SerializeField] private AnimationCurve _InertDistrib;
		[SerializeField] private float _Radius;
		[SerializeField] private AnimationCurve _RadiusDistrib;
		[SerializeField] private float _EndLength;
		[SerializeField] private Vector3 _EndOffset;
		[SerializeField] private Vector3 _Gravity;
		[SerializeField] private Vector3 _Force;
		[SerializeField] private List<string> _colliders;
		[SerializeField] private List<string> _Exclusions;
		[SerializeField] private DynamicBone.FreezeAxis _FreezeAxis;
		[SerializeField] private bool _DistantDisable;
		[SerializeField] private Transform _ReferenceObject;
		[SerializeField] private float _DistanceToObject;

		public string Root
		{
			get { return _root; }
#if UNITY_EDITOR
			set { _root = value; }
#endif
		}

		public float UpdateRate
		{
			get { return _UpdateRate; }
#if UNITY_EDITOR
			set { _UpdateRate = value; }
#endif
		}

		public DynamicBone.UpdateMode UpdateMode
		{
			get { return _UpdateMode; }
#if UNITY_EDITOR
			set { _UpdateMode = value; }
#endif
		}

		public float Damping
		{
			get { return _Damping; }
#if UNITY_EDITOR
			set { _Damping = value; }
#endif
		}

		public AnimationCurve DampingDistrib
		{
			get { return _DampingDistrib; }
#if UNITY_EDITOR
			set { _DampingDistrib = value; }
#endif
		}

		public float Elasticity
		{
			get { return _Elasticity; }
#if UNITY_EDITOR
			set { _Elasticity = value; }
#endif
		}

		public AnimationCurve ElasticityDistrib
		{
			get { return _ElasticityDistrib; }
#if UNITY_EDITOR
			set { _ElasticityDistrib = value; }
#endif
		}

		public float Stiffness
		{
			get { return _Stiffness; }
#if UNITY_EDITOR
			set { _Stiffness = value; }
#endif
		}

		public AnimationCurve StiffnessDistrib
		{
			get { return _StiffnessDistrib; }
#if UNITY_EDITOR
			set { _StiffnessDistrib = value; }
#endif
		}

		public float Inert
		{
			get { return _Inert; }
#if UNITY_EDITOR
			set { _Inert = value; }
#endif
		}

		public AnimationCurve InertDistrib
		{
			get { return _InertDistrib; }
#if UNITY_EDITOR
			set { _InertDistrib = value; }
#endif
		}

		public float Radius
		{
			get { return _Radius; }
#if UNITY_EDITOR
			set { _Radius = value; }
#endif
		}

		public AnimationCurve RadiusDistrib
		{
			get { return _RadiusDistrib; }
#if UNITY_EDITOR
			set { _RadiusDistrib = value; }
#endif
		}

		public float EndLength
		{
			get { return _EndLength; }
#if UNITY_EDITOR
			set { _EndLength = value; }
#endif
		}

		public Vector3 EndOffset
		{
			get { return _EndOffset; }
#if UNITY_EDITOR
			set { _EndOffset = value; }
#endif
		}

		public Vector3 Gravity
		{
			get { return _Gravity; }
#if UNITY_EDITOR
			set { _Gravity = value; }
#endif
		}

		public Vector3 Force
		{
			get { return _Force; }
#if UNITY_EDITOR
			set { _Force = value; }
#endif
		}

		public List<string> Colliders
		{
			get { return _colliders; }
#if UNITY_EDITOR
			set { _colliders = value; }
#endif
		}

#if UNITY_EDITOR
		public void Copy_colliders(List<string> list)
		{
			_colliders = new List<string>(list);
		}
#endif

		public List<string> Exclusions
		{
			get { return _Exclusions; }
#if UNITY_EDITOR
			set { _Exclusions = value; }
#endif
		}

#if UNITY_EDITOR
		public void Copy_Exclusions(List<string> list)
		{
			_Exclusions = new List<string>(list);
		}
#endif

		public DynamicBone.FreezeAxis FreezeAxis
		{
			get { return _FreezeAxis; }
#if UNITY_EDITOR
			set { _FreezeAxis = value; }
#endif
		}

		public bool DistantDisable
		{
			get { return _DistantDisable; }
#if UNITY_EDITOR
			set { _DistantDisable = value; }
#endif
		}

		public Transform ReferenceObject
		{
			get { return _ReferenceObject; }
#if UNITY_EDITOR
			set { _ReferenceObject = value; }
#endif
		}

		public float DistanceToObject
		{
			get { return _DistanceToObject; }
#if UNITY_EDITOR
			set { _DistanceToObject = value; }
#endif
		}

#if UNITY_EDITOR
		public void Copy(TA_DBProp dt)
		{
			_root = dt.Root;
			_UpdateRate = dt.UpdateRate;
			_UpdateMode = dt.UpdateMode;
			_Damping = dt.Damping;
			_DampingDistrib = dt.DampingDistrib;
			_Elasticity = dt.Elasticity;
			_ElasticityDistrib = dt.ElasticityDistrib;
			_Stiffness = dt.Stiffness;
			_StiffnessDistrib = dt.StiffnessDistrib;
			_Inert = dt.Inert;
			_InertDistrib = dt.InertDistrib;
			_Radius = dt.Radius;
			_RadiusDistrib = dt.RadiusDistrib;
			_EndLength = dt.EndLength;
			_EndOffset = dt.EndOffset;
			_Gravity = dt.Gravity;
			_Force = dt.Force;

			Copy_colliders(dt.Colliders);
			Copy_Exclusions(dt.Exclusions);

			_FreezeAxis = dt.FreezeAxis;
			_DistantDisable = dt.DistantDisable;
			_ReferenceObject = dt.ReferenceObject;
			_DistanceToObject = dt.DistanceToObject;

		}
#endif
	}

	[Serializable]
	public class TA_DBNode
	{
		[SerializeField] private string _name;
		[SerializeField] private string _parent;
		[SerializeField] private TA_DBProp _dbprop;

		public string Name
		{
			get { return _name; }
#if UNITY_EDITOR
			set { _name = value; }
#endif
		}

		public string Parent
		{
			get { return _parent; }
#if UNITY_EDITOR
			set { _parent = value; }
#endif
		}

		public TA_DBProp DBProp
		{
			get { return _dbprop; }
#if UNITY_EDITOR
			set { _dbprop = value; }
#endif
		}
	}

	[Serializable]
	public class TA_DBCProp
	{
		[SerializeField] private DynamicBoneCollider.Direction _Direction;
		[SerializeField] private Vector3 _Center;
		[SerializeField] private DynamicBoneCollider.Bound _Bound;
		[SerializeField] private float _Radius;
		[SerializeField] private float _Height;

		public DynamicBoneCollider.Direction Direction
		{
			get { return _Direction; }
#if UNITY_EDITOR
			set { _Direction = value; }
#endif
		}

		public Vector3 Center
		{
			get { return _Center; }
#if UNITY_EDITOR
			set { _Center = value; }
#endif
		}

		public DynamicBoneCollider.Bound Bound
		{
			get { return _Bound; }
#if UNITY_EDITOR
			set { _Bound = value; }
#endif
		}

		public float Radius
		{
			get { return _Radius; }
#if UNITY_EDITOR
			set { _Radius = value; }
#endif
		}

		public float Height
		{
			get { return _Height; }
#if UNITY_EDITOR
			set { _Height = value; }
#endif
		}

	}

	[Serializable]
	public class TA_DBCNode
	{
		[SerializeField] private string _name;
		[SerializeField] private string _parent;
		[SerializeField] private Vector3 _localPosition;
		[SerializeField] private Quaternion _localRotation;
		[SerializeField] private Vector3 _localScale;
		[SerializeField] private TA_DBCProp _dbcprop;

		public string Name
		{
			get { return _name; }
#if UNITY_EDITOR
			set { _name = value; }
#endif
		}

		public string Parent
		{
			get { return _parent; }
#if UNITY_EDITOR
			set { _parent = value; }
#endif
		}

		public Vector3 LocalPosition
		{
			get { return _localPosition; }
#if UNITY_EDITOR
			set { _localPosition = value; }
#endif
		}

		public Quaternion LocalRotation
		{
			get { return _localRotation; }
#if UNITY_EDITOR
			set { _localRotation = value; }
#endif
		}

		public Vector3 LocalScale
		{
			get { return _localScale; }
#if UNITY_EDITOR
			set { _localScale = value; }
#endif
		}

		public TA_DBCProp DBCProp
		{
			get { return _dbcprop; }
#if UNITY_EDITOR
			set { _dbcprop = value; }
#endif
		}
	}

}
