using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using UFrame.NodeGraph;

using UnityEngine;
using UnityEngine.Networking;

namespace AIScripting.Work
{
    [CustomNode("OpenCC", 0, Define.GROUP)]
    public class OpenCCNode : ScriptNodeBase
    {
        public string conversation_id = "295948";
        public string model = "gpt-4-1106-preview";
        public string Authorization = "cb-dabd1913552249b3b14670b3ada4227c";
        public Ref<string> we_url = new Ref<string>("we_url");
        public Ref<string> input = new Ref<string>("input_text");
        public Ref<string> output = new Ref<string>("output_text");
        public string saveFilePath;
        private LitCoroutine _litCoroutine;

        [Header("消息接受key")]
        [SerializeField] protected string eventReceiveKey = "uframe_receive_message";

        protected override void OnProcess()
        {
            PostMsg(input, (result) =>
            {
                output.SetValue(result);
                DoFinish();
            });
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        public virtual void PostMsg(string _msg, Action<string> _callback)
        {
            _litCoroutine = Owner.StartCoroutine(Request(_msg,  _callback));
        }

        public class DownloadHandlerMessageQueue : DownloadHandlerScript
        {
            public StringBuilder allText = new StringBuilder();
            public Action<string> onReceive { get; set; }
            public bool Finished { get; internal set; }

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (Finished)
                    return false;

                var text = Encoding.UTF8.GetString(data, 0, dataLength);
               
                var lines = text.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    System.IO.File.AppendAllText("D:/uframeCC.txt", line,Encoding.UTF8);
                    System.IO.File.AppendAllText("D:/uframeCC.txt", "\n");
                    if (line.StartsWith("data:"))
                    {
                        if(line.Length > 6)
                        {
                            var textData = GetText(line.Substring(6, line.Length - 7));
                            Finished = textData == "[DONE]" || textData == "聊天消息不能为空";
                            if (Finished)
                            {
                                break;
                            }
                            if (textData != "连接成功")
                            {
                                allText.Append(textData);
                                onReceive?.Invoke(textData);
                            }
                        }
                    }
                    else if(line.StartsWith("id:") || (line.StartsWith("retry:") || (line.StartsWith("event:"))))
                    {
                        //Debug.Log("ignore:" + line);
                    }else
                    {
                        var textData = GetText(line).Trim('\"');
                        allText.Append(textData);
                        onReceive?.Invoke(textData);
                    }
                }
                return base.ReceiveData(data, dataLength);
            }
            private string GetText(string textData)
            {
                return textData.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\\"", "\"");
            }
        }
        /// <summary>
        /// 调用接口
        /// </summary>
        /// <param name="_postWord"></param>
        /// <param name="_callback"></param>
        /// <returns></returns>
        public IEnumerator Request(string msg,System.Action<string> _callback)
        {
            msg = UnityWebRequest.EscapeURL($"{msg}");
            var args = new System.Collections.Generic.Dictionary<string,string>();
            args.Add("content", msg);
            args.Add("conversation_id", conversation_id);
            args.Add("from_user", "zouhangte%40uframe.cn");
            args.Add("model", model);
            args.Add("max_tokens", "1024");
            args.Add("temperature", "1");
            args.Add("presence_penalty", "0.6");
            args.Add("add_context", "true");
            args.Add("use_context", "true");
            var form = new WWWForm();
            foreach (var item in args)
                form.AddField(item.Key, item.Value);
            var url = we_url.Value;
            url = new System.Uri(url).AbsoluteUri;
            Debug.Log(System.DateTime.Now.Ticks + ",request1:" + url);
            long startTime = System.DateTime.Now.Ticks;
            UnityWebRequest request = new UnityWebRequest(url, "OPTIONS");
            {
                request.SetRequestHeader("Content-Type", "text/event-stream;charset=UTF-8");
                request.SetRequestHeader("Accept", "*/*");
                request.SetRequestHeader("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
                request.uploadHandler = new UploadHandlerRaw(form.data);
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    yield return null;
                    _asyncOp.SetProgress(operation.progress);
                }
                if (request.responseCode == 200)
                {
                    yield return Request2(url, args, _callback);
                }
                else
                {
                    _callback?.Invoke(null);
                    Debug.LogError(request.error);
                }
                request.Dispose();
                Debug.Log(System.DateTime.Now.Ticks + ",Ollama耗时：" + (System.DateTime.Now.Ticks - startTime) / 10000000);
            }
        }
        /// <summary>
        /// 收到回复
        /// </summary>
        /// <param name="data"></param>
        private void OnReceive(string data)
        {
            Owner.SendEvent(eventReceiveKey, data);
            output.SetValue(output.Value + data);
        }

        public IEnumerator Request2(string url, Dictionary<string, string> args, System.Action<string> _callback)
        {
            var argsArr = new StringBuilder();
            var index = 0;
            foreach (var item in args)
            {
                if(index++ > 0)
                    argsArr.Append("&");
                argsArr.Append($"{item.Key}={item.Value}");
            }
            url = new System.Uri(url+"?" + argsArr.ToString()).AbsoluteUri;
            long startTime = System.DateTime.Now.Ticks;
            Debug.Log(System.DateTime.Now.Ticks + ",request2:" + url);
            UnityWebRequest request = new UnityWebRequest(url, "GET");
            {
                var downloadHandler = new DownloadHandlerMessageQueue();
                downloadHandler.onReceive = OnReceive;
                request.downloadHandler = downloadHandler;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "*/*");
                request.SetRequestHeader("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
                request.SetRequestHeader("Accept-Encoding", "gzip, deflate, br, zstd");
                request.SetRequestHeader("Authorization", Authorization);
                request.SetRequestHeader("User-Agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Mobile Safari/537.36 Edg/124.0.0.0");
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    yield return null;
                    _asyncOp.SetProgress(operation.progress);
                    if(downloadHandler.Finished)
                    {
                        break;
                    }
                }
                if (request.responseCode == 200)
                {
                    string _msgBack = downloadHandler.allText.ToString();
                    if (!string.IsNullOrEmpty(_msgBack))
                    {
                        _callback(_msgBack);
                    }
                    else
                    {
                        _callback(null);
                    }
                }
                else
                {
                    _callback?.Invoke(null);
                    Debug.LogError(request.downloadHandler.error);
                }
                request.Dispose();
                Debug.Log(System.DateTime.Now.Ticks + ",Ollama耗时：" + (System.DateTime.Now.Ticks - startTime) / 10000000);
            }
        }
    }
}
