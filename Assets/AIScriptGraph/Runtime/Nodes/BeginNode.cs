using System.Collections;
using System.Collections.Generic;
using UFrame.NodeGraph;
using UFrame.NodeGraph.DataModel;
using UnityEngine;

namespace AIScripting
{
    [CustomNode("Begin",group:Define.GROUP)]
    public class BeginNode : ScriptNodeBase
    {
        public override int Style => enable ? 7 : 0;
        protected override void OnProcess()
        {
            DoFinish(true);
        }
    }
}
