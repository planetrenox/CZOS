namespace TypeADBSetupUtility
{
	using System;
	using System.IO;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;

	public class TA_DBSetuptools : EditorWindow
	{

		[MenuItem("TypeATools/DynamicBone SetupUtility")]

		private static void Create()
		{
			GetWindow<TA_DBSetuptools>("TypeATools");
		}

		private TA_DBSDList _TA_DBSDList;
		private TA_DBSetupData _TA_DBSetupData;
		private const string ASSET_PATH = "Assets/TypeATools/DBSetupUtility/Data/";
		private const string INITFILE_PATH = "/TypeATools/DBSetupUtility/Data/path.txt";
		private string _dataName = "";
		private string settingsname = "";

		private string _plsettings = "Please check the setting.";
		private string _nonDBdata = "There is no data of DynamicBone.";
		private string _noncollider = "There is no data of DynamicBoneCollider.";
		private string _obnotfound = "The object could not be found.";
		private const string PREFIX = "DBCol_";
		private const int PREFIXSIZE = 6;
		private Dictionary<string, GameObject> _nodes = new Dictionary<string, GameObject>();
		private Dictionary<string, GameObject> _refnodes = new Dictionary<string, GameObject>();
		private Dictionary<string, GameObject> _dbnodes = new Dictionary<string, GameObject>();

		private bool error = false;

		private void OnGUI()
		{
			if(_TA_DBSDList == null) InitDtList();

			GUILayout.Space(12);
			EditorGUILayout.LabelField("TypeA DynamicBone SetupUtility", EditorStyles.boldLabel);

			GUILayout.Space(12);
			EditorGUILayout.BeginHorizontal(GUI.skin.box, GUILayout.ExpandWidth(true));
			{
				string sname = DrawAssetButton();

				EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
				{
					GUILayout.Space(8);
					if(sname != "") settingsname = sname;
					settingsname = EditorGUILayout.TextField("SettingsName", settingsname);

					GUILayout.Space(12);
					if (settingsname != "")
					{
						DrawActionButton();
						InputField();

						EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
						{
							DrawDBAction();
						}
						EditorGUILayout.EndVertical();
					}

				}
				EditorGUILayout.EndVertical();

			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
			{
				_dataName = EditorGUILayout.TextField("Save Path", _dataName);

				EditorGUILayout.BeginHorizontal(GUI.skin.box, GUILayout.ExpandWidth(true));
				{
					if (GUILayout.Button("Load"))
					{
						GUI.FocusControl("");
	
						Import(_dataName);
					}
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();

			_TA_DBSDList.LastEdit = settingsname;
			error = false;
		}

		private void OnLostFocus()
		{
//			Export();
		}

		public void DrawDBAction()
		{
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Attach") && !error)
			{
				GUI.FocusControl("");

				AttatchCollider();
				AttachDynamicBone();
			}

			if (GUILayout.Button("Read") && !error)
			{
				GUI.FocusControl("");

				GetDBNodes();
			}

			if (GUILayout.Button("Delete"))
			{
				GUI.FocusControl("");

				ClearDynamicBone();
				ClearDynamicBoneCollider();
			}

			EditorGUILayout.EndHorizontal();

		}

		public void ClearDynamicBone()
		{
			_nodes.Clear();
			GetNodes(_TA_DBSetupData.RootName, _TA_DBSetupData.ArmatureName, ref _nodes);

			foreach (KeyValuePair<string, GameObject> p in _nodes)
			{
				Component c;
				if (c = p.Value.GetComponent<DynamicBone>())
				{
					DestroyImmediate(c);
				}
			}

			_nodes.Clear();
			GetNodes(_TA_DBSetupData.RootName, _TA_DBSetupData.DBNodeRoot, ref _nodes);

			foreach (KeyValuePair<string, GameObject> p in _nodes)
			{
				Component c;
				if (c = p.Value.GetComponent<DynamicBone>())
				{
					DestroyImmediate(c);
				}
			}

			Reset();
		}

		public void ClearDynamicBoneCollider()
		{
			_nodes.Clear();
			GetNodes(_TA_DBSetupData.RootName, _TA_DBSetupData.ArmatureName, ref _nodes);

			List<string> ls = new List<string>();
			foreach (KeyValuePair<string, GameObject> p in _nodes)
			{
				ls.Add(p.Key);
			}

			foreach (string s in ls)
			{
				if (_nodes[s] != null)
				{
					if (_nodes[s].GetComponent<DynamicBoneCollider>())
					{
						DestroyImmediate(_nodes[s]);
						_nodes.Remove(s);
					}

				}
			}

			_nodes.Clear();
			GetNodes(_TA_DBSetupData.RootName, _TA_DBSetupData.DBNodeRoot, ref _nodes);

			ls.Clear();
			foreach (KeyValuePair<string, GameObject> p in _nodes)
			{
				ls.Add(p.Key);
			}

			foreach (string s in ls)
			{
				if (_nodes[s] != null)
				{
					if (_nodes[s].GetComponent<DynamicBoneCollider>())
					{
						DestroyImmediate(_nodes[s]);
						_nodes.Remove(s);
					}

				}
			}

			Reset();
		}

		public void AttachDynamicBone()
		{
			if (_TA_DBSDList.Is_overrideDB) ClearDynamicBone();

			_nodes.Clear();
			GetNodes(_TA_DBSetupData.RootName, _TA_DBSetupData.ArmatureName, ref _nodes);
			if (error) return;

			_dbnodes.Clear();
			GetNodes(_TA_DBSetupData.RootName, _TA_DBSetupData.DBNodeRoot, ref _dbnodes);
			if (error) return;

			if (_TA_DBSetupData.DBNodes == null || _TA_DBSetupData.DBNodes.Count == 0)
			{
				TA_Dialog(_nonDBdata);
				error = true;
				return;
			}

			List<string> msls = new List<string>();

			foreach (TA_DBNode dbnode in _TA_DBSetupData.DBNodes)
			{
				if (!_dbnodes.ContainsKey(dbnode.Name)) continue;

				DynamicBone db = _dbnodes[dbnode.Name].GetComponent<DynamicBone>(); 
				if(db != null)
				{
					if(_TA_DBSDList.Is_ReplaceColliders) SetDBinColliders(dbnode, db);
					continue;
				}
				else
				{
					db = _dbnodes[dbnode.Name].AddComponent<DynamicBone>();
				}

				SetDB(dbnode, db);
				msls.Add(dbnode.Name);
			}

			if (!error)
			{
				int c = msls.Count;
				msls.Insert(0, "Attached " + c + " DynamicBone.");
				TA_Dialog(msls);
			}

		}

		public void SetDB(TA_DBNode node, DynamicBone db)
		{
			db.m_Root = _nodes[node.DBProp.Root].transform;
			db.m_UpdateRate = node.DBProp.UpdateRate;
			db.m_UpdateMode = node.DBProp.UpdateMode;
			db.m_Damping = node.DBProp.Damping;
			db.m_DampingDistrib = node.DBProp.DampingDistrib;
			db.m_Elasticity = node.DBProp.Elasticity;
			db.m_ElasticityDistrib = node.DBProp.ElasticityDistrib;
			db.m_Stiffness = node.DBProp.Stiffness;
			db.m_StiffnessDistrib = node.DBProp.StiffnessDistrib;
			db.m_Inert = node.DBProp.Inert;
			db.m_InertDistrib = node.DBProp.InertDistrib;
			db.m_Radius = node.DBProp.Radius;
			db.m_RadiusDistrib = node.DBProp.RadiusDistrib;
			db.m_EndLength = node.DBProp.EndLength;
			db.m_EndOffset = node.DBProp.EndOffset;
			db.m_Gravity = node.DBProp.Gravity;
			db.m_Force = node.DBProp.Force;

			SetDBinColliders(node, db);

			if (node.DBProp.Exclusions != null)
			{
				List<Transform> ts = new List<Transform>();
				foreach (string s in node.DBProp.Exclusions)
				{
					if (!_nodes.ContainsKey(s)) continue;
					ts.Add(_nodes[s].transform);
				}
				db.m_Exclusions = ts;
			}

			db.m_FreezeAxis = node.DBProp.FreezeAxis;
			db.m_DistantDisable = node.DBProp.DistantDisable;
			db.m_ReferenceObject = node.DBProp.ReferenceObject;
			db.m_DistanceToObject = node.DBProp.DistanceToObject;
		}

		public void SetDBinColliders(TA_DBNode node, DynamicBone db)
		{
			if (node.DBProp.Colliders != null)
			{
				List<DynamicBoneColliderBase> dbcs = new List<DynamicBoneColliderBase>();
				foreach (string s in node.DBProp.Colliders)
				{
					if (!_nodes.ContainsKey(s)) continue;
					dbcs.Add(_nodes[s].GetComponent<DynamicBoneCollider>());
				}
				db.m_Colliders = dbcs;
			}
		}

		public void AttatchCollider()
		{
			if (_TA_DBSDList.Is_overrideDBC) ClearDynamicBoneCollider();

			_nodes.Clear();
			GetNodes(_TA_DBSetupData.RootName, _TA_DBSetupData.ArmatureName, ref _nodes);
			if (error) return;

			if (_TA_DBSetupData.DBCNodes == null || _TA_DBSetupData.DBCNodes.Count == 0)
			{
				TA_Dialog(_noncollider);
				return;
			}

			List<string> msls = new List<string>();

			foreach (TA_DBCNode dbcnode in  _TA_DBSetupData.DBCNodes)
			{
				if (!_nodes.ContainsKey(dbcnode.Parent)) continue;
				if (_nodes.ContainsKey(dbcnode.Name)) continue;
				GameObject ob = SetDBCollider(dbcnode);
				_nodes[ob.name] = ob;
				msls.Add(ob.name);
			}

			if (!error)
			{
				int c = msls.Count;
				msls.Insert(0, "Attached " + c + " Colliders.");
				TA_Dialog(msls);
			}

		}

		public GameObject SetDBCollider(TA_DBCNode node)
		{
			GameObject go = new GameObject(node.Name);

			go.transform.parent = _nodes[node.Parent].transform;
			go.transform.localPosition = node.LocalPosition;
			go.transform.localRotation = node.LocalRotation;
			go.transform.localScale = node.LocalScale;

			DynamicBoneCollider dbc = go.AddComponent<DynamicBoneCollider>();
			dbc.m_Direction = node.DBCProp.Direction;
			dbc.m_Center = node.DBCProp.Center;
			dbc.m_Bound = node.DBCProp.Bound;
			dbc.m_Radius = node.DBCProp.Radius;
			dbc.m_Height = node.DBCProp.Height;

			return go;
		}

		public void GetDBNodes()
		{
			_refnodes.Clear();
			GetNodes(_TA_DBSetupData.DBRefObjRoot, _TA_DBSetupData.DBRefObjRoot, ref _refnodes);
			if (error) return;

			if (_TA_DBSetupData.DBNodes == null)
			{
				_TA_DBSetupData.DBNodes = new List<TA_DBNode>();
			}
			_TA_DBSetupData.DBNodes.Clear();

			if (_TA_DBSetupData.DBCNodes == null)
			{
				_TA_DBSetupData.DBCNodes = new List<TA_DBCNode>();
			}
			_TA_DBSetupData.DBCNodes.Clear();

			List<string> msdb = new List<string>();
			List<string> msdbc = new List<string>();

			foreach (KeyValuePair<string, GameObject> pair in _refnodes)
			{
				Component[] cmps = pair.Value.GetComponents<Component>();
				foreach(Component c in cmps)
				{
					if(c.GetType() == typeof(DynamicBone))
					{
						TA_DBNode dbnode = new TA_DBNode();
						dbnode.Name = pair.Key;
						dbnode.Parent = pair.Value.transform.parent.name;
						dbnode.DBProp = GetDBPropertys(pair.Value);
						_TA_DBSetupData.DBNodes.Add(dbnode);

						msdb.Add(dbnode.Name);
					}

					if (c.GetType() == typeof(DynamicBoneCollider))
					{
						TA_DBCNode dbcnode = new TA_DBCNode();
						dbcnode.Name = pair.Key;
						dbcnode.Parent = pair.Value.transform.parent.name;
						dbcnode.LocalPosition = pair.Value.transform.localPosition;
						dbcnode.LocalRotation = pair.Value.transform.localRotation;
						dbcnode.LocalScale = pair.Value.transform.localScale;
						dbcnode.DBCProp = GetDBCPropertys(pair.Value);
						_TA_DBSetupData.DBCNodes.Add(dbcnode);

						msdbc.Add(dbcnode.Name);
					}

				}
			}

			if (!error)
			{
				List<string> ls = new List<string>();
				ls.Add("The setting of " + msdb.Count + " has been read.");
				ls.Add("The collider data of " + msdbc.Count + " has been read.");
				foreach(string s in msdb) ls.Add(s);
				foreach (string s in msdbc) ls.Add(s);
				
				TA_Dialog(ls);
			}

		}

		public TA_DBCProp GetDBCPropertys(GameObject go)
		{
			TA_DBCProp prop = new TA_DBCProp();
			DynamicBoneCollider dbc = go.GetComponent<DynamicBoneCollider>();

			prop.Direction = dbc.m_Direction;
			prop.Center = dbc.m_Center;
			prop.Bound = dbc.m_Bound;
			prop.Radius = dbc.m_Radius;
			prop.Height = dbc.m_Height;

			return prop;
		}

		public TA_DBProp GetDBPropertys(GameObject go)
		{
			TA_DBProp prop = new TA_DBProp();

			DynamicBone db = go.GetComponent<DynamicBone>();
			prop.Root = db.m_Root.name;
			prop.UpdateRate = db.m_UpdateRate;
			prop.UpdateMode = db.m_UpdateMode;
			prop.Damping = db.m_Damping;
			prop.DampingDistrib = db.m_DampingDistrib;
			prop.Elasticity = db.m_Elasticity;
			prop.ElasticityDistrib = db.m_ElasticityDistrib;
			prop.Stiffness = db.m_Stiffness;
			prop.StiffnessDistrib = db.m_StiffnessDistrib;
			prop.Inert = db.m_Inert;
			prop.InertDistrib = db.m_InertDistrib;
			prop.Radius = db.m_Radius;
			prop.RadiusDistrib = db.m_RadiusDistrib;
			prop.EndLength = db.m_EndLength;
			prop.EndOffset = db.m_EndOffset;
			prop.Gravity = db.m_Gravity;
			prop.Force = db.m_Force;

			List<DynamicBoneColliderBase> dbcbl = go.GetComponent<DynamicBone>().m_Colliders;
			if (dbcbl != null)
			{
				List<string> cols = new List<string>();
				foreach (DynamicBoneColliderBase cb in go.GetComponent<DynamicBone>().m_Colliders)
				{
					if (cb != null) cols.Add(cb.name);
				}
				prop.Colliders = cols;
			}

			List<Transform> el = go.GetComponent<DynamicBone>().m_Exclusions;
			if (el != null)
			{
				List<string> excs = new List<string>();
				foreach (Transform t in go.GetComponent<DynamicBone>().m_Exclusions)
				{
					if (t != null) excs.Add(t.name);
				}
				prop.Exclusions = excs;
			}

			prop.FreezeAxis = go.GetComponent<DynamicBone>().m_FreezeAxis;
			prop.DistantDisable = go.GetComponent<DynamicBone>().m_DistantDisable;
			prop.ReferenceObject = go.GetComponent<DynamicBone>().m_ReferenceObject;
			prop.DistanceToObject = go.GetComponent<DynamicBone>().m_DistanceToObject;

			return prop;
		}

		public GameObject FindObjectInScene(string name)
		{
			UnityEngine.Object[] rs = Resources.FindObjectsOfTypeAll(typeof(GameObject));
			GameObject go = null;
			foreach (GameObject r in rs)
			{
				if (r.name == name)
				{
					go = r;
					break;
				}
			}
			return go;
		}

		public void GetNodes(string root, string name, ref Dictionary<string, GameObject> dic)
		{
			if(root == "" || name == "")
			{
				TA_Dialog(_plsettings);
				error = true;
				return;
			}

			GameObject go = FindObjectInScene(root);

			if(go == null)
			{
				List<string> ms = new List<string>();
				ms.Add(_obnotfound);
				ms.Add(_plsettings);
				TA_Dialog(ms);
				error = true;
				return;
			}

			if (root != name)
			{
				go = GetChildren(go, name);
			}

			if(go == null)
			{
				TA_Dialog(_plsettings);
				error = true;
				return;
			}

			if (dic.Count != 0) dic.Clear();
			dic[name] = go;
			GetChildren(go, ref dic);
		}

		public GameObject GetChildren(GameObject go, string name)
		{
			Transform children = go.GetComponentInChildren<Transform>();
			if (children.childCount == 0) return null;
			GameObject g = null;

			foreach (Transform child in children)
			{
				if (child.name == name)
				{
					g = child.gameObject;
					break;
				}
				if(GetChildren(child.gameObject, name) != null) break;
			}
			return g;
		}

		public void GetChildren(GameObject go, ref Dictionary<string, GameObject> dic)
		{
			Transform children = go.GetComponentInChildren<Transform>();
			if (children.childCount == 0) return;

			foreach (Transform child in children)
			{
				dic[child.name] = child.gameObject;
				GetChildren(child.gameObject, ref dic);
			}

		}

		public void TA_Dialog( string s)
		{
			List<string> ls = new List<string>();
			ls.Add(s);
			TA_Dialog(ls);
		}

		public void TA_Dialog(List<string> ls)
		{
			EditorMassageDialog w;
			w = CreateInstance<EditorMassageDialog>();
			w.Massage = ls;
			w.ShowUtility();
		}

		public void InputField()
		{
			EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

			GUILayout.Space(8);

			_TA_DBSetupData.RootName = EditorGUILayout.TextField("RootObject", _TA_DBSetupData.RootName);
			GUILayout.Space(4);

			_TA_DBSetupData.ArmatureName = EditorGUILayout.TextField("BoneRoot", _TA_DBSetupData.ArmatureName);
			GUILayout.Space(4);

			_TA_DBSetupData.DBRefObjRoot= EditorGUILayout.TextField("ReferenceObject", _TA_DBSetupData.DBRefObjRoot);
			GUILayout.Space(4);

			_TA_DBSetupData.DBNodeRoot = EditorGUILayout.TextField("DynamicBoneRoot", _TA_DBSetupData.DBNodeRoot);
			GUILayout.Space(4);

			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.LabelField("Override", GUILayout.Width(80));
			EditorGUILayout.LabelField("DynamicBone", GUILayout.Width(106));
			_TA_DBSDList.Is_overrideDB = EditorGUILayout.Toggle(_TA_DBSDList.Is_overrideDB);
			EditorGUILayout.LabelField("Collider", GUILayout.Width(80));
			_TA_DBSDList.Is_overrideDBC = EditorGUILayout.Toggle(_TA_DBSDList.Is_overrideDBC);

			EditorGUILayout.EndHorizontal();

			if (!_TA_DBSDList.Is_overrideDB)
			{
				EditorGUILayout.BeginHorizontal();

				EditorGUILayout.LabelField("", GUILayout.Width(80));
				EditorGUILayout.LabelField("Replace Colliders", GUILayout.Width(106));
				_TA_DBSDList.Is_ReplaceColliders = EditorGUILayout.Toggle(_TA_DBSDList.Is_ReplaceColliders);

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.EndVertical();

		}

		public string DrawAssetButton()
		{

			EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true), GUILayout.Width(160));

			GUILayout.Space(8);

			string name = "";

			foreach(TA_DBSetupData dt in _TA_DBSDList.DataList)
			{
				CheckSettingsName(dt);
				if (GUILayout.Button(dt.SettingsName))
				{
					GUI.FocusControl("");
					_TA_DBSetupData = dt;
					name = dt.SettingsName;
				}

			}
			GUILayout.Space(8);

			EditorGUILayout.EndVertical();

			_TA_DBSDList.LastEdit = name;
			return name;
		}

		public void DrawActionButton()
		{
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Save"))
			{
				GUI.FocusControl("");
				Export();
			}
			if (GUILayout.Button("Create"))
			{
				GUI.FocusControl("");
				Create(settingsname);
			}
			if (GUILayout.Button("Delete"))
			{
				GUI.FocusControl("");
				Delete(settingsname);
			}

			EditorGUILayout.EndHorizontal();
			GUILayout.Space(8);
		}

		private void InitDtList()
		{
			_TA_DBSDList = ScriptableObject.CreateInstance<TA_DBSDList>();
			_TA_DBSetupData = new TA_DBSetupData();
			Import();
		}

		private void Reset()
		{
			_nodes.Clear();
			_refnodes.Clear();
			_dbnodes.Clear();
		}

		private void Create(string name)
		{
			TA_DBSetupData dt = _TA_DBSDList.DataList.Find(d => d.SettingsName == name);
			if (dt != null)	return;

			TA_DBSetupData newdt = new TA_DBSetupData();
			newdt.SettingsName = settingsname;
			CheckSettingsName(newdt);
			newdt.RootName = name;
			_TA_DBSDList.DataList.Add(newdt);
			_TA_DBSetupData = newdt;
			_TA_DBSDList.LastEdit = settingsname;
		}

		private void Delete(string name)
		{
			TA_DBSetupData dt = _TA_DBSDList.DataList.Find(d => d.SettingsName == name);
			if (dt == null) return;

			_TA_DBSDList.DataList.Remove(dt);
			_TA_DBSetupData = new TA_DBSetupData();
			_TA_DBSDList.LastEdit = "";
		}

		private void Import()
		{
			_dataName = ReadText(INITFILE_PATH);
			Import(_dataName);
		}

		private void Import(string name)
		{
			WriteText(INITFILE_PATH, _dataName);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			string dtpath = ASSET_PATH + name + ".asset";

			TA_DBSDList list = AssetDatabase.LoadAssetAtPath<TA_DBSDList>(dtpath);
			if (list == null)
			{
				_TA_DBSDList.DataList = new List<TA_DBSetupData>();
				_TA_DBSetupData = new TA_DBSetupData();
				return;
			}

			_TA_DBSDList = AssetDatabase.LoadAssetAtPath<TA_DBSDList>(dtpath);
			if (_TA_DBSDList.LastEdit != "" && _TA_DBSDList.DataList.Count != 0)
			{
				settingsname = _TA_DBSDList.LastEdit;
				TA_DBSetupData dt = _TA_DBSDList.DataList.Find(d => d.SettingsName == settingsname);
				_TA_DBSetupData = dt;
			}

		}

		private void Export()
		{

			string p = INITFILE_PATH;
			if (_dataName == "")
			{
				_dataName = ReadText(p);
			}
			else
			{
				WriteText( p, _dataName);
			}

			_TA_DBSetupData.SettingsName = settingsname; 

			CheckSettingsName();

			string dtpath = ASSET_PATH + _dataName + ".asset";

			if (!AssetDatabase.Contains(_TA_DBSDList as UnityEngine.Object))
			{
				string directory = Path.GetDirectoryName(dtpath);
				if (!Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory);
				}
				AssetDatabase.CreateAsset(_TA_DBSDList, dtpath);
			}

			_TA_DBSDList.hideFlags = HideFlags.NotEditable;
			EditorUtility.SetDirty(_TA_DBSDList);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		public void CheckSettingsName( TA_DBSetupData dt)
		{
			if(dt.SettingsName == "")
			{
				dt.SettingsName = dt.RootName;
			}

		}

		public void CheckSettingsName()
		{
			foreach(TA_DBSetupData dt in _TA_DBSDList.DataList)
			{
				if (dt.SettingsName == "")
				{
					dt.SettingsName = dt.RootName;
				}
			}
		}

		public bool WriteText(string path, string text)
		{
			try
			{
				using (StreamWriter writer = new StreamWriter(Application.dataPath + path, false))
				{
					writer.Write(text);
					writer.Flush();
					writer.Close();
				}
			}
			catch (Exception e)
			{
				Debug.Log(e.Message);
				return false;
			}
			return true;
		}

		public string ReadText(string path)
		{
			string strStream = "";
			try
			{
				using (StreamReader sr = new StreamReader(Application.dataPath + path))
				{
					strStream = sr.ReadToEnd();
					sr.Close();
				}
			}
			catch (Exception e)
			{
				Debug.Log(e.Message);
			}

			return strStream;
		}
	}

	public sealed class EditorMassageDialog : EditorWindow
	{
		static EditorMassageDialog exampleWindow;

		public List<string> Massage;
		private Vector2 v = new Vector2(0, 0);

		static void Open()
		{
			if (exampleWindow == null)
			{
				exampleWindow = CreateInstance<EditorMassageDialog>();
			}

			var buttonRect = new Rect(100, 100, 300, 100);
			var windowSize = new Vector2(300, 100);
			exampleWindow.ShowAsDropDown(buttonRect, windowSize);
		}

		private void OnGUI()
		{
			EditorGUILayout.BeginVertical(GUI.skin.label, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			if(Massage != null)
			{
				if(Massage.Count != 0)
				{
					v = EditorGUILayout.BeginScrollView(v, false, false);

					for(int i=0;i < Massage.Count; ++i)
					{
						EditorGUILayout.LabelField(Massage[i]);
					}

					EditorGUILayout.EndScrollView();
				}
			}
			EditorGUILayout.EndVertical();
			if (GUILayout.Button("OK"))
			{
				Close();
			}

		}
	}

	public class Node
	{
		public string name;
		public GameObject gameObject;
	}
}
