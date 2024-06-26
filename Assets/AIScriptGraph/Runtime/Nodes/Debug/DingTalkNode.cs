using System.Collections;
using UFrame.NodeGraph;

using UnityEngine;
using UnityEngine.Networking;

namespace AIScripting.Debugger
{
    [CustomNode("DingTalk", 0, Define.GROUP)]
    public class DingTalkNode : ScriptNodeBase
    {
        public Ref<string> talk_url = new Ref<string>("talk_url", "https://oapi.dingtalk.com/robot/send?access_token=0d5881be6ebc0ce565481930584fd579ae7ccfa076ab6cddd05432f2bb615382");
        public Ref<string> talk_text;
        private LitCoroutine _litCoroutine;
        
        protected override void OnProcess()
        {
            _litCoroutine = Owner.StartCoroutine(SendRequest());
        }

        [System.Serializable]
        public class SendData
        {
            public Text text;
            public string msgtype;

            [System.Serializable]
            public class Text
            {
                public string content;
            }
        }

        private IEnumerator SendRequest()
        {
            var sendData = new SendData() { 
                text = new SendData.Text() { content = "新闻:" + talk_text.Value },
                msgtype = "text" 
           };
            var req = new UnityWebRequest(talk_url);
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(sendData)));
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                DoFinish(true);
            }
            else
            {
                DoFinish(false);
            }
        }
    }
}
