using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace BlackCatPool
{
    [CustomEditor(typeof(ObjectPoolManager))]
    public class ObjectPoolManagerDrawer : Editor
    {
        private float height = EditorGUIUtility.singleLineHeight;        

        private ReorderableList collapsedList;
        private ReorderableList reorderableList;
        private SerializedObject so;
        private SerializedProperty list;
        private SerializedProperty listExpanded;

        private int listCount;
        private bool poolDetailExpanded = false;        
        private readonly List<int> emptyList = new List<int>();
        private Dictionary<string, bool> poolDetails = new Dictionary<string, bool>();

        private void OnEnable()
        {
            so = new SerializedObject(target);
            list = so.FindProperty("poolObjects");
            listExpanded = so.FindProperty("listExpanded");
            reorderableList = new ReorderableList(so, list, true, true, true, false);
            collapsedList = new ReorderableList(emptyList, typeof(int), false, true, false, false);

            reorderableList.drawHeaderCallback += OnDrawHeader;
            reorderableList.drawElementCallback += OnDrawElement;
            reorderableList.elementHeightCallback += OnGetElementHeight;
            reorderableList.onAddCallback += OnAdd;

            collapsedList.drawHeaderCallback += OnDrawCollapsedHeader;
            collapsedList.elementHeight = -3;

            listCount = list.arraySize;

            EditorApplication.update += Update;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Update;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            CustomDrawerHelper.Space(1);

            EditorGUILayout.PropertyField(so.FindProperty("persistBetweenScenes"));
            EditorGUILayout.PropertyField(so.FindProperty("hierarchyOrganisation"));

            CustomDrawerHelper.Space(1);

            if (listExpanded.boolValue)
            {
                reorderableList.DoLayoutList();
            }
            else
            {
                collapsedList.DoLayoutList();
            }

            if (Application.isPlaying)
            {
                CustomDrawerHelper.Space(2);

                poolDetailExpanded = EditorGUILayout.Foldout(poolDetailExpanded, "Object Pools", true);
                if (poolDetailExpanded)
                {
                    Color32 lineColour = new Color32(99, 99, 99, 255);

                    EditorGUI.DrawRect(EditorGUILayout.GetControlRect().MoveX(-11).AddWidth(11).WithHeight(1), lineColour);
                    EditorGUILayout.Space(-height);

                    EditorGUI.indentLevel++;
                    var pools = ObjectPoolManager.Instance.GetPools();

                    for (int i = 0; i < pools.Count; i++)
                    {
                        var pool = pools[i];

                        EditorGUILayout.BeginHorizontal();

                        bool expandedDetail = false;
                        if (poolDetails.TryGetValue(pool.Key, out var isExpanded))
                        {
                            expandedDetail = isExpanded;
                        }
                        else
                        {
                            poolDetails.Add(pool.Key, true);
                        }

                        expandedDetail = EditorGUILayout.Foldout(expandedDetail, pool.Key, true);
                        if (GUILayout.Button("Ping Pool", GUILayout.Width(125)))
                        {
                            EditorGUIUtility.PingObject(pool.Value);
                        }

                        EditorGUILayout.EndHorizontal();

                        if (expandedDetail)
                        {
                            EditorGUI.indentLevel++;

                            float labelWidth = CustomDrawerHelper.CalculateLabelWidth("Pooled Object") + 30;

                            Rect h_line = EditorGUILayout.GetControlRect().MoveX(8).WithWidth(1).WithHeight(height * 3f - 4);
                            Rect v_line = h_line.MoveY(h_line.height).WithWidth(11).WithHeight(1);
                            EditorGUI.DrawRect(h_line, lineColour);
                            EditorGUI.DrawRect(v_line, lineColour);
                            EditorGUILayout.Space(-height - 4);

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Pooled Object", GUILayout.Width(labelWidth));
                            EditorGUILayout.ObjectField(pool.Value.PooledObject, typeof(GameObject), false);
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Active Count", GUILayout.Width(labelWidth));
                            EditorGUILayout.LabelField($"{pool.Value.ActiveCount} / {pool.Value.Capacity}");
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("", GUILayout.Width(27));
                            if (GUILayout.Button("Remove Pool", GUILayout.Width(85)))
                            {
                                pool.Value.RemovePool();
                                poolDetails.Remove(pool.Key);
                            }
                            EditorGUILayout.EndHorizontal();

                            EditorGUI.indentLevel--;
                        }

                        poolDetails[pool.Key] = expandedDetail;
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
            }
        }

        private void Update()
        {
            if (Application.isPlaying && poolDetailExpanded)
            {
                Repaint();
            }
        }

        private void OnDrawCollapsedHeader(Rect rect)
        {
            EditorGUI.BeginProperty(rect, new GUIContent("Object Pools"), list);

            listExpanded.boolValue = EditorGUI.Foldout(rect.WithX(rect.x - 7), listExpanded.boolValue, GUIContent.none);

            Rect r_header = rect.WithHeight(height);
            EditorGUI.LabelField(r_header, "Object Pools", EditorStyles.boldLabel);

            Rect r_count = r_header.AppendRight().MoveX(-32).WithWidth(30);
            GUI.SetNextControlName("element_count");
            listCount = EditorGUI.IntField(r_count, listCount);
            if (GUI.GetNameOfFocusedControl() != "element_count" && list.arraySize != listCount)
            {
                list.arraySize = listCount;
                so.ApplyModifiedProperties();
                so.Update();
            }

            EditorGUI.EndProperty();
        }

        private void OnDrawHeader(Rect rect)
        {
            listExpanded.boolValue = EditorGUI.Foldout(rect.WithX(rect.x - 7), listExpanded.boolValue, GUIContent.none);

            Rect r_header = rect.WithHeight(height);
            EditorGUI.LabelField(r_header, "Object Pools", EditorStyles.boldLabel);

            Rect r_count = r_header.AppendRight().MoveX(-32).WithWidth(30);
            GUI.SetNextControlName("element_count");
            listCount = EditorGUI.IntField(r_count, listCount);
            if (GUI.GetNameOfFocusedControl() != "element_count" && list.arraySize != listCount)
            {
                list.arraySize = listCount;
                so.ApplyModifiedProperties();
                so.Update();
            }
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index >= list.arraySize)
            {
                return;
            }
            
            var element = list.GetArrayElementAtIndex(index);

            var prop_expanded = element.FindPropertyRelative("foldoutExpanded");
            var prop_id = element.FindPropertyRelative("identifier");

            string foldoutHeader = string.IsNullOrEmpty(prop_id.stringValue) ? "Element " + index : prop_id.stringValue;

            Rect r_foldout = rect.CutLeft(10).CutTop(3).WithHeight(height);
            Rect r_remove = r_foldout.AppendRight(-77).WithWidth(80);
            Rect r_expand = r_remove.AppendLeft(5);

            EditorGUI.LabelField(r_foldout, foldoutHeader, EditorStyles.boldLabel);
            prop_expanded.boolValue = EditorGUI.Foldout(r_foldout, prop_expanded.boolValue, GUIContent.none);
            if (GUI.Button(r_remove, "-"))
            {
                list.DeleteArrayElementAtIndex(index);
                listCount--;
                return;
            }

            if (prop_expanded.boolValue)
            {
                var prop_prefab = element.FindPropertyRelative("poolObject");
                var prop_cap = element.FindPropertyRelative("capacity");
                var prop_expandable = element.FindPropertyRelative("isExpandable");                
                var prop_persist = element.FindPropertyRelative("persistBetweenScenes");
                var prop_create = element.FindPropertyRelative("createTime");
                var prop_name = element.FindPropertyRelative("sceneName");

                float width = CustomDrawerHelper.CalculateLabelWidth("Persist Between Scenes");

                Rect label_id = r_foldout.CutLeft(10).AppendBottom(2).WithWidth(width);
                Rect label_prefab = label_id.AppendBottom(2);
                Rect label_cap = label_prefab.AppendBottom(2);
                Rect label_expendable = label_cap.AppendBottom(2);                
                Rect label_persist = label_expendable.AppendBottom(2);
                Rect label_create = label_persist.AppendBottom(2);
                Rect label_name = label_create.AppendBottom(2);

                EditorGUI.LabelField(label_id, "Identifier");
                EditorGUI.LabelField(label_prefab, "Pool Object");
                EditorGUI.LabelField(label_cap, "Pool Capacity");
                EditorGUI.LabelField(label_expendable, "Expandable");                
                EditorGUI.LabelField(label_persist, "Persist Between Scenes");
                EditorGUI.LabelField(label_create, "Pool Creation Time");

                Rect r_id = label_id.AppendRight(5).WithWidth(rect.width - width - 22);
                Rect r_prefab = r_id.AppendBottom(2);
                Rect r_cap = r_prefab.AppendBottom(2);
                Rect r_expandable = r_cap.AppendBottom(2);                 
                Rect r_persist = r_expandable.AppendBottom(2);
                Rect r_create = r_persist.AppendBottom(2);
                Rect r_name = r_create.AppendBottom(2);

                prop_id.stringValue = EditorGUI.TextField(r_id, prop_id.stringValue);
                prop_prefab.objectReferenceValue = EditorGUI.ObjectField(r_prefab, prop_prefab.objectReferenceValue, typeof(GameObject), true);
                prop_cap.intValue = EditorGUI.IntField(r_cap, prop_cap.intValue);
                prop_expandable.boolValue = EditorGUI.Toggle(r_expandable, prop_expandable.boolValue);                
                prop_persist.boolValue = EditorGUI.Toggle(r_persist, prop_persist.boolValue);

                var createTime = (PoolObjectData.CreateTime)prop_create.enumValueIndex;
                createTime = (PoolObjectData.CreateTime)EditorGUI.EnumPopup(r_create, (PoolObjectData.CreateTime)prop_create.enumValueIndex);
                prop_create.enumValueIndex = (int)createTime;

                bool createInScene = createTime == PoolObjectData.CreateTime.SceneLoaded;
                if (createInScene)
                {
                    EditorGUI.LabelField(label_name, "Scene Name");
                    prop_name.stringValue = EditorGUI.TextField(r_name, prop_name.stringValue);
                }

                Rect v_line = new Rect(r_foldout.x - 7, label_id.y, 1, label_id.height * 6 + 1 + (createInScene ? label_id.height + 2 : 0));
                Rect h_line = v_line.WithWidth(11).WithHeight(1);

                Color32 lineColour = new Color32(99, 99, 99, 255);
                EditorGUI.DrawRect(v_line, lineColour);
                EditorGUI.DrawRect(h_line.WithY((createInScene ? label_name.y : label_create.y) + height / 2), lineColour); 
            }
        }

        private float OnGetElementHeight(int index)
        {
            var element = list.GetArrayElementAtIndex(index);
            float h = height + 4;

            if (element.FindPropertyRelative("foldoutExpanded").boolValue)
            {
                h += height * 6 + 12;
                if ((PoolObjectData.CreateTime)element.FindPropertyRelative("createTime").enumValueIndex == PoolObjectData.CreateTime.SceneLoaded)
                {
                    h += height + 2;
                }
            }
            return h;
        }

        private void OnAdd(ReorderableList list)
        {
            listCount++;
        }
    }
}