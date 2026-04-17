using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Hierarchy
{
    public class GetComponentInspectorPropertiesTool : ITool
    {
        public string Name => "get_component_inspector_properties";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"]?.ToString() : "";
            string componentTypeName = args.ContainsKey("componentType") ? args["componentType"]?.ToString() : "";
            if (string.IsNullOrEmpty(gameObjectPath))
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            if (string.IsNullOrEmpty(componentTypeName))
                return ToolUtils.CreateErrorResponse("componentType is required");

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{ToolUtils.EscapeJsonString(gameObjectPath)}' not found");

            Component target = null;
            var components = obj.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                var type = comp.GetType();
                if (string.Equals(type.Name, componentTypeName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(type.FullName, componentTypeName, StringComparison.OrdinalIgnoreCase))
                {
                    target = comp;
                    break;
                }
            }

            if (target == null)
                return ToolUtils.CreateErrorResponse($"Component '{ToolUtils.EscapeJsonString(componentTypeName)}' not found on '{ToolUtils.EscapeJsonString(gameObjectPath)}'");

            bool onlyReferences = false;
            if (args.ContainsKey("onlyReferences"))
            {
                if (args["onlyReferences"] is bool b) onlyReferences = b;
                else bool.TryParse(args["onlyReferences"]?.ToString(), out onlyReferences);
            }

            bool onlyUnassigned = false;
            if (args.ContainsKey("onlyUnassigned"))
            {
                if (args["onlyUnassigned"] is bool b) onlyUnassigned = b;
                else bool.TryParse(args["onlyUnassigned"]?.ToString(), out onlyUnassigned);
            }

            bool onlyTopLevel = true;
            if (args.ContainsKey("onlyTopLevel"))
            {
                if (args["onlyTopLevel"] is bool b) onlyTopLevel = b;
                else bool.TryParse(args["onlyTopLevel"]?.ToString(), out onlyTopLevel);
            }

            var propertiesFilter = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if ((args.ContainsKey("propertyFilter") ? args["propertyFilter"] : args.ContainsKey("properties") ? args["properties"] : null) is IEnumerable<object> propObjs)
            {
                foreach (var p in propObjs)
                {
                    if (p == null) continue;
                    string name = p.ToString();
                    if (!string.IsNullOrEmpty(name))
                        propertiesFilter.Add(name);
                }
            }

            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"gameObjectPath\":\"{ToolUtils.EscapeJsonString(ToolUtils.GetGameObjectPath(obj))}\"");
            sb.Append($",\"componentType\":\"{ToolUtils.EscapeJsonString(target.GetType().Name)}\"");
            sb.Append(",\"properties\":[");

            bool firstProp = true;
            var so = new SerializedObject(target);
            var prop = so.GetIterator();
            bool enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.name == "m_Script") continue;
                if (onlyTopLevel && prop.depth > 0) continue;
                if (propertiesFilter.Count > 0 && !propertiesFilter.Contains(prop.name) && !propertiesFilter.Contains(prop.propertyPath))
                    continue;

                bool isReference = prop.propertyType == SerializedPropertyType.ObjectReference;
                if (onlyReferences && !isReference) continue;

                bool isAssigned = !isReference || prop.objectReferenceValue != null;
                if (onlyUnassigned && isAssigned) continue;

                if (!firstProp) sb.Append(",");
                firstProp = false;
                sb.Append("{");
                sb.Append($"\"name\":\"{ToolUtils.EscapeJsonString(prop.displayName)}\"");
                sb.Append($",\"internalName\":\"{ToolUtils.EscapeJsonString(prop.name)}\"");
                sb.Append($",\"path\":\"{ToolUtils.EscapeJsonString(prop.propertyPath)}\"");
                sb.Append($",\"type\":\"{ToolUtils.EscapeJsonString(prop.propertyType.ToString())}\"");
                sb.Append($",\"isAssigned\":{isAssigned.ToString().ToLower()}");
                sb.Append($",\"value\":{ToolUtils.SerializeSerializedPropertyValue(prop)}");
                sb.Append("}");
            }

            sb.Append("]");
            sb.Append("}");
            return sb.ToString();
        }
    }
}
