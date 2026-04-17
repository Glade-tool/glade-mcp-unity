using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class ModifyAnimationEventTool : ITool
    {
        public string Name => "modify_animation_event";

        public string Execute(Dictionary<string, object> args)
        {
            string clipPath = args.ContainsKey("clipPath") ? args["clipPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(clipPath))
                return ToolUtils.CreateErrorResponse("clipPath is required");
            
            int eventIndex = -1;
            if (args.ContainsKey("eventIndex"))
            {
                if (args["eventIndex"] is int i) eventIndex = i;
                else if (args["eventIndex"] is float f) eventIndex = (int)f;
                else int.TryParse(args["eventIndex"].ToString(), out eventIndex);
            }
            
            if (eventIndex < 0)
                return ToolUtils.CreateErrorResponse("eventIndex is required");
            
            // Ensure path starts with Assets/
            if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                clipPath = "Assets/" + clipPath;
            
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
                return ToolUtils.CreateErrorResponse($"AnimationClip not found at '{clipPath}'");
            
            // Get events
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            
            // Validate eventIndex
            if (eventIndex >= events.Length)
                return ToolUtils.CreateErrorResponse($"Event index {eventIndex} out of range. Clip has {events.Length} event(s).");
            
            // Record Undo BEFORE modifications
            Undo.RecordObject(clip, $"Modify Animation Event: {clipPath}");
            
            // Modify event at index
            AnimationEvent evt = events[eventIndex];
            bool modified = false;
            
            if (args.ContainsKey("time"))
            {
                float time = 0f;
                if (args["time"] is float tf) time = tf;
                else float.TryParse(args["time"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out time);
                
                evt.time = time;
                modified = true;
            }
            
            if (args.ContainsKey("functionName"))
            {
                evt.functionName = args["functionName"].ToString();
                modified = true;
            }
            
            if (args.ContainsKey("parameter"))
            {
                // Parameter can be string, int, or float - try to determine type
                string paramStr = args["parameter"].ToString();
                
                // Try parsing as float first (covers both float and int)
                if (float.TryParse(paramStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float floatVal))
                {
                    // Check if it's actually an integer
                    if (int.TryParse(paramStr, out int intVal) && floatVal == intVal)
                    {
                        evt.intParameter = intVal;
                        evt.floatParameter = 0f;
                    }
                    else
                    {
                        evt.floatParameter = floatVal;
                        evt.intParameter = 0;
                    }
                }
                else
                {
                    // Treat as string parameter
                    evt.stringParameter = paramStr;
                }
                modified = true;
            }
            
            // Also support explicit parameter types
            if (args.ContainsKey("floatParameter"))
            {
                float floatParam = 0f;
                if (args["floatParameter"] is float f) floatParam = f;
                else float.TryParse(args["floatParameter"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out floatParam);
                evt.floatParameter = floatParam;
                modified = true;
            }
            
            if (args.ContainsKey("intParameter"))
            {
                int intParam = 0;
                if (args["intParameter"] is int i) intParam = i;
                else if (args["intParameter"] is float f) intParam = (int)f;
                else int.TryParse(args["intParameter"].ToString(), out intParam);
                evt.intParameter = intParam;
                modified = true;
            }
            
            if (args.ContainsKey("stringParameter"))
            {
                evt.stringParameter = args["stringParameter"].ToString();
                modified = true;
            }
            
            // Replace event in array
            if (modified)
            {
                events[eventIndex] = evt;
                AnimationUtility.SetAnimationEvents(clip, events);
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();
            }
            
            var extras = new Dictionary<string, object>
            {
                { "eventIndex", eventIndex },
                { "time", evt.time },
                { "functionName", evt.functionName },
                { "modified", modified }
            };
            
            return ToolUtils.CreateSuccessResponse(modified ? $"Modified animation event at index {eventIndex}" : $"No changes made to event at index {eventIndex}", extras);
        }
    }
}
