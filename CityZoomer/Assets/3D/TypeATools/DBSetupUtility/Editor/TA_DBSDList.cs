namespace TypeADBSetupUtility
{
	using System.Collections;
	using System.Collections.Generic;
	using System;
	using UnityEngine;


	[Serializable]
	public class TA_DBSDList : ScriptableObject
	{
		[SerializeField] private string _lastEdit;
		[SerializeField] private bool _is_overrideDB;
		[SerializeField] private bool _is_replaceColliders;
		[SerializeField] private bool _is_overrideDBC;
		[SerializeField] private List<TA_DBSetupData> _dataList;

		public string LastEdit
		{
			get { return _lastEdit; }
#if UNITY_EDITOR
			set { _lastEdit = value; }
#endif
		}

		public bool Is_overrideDB
		{
			get { return _is_overrideDB; }
#if UNITY_EDITOR
			set { _is_overrideDB = value; }
#endif
		}

		public bool Is_ReplaceColliders
		{
			get { return _is_replaceColliders; }
#if UNITY_EDITOR
			set { _is_replaceColliders = value; }
#endif
		}

		public bool Is_overrideDBC
		{
			get { return _is_overrideDBC; }
#if UNITY_EDITOR
			set { _is_overrideDBC = value; }
#endif
		}

		public List<TA_DBSetupData> DataList
		{
			get { return _dataList; }
#if UNITY_EDITOR
			set { _dataList = value; }
#endif
		}
#if UNITY_EDITOR
		public void CopyList(List<TA_DBSetupData> dic)
		{
			_dataList = new List<TA_DBSetupData>(dic);
			Debug.Log("copy");
		}
#endif

#if UNITY_EDITOR
		public void Copy(TA_DBSDList dtlist)
		{
			_lastEdit = dtlist.LastEdit;
			CopyList(dtlist.DataList);
		}
#endif
	}


	[Serializable]
	public class TA_DBSetupData
	{
		[SerializeField] private string _settingsName;

		[SerializeField] private string _rootName;
		[SerializeField] private string _armatureName = "Armature";
		[SerializeField] private string _dbrefObjRoot;
		[SerializeField] private string _dbNodeRoot = "DynamicBone";
		[SerializeField] private List<TA_DBNode> _dbnodes;
		[SerializeField] private List<TA_DBCNode> _dbcnodes;

		public string SettingsName
		{
			get { return _settingsName; }
#if UNITY_EDITOR
			set { _settingsName = value; }
#endif
		}

		public string RootName
		{
			get { return _rootName; }
#if UNITY_EDITOR
			set { _rootName = value; }
#endif
		}

		public string ArmatureName
		{
			get { return _armatureName; }
#if UNITY_EDITOR
			set { _armatureName = value; }
#endif
		}

		public string DBRefObjRoot
		{
			get { return _dbrefObjRoot; }
#if UNITY_EDITOR
			set { _dbrefObjRoot = value; }
#endif
		}

		public string DBNodeRoot
		{
			get { return _dbNodeRoot; }
#if UNITY_EDITOR
			set { _dbNodeRoot = value; }
#endif
		}

		public List<TA_DBNode> DBNodes
		{
			get { return _dbnodes; }
#if UNITY_EDITOR
			set { _dbnodes = value; }
#endif
		}
#if UNITY_EDITOR
		public void CopyDBNodes(List<TA_DBNode> list)
		{
			_dbnodes = new List<TA_DBNode>(list);
		}
#endif

		public List<TA_DBCNode> DBCNodes
		{
			get { return _dbcnodes; }
#if UNITY_EDITOR
			set { _dbcnodes = value; }
#endif
		}
#if UNITY_EDITOR
		public void CopyDBCNodes(List<TA_DBCNode> list)
		{
			_dbcnodes = new List<TA_DBCNode>(list);
		}
#endif

#if UNITY_EDITOR
		public void Clear()
		{
			_rootName = "";
			_armatureName = "Armature";
			_dbrefObjRoot = "";
			_dbNodeRoot = "DynamicBone";
			if (_dbnodes != null) _dbnodes.Clear();
		}
#endif

#if UNITY_EDITOR
		public void Copy(TA_DBSetupData sd)
		{
			_rootName = sd.RootName;
			_armatureName = sd.ArmatureName;
			_dbrefObjRoot = sd.DBRefObjRoot;
			_dbNodeRoot = sd.DBNodeRoot;
			CopyDBNodes(sd.DBNodes);
			CopyDBCNodes(sd.DBCNodes);
		}
#endif
	}

}
