using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class AddAnimationEventTool : ITool
    {
        public string Name => "add_animation_event";

        public string Execute(Dictionary<string, object> args)
        {
            string clipPath = args.ContainsKey("clipPath") ? args["clipPath"].ToString() : "";
            string functionName = args.ContainsKey("functionName") ? args["functionName"].ToString() : "";
            
            if (string.IsNullOrEmpty(clipPath))
                return ToolUtils.CreateErrorResponse("clipPath is required");
            
            if (string.IsNullOrEmpty(functionName))
                return ToolUtils.CreateErrorResponse("functionName is required");
            
            if (!args.ContainsKey("time"))
                return ToolUtils.CreateErrorResponse("time is required");
            
            // Ensure path starts with Assets/
            if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                clipPath = "Assets/" + clipPath;
            
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
                return ToolUtils.CreateErrorResponse($"AnimationClip not found at '{clipPath}'");
            
            // Get time
            float time = 0f;
            if (args["time"] is float tf) time = tf;
            else float.TryParse(args["time"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out time);
            
            // Record for undo
            Undo.RecordObject(clip, $"Add Animation Event: {clipPath}");
            
            // Get existing events
            AnimationEvent[] existingEvents = AnimationUtility.GetAnimationEvents(clip);
            List<AnimationEvent> events = new List<AnimationEvent>(existingEvents);
            
            // Create new event
            AnimationEvent newEvent = new AnimationEvent
            {
                time = time,
                functionName = functionName
            };
            
            // Set parameters
            if (args.ContainsKey("floatParameter"))
            {
                float floatParam = 0f;
                if (args["floatParameter"] is float f) floatParam = f;
                else float.TryParse(args["floatParameter"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out floatParam);
                newEvent.floatParameter = floatParam;
            }
            
            if (args.ContainsKey("intParameter"))
            {
                int intParam = 0;
                if (args["intParameter"] is int i) intParam = i;
                else if (args["intParameter"] is float f) intParam = (int)f;
                else int.TryParse(args["intParameter"].ToString(), out intParam);
                newEvent.intParameter = intParam;
            }
            
            if (args.ContainsKey("stringParameter"))
            {
                newEvent.stringParameter = args["stringParameter"].ToString();
            }
            
            if (args.ContainsKey("objectReferenceParameter"))
            {
                string objPath = args["objectReferenceParameter"].ToString();
                if (!objPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    objPath = "Assets/" + objPath;
                
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(objPath);
                newEvent.objectReferenceParameter = obj;
            }
            
            events.Add(newEvent);
            
            // Set all events
            AnimationUtility.SetAnimationEvents(clip, events.ToArray());
            
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            
            var extras = new Dictionary<string, object>
            {
                { "eventAdded", true }
            };
            
            return ToolUtils.CreateSuccessResponse($"Added animation event '{functionName}' at time {time}", extras);
        }
    }
}
