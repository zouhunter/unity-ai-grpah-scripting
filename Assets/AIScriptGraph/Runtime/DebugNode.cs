using UnityEngine;
using UFrame.NodeGraph;

namespace AIScripting
{
    [CustomNode("Debug",group:"AIScripting")]
    public class DebugNode : ScriptNodeBase
    {
        public Ref<string> info;
        public string format;
        public LogType logType;

        protected override int InCount => int.MaxValue;

        protected override void OnProcess()
        {
            if(string.IsNullOrEmpty(format))
            {
                Debug.unityLogger.LogFormat(logType, "{0}:{1}",Title,info.Value);
            }
            else if (!format.Contains("{0}"))
            {
                Debug.unityLogger.LogFormat(logType, "{0}.{1}:{2}", Title, format, info.Value);
            }
            else
            {
                Debug.unityLogger.LogFormat(logType, "{0}.{1}",Title,string.Format(format,info.Value));
            }
            DoFinish(true);
        }
    }
}