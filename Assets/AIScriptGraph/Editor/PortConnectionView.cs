
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UFrame.NodeGraph;
using UFrame.NodeGraph.DataModel;
using UFrame.NodeGraph.DefultSkin;
using UnityEditor;
using UnityEngine;

namespace AIScripting
{
    [CustomView(typeof(PortConnection))]
    public class PortConnectionView : ConnectionView
    {
        public override void OnDrawLabel(Vector2 centerPos, string label)
        {
            //base.OnDrawLabel(centerPos, label);
        }
    }
}