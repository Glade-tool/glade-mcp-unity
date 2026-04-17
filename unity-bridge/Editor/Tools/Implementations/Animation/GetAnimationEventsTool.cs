using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class GetAnimationEventsTool : ITool
    {
        public string Name => "get_animation_events";

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
            
            // Get animation events
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            
            List<Dictionary<string, object>> eventsList = new List<Dictionary<string, object>>();
            
            foreach (var evt in events)
            {
                Dictionary<string, object> eventData = new Dictionary<string, object>
                {
                    { "time", evt.time },
                    { "functionName", evt.functionName },
                    { "floatParameter", evt.floatParameter },
                    { "intParameter", evt.intParameter },
                    { "stringParameter", evt.stringParameter ?? "" }
                };
                
                // Get object reference path if present
                if (evt.objectReferenceParameter != null)
                {
                    string objPath = AssetDatabase.GetAssetPath(evt.objectReferenceParameter);
                    eventData["objectReferenceParameter"] = objPath;
                }
                else
                {
                    eventData["objectReferenceParameter"] = "";
                }
                
                eventsList.Add(eventData);
            }
            
            var extras = new Dictionary<string, object>
            {
                { "events", eventsList }
            };
            
            return ToolUtils.CreateSuccessResponse($"Found {eventsList.Count} animation event(s) in AnimationClip", extras);
        }
    }
}
