using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Components
{
    public class SetObjectReferenceTool : ITool
    {
        public string Name => "set_object_reference";

        public string Execute(Dictionary<string, object> args)
        {
            string targetGameObjectPath = args.ContainsKey("targetGameObject") ? args["targetGameObject"].ToString() : "";
            string componentType = args.ContainsKey("componentType") ? args["componentType"].ToString() : "";
            string fieldName = args.ContainsKey("fieldName") ? args["fieldName"].ToString() : "";
            string sourceGameObjectPath = args.ContainsKey("sourceGameObject") ? args["sourceGameObject"].ToString() : "";
            string sourceType = args.ContainsKey("sourceType") ? args["sourceType"].ToString() : "Transform";
            
            if (string.IsNullOrEmpty(targetGameObjectPath))
            {
                return ToolUtils.CreateErrorResponse("targetGameObject is required");
            }
            
            if (string.IsNullOrEmpty(componentType))
            {
                return ToolUtils.CreateErrorResponse("componentType is required");
            }
            
            if (string.IsNullOrEmpty(fieldName))
            {
                return ToolUtils.CreateErrorResponse("fieldName is required");
            }
            
            if (string.IsNullOrEmpty(sourceGameObjectPath))
            {
                return ToolUtils.CreateErrorResponse("sourceGameObject is required");
            }
            
            // Find target GameObject
            UnityEngine.GameObject targetObj = ToolUtils.FindGameObjectByPath(targetGameObjectPath);
            if (targetObj == null)
            {
                return ToolUtils.CreateErrorResponse($"Target GameObject '{targetGameObjectPath}' not found");
            }
            
            // Find source GameObject
            UnityEngine.GameObject sourceObj = ToolUtils.FindGameObjectByPath(sourceGameObjectPath);
            if (sourceObj == null)
            {
                return ToolUtils.CreateErrorResponse($"Source GameObject '{sourceGameObjectPath}' not found");
            }
            
            // Find component type on target
            System.Type compType = ToolUtils.FindComponentType(componentType);
            if (compType == null)
            {
                return ToolUtils.CreateErrorResponse($"Component type '{componentType}' not found");
            }
            
            Component targetComp = targetObj.GetComponent(compType);
            if (targetComp == null)
            {
                return ToolUtils.CreateErrorResponse($"Component '{componentType}' not found on '{targetGameObjectPath}'");
            }
            
            // Determine what to assign from source
            UnityEngine.Object sourceReference = null;
            string assignedType = sourceType;
            
            if (string.IsNullOrEmpty(sourceType) || sourceType.Equals("Transform", StringComparison.OrdinalIgnoreCase))
            {
                sourceReference = sourceObj.transform;
                assignedType = "Transform";
            }
            else if (sourceType.Equals("GameObject", StringComparison.OrdinalIgnoreCase))
            {
                sourceReference = sourceObj;
                assignedType = "GameObject";
            }
            else
            {
                // Try to find a component of the specified type on the source object
                System.Type sourceCompType = ToolUtils.FindComponentType(sourceType);
                if (sourceCompType != null)
                {
                    Component sourceComp = sourceObj.GetComponent(sourceCompType);
                    if (sourceComp != null)
                    {
                        sourceReference = sourceComp;
                        assignedType = sourceType;
                    }
                    else
                    {
                        return ToolUtils.CreateErrorResponse($"Component '{sourceType}' not found on source GameObject '{sourceGameObjectPath}'");
                    }
                }
                else
                {
                    // Default to Transform if source type not recognized
                    sourceReference = sourceObj.transform;
                    assignedType = "Transform";
                }
            }
            
            // Resolve the field/property name through the same 5-layer chain used by
            // SetComponentPropertyTool / SetScriptComponentPropertyTool:
            //   1. Exact reflection field
            //   2. Case-insensitive reflection field
            //   3. Exact reflection property
            //   4. Case-insensitive reflection property
            //   5. SerializedObject.FindProperty (exact then case-insensitive walk)
            // The SerializedObject path rescues private [SerializeField] refs whose
            // declared C# type is more specific than Component/Transform/GameObject —
            // Unity's SerializedProperty.objectReferenceValue accepts the assignment
            // as long as the runtime type matches what the serialized type wants.
            var bindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var bindingFlagsIgnoreCase = bindingFlags | System.Reflection.BindingFlags.IgnoreCase;

            // Try field first (most common for serialized references)
            var field = compType.GetField(fieldName, bindingFlags)
                    ?? compType.GetField(fieldName, bindingFlagsIgnoreCase);
            if (field != null && !field.IsLiteral && !field.IsInitOnly)
            {
                var resolvedRef = CoerceSourceForField(field.FieldType, sourceObj, sourceReference, out var resolvedType);
                if (resolvedRef == null)
                {
                    return ToolUtils.CreateErrorResponse(
                        $"Field '{field.Name}' is of type '{field.FieldType.Name}' but source '{sourceGameObjectPath}' " +
                        $"has no matching component. Pass sourceType='{field.FieldType.Name}' (or a subclass) if the source provides one.");
                }

                try
                {
                    Undo.RecordObject(targetComp, $"Set Reference: {targetGameObjectPath}.{componentType}.{field.Name}");
                    field.SetValue(targetComp, resolvedRef);
                    EditorUtility.SetDirty(targetComp);

                    return ToolUtils.CreateSuccessResponse(
                        $"Set field '{field.Name}' on '{componentType}' of '{targetGameObjectPath}' to reference {resolvedType} of '{sourceGameObjectPath}'");
                }
                catch (Exception e)
                {
                    return ToolUtils.CreateErrorResponse($"Failed to set field: {e.Message}");
                }
            }

            // Try property
            var prop = compType.GetProperty(fieldName, bindingFlags)
                    ?? compType.GetProperty(fieldName, bindingFlagsIgnoreCase);
            if (prop != null && prop.CanWrite)
            {
                var resolvedRef = CoerceSourceForField(prop.PropertyType, sourceObj, sourceReference, out var resolvedType);
                if (resolvedRef == null)
                {
                    return ToolUtils.CreateErrorResponse(
                        $"Property '{prop.Name}' is of type '{prop.PropertyType.Name}' but source '{sourceGameObjectPath}' " +
                        $"has no matching component. Pass sourceType='{prop.PropertyType.Name}' (or a subclass) if the source provides one.");
                }

                try
                {
                    Undo.RecordObject(targetComp, $"Set Reference: {targetGameObjectPath}.{componentType}.{prop.Name}");
                    prop.SetValue(targetComp, resolvedRef);
                    EditorUtility.SetDirty(targetComp);

                    return ToolUtils.CreateSuccessResponse(
                        $"Set property '{prop.Name}' on '{componentType}' of '{targetGameObjectPath}' to reference {resolvedType} of '{sourceGameObjectPath}'");
                }
                catch (Exception e)
                {
                    return ToolUtils.CreateErrorResponse($"Failed to set property: {e.Message}");
                }
            }

            // SerializedObject fallback — routes through Unity's editor serialization,
            // which accepts any UnityEngine.Object and validates at write time. Rescues
            // serialized field names that reflection missed (e.g. private backing fields
            // whose declared name differs from the inspector-facing label).
            var serializedResult = TrySetReferenceViaSerializedObject(
                targetComp, fieldName, sourceObj, sourceReference, assignedType,
                targetGameObjectPath, componentType, sourceGameObjectPath);
            if (serializedResult != null) return serializedResult;

            // Enumerate both reflection-visible and serialized names so the next retry has a full picture.
            var reflectionFields = compType.GetFields(bindingFlags)
                .Where(f => !f.IsLiteral && !f.IsInitOnly)
                .Select(f => f.Name);
            var reflectionProps = compType.GetProperties(bindingFlags)
                .Where(p => p.CanWrite)
                .Select(p => p.Name);
            var reflectionNames = reflectionFields.Concat(reflectionProps).Distinct().ToList();

            var serializedNames = new List<string>();
            try
            {
                var so = new SerializedObject(targetComp);
                var iter = so.GetIterator();
                bool enterChildren = true;
                while (iter.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (iter.name == "m_Script") continue;
                    if (iter.propertyType == SerializedPropertyType.ObjectReference)
                        serializedNames.Add(iter.name);
                }
            }
            catch { /* surface whatever we have */ }

            var reflectionList = reflectionNames.Count > 0
                ? $" Reflection-visible on '{componentType}': {string.Join(", ", reflectionNames)}."
                : "";
            var serializedList = serializedNames.Count > 0
                ? $" Serialized object-reference fields: {string.Join(", ", serializedNames)}."
                : "";
            return ToolUtils.CreateErrorResponse(
                $"Field or property '{fieldName}' not found on '{componentType}'.{reflectionList}{serializedList}");
        }

        /// <summary>
        /// Coerce the source reference to match the declared field/property type.
        /// If the declared type is Component or a specific component subclass and
        /// sourceReference is the wrong shape, re-resolve from the source GameObject.
        /// Returns null when no match is possible.
        /// </summary>
        private static UnityEngine.Object CoerceSourceForField(
            System.Type declaredType,
            UnityEngine.GameObject sourceObj,
            UnityEngine.Object sourceReference,
            out string resolvedType)
        {
            if (declaredType.IsInstanceOfType(sourceReference))
            {
                resolvedType = sourceReference.GetType().Name;
                return sourceReference;
            }
            if (declaredType == typeof(UnityEngine.GameObject))
            {
                resolvedType = "GameObject";
                return sourceObj;
            }
            if (typeof(Component).IsAssignableFrom(declaredType))
            {
                var comp = sourceObj.GetComponent(declaredType);
                if (comp != null)
                {
                    resolvedType = declaredType.Name;
                    return comp;
                }
            }
            if (typeof(UnityEngine.Object).IsAssignableFrom(declaredType)
                && declaredType.IsInstanceOfType(sourceObj))
            {
                resolvedType = declaredType.Name;
                return sourceObj;
            }
            resolvedType = null;
            return null;
        }

        /// <summary>
        /// Try to set a reference via SerializedObject.FindProperty — case-insensitive
        /// walk if exact name misses. Returns null when the name can't be resolved so
        /// the caller can surface a "not found" error with all available names.
        /// </summary>
        private static string TrySetReferenceViaSerializedObject(
            Component targetComp,
            string fieldName,
            UnityEngine.GameObject sourceObj,
            UnityEngine.Object sourceReference,
            string assignedType,
            string targetGameObjectPath,
            string componentType,
            string sourceGameObjectPath)
        {
            try
            {
                var so = new SerializedObject(targetComp);
                var sp = so.FindProperty(fieldName);
                if (sp == null)
                {
                    var iter = so.GetIterator();
                    bool enterChildren = true;
                    while (iter.NextVisible(enterChildren))
                    {
                        enterChildren = false;
                        if (iter.name == "m_Script") continue;
                        if (string.Equals(iter.name, fieldName, StringComparison.OrdinalIgnoreCase))
                        {
                            sp = so.FindProperty(iter.propertyPath);
                            break;
                        }
                    }
                }
                if (sp == null) return null;

                if (sp.propertyType != SerializedPropertyType.ObjectReference)
                {
                    return ToolUtils.CreateErrorResponse(
                        $"Serialized field '{sp.name}' on '{componentType}' is type {sp.propertyType}, not an object reference. " +
                        $"Use set_component_property or set_script_component_property for non-reference fields.");
                }

                Undo.RecordObject(targetComp, $"Set Reference: {targetGameObjectPath}.{componentType}.{sp.name}");
                sp.objectReferenceValue = sourceReference;
                so.ApplyModifiedProperties();

                // Verify the assignment stuck — Unity silently drops refs whose type
                // the serialized field rejects (e.g. assigning a Light to a Rigidbody slot).
                if (sp.objectReferenceValue != sourceReference && sourceReference != null)
                {
                    // Retry with the GameObject itself or a looked-up component on the source.
                    var go = sourceReference as UnityEngine.GameObject ?? sourceObj;
                    sp.objectReferenceValue = go;
                    so.ApplyModifiedProperties();
                    if (sp.objectReferenceValue == null)
                    {
                        return ToolUtils.CreateErrorResponse(
                            $"Unity rejected the reference assignment to serialized field '{sp.name}' on '{componentType}' — " +
                            $"source '{sourceGameObjectPath}' ({assignedType}) is not compatible with the field's declared type.");
                    }
                    assignedType = "GameObject";
                }
                EditorUtility.SetDirty(targetComp);

                return ToolUtils.CreateSuccessResponse(
                    $"Set serialized reference '{sp.name}' on '{componentType}' of '{targetGameObjectPath}' to {assignedType} of '{sourceGameObjectPath}'");
            }
            catch (Exception e)
            {
                return ToolUtils.CreateErrorResponse($"Failed to set reference via SerializedObject: {e.Message}");
            }
        }
    }
}
