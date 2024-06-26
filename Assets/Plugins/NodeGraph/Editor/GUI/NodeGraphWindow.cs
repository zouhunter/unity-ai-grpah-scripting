/***********************************************************************************//*
*  作者: 邹杭特                                                                       *
*  时间: 2021-04-24                                                                   *
*  版本: master_5122a2                                                                *
*  功能:                                                                              *
*   - 主窗口                                                                          *
*//************************************************************************************/

using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UFrame.NodeGraph.DataModel;

namespace UFrame.NodeGraph
{
    public class NodeGraphWindow : EditorWindow
    {
        [Serializable]
        public class SavedSelection
        {
            [SerializeField]
            public List<NodeGUI> nodes;
            [SerializeField]
            public List<ConnectionGUI> connections;
            [SerializeField]
            private float m_pasteOffset = kPasteOffset;

            static readonly float kPasteOffset = 20.0f;

            public SavedSelection(SavedSelection s)
            {
                nodes = new List<NodeGUI>(s.nodes);
                connections = new List<ConnectionGUI>(s.connections);
            }

            public SavedSelection()
            {
                nodes = new List<NodeGUI>();
                connections = new List<ConnectionGUI>();
            }

            public SavedSelection(IEnumerable<NodeGUI> n, IEnumerable<ConnectionGUI> c)
            {
                nodes = new List<NodeGUI>(n);
                connections = new List<ConnectionGUI>(c);
            }

            public bool IsSelected
            {
                get
                {
                    return (nodes.Count + connections.Count) > 0;
                }
            }

            public float PasteOffset
            {
                get
                {
                    return m_pasteOffset;
                }
            }

            public void IncrementPasteOffset()
            {
                m_pasteOffset += kPasteOffset;
            }

            public void Add(NodeGUI n)
            {
                nodes.Add(n);
            }

            public void Add(ConnectionGUI c)
            {
                connections.Add(c);
            }

            public void Remove(NodeGUI n)
            {
                nodes.Remove(n);
            }

            public void Remove(ConnectionGUI c)
            {
                connections.Remove(c);
            }

            public void Toggle(NodeGUI n)
            {
                if (nodes.Contains(n))
                {
                    nodes.Remove(n);
                }
                else
                {
                    nodes.Add(n);
                }
            }

            public void Toggle(ConnectionGUI c)
            {
                if (connections.Contains(c))
                {
                    connections.Remove(c);
                }
                else
                {
                    connections.Add(c);
                }
            }

            public void Clear(NodeGraph.NodeGraphController controller, bool deactivate = false)
            {
                if (deactivate)
                {
                    foreach (var n in nodes)
                    {
                        n.SetActive(false);
                    }

                    foreach (var c in connections)
                    {
                        c.SetActive(false);
                    }
                }

                nodes.Clear();
                connections.Clear();
            }
        }

        // hold selection start data.
        public class SelectPoint
        {
            public readonly float x;
            public readonly float y;

            public SelectPoint(Vector2 position)
            {
                this.x = position.x;
                this.y = position.y;
            }
        }

        public enum ModifyMode
        {
            NONE,
            CONNECTING,
            SELECTING,
            DRAGGING
        }


        [SerializeField]
        private List<NodeGUI> nodes = new List<NodeGUI>();
        [SerializeField]
        private List<ConnectionGUI> connections = new List<ConnectionGUI>();

        [SerializeField]
        private SavedSelection activeSelection = null;
        [SerializeField]
        private SavedSelection copiedSelection = null;

        private bool _showErrors;
        private bool showErrors
        {
            get
            {
                return _showErrors;
            }
            set
            {
                if (_showErrors != value)
                {
                    _showErrors = value;
                    OnGraphRegionChanged();
                }
            }
        }
        private bool showVerboseLog;
        private NodeEvent currentEventSource;
        private Texture2D _selectionTex;
        private GUIContent _reloadButtonTexture;
        private ModifyMode modifyMode;
        private Vector2 scrollPos
        { get { return scrollViewContainer.scrollPos; } }

        private Vector2 errorScrollPos = new Vector2(0, 0);
        private Rect graphRegion = new Rect();
        private SelectPoint selectStartMousePosition;
        private GraphBackground background = new GraphBackground();
        private string graphAssetPath;
        private string graphAssetName;
        private NodeGraphController controller;
        private ScrollViewContainer _scrollViewContainer;
        private ScrollViewContainer scrollViewContainer
        {
            get
            {
                if(_scrollViewContainer == null)
                {
                    _scrollViewContainer = CreateScrollViewContainer();
                }
                return _scrollViewContainer;
            }
        }

        private Vector2 m_LastMousePosition;
        private Vector2 m_DragNodeDistance;
        private Vector2 old_graphRect;
        private readonly Dictionary<NodeGUI, Vector2> m_InitialDragNodePositions = new Dictionary<NodeGUI, Vector2>();

        private static bool autoOpen = true;
        private static readonly string kPREFKEY_LASTEDITEDGRAPH = "AssetBundles.GraphTool.LastEditedGraph";
        static readonly int kDragNodesControlID = "NodeGraph.DataModelTool.HandleDragNodes".GetHashCode();
        private const string CreateAssetMenu = "Assets/Create/NodeGraphObj";
        public const string GUI_TEXT_MENU_OPEN = "Window/UFrame/Graph-Editor/NodeGraph";

        private string[] controllerTypes;
        private int selected;
        private Rect contentRect;
        private GUIContent ReloadButtonTexture
        {
            get
            {
                if (_reloadButtonTexture == null)
                {
                    _reloadButtonTexture = EditorGUIUtility.IconContent("RotateTool");
                }
                return _reloadButtonTexture;
            }
        }

        private bool IsAnyIssueFound
        {
            get
            {
                if (controller == null)
                {
                    return true;
                }
                return controller.IsAnyIssueFound;
            }
        }

        /*
         * An alternative way to get Window, becuase
         * GetWindow<NodeGraph.DataModelEditorWindow>() forces window to be active and present
         */
        public static NodeGraphWindow Window
        {
            get
            {
                NodeGraphWindow[] windows = Resources.FindObjectsOfTypeAll<NodeGraphWindow>();
                if (windows.Length > 0)
                {
                    return windows[0];
                }

                return null;
            }
        }

        [MenuItem(GUI_TEXT_MENU_OPEN, false, 1)]
        public static void Open()
        {
            autoOpen = true;
            var window = GetWindow<NodeGraphWindow>("节点编辑器",true);
            if(window)
            {
                window.Show(true);
                //window.position = new Rect(0, 0, 100, 100);
            }
        }

        [UnityEditor.Callbacks.OnOpenAsset()]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var graph = EditorUtility.InstanceIDToObject(instanceID) as NodeGraphObj;
            if (graph != null)
            {
                var window = GetWindow<NodeGraphWindow>();
                window.OpenGraph(graph);
                return true;
            }
            return false;
        }

        public void OnEnable()
        {
            Init();
        }

        public void OnDisable()
        {
            LogUtility.Logger.Log("OnDisable");
            if (controller != null)
            {
                if (controller.TargetGraph != null)
                {
                    controller.SaveGraph(nodes.Select(x => x.Data).ToList(), connections.Select(x => x.Data).ToList(), true);
                }
                EditorUtility.SetDirty(controller.TargetGraph);
            }
        }

        public void OnGUI()
        {
            if (controller == null)
            {
                DrawNoGraphGUI();
            }
            else
            {
                DrawGUIToolBar();

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (showErrors)
                    {
                        GUILayoutUtility.GetRect(graphRegion.width, graphRegion.height);
                        background.Draw(graphRegion, scrollPos, scrollViewContainer.ZoomSize);
                        DrawGUINodeErrors(200);
                    }
                    else
                    {
                        GUILayoutUtility.GetRect(graphRegion.width, graphRegion.height);
                        background.Draw(graphRegion, scrollPos, scrollViewContainer.ZoomSize);
                    }
                }

                if (!string.IsNullOrEmpty(graphAssetPath))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(graphAssetPath, "MiniLabel");
                    }
                }

                if (controller.IsAnyIssueFound)
                {
                    Rect msgRgn = new Rect((graphRegion.width - 250f) / 2f, graphRegion.y + 8f, 250f, 36f);
                    EditorGUI.HelpBox(msgRgn, "All errors needs to be fixed before building.", MessageType.Error);
                }

                if (old_graphRect != position.size)
                {
                    old_graphRect = position.size;
                    OnGraphRegionChanged();
                }
            }
        }

        public void OnFocus()
        {
            // update handlers. these static handlers are erase when window is full-screened and badk to normal window.
            modifyMode = ModifyMode.NONE;
            NodeGUIUtility.NodeEventHandler = HandleNodeEvent;
            ConnectionGUIUtility.ConnectionEventHandler = HandleConnectionEvent;
            HandleSelectionChange();
        }

        public void OnLostFocus()
        {
            modifyMode = ModifyMode.NONE;
        }

        public void OnProjectChange()
        {
            HandleSelectionChange();
            Repaint();
            scrollViewContainer.Refesh();
        }

        public void OnSelectionChange()
        {
            HandleSelectionChange();
            Repaint();
            scrollViewContainer.Refesh();
        }

        public void HandleSelectionChange()
        {
            NodeGraphObj selectedGraph = null;

            if (Selection.activeObject is NodeGraphObj && EditorUtility.IsPersistent(Selection.activeObject))
            {
                selectedGraph = Selection.activeObject as NodeGraphObj;
            }

            if (selectedGraph != null && (controller == null || selectedGraph != controller.TargetGraph))
            {
                OpenGraph(selectedGraph);
            }
        }

        public void SelectNode(string nodeId)
        {
            var selectObject = nodes.Find(node => node.Id == nodeId);
            foreach (var node in nodes)
            {
                node.SetActive(node == selectObject);
            }
        }

        private void Init()
        {
            LogUtility.Logger.filterLogType = LogType.Warning;

            this.titleContent = new GUIContent("NodeGraph");
            this.minSize = new Vector2(600f, 300f);
            this.wantsMouseMove = true;
            //target = EditorUserBuildSettings.activeBuildTarget;

            Undo.undoRedoPerformed += () =>
            {
                Setup();
                Repaint();
            };

            modifyMode = ModifyMode.NONE;
            NodeGUIUtility.NodeEventHandler = HandleNodeEvent;
            ConnectionGUIUtility.ConnectionEventHandler = HandleConnectionEvent;

            if (autoOpen)
            {
                string lastGraphAssetPath = EditorPrefs.GetString(kPREFKEY_LASTEDITEDGRAPH);
                if (!string.IsNullOrEmpty(lastGraphAssetPath))
                {
                    var graph = AssetDatabase.LoadAssetAtPath<NodeGraphObj>(lastGraphAssetPath);
                    if (graph != null)
                    {
                        OpenGraph(graph);
                    }
                }
            }


            controllerTypes = UserDefineUtility.CustomControllerTypes.ConvertAll<string>(x => x.FullName).ToArray();
        }

        protected ScrollViewContainer CreateScrollViewContainer()
        {
            var scrollViewContainer = new ScrollViewContainer();
            scrollViewContainer.onGUI = DrawNodeGraphContent;
            graphRegion = new Rect(0, EditorGUIUtility.singleLineHeight, position.width, position.height - 2 * EditorGUIUtility.singleLineHeight);
            scrollViewContainer.Start(this.rootVisualElement, graphRegion);
            return scrollViewContainer;
        }

        private void OnGraphRegionChanged()
        {
            if(showErrors)
            {
                graphRegion = new Rect(0, EditorGUIUtility.singleLineHeight, position.width - 200, position.height - 2 * EditorGUIUtility.singleLineHeight);
            }
            else
            {
                graphRegion = new Rect(0, EditorGUIUtility.singleLineHeight, position.width, position.height - 2 * EditorGUIUtility.singleLineHeight);
            }

            if(scrollViewContainer != null)
            {
                scrollViewContainer.UpdateScale(graphRegion);
                scrollViewContainer.Refesh();
            }
        }

        private void ShowErrorOnNodes()
        {
            foreach (var node in nodes)
            {
                node.ResetErrorStatus();
                var errorsForeachNode = controller.Issues.Where(e => e.Id == node.Id).Select(e => e.reason).ToList();
                if (errorsForeachNode.Any())
                {
                    node.AppendErrorSources(errorsForeachNode);
                }
            }
        }

        private void SetGraphAssetPath(string newPath)
        {
            if (newPath == null)
            {
                graphAssetPath = null;
                graphAssetName = null;
            }
            else
            {
                graphAssetPath = newPath;
                graphAssetName = Path.GetFileNameWithoutExtension(graphAssetPath);
                if (graphAssetName.Length > NGEditorSettings.GUI.TOOLBAR_GRAPHNAMEMENU_CHAR_LENGTH)
                {
                    graphAssetName = graphAssetName.Substring(0, NGEditorSettings.GUI.TOOLBAR_GRAPHNAMEMENU_CHAR_LENGTH) + "...";
                }

                EditorPrefs.SetString(kPREFKEY_LASTEDITEDGRAPH, graphAssetPath);
            }
        }

        public void OpenGraph(string path)
        {
            NodeGraphObj graph = AssetDatabase.LoadAssetAtPath<NodeGraphObj>(path);
            if (graph == null)
            {
                throw new NodeGraph.DataModelException("Could not open graph:" + path);
            }
            OpenGraph(graph);
        }

        public void OpenGraph(NodeGraphObj graph)
        {
            ClearGraph();

            SetGraphAssetPath(AssetDatabase.GetAssetPath(graph));

            modifyMode = ModifyMode.NONE;

            errorScrollPos = new Vector2(0, 0);

            selectStartMousePosition = null;
            activeSelection = null;
            currentEventSource = null;

            controller = UserDefineUtility.CreateController(graph.ControllerType); //new NodeGraph.NodeGraphController(graph);
            if(controller == null)
            {
                Debug.LogError("graph error type:" + graph.ControllerType);
                return;
            }
            else
            {
                controller.TargetGraph = graph;
            }
            ConstructGraphGUI();
            Setup();

            //Selection.activeObject = graph;
            NodeConnectionUtility.Reset();
        }

        private void ClearGraph()
        {
            modifyMode = ModifyMode.NONE;
            SetGraphAssetPath(null);
            controller = null;
            nodes.Clear();
            connections.Clear();

            selectStartMousePosition = null;
            activeSelection = null;
            currentEventSource = null;
        }

        private void CreateNewGraphFromDialog(string controllerType)
        {
            controller = UserDefineUtility.CreateController(controllerType);
            var graph = controller.CreateNodeGraphObject();
            DelyAccept(graph, OpenGraph);
        }

        private void DelyAccept(NodeGraphObj graph, UnityEngine.Events.UnityAction<NodeGraphObj> onGet)
        {
            if (graph == null || onGet == null) return;

            EditorApplication.update = () =>
            {
                var path = AssetDatabase.GetAssetPath(graph);
                if (!string.IsNullOrEmpty(path))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<NodeGraphObj>(path);
                    onGet.Invoke(prefab);
                    EditorApplication.update = null;
                }
            };
        }

        /**
         * Get WindowId does not collide with other nodeGUIs
         */
        private static int GetSafeWindowId(List<NodeGUI> nodeGUIs)
        {
            int id = -1;

            foreach (var nodeGui in nodeGUIs)
            {
                if (nodeGui.WindowId > id)
                {
                    id = nodeGui.WindowId;
                }
            }
            return id + 1;
        }

        /**
         * Creates Graph structure with NodeGUI and ConnectionGUI from SaveData
         */
        private void ConstructGraphGUI()
        {
            var activeGraph = controller.TargetGraph;

            if (activeGraph == null) return;

            var currentNodes = new List<NodeGUI>();
            var currentConnections = new List<ConnectionGUI>();

            foreach (var node in activeGraph.Nodes)
            {
                var newNodeGUI = new NodeGUI(controller, node);
                newNodeGUI.WindowId = GetSafeWindowId(currentNodes);
                currentNodes.Add(newNodeGUI);
            }

            // load connections
            foreach (var c in activeGraph.Connections)
            {
                var startNode = currentNodes.Find(node => node.Id == c.FromNodeId);
                if (startNode == null)
                {
                    continue;
                }

                var endNode = currentNodes.Find(node => node.Id == c.ToNodeId);
                if (endNode == null)
                {
                    continue;
                }
                var startPoint = startNode.Data.FindConnectionPoint(c.FromNodeConnectionPointId);
                var endPoint = endNode.Data.FindConnectionPoint(c.ToNodeConnectionPointId);

                currentConnections.Add(ConnectionGUI.LoadConnection(c, startPoint, endPoint));
            }

            nodes = currentNodes;
            connections = currentConnections;
        }


        private void Setup(bool forceVisitAll = false)
        {
            EditorUtility.ClearProgressBar();

            if (controller == null)
            {
                return;
            }

            foreach (var node in nodes)
            {
                node.Controller = controller;
                node.Data.Validate();
                node.Data.Object.Initialize(node.Data);
                node.HideProgress();
            }

            foreach (var connection in connections)
            {
                connection.Data.Validate();
            }

            controller.SaveGraph(nodes.Where(x => x != null).Select(x => x.Data).ToList(), connections.Where(x => x != null).Select(x => x.Data).ToList()); ;
            // update static all node names.
            NodeGUIUtility.allNodeNames = new List<string>(nodes.Select(node => node.Name).ToList());

            controller.Perform();

            ShowErrorOnNodes();

            EditorUtility.ClearProgressBar();
        }

        private void Validate(NodeGUI node)
        {

            EditorUtility.ClearProgressBar();
            if (controller == null)
            {
                return;
            }

            try
            {
                node.ResetErrorStatus();
                node.HideProgress();

                controller.SaveGraph(nodes.Select(x => x.Data).ToList(), connections.Select(x => x.Data).ToList()); ;
                controller.CheckValidate(node);

                //RefreshInspector(controller.StreamManager);
                ShowErrorOnNodes();
            }
            catch (Exception e)
            {
                LogUtility.Logger.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        private void DrawGUIToolBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                #region SelectGraph
                if (GUILayout.Button(new GUIContent(graphAssetName, "Select graph"), EditorStyles.toolbarPopup, GUILayout.Width(NGEditorSettings.GUI.TOOLBAR_GRAPHNAMEMENU_WIDTH), GUILayout.Height(NGEditorSettings.GUI.TOOLBAR_HEIGHT)))
                {
                    GenericMenu menu = new GenericMenu();

                    var guids = AssetDatabase.FindAssets(NGSettings.GRAPH_SEARCH_CONDITION);
                    var nameList = new List<string>();

                    foreach (var guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        string name = Path.GetFileNameWithoutExtension(path);

                        // GenericMenu can't have multiple menu item with the same name
                        // Avoid name overlap
                        string menuName = name;
                        int i = 1;
                        while (nameList.Contains(menuName))
                        {
                            menuName = string.Format("{0} ({1})", name, i++);
                        }

                        menu.AddItem(new GUIContent(menuName), false, () =>
                        {
                            if (path != graphAssetPath)
                            {
                                var graph = AssetDatabase.LoadAssetAtPath<NodeGraphObj>(path);
                                OpenGraph(graph);
                            }
                            UnityEditor.EditorGUIUtility.PingObject(controller.TargetGraph);
                        });
                        nameList.Add(menuName);
                    }

                    menu.AddSeparator("");

                    if (controllerTypes != null)
                    {
                        foreach (var ct in controllerTypes)
                        {
                            string contollerType = ct;
                            menu.AddItem(new GUIContent(string.Format("Create New/({0})Graph", contollerType)), false, () =>
                            {
                                CreateNewGraphFromDialog(contollerType);
                            });
                            menu.AddSeparator("");
                        }
                    }


                    //menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Import/Import JSON Graph to current graph..."), false, () =>
                    {
                        var graph = controller.TargetGraph;
                        if (JSONGraphUtility.ImportJSONToGraphFromDialog(ref graph))
                        {
                            OpenGraph(graph);
                        }
                    });
                    menu.AddSeparator("Import/");
                    menu.AddItem(new GUIContent("Import/Import JSON Graph and create new..."), false, () =>
                    {
                        NodeGraphObj graph = null;
                        if (JSONGraphUtility.ImportJSONToGraphFromDialog(ref graph))
                        {
                            OpenGraph(graph);
                        }
                        else
                        {
                            Debug.Log("import faild!");
                        }
                    });
                    menu.AddItem(new GUIContent("Import/Import JSON Graphs in folder..."), false, () =>
                    {
                        JSONGraphUtility.ImportAllJSONInDirectoryToGraphFromDialog();
                    });
                    menu.AddItem(new GUIContent("Export/Export current graph to JSON..."), false, () =>
                    {
                        JSONGraphUtility.ExportGraphToJSONFromDialog(controller.TargetGraph);
                    });
                    menu.AddItem(new GUIContent("Export/Export all graphs to JSON..."), false, () =>
                    {
                        JSONGraphUtility.ExportAllGraphsToJSONFromDialog();
                    });

                    //if (ConfigGraph.IsImportableDataAvailableAtDisk())
                    //{
                    //    menu.AddSeparator("");
                    //    menu.AddItem(new GUIContent("Import previous version..."), false, () =>
                    //    {
                    //        CreateNewGraphFromImport();
                    //    });
                    //}

                    menu.DropDown(new Rect(4f, 8f, 0f, 0f));
                }
                #endregion

                GUILayout.Space(4);

                #region Refresh
                if (GUILayout.Button(new GUIContent("Refresh", ReloadButtonTexture.image, "Refresh and reload"), EditorStyles.toolbarButton, GUILayout.Width(80), GUILayout.Height(NGEditorSettings.GUI.TOOLBAR_HEIGHT)))
                {
                    OnGraphRegionChanged();
                    Setup();
                }
                #endregion


                showErrors = GUILayout.Toggle(showErrors, "Show Error", EditorStyles.toolbarButton, GUILayout.Height(NGEditorSettings.GUI.TOOLBAR_HEIGHT));

                GUILayout.Space(4);

                showVerboseLog = GUILayout.Toggle(showVerboseLog, "Show Verbose Log", EditorStyles.toolbarButton, GUILayout.Height(NGEditorSettings.GUI.TOOLBAR_HEIGHT));
                LogUtility.Logger.filterLogType = (showVerboseLog) ? LogType.Log : LogType.Warning;

                GUILayout.Space(4);

                GUILayout.Label(string.Format("canvas:[{0}]", scrollViewContainer.ZoomSize.ToString("0.0")));
                GUILayout.Label(scrollViewContainer.minZoomSize.ToString());
                scrollViewContainer.ZoomSize = GUILayout.HorizontalSlider(scrollViewContainer.ZoomSize, scrollViewContainer.minZoomSize, scrollViewContainer.maxZoomSize, GUILayout.Width(graphRegion.width * 0.1f));
                GUILayout.Label(scrollViewContainer.maxZoomSize.ToString());

                GUILayout.FlexibleSpace();

                GUIStyle tbLabel = new GUIStyle(EditorStyles.toolbar);

                tbLabel.alignment = TextAnchor.MiddleCenter;

                GUIStyle tbLabelTarget = new GUIStyle(tbLabel);
                tbLabelTarget.fontStyle = FontStyle.Bold;

                EditorGUI.BeginDisabledGroup(controller.IsAnyIssueFound);
                {
                    if (GUILayout.Button("Build", EditorStyles.toolbarButton, GUILayout.Height(NGEditorSettings.GUI.TOOLBAR_HEIGHT)))
                    {
                        EditorApplication.delayCall += controller.Build;
                        //Debug.Log("Build Clicked");
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        static readonly string kGUIDELINETEXT = "To configure node workflow, create an Node Graph.";
        static readonly string kCREATEBUTTON = "Create";
        //static readonly string kIMPORTBUTTON = "Import previous version";

        private void DrawNoGraphGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.FlexibleSpace();
                   
                    var guideline = new GUIContent(kGUIDELINETEXT);
                    var size = GUI.skin.label.CalcSize(guideline);
                    if (controllerTypes != null && controllerTypes.Length > 0)
                    {
                        GUILayout.Label(kGUIDELINETEXT);
                        selected = EditorGUILayout.Popup(selected, controllerTypes);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            //bool showImport = ConfigGraph.IsImportableDataAvailableAtDisk();
                            float spaceWidth = (size.x - 100f) / 2f;

                            GUILayout.Space(spaceWidth);
                            if (GUILayout.Button(kCREATEBUTTON, GUILayout.Width(100f), GUILayout.ExpandWidth(false)))
                            {
                                CreateNewGraphFromDialog(controllerTypes[selected]);
                            }
                        }
                    }
                    else
                    {
                        var rect = GUILayoutUtility.GetRect(60, 60);
                        EditorGUI.HelpBox(rect, "请自行定义控制器", MessageType.Error);
                    }

                    GUILayout.FlexibleSpace();
                }
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawGUINodeErrors(float width)
        {
            errorScrollPos = EditorGUILayout.BeginScrollView(errorScrollPos, GUI.skin.box, GUILayout.Width(width));
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    foreach (NodeException e in controller.Issues)
                    {
                        EditorGUILayout.HelpBox(e.reason, MessageType.Error);
                        if (GUILayout.Button("Go to Node"))
                        {
                            SelectNode(e.Id);
                        }
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawNodeGraphContent()
        {
            if (controller == null)
                return;

            #region DrawInteranl
            if (connections == null)
                Window.Close();

            contentRect = new Rect(Vector2.zero, graphRegion.size / scrollViewContainer.minZoomSize);

            // draw node window x N.
            {
                BeginWindows();

                nodes.ForEach(node => {
                    node.ClampRect(contentRect);
                    node.SetOffset(contentRect.center);
                    node.DrawNode();
                });

                HandleDragNodes();

                EndWindows();
            }

            ClearBadConnection();

            // draw connections.
            foreach (var con in connections)
            {
                con.SetOffset(contentRect.center);
                con.DrawConnection(nodes);
            }

            // draw connection input point marks.
            foreach (var node in nodes)
            {
                node.DrawConnectionInputPointMark(currentEventSource, modifyMode == ModifyMode.CONNECTING);
            }

            // draw connection output point marks.
            foreach (var node in nodes)
            {
                node.DrawConnectionOutputPointMark(currentEventSource, modifyMode == ModifyMode.CONNECTING, Event.current);
            }

            var mousePosition = Event.current.mousePosition;
            // draw connecting line if modifing connection.
            switch (modifyMode)
            {
                case ModifyMode.CONNECTING:
                    {
                        // from start node to mouse.
                        DrawStraightLineFromCurrentEventSourcePointTo(mousePosition, currentEventSource);
                        break;
                    }
                case ModifyMode.SELECTING:
                    {
                        float lx = Mathf.Max(selectStartMousePosition.x, Event.current.mousePosition.x);
                        float ly = Mathf.Max(selectStartMousePosition.y, mousePosition.y);
                        float sx = Mathf.Min(selectStartMousePosition.x, Event.current.mousePosition.x);
                        float sy = Mathf.Min(selectStartMousePosition.y, mousePosition.y);
                        Rect sel = new Rect(sx, sy, lx - sx, ly - sy);
                        GUI.Label(sel, string.Empty, "SelectionRect");
                        break;
                    }
            }

            // handle Graph GUI events
            HandleGraphGUIEvents(contentRect);
            HandleDragAndDropGUI(contentRect);
            HandleGUIEvent();
            #endregion
        }

        private void ClearBadConnection()
        {
            var connectionsCopy = connections.ToArray();
            foreach (var connection in connectionsCopy)
            {
                if (!connection.IsValid(nodes))
                {
                    connections.Remove(connection);
                }
            }
        }

        private void HandleGraphGUIEvents(Rect rect)
        {
            //mouse drag event handling.
            switch (Event.current.type)
            {
                // draw line while dragging.
                case EventType.MouseDrag:
                    {
                        switch (modifyMode)
                        {
                            case ModifyMode.NONE:
                                {
                                    switch (Event.current.button)
                                    {
                                        case 0:
                                            {
                                                // left click
                                                if (rect.Contains(Event.current.mousePosition))
                                                {
                                                    selectStartMousePosition = new SelectPoint(Event.current.mousePosition);
                                                    modifyMode = ModifyMode.SELECTING;
                                                }
                                                break;
                                            }
                                    }
                                    break;
                                }
                            case ModifyMode.SELECTING:
                                {
                                    // do nothing.
                                    break;
                                }
                        }

                        HandleUtility.Repaint();
                        Event.current.Use();
                        break;
                    }
            }

            // mouse up event handling.
            // use rawType for detect for detectiong mouse-up which raises outside of window.
            switch (Event.current.rawType)
            {
                case EventType.MouseUp:
                    {
                        switch (modifyMode)
                        {
                            /*
                                select contained nodes & connections.
                            */
                            case ModifyMode.SELECTING:
                                {

                                    if (selectStartMousePosition == null)
                                    {
                                        break;
                                    }

                                    var x = 0f;
                                    var y = 0f;
                                    var width = 0f;
                                    var height = 0f;

                                    if (Event.current.mousePosition.x < selectStartMousePosition.x)
                                    {
                                        x = Event.current.mousePosition.x;
                                        width = selectStartMousePosition.x - Event.current.mousePosition.x;
                                    }
                                    if (selectStartMousePosition.x < Event.current.mousePosition.x)
                                    {
                                        x = selectStartMousePosition.x;
                                        width = Event.current.mousePosition.x - selectStartMousePosition.x;
                                    }

                                    if (Event.current.mousePosition.y < selectStartMousePosition.y)
                                    {
                                        y = Event.current.mousePosition.y;
                                        height = selectStartMousePosition.y - Event.current.mousePosition.y;
                                    }
                                    if (selectStartMousePosition.y < Event.current.mousePosition.y)
                                    {
                                        y = selectStartMousePosition.y;
                                        height = Event.current.mousePosition.y - selectStartMousePosition.y;
                                    }

                                    Undo.RecordObject(this, "Select Objects");

                                    if (activeSelection == null)
                                    {
                                        activeSelection = new SavedSelection();
                                    }

                                    // if shift key is not pressed, clear current selection
                                    if (!Event.current.shift)
                                    {
                                        activeSelection.Clear(controller);
                                    }

                                    var selectedRect = new Rect(x, y, width, height);
                                    foreach (var node in nodes)
                                    {
                                        if (node.GetRect(true).Overlaps(selectedRect))
                                        {
                                            activeSelection.Add(node);
                                        }
                                    }

                                    foreach (var connection in connections)
                                    {
                                        // get contained connection badge.
                                        if (connection.GetRect().Overlaps(selectedRect))
                                        {
                                            activeSelection.Add(connection);
                                        }
                                    }

                                    UpdateActiveObjects(activeSelection);

                                    selectStartMousePosition = null;
                                    modifyMode = ModifyMode.NONE;

                                    HandleUtility.Repaint();
                                    Event.current.Use();
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        private void HandleDragAndDropGUI(Rect dragdropArea)
        {
            Event evt = Event.current;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                    if (!dragdropArea.Contains(evt.mousePosition))
                        return;
                    controller.OnDragUpdated();
                    break;
                case EventType.DragPerform:
                    if (!dragdropArea.Contains(evt.mousePosition))
                        return;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        var result = controller.OnDragAccept(DragAndDrop.objectReferences);
                        if (result != null)
                        {
                            foreach (var item in result)
                            {
                                AddNodeFromGUI(item.Value, item.Key, evt.mousePosition.x, evt.mousePosition.y);
                            }
                            Setup();
                            Repaint();
                        }

                        Event.current.Use();
                    }
                    break;
            }
        }

        private void HandleGUIEvent()
        {
            var isValidSelection = activeSelection != null && activeSelection.IsSelected;
            var isValidCopy = copiedSelection != null && copiedSelection.IsSelected;

            /*
                Event Handling:
                - Supporting dragging script into window to create node.
                - Context Menu	
                - NodeGUI connection.
                - Command(Delete, Copy, etc...)
            */
            switch (Event.current.type)
            {
                // show context menu
                case EventType.ContextClick:
                    {
                        ShowNodeCreateContextMenu(Event.current.mousePosition);
                        break;
                    }

                /*
                    Handling mouseUp at empty space. 
                */
                case EventType.MouseUp:
                    {
                        modifyMode = ModifyMode.NONE;
                        HandleUtility.Repaint();

                        if (activeSelection != null && activeSelection.IsSelected)
                        {
                            Undo.RecordObject(this, "Unselect");

                            activeSelection.Clear(controller);
                            UpdateActiveObjects(activeSelection);
                        }
                        // clear inspector
                        if (Selection.activeObject is NodeGUIInspectorHelper || Selection.activeObject is ConnectionGUIInspectorHelper)
                        {
                            Selection.activeObject = null;
                        }
                        break;
                    }

                case EventType.ValidateCommand:
                    {
                        switch (Event.current.commandName)
                        {
                            case "SoftDelete":
                            case "Delete":
                            case "Copy":
                            case "Cut":
                                {
                                    if (isValidSelection)
                                    {
                                        Event.current.Use();
                                    }
                                    break;
                                }

                            case "Paste":
                                {
                                    if (isValidCopy)
                                    {
                                        Event.current.Use();
                                    }
                                    break;
                                }

                            case "SelectAll":
                                {
                                    Event.current.Use();
                                    break;
                                }
                        }
                        break;
                    }

                case EventType.ExecuteCommand:
                    {
                        switch (Event.current.commandName)
                        {
                            // Delete active node or connection.
                            case "SoftDelete":
                            case "Delete":
                                {
                                    if (!isValidSelection)
                                    {
                                        break;
                                    }
                                    DeleteSelected();

                                    Event.current.Use();
                                    break;
                                }

                            case "Copy":
                                {
                                    if (!isValidSelection)
                                    {
                                        break;
                                    }

                                    Undo.RecordObject(this, "Copy Selected");

                                    copiedSelection = new SavedSelection(activeSelection);

                                    Event.current.Use();
                                    break;
                                }

                            case "Cut":
                                {
                                    if (!isValidSelection)
                                    {
                                        break;
                                    }

                                    Undo.RecordObject(this, "Cut Selected");

                                    copiedSelection = new SavedSelection(activeSelection);

                                    foreach (var n in activeSelection.nodes)
                                    {
                                        DeleteNode(n.Id);
                                    }

                                    foreach (var c in activeSelection.connections)
                                    {
                                        DeleteConnection(c.Id);
                                    }
                                    activeSelection.Clear(controller);
                                    UpdateActiveObjects(activeSelection);

                                    Setup();
                                    //InitializeGraph();

                                    Event.current.Use();
                                    break;
                                }

                            case "Paste":
                                {
                                    if (!isValidCopy)
                                    {
                                        break;
                                    }

                                    Undo.RecordObject(this, "Paste");

                                    Dictionary<NodeGUI, NodeGUI> nodeLookup = new Dictionary<NodeGUI, NodeGUI>();

                                    foreach (var copiedNode in copiedSelection.nodes)
                                    {
                                        var newNode = DuplicateNode(copiedNode, copiedSelection.PasteOffset);
                                        nodeLookup.Add(copiedNode, newNode);
                                    }

                                    foreach (var copiedConnection in copiedSelection.connections)
                                    {
                                        DuplicateConnection(copiedConnection, nodeLookup);
                                    }


                                    copiedSelection.IncrementPasteOffset();

                                    Setup();

                                    Event.current.Use();
                                    break;
                                }
                            case "SelectAll":
                                {
                                    Undo.RecordObject(this, "Select All Objects");

                                    if (activeSelection == null)
                                    {
                                        activeSelection = new SavedSelection();
                                    }

                                    activeSelection.Clear(controller);
                                    nodes.ForEach(n => activeSelection.Add(n));
                                    connections.ForEach(c => activeSelection.Add(c));

                                    UpdateActiveObjects(activeSelection);

                                    Event.current.Use();
                                    break;
                                }

                            default:
                                {
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        private void DeleteSelected()
        {
            Undo.RecordObject(this, "Delete Selected");

            foreach (var n in activeSelection.nodes)
            {
                DeleteNode(n.Id);
            }

            foreach (var c in activeSelection.connections)
            {
                DeleteConnection(c.Id);
            }

            activeSelection.Clear(controller);
            UpdateActiveObjects(activeSelection);

            Setup();
        }

        private void ShowNodeCreateContextMenu(Vector2 pos)
        {
            var menu = new GenericMenu();
            var customNodes = NodeConnectionUtility.CustomNodeTypes(controller.Group);
            customNodes.Sort();
            for (int i = 0; i < customNodes.Count; ++i)
            {
                // workaround: avoiding compilier closure bug
                var index = i;
                var assembleName = customNodes[index].type.Namespace;
                var path = customNodes[index].node.Name;
                if (!string.IsNullOrEmpty(assembleName))
                    path = $"{assembleName}/{path}";

                menu.AddItem(
                    new GUIContent(path),
                    false,
                    () =>
                    {
                        AddNodeFromGUI(customNodes[index].CreateInstance(), GetNodeNameFromMenu(path), pos.x, pos.y);
                        Setup();
                        Repaint();
                        scrollViewContainer.Refesh();
                    }
                );
            }

            menu.ShowAsContext();
        }

        private string GetNodeNameFromMenu(string nodeMenuName)
        {
            var slashIndex = nodeMenuName.LastIndexOf('/');
            return nodeMenuName.Substring(slashIndex + 1);
        }

        private void AddNodeFromGUI(Node n, string guiName, float x, float y)
        {
            string nodeName = guiName;
            x -= contentRect.center.x;
            y -= contentRect.center.y;
            NodeGUI newNode = new NodeGUI(controller, new NodeData(nodeName, n, x, y));
            newNode.SetOffset(contentRect.center);
            Undo.RecordObject(this, "Add " + guiName + " Node");
            AddNodeGUI(newNode);
        }

        private void DrawStraightLineFromCurrentEventSourcePointTo(Vector2 to, NodeEvent eventSource)
        {
            if (eventSource == null)
            {
                return;
            }
            var p = eventSource.point.GetGlobalPosition(eventSource.eventSourceNode.Region);
            p += contentRect.center;
            Handles.DrawLine(new Vector3(p.x, p.y, 0f), new Vector3(to.x, to.y, 0f));
        }

        /**
         * Handle Node Event
        */
        private void HandleNodeEvent(NodeEvent e)
        {

            switch (modifyMode)
            {
                /*
                 * During Mouse-drag opration to connect to other node
                 */
                case ModifyMode.CONNECTING:
                    switch (e.eventType)
                    {
                        /*
                            connection established between 2 nodes
                        */
                        case NodeEvent.EventType.EVENT_CONNECTION_ESTABLISHED:
                            {
                                // finish connecting mode.
                                modifyMode = ModifyMode.NONE;

                                if (currentEventSource == null)
                                {
                                    break;
                                }

                                var sourceNode = currentEventSource.eventSourceNode;
                                var sourceConnectionPoint = currentEventSource.point;

                                var targetNode = e.eventSourceNode;
                                var targetConnectionPoint = e.point;

                                if (sourceNode.Id == targetNode.Id)
                                {
                                    break;
                                }

                                if (!IsConnectablePointFromTo(sourceConnectionPoint, targetConnectionPoint))
                                {
                                    break;
                                }

                                var startNode = sourceNode;
                                var startConnectionPoint = sourceConnectionPoint;
                                var endNode = targetNode;
                                var endConnectionPoint = targetConnectionPoint;

                                // reverse if connected from input to output.
                                if (sourceConnectionPoint.IsInput)
                                {
                                    startNode = targetNode;
                                    startConnectionPoint = targetConnectionPoint;
                                    endNode = sourceNode;
                                    endConnectionPoint = sourceConnectionPoint;
                                }

                                var outputPoint = startConnectionPoint;
                                var inputPoint = endConnectionPoint;
                                //var label = startConnectionPoint.Label;
                                var type = controller.GetConnectType(outputPoint, inputPoint);

                                // if two nodes are not supposed to connect, dismiss
                                if (type == null)
                                {
                                    break;
                                }

                                AddConnection(type, startNode, outputPoint, endNode, inputPoint);
                                Setup();
                                break;
                            }

                        /*
                            connecting operation ended.
                        */
                        case NodeEvent.EventType.EVENT_CONNECTING_END:
                            {
                                // finish connecting mode.
                                modifyMode = ModifyMode.NONE;

                                /*
                                    connect when dropped target is connectable from start connectionPoint.
                                */
                                var node = FindNodeByPosition(e.globalMousePosition);
                                if (node == null)
                                {
                                    break;
                                }

                                // ignore if target node is source itself.
                                if (node == e.eventSourceNode)
                                {
                                    break;
                                }

                                var pointAtPosition = node.FindConnectionPointByPosition(e.globalMousePosition);
                                if (pointAtPosition == null)
                                {
                                    break;
                                }

                                var sourcePoint = currentEventSource.point;

                                // limit by connectable or not.
                                if (!IsConnectablePointFromTo(sourcePoint, pointAtPosition))
                                {
                                    break;
                                }

                                var isInput = currentEventSource.point.IsInput;
                                var startNode = (isInput) ? node : e.eventSourceNode;
                                var endNode = (isInput) ? e.eventSourceNode : node;
                                var startConnectionPoint = (isInput) ? pointAtPosition : currentEventSource.point;
                                var endConnectionPoint = (isInput) ? currentEventSource.point : pointAtPosition;
                                var outputPoint = startConnectionPoint;
                                var inputPoint = endConnectionPoint;
                                //var label = startConnectionPoint.Label;

                                // if two nodes are not supposed to connect, dismiss
                                var label = controller.GetConnectType(outputPoint, inputPoint);
                                if (label == null)
                                {
                                    break;
                                }

                                AddConnection(label,/*type, */startNode, outputPoint, endNode, inputPoint);
                                Setup();
                                break;
                            }

                        default:
                            {
                                modifyMode = ModifyMode.NONE;
                                break;
                            }
                    }
                    break;
                /*
                 * 
                 */
                case ModifyMode.NONE:
                    switch (e.eventType)
                    {
                        /*
                            start connection handling.
                        */
                        case NodeEvent.EventType.EVENT_CONNECTING_BEGIN:
                            modifyMode = ModifyMode.CONNECTING;
                            currentEventSource = e;
                            break;

                        case NodeEvent.EventType.EVENT_NODE_DELETE:
                            DeleteSelected();
                            break;

                        /*
                            node clicked.
                        */
                        case NodeEvent.EventType.EVENT_NODE_CLICKED:
                            {
                                var clickedNode = e.eventSourceNode;

                                if (activeSelection != null && activeSelection.nodes.Contains(clickedNode))
                                {
                                    break;
                                }

                                if (Event.current.shift)
                                {
                                    Undo.RecordObject(this, "Toggle " + clickedNode.Name + " Selection");
                                    if (activeSelection == null)
                                    {
                                        activeSelection = new SavedSelection();
                                    }
                                    activeSelection.Toggle(clickedNode);
                                }
                                else
                                {
                                    Undo.RecordObject(this, "Select " + clickedNode.Name);
                                    if (activeSelection == null)
                                    {
                                        activeSelection = new SavedSelection();
                                    }
                                    activeSelection.Clear(controller);
                                    activeSelection.Add(clickedNode);
                                }

                                UpdateActiveObjects(activeSelection);
                                break;
                            }
                        case NodeEvent.EventType.EVENT_NODE_UPDATED:
                            {
                                break;
                            }

                        default:
                            break;
                    }
                    break;
            }

            switch (e.eventType)
            {
                case NodeEvent.EventType.EVENT_DELETE_ALL_CONNECTIONS_TO_POINT:
                    {
                        // deleting all connections to this point
                        connections.RemoveAll(c => (c.InputPoint == e.point || c.OutputPoint == e.point));
                        Repaint();
                        break;
                    }
                case NodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED:
                    {
                        // deleting point is handled by caller, so we are deleting connections associated with it.
                        connections.RemoveAll(c => (c.InputPoint == e.point || c.OutputPoint == e.point));
                        Repaint();
                        break;
                    }
                case NodeEvent.EventType.EVENT_CONNECTIONPOINT_LABELCHANGED:
                    {
                        // point label change is handled by caller, so we are changing label of connection associated with it.
                        var affectingConnections = connections.FindAll(c => c.OutputPoint.Id == e.point.Id);
                        affectingConnections.ForEach(c => c.ConnectionName = e.point.Type);
                        Repaint();
                        break;
                    }
                case NodeEvent.EventType.EVENT_NODE_UPDATED:
                    {
                        Validate(e.eventSourceNode);
                        break;
                    }

                case NodeEvent.EventType.EVENT_RECORDUNDO:
                    {
                        Undo.RecordObject(this, e.message);
                        break;
                    }
                case NodeEvent.EventType.EVENT_SAVE:
                    Setup();
                    Repaint();
                    break;
            }
        }

        private void HandleDragNodes()
        {
            Event evt = Event.current;
            int id = GUIUtility.GetControlID(kDragNodesControlID, FocusType.Passive);

            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (modifyMode == ModifyMode.NONE)
                    {
                        if (evt.button == 0)
                        {
                            if (activeSelection != null && activeSelection.nodes.Count > 0)
                            {
                                bool mouseInSelectedNode = false;
                                foreach (var n in activeSelection.nodes)
                                {
                                    if (n.GetRect(true).Contains(evt.mousePosition))
                                    {
                                        mouseInSelectedNode = true;
                                        break;
                                    }
                                }

                                if (mouseInSelectedNode)
                                {
                                    modifyMode = ModifyMode.DRAGGING;
                                    m_LastMousePosition = evt.mousePosition;
                                    m_DragNodeDistance = Vector2.zero;

                                    foreach (var n in activeSelection.nodes)
                                    {
                                        m_InitialDragNodePositions[n] = n.GetPos();
                                    }

                                    GUIUtility.hotControl = id;
                                    evt.Use();
                                }
                            }
                        }
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        m_InitialDragNodePositions.Clear();
                        GUIUtility.hotControl = 0;
                        modifyMode = ModifyMode.NONE;
                        evt.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        m_DragNodeDistance += evt.mousePosition - m_LastMousePosition;
                        m_LastMousePosition = evt.mousePosition;

                        foreach (var n in activeSelection.nodes)
                        {
                            Vector2 newPosition = n.GetPos();
                            Vector2 initialPosition = m_InitialDragNodePositions[n];
                            newPosition.x = initialPosition.x + m_DragNodeDistance.x;
                            newPosition.y = initialPosition.y + m_DragNodeDistance.y;
                            n.SetPos(SnapPositionToGrid(newPosition));
                        }
                        evt.Use();
                    }
                    break;
            }
        }

        protected static Vector2 SnapPositionToGrid(Vector2 position)
        {
            float gridSize = UserPreference.EditorWindowGridSize;

            int xCell = Mathf.RoundToInt(position.x / gridSize);
            int yCell = Mathf.RoundToInt(position.y / gridSize);

            position.x = xCell * gridSize;
            position.y = yCell * gridSize;

            return position;
        }

        public NodeGUI DuplicateNode(NodeGUI node, float offset)
        {
            var newNode = node.Duplicate(
             controller,
             node.GetX() + offset,
             node.GetY() + offset
         );
            AddNodeGUI(newNode);
            return newNode;
        }

        public void DuplicateConnection(ConnectionGUI con, Dictionary<NodeGUI, NodeGUI> nodeLookup)
        {

            var srcNodes = nodeLookup.Keys;

            var srcFrom = srcNodes.Where(n => n.Id == con.Data.FromNodeId).FirstOrDefault();
            var srcTo = srcNodes.Where(n => n.Id == con.Data.ToNodeId).FirstOrDefault();

            if (srcFrom == null || srcTo == null)
            {
                return;
            }

            var fromPointIndex = srcFrom.Data.OutputPoints.FindIndex(p => p.Id == con.Data.FromNodeConnectionPointId);
            var inPointIndex = srcTo.Data.InputPoints.FindIndex(p => p.Id == con.Data.ToNodeConnectionPointId);

            if (fromPointIndex < 0 || inPointIndex < 0)
            {
                return;
            }

            var dstFrom = nodeLookup[srcFrom];
            var dstTo = nodeLookup[srcTo];
            var dstFromPoint = dstFrom.Data.OutputPoints[fromPointIndex];
            var dstToPoint = dstTo.Data.InputPoints[inPointIndex];
            //var type = srcFrom.Data.Operation.Object.NodeOutputType;
            AddConnection(con.ConnectionName,/*type,*/ dstFrom, dstFromPoint, dstTo, dstToPoint);
        }

        private void AddNodeGUI(NodeGUI newNode)
        {

            int id = -1;

            foreach (var node in nodes)
            {
                if (node.WindowId > id)
                {
                    id = node.WindowId;
                }
            }

            newNode.WindowId = id + 1;

            nodes.Add(newNode);
        }

        public void DeleteNode(string deletingNodeId)
        {
            var deletedNodeIndex = nodes.FindIndex(node => node.Id == deletingNodeId);
            if (0 <= deletedNodeIndex)
            {
                var n = nodes[deletedNodeIndex];
                n.SetActive(false);
                ScriptObjectCatchUtil.Catch(n.Data.Id, n.Data.Object);
                nodes.RemoveAt(deletedNodeIndex);
            }
        }

        /// <summary>
        /// 处理连接事件
        /// </summary>
        /// <param name="e"></param>
        public void HandleConnectionEvent(ConnectionEvent e)
        {
            modifyMode = ModifyMode.NONE;

            switch (modifyMode)
            {
                case ModifyMode.NONE:
                    {
                        switch (e.eventType)
                        {

                            case ConnectionEvent.EventType.EVENT_CONNECTION_TAPPED:
                                {
                                    if (Event.current.shift)
                                    {
                                        Undo.RecordObject(this, "Toggle Select Connection");
                                        if (activeSelection == null)
                                        {
                                            activeSelection = new SavedSelection();
                                        }
                                        activeSelection.Toggle(e.eventSourceCon);
                                        UpdateActiveObjects(activeSelection);
                                        break;
                                    }
                                    else
                                    {
                                        Undo.RecordObject(this, "Select Connection");
                                        if (activeSelection == null)
                                        {
                                            activeSelection = new SavedSelection();
                                        }
                                        activeSelection.Clear(controller);
                                        activeSelection.Add(e.eventSourceCon);
                                        UpdateActiveObjects(activeSelection);
                                        break;
                                    }
                                }
                            case ConnectionEvent.EventType.EVENT_CONNECTION_DELETED:
                                {
                                    Undo.RecordObject(this, "Delete Connection");

                                    var deletedConnectionId = e.eventSourceCon.Id;

                                    DeleteConnection(deletedConnectionId);
                                    activeSelection.Clear(controller);
                                    UpdateActiveObjects(activeSelection);

                                    Setup();
                                    Repaint();
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        private void UpdateActiveObjects(SavedSelection selection)
        {
            foreach (var n in nodes)
            {
                n.SetActive(selection.nodes.Contains(n));
            }

            foreach (var c in connections)
            {
                c.SetActive(selection.connections.Contains(c));
            }
        }

        /**
            create new connection if same relationship is not exist yet.
        */
        private void AddConnection(string type, NodeGUI startNode, ConnectionPointData startPoint, NodeGUI endNode, ConnectionPointData endPoint)
        {
            Undo.RecordObject(this, "Add Connection");

            //自定义最大连接数
            TryAutoDeleteConnection(startNode, startPoint, endNode, endPoint);

            if (!connections.ContainsConnection(startPoint, endPoint))
            {
                connections.Add(controller.CreateConnection(type, startPoint, endPoint));
            }
        }
        /// <summary>
        /// 删除超过连接数的线
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="startPoint"></param>
        /// <param name="endNode"></param>
        /// <param name="endPoint"></param>
        private void TryAutoDeleteConnection(NodeGUI startNode, ConnectionPointData startPoint, NodeGUI endNode, ConnectionPointData endPoint)
        {
            //可以用于删除已经连接上的节线
            var connectionsFromThisNode = connections
                .Where(con => con.OutputNodeId == startNode.Id)
                .Where(con => con.OutputPoint == startPoint)
                .ToList();

            var connectionsToTargetNode = connections
                 .Where(con => con.InputNodeId == endNode.Id)
                .Where(con => con.InputPoint == endPoint)
                .ToList();

            if (connectionsFromThisNode.Count >= startPoint.Max)
            {
                var waitDelete = connectionsFromThisNode[0];
                DeleteConnection(waitDelete.Id);
                if (activeSelection != null)
                {
                    activeSelection.Remove(waitDelete);
                }
            }

            if (connectionsToTargetNode.Count >= endPoint.Max)
            {
                var waitDelete = connectionsToTargetNode[0];
                DeleteConnection(waitDelete.Id);
                if (activeSelection != null)
                {
                    activeSelection.Remove(waitDelete);
                }
            }
        }

        private NodeGUI FindNodeByPosition(Vector2 globalPos)
        {
            return nodes.Find(n => n.Conitains(globalPos));
        }

        private bool IsConnectablePointFromTo(ConnectionPointData sourcePoint, ConnectionPointData destPoint)
        {
            if (sourcePoint.IsInput)
            {
                return destPoint.IsOutput;
            }
            else
            {
                return destPoint.IsInput;
            }
        }

        private void DeleteConnection(string id)
        {
            var deletedConnectionIndex = connections.FindIndex(con => con.Id == id);
            if (0 <= deletedConnectionIndex)
            {
                var c = connections[deletedConnectionIndex];
                c.SetActive(false);
                //Debug.Log("catch:" + c.Data.Id);
                ScriptObjectCatchUtil.Catch(c.Data.Id, c.Data.Object);
                connections.RemoveAt(deletedConnectionIndex);
            }
        }

        public int GetUnusedWindowId()
        {
            int highest = 0;
            nodes.ForEach((NodeGUI n) => { if (n.WindowId > highest) highest = n.WindowId; });
            return highest + 1;
        }
    }
}
