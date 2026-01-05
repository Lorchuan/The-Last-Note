#if UNITY_EDITOR // <--- 宏定义的开始
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// =================================================
// 剧情编辑器窗口
// =================================================
public class DialogueEditorWindow : EditorWindow
{
    private DialogueSystem dialogueSystem;
    private Vector2 scrollPosition;

    [MenuItem("VR魔法游戏/剧情编辑器")]
    public static void ShowWindow()
    {
        GetWindow<DialogueEditorWindow>("剧情编辑器");
    }
#if UNITY_EDITOR
    void OnGUI()
    {
        GUILayout.Label("剧情对话编辑器", EditorStyles.boldLabel);

        if (dialogueSystem == null)
        {
            dialogueSystem = FindObjectOfType<DialogueSystem>();
            if (dialogueSystem == null)
            {
                GUILayout.Label("未找到DialogueSystem组件");
                return;
            }
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        for (int i = 0; i < dialogueSystem.dialogueSequences.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");
            DialogueSequence sequence = dialogueSystem.dialogueSequences[i];

            EditorGUILayout.LabelField($"序列 {i}: {sequence.sequenceId}", EditorStyles.boldLabel);

            sequence.sequenceId = EditorGUILayout.TextField("序列ID", sequence.sequenceId);

            EditorGUILayout.BeginHorizontal();
            sequence.nextSequenceId = EditorGUILayout.TextField("下一序列ID", sequence.nextSequenceId);
            sequence.autoPlayNext = EditorGUILayout.ToggleLeft("自动跳转下一条", sequence.autoPlayNext, GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("对话内容:");

            for (int j = 0; j < sequence.lines.Count; j++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"句{j + 1}", GUILayout.Width(30));
                sequence.lines[j].characterName = EditorGUILayout.TextField(sequence.lines[j].characterName, GUILayout.Width(80));
                sequence.lines[j].dialogueText = EditorGUILayout.TextArea(sequence.lines[j].dialogueText);

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    sequence.lines.RemoveAt(j);
                    j--;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ 添加对话行"))
            {
                sequence.lines.Add(new DialogueLine("角色名", "对话内容"));
            }

            if (GUILayout.Button("删除此序列"))
            {
                dialogueSystem.dialogueSequences.RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("创建新序列"))
        {
            DialogueSequence newSeq = new DialogueSequence();
            newSeq.sequenceId = "new_sequence";
            dialogueSystem.dialogueSequences.Add(newSeq);
        }

        if (GUILayout.Button("保存更改"))
        {
            EditorUtility.SetDirty(dialogueSystem);
        }
    }
#endif
}
// =================================================
// 任务编辑器窗口
// =================================================
public class TaskEditorWindow : EditorWindow
{
    private TaskDisplayUI taskDisplayUI;
    private Vector2 scrollPosition;

    [MenuItem("VR魔法游戏/任务编辑器")]
    public static void ShowWindow()
    {
        GetWindow<TaskEditorWindow>("任务编辑器");
    }

    void OnGUI()
    {
        GUILayout.Label("任务编辑器 (升级版)", EditorStyles.boldLabel);

        if (taskDisplayUI == null)
        {
            taskDisplayUI = FindObjectOfType<TaskDisplayUI>();
            if (taskDisplayUI == null)
            {
                GUILayout.Label("未找到TaskDisplayUI组件");
                return;
            }
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        List<Task> tasks = taskDisplayUI.GetTasks();

        for (int i = 0; i < tasks.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");
            Task task = tasks[i];

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"任务 {i + 1}", EditorStyles.boldLabel, GUILayout.Width(60));
            task.taskType = (TaskType)EditorGUILayout.EnumPopup(task.taskType);
            EditorGUILayout.EndHorizontal();

            task.taskId = EditorGUILayout.TextField("ID", task.taskId);
            task.taskName = EditorGUILayout.TextField("名称", task.taskName);
            task.description = EditorGUILayout.TextField("描述", task.description);
            task.requiredProgress = EditorGUILayout.IntField("目标进度", task.requiredProgress);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("类型特定设置:", EditorStyles.boldLabel);

            switch (task.taskType)
            {
                case TaskType.Movement:
                    GUI.backgroundColor = Color.cyan;
                    EditorGUILayout.BeginVertical("helpBox");
                    task.targetPosition = EditorGUILayout.Vector3Field("目标坐标 (光柱位置)", task.targetPosition);
                    task.detectRadius = EditorGUILayout.FloatField("判定半径", task.detectRadius);

                    if (GUILayout.Button("使用当前Scene相机位置"))
                    {
                        if (SceneView.lastActiveSceneView != null)
                        {
                            Vector3 camPos = SceneView.lastActiveSceneView.camera.transform.position;
                            RaycastHit hit;
                            if (Physics.Raycast(camPos, Vector3.down, out hit, 100f))
                                task.targetPosition = hit.point;
                            else
                                task.targetPosition = new Vector3(camPos.x, 0, camPos.z);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    GUI.backgroundColor = Color.white;
                    break;

                case TaskType.SpellCast:
                    GUI.backgroundColor = Color.yellow;
                    EditorGUILayout.BeginVertical("helpBox");
                    task.targetSpellId = EditorGUILayout.IntField("目标技能ID", task.targetSpellId);
                    EditorGUILayout.HelpBox("技能ID参考: 0=火球, 1=闪电, 2=火爆, 6=传送", MessageType.Info);
                    EditorGUILayout.EndVertical();
                    GUI.backgroundColor = Color.white;
                    break;

                case TaskType.Generic:
                default:
                    EditorGUILayout.HelpBox("通用任务：需通过代码调用 UpdateTaskProgress 更新进度", MessageType.None);
                    break;
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            task.currentProgress = EditorGUILayout.IntField("当前进度", task.currentProgress);
            task.isActive = EditorGUILayout.Toggle("激活中", task.isActive);
            task.isCompleted = EditorGUILayout.Toggle("已完成", task.isCompleted);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("删除此任务"))
            {
                taskDisplayUI.GetTasks().RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("添加新任务"))
        {
            taskDisplayUI.AddTask("new_task", "新任务", "描述", 1);
        }

        if (GUILayout.Button("保存所有更改"))
        {
            EditorUtility.SetDirty(taskDisplayUI);
            Debug.Log("任务数据已保存");
        }
    }
}
#endif // <--- 移到了文件的最后一行！