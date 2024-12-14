using System;
using UnityEngine;
using System.Text;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Behaviour = BehaviorDesigner.Runtime.Behavior;

public static class REUnityBehaviourDesignerHelper
{
    static readonly Type behaviorTreeType;
    static readonly Type invokeMethodType;
    static readonly Dictionary<int, string> indentsCache;

    static REUnityBehaviourDesignerHelper()
    {
        indentsCache = new Dictionary<int, string>();
        behaviorTreeType = Type.GetType("BehaviorDesigner.Runtime.BehaviorTree, Assembly-CSharp");
        invokeMethodType = Type.GetType("BehaviorDesigner.Runtime.Tasks.InvokeMethod, Assembly-CSharp");
    }

    public class InvokeMethodProxy
    {
        const int MAX_PARAMETERS = 4;

        static readonly REReflectionHelper.FieldInfo<Task, SharedVariable<GameObject>> targetGameObjectField;
        static readonly REReflectionHelper.FieldInfo<Task, SharedVariable<string>> componentNameField;
        static readonly REReflectionHelper.FieldInfo<Task, SharedVariable<string>> methodNameField;
        static readonly REReflectionHelper.FieldInfo<Task, SharedVariable>[] parameterFields;

        static InvokeMethodProxy()
        {
            var invokeMethodType = Type.GetType("BehaviorDesigner.Runtime.Tasks.InvokeMethod, Assembly-CSharp");

            targetGameObjectField = REReflectionHelper.GetFieldInfo<Task, SharedVariable<GameObject>>(invokeMethodType, "targetGameObject", REReflectionHelper.EzBindingFlags.PublicInternal_Instance);
            componentNameField = REReflectionHelper.GetFieldInfo<Task, SharedVariable<string>>(invokeMethodType, "componentName", REReflectionHelper.EzBindingFlags.PublicInternal_Instance);
            methodNameField = REReflectionHelper.GetFieldInfo<Task, SharedVariable<string>>(invokeMethodType, "methodName", REReflectionHelper.EzBindingFlags.PublicInternal_Instance);

            parameterFields = new REReflectionHelper.FieldInfo<Task, SharedVariable>[4];
            for (int index = 0; index < MAX_PARAMETERS; index++)
            {
                string paramName = $"parameter{index + 1}";
                parameterFields[index] = REReflectionHelper.GetFieldInfo<Task, SharedVariable>(invokeMethodType, paramName, REReflectionHelper.EzBindingFlags.PublicInternal_Instance);
            }
        }

        readonly Task invokeMethod;

        public InvokeMethodProxy(Task invokeMethod)
        {
            if (invokeMethod == null)
            {
                throw new ArgumentNullException(nameof(invokeMethod));
            }
            this.invokeMethod = invokeMethod;
        }

        public SharedVariable<GameObject> TargetGameObject
        {
            get => targetGameObjectField.GetValue(invokeMethod);
            set => targetGameObjectField.SetValue(invokeMethod, value);
        }

        public SharedVariable<string> ComponentName
        {
            get => componentNameField.GetValue(invokeMethod);
            set => componentNameField.SetValue(invokeMethod, value);
        }

        public SharedVariable<string> MethodName
        {
            get => methodNameField.GetValue(invokeMethod);
            set => methodNameField.SetValue(invokeMethod, value);
        }

        public SharedVariable GetParameter(int index)
        {
            if (index < 0 || index >= MAX_PARAMETERS)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return parameterFields[index].GetValue(invokeMethod);
        }

        public void SetParameter(int index, SharedVariable value)
        {
            if (index < 0 || index >= MAX_PARAMETERS)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            parameterFields[index].SetValue(invokeMethod, value);
        }
    }

    public static Behaviour GetBehaviourTree(GameObject gameObject)
    {
        var behaviourTree = (Behaviour)gameObject.GetComponent(behaviorTreeType);
        return behaviourTree;
    }

    public static InvokeMethodProxy GetInvokeMethodTask(Behavior behaviourTree, params int[] taskIndices)
    {
        var task = GetBehaviourTask(behaviourTree, taskIndices);
        return new InvokeMethodProxy(task);
    }

    public static Task GetBehaviourTask(Behavior behaviourTree, params int[] taskIndices)
    {
        var behaviourSource = behaviourTree.GetBehaviorSource();
        behaviourSource.CheckForSerialization(false, null);

        var rootTask = behaviourSource.RootTask;
        if (rootTask == null)
        {
            RELogger.Log($"Error: RootTask is null!");
            return null;
        }

        var currTask = rootTask;
        for (int depth = 0; depth < taskIndices.Length; depth++)
        {
            var parentTask = currTask as ParentTask;
            if (parentTask == null)
            {
                RELogger.Log($"Error: Task at depth {depth} is not traversable! - Path: {BuildTaskPath(rootTask, taskIndices, depth)}");
                return null;
            }

            var taskIndex = taskIndices[depth];
            var children = parentTask.Children;
            if (taskIndex < 0 || taskIndex >= children.Count)
            {
                RELogger.Log($"Error: Invalid path index {taskIndex} at depth {depth}! - Path: {BuildTaskPath(rootTask, taskIndices, depth)}");
                return null;
            }
            currTask = children[taskIndex];
        }
        return currTask;
    }

    private static string BuildTaskPath(Task rootTask, int[] taskIndices, int depth)
    {
        var currTask = rootTask;
        var path = new List<string>()
        {
            $"{currTask.FriendlyName} [{currTask.GetType().Name}]"
        };
        for (int index = 0; index < depth; index++)
        {
            var taskIndex = taskIndices[index];
            var parentTask = (ParentTask)currTask;
            currTask = parentTask.Children[taskIndex];
            path.Add($"{currTask.FriendlyName} [{currTask.GetType().Name}]");
        }
        return string.Join(" => ", path);
    }

    public static void DumpInvokeMethod(InvokeMethodProxy invokeMethod)
    {
        RELogger.Log("=== InvokeMethod Dump ===");
        
        RELogger.Log($"Target GameObject: {invokeMethod.TargetGameObject?.Value?.name ?? "<null>"}");
        RELogger.Log($"Component Name: {invokeMethod.ComponentName?.Value ?? "<null>"}");
        RELogger.Log($"Method Name: {invokeMethod.MethodName?.Value ?? "<null>"}");

        RELogger.Log($"Parameter 1 = {invokeMethod.GetParameter(0)?.GetValue() ?? "<null>"}");
        RELogger.Log($"Parameter 2 = {invokeMethod.GetParameter(1)?.GetValue() ?? "<null>"}");
        RELogger.Log($"Parameter 3 = {invokeMethod.GetParameter(2)?.GetValue() ?? "<null>"}");
        RELogger.Log($"Parameter 4 = {invokeMethod.GetParameter(3)?.GetValue() ?? "<null>"}");

        RELogger.Log("============================");
    }

    public static void DumpBehaviourTree(GameObject gameObject)
    {
        var behaviourTree = GetBehaviourTree(gameObject);
        if (behaviourTree == null)
        {
            return;
        }

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"\"{gameObject.name}\" has the following Behaviour Tree:");
        stringBuilder.AppendLine($"Name: {behaviourTree.BehaviorName}");
        stringBuilder.AppendLine($"Description: {behaviourTree.BehaviorDescription}");

        var behaviourSource = behaviourTree.GetBehaviorSource();
        var externalBehaviourType = behaviourTree.ExternalBehavior?.GetType();
        stringBuilder.AppendLine($"ExternalBehaviour: {externalBehaviourType?.ToString() ?? "<null>"}");

        var variables = behaviourSource.GetAllVariables();
        stringBuilder.AppendLine($"Variables ({variables.Count}): ");
        foreach (var variable in variables)
        {
            stringBuilder.AppendLine($"- {variable.Name} = {variable.GetValue()}");
        }

        DumpTask(behaviourSource.RootTask, 0, stringBuilder);

        RELogger.LogOnce(stringBuilder.ToString(), $"{gameObject.name}_behaviourTree");
    }

    private static void DumpTask(Task task, int level, StringBuilder stringBuilder)
    {
        if (task == null)
        {
            return;
        }

        var indent = GetOrCreateIndent(level);
        stringBuilder.AppendLine($"{indent}{task.FriendlyName} [{task.GetType().FullName}]");
        if (task is ParentTask parentTask)
        {
            DumpTasks(parentTask.Children, level + 1, stringBuilder);
        }
    }

    private static string GetOrCreateIndent(int level)
    {
        if (!indentsCache.TryGetValue(level, out string indent))
        {
            indent = new string('-', level * 2);
            indentsCache.Add(level, indent);
        }
        return indent;
    }

    private static void DumpTasks(List<Task> tasks, int level, StringBuilder stringBuilder)
    {
        if (tasks == null)
        {
            return;
        }
        foreach	(var task in tasks)
        {
            DumpTask(task, level, stringBuilder);
        }
    }
}
