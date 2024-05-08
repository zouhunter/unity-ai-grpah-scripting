using AIScripting;

using System.Collections;
using System.Collections.Generic;

using UFrame.NodeGraph;
using UFrame.NodeGraph.DataModel;

using UnityEditor;
using UnityEditor.MemoryProfiler;

using UnityEngine;

namespace AIScripting
{
    public class AIScriptingController : NodeGraphController
    {
        public override string Group => "AIScripting";

        public override NodeGraphObj CreateNodeGraphObject()
        {
            var graph = ScriptableObject.CreateInstance<AIScriptGraph>();
            graph.ControllerType = GetType().FullName;
            ProjectWindowUtil.CreateAsset(graph, Group + ".asset");
            return graph;
        }

        public override NodeView CreateDefaultNodeView()
        {
            return new ScriptingNodeView();
        }

        public override void BuildFromGraph(NodeGraphObj graph)
        {
            base.BuildFromGraph(graph);
            var graphDirector = new GraphDirector(graph as AIScriptGraph);
            var op = graphDirector.Run();
            op.RegistComplete((x)=> { Debug.Log("graph finished!"); });
        }

        public override ConnectionGUI CreateConnection(string type, ConnectionPointData output, ConnectionPointData input)
        {
            var connection = ScriptableObject.CreateInstance<PortConnection>();
            connection.type = type;
            return new ConnectionGUI(
               new ConnectionData(type, connection, output, input),
               output,
               input
           );
        }
    }
}