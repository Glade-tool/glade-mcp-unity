using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class RemoveAnimationEventTool : ITool
    {
        public string Name => "remove_animation_event";

        public string Execute(Dictionary<string, object> args)
        {
            string clipPath = args.ContainsKey("clipPath") ? args["clipPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(clipPath))
                return ToolUtils.CreateErrorResponse("clipPath is required");
            
            // Ensure path starts with Assets/
            if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                clipPath = "Assets/" + clipPath;
            
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
                return ToolUtils.CreateErrorResponse($"AnimationClip not found at '{clipPath}'");
            
            // Record for undo
            Undo.RecordObject(clip, $"Remove Animation Event: {clipPath}");
            
            // Get existing events
            AnimationEvent[] existingEvents = AnimationUtility.GetAnimationEvents(clip);
            List<AnimationEvent> events = new List<AnimationEvent>(existingEvents);
            
            int initialCount = events.Count;
            
            // Remove by index
            if (args.ContainsKey("eventIndex"))
            {
                int eventIndex = -1;
                if (args["eventIndex"] is int i) eventIndex = i;
                else if (args["eventIndex"] is float f) eventIndex = (int)f;
                else int.TryParse(args["eventIndex"].ToString(), out eventIndex);
                
                if (eventIndex >= 0 && eventIndex < events.Count)
                {
                    events.RemoveAt(eventIndex);
                }
            }
            // Remove by time
            else if (args.ContainsKey("time"))
            {
                float time = 0f;
                if (args["time"] is float f) time = f;
                else float.TryParse(args["time"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out time);
                
                events.RemoveAll(e => Mathf.Approximately(e.time, time));
            }
            // Remove by function name
            else if (args.ContainsKey("functionName"))
            {
                string functionName = args["functionName"].ToString();
                events.RemoveAll(e => e.functionName == functionName);
            }
            else
            {
                return ToolUtils.CreateErrorResponse("One of eventIndex, time, or functionName is required");
            }
            
            int eventsRemoved = initialCount - events.Count;
            
            if (eventsRemoved > 0)
            {
                // Set remaining events
                AnimationUtility.SetAnimationEvents(clip, events.ToArray());
                
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();
            }
            
            var extras = new Dictionary<string, object>
            {
                { "eventRemoved", eventsRemoved }
            };
            
            return ToolUtils.CreateSuccessResponse($"Removed {eventsRemoved} animation event(s)", extras);
        }
    }
}
