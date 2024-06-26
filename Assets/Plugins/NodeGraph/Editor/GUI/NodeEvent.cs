/***********************************************************************************//*
*  作者: 邹杭特                                                                       *
*  时间: 2021-04-24                                                                   *
*  版本: master_5122a2                                                                *
*  功能:                                                                              *
*   - 节点事件                                                                        *
*//************************************************************************************/

using UnityEngine;
using UFrame.NodeGraph.DataModel;

namespace UFrame.NodeGraph
{
    public class NodeEvent
    {
        public enum EventType
        {
            EVENT_NONE,

            EVENT_CONNECTING_BEGIN,
            EVENT_CONNECTING_END,
            EVENT_CONNECTION_ESTABLISHED,

            EVENT_NODE_UPDATED,

            EVENT_NODE_CLICKED,

            EVENT_CONNECTIONPOINT_DELETED,
            EVENT_CONNECTIONPOINT_LABELCHANGED,

            EVENT_DELETE_ALL_CONNECTIONS_TO_POINT,

            EVENT_NODE_DELETE,

            EVENT_RECORDUNDO,
            EVENT_SAVE,
        }

        public readonly EventType eventType;
        public readonly NodeGUI eventSourceNode;
        public readonly ConnectionPointData point;
        public readonly Vector2 position;
        public readonly Vector2 globalMousePosition;
        public readonly string message;

        public NodeEvent(EventType type, NodeGUI node, Vector2 localMousePos, ConnectionPointData point)
        {
            this.eventType = type;
            this.eventSourceNode = node;
            this.point = point;
            this.position = localMousePos;
            this.globalMousePosition = new Vector2(localMousePos.x + node.GetX(), localMousePos.y + node.GetY());
        }

        public NodeEvent(EventType type, NodeGUI node)
        {
            this.eventType = type;
            this.eventSourceNode = node;
        }

        public NodeEvent(EventType type, string message)
        {
            this.eventType = type;
            this.message = message;
        }

        public NodeEvent(EventType type)
        {
            this.eventType = type;
        }
    }
}