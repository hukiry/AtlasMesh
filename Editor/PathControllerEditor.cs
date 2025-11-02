
// 在编辑器中显示工具栏
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PathController))]
public class PathControllerEditor : Editor
{
    PathController pathController;
    private void OnEnable()
    {
        pathController = target as PathController;
    }

    public override void OnInspectorGUI()
    {
        PathController controller = (PathController)target;

        DrawDefaultInspector();

        GUILayout.Space(10);

        // 添加按钮
        if (GUILayout.Button("添加路径点"))
        {
            Undo.RecordObject(controller, "添加路径点");
            Vector3 newPoint = controller.pathPoints.Count > 0
                ? controller.pathPoints[controller.pathPoints.Count - 1] + Vector3.forward * 5
                : controller.transform.position;
            controller.pathPoints.Add(newPoint);
            controller.selectedPointIndex = controller.pathPoints.Count - 1;
            controller.isDragging = false;
            EditorUtility.SetDirty(controller);
        }

        // 删除按钮
        if (GUILayout.Button("删除选中点") && controller.selectedPointIndex != -1)
        {
            this.RemoveSelectedPoint();
        }

        // 清空按钮
        if (GUILayout.Button("清空所有点") && controller.pathPoints.Count > 0)
        {
            if (EditorUtility.DisplayDialog("确认", "确定要清空所有路径点吗？", "是", "否"))
            {
                Undo.RecordObject(controller, "清空路径点");
                controller.pathPoints.Clear();
                controller.selectedPointIndex = -1;
                controller.isDragging = false;
                EditorUtility.SetDirty(controller);
            }
        }
    }

    // 场景视图中的交互
    private void OnSceneGUI()
    {
        // 处理路径点的选择和移动
        for (int i = 0; i < pathController.pathPoints.Count; i++)
        {
            // 根据状态设置不同颜色
            if (i == pathController.selectedPointIndex && pathController.isDragging)
            {
                Handles.color = pathController.draggingColor;
            }
            else if (i == pathController.selectedPointIndex)
            {
                Handles.color = pathController.selectedColor;
            }
            else
            {
                Handles.color = pathController.normalColor;
            }

            // 创建可拖动的位置手柄
            //var fmh_79_17_638958142474068499 = Quaternion.identity; 

            var fmh_81_17_638972619421699770 = Quaternion.identity; Vector3 newPosition = Handles.FreeMoveHandle(
                pathController.pathPoints[i],
                pathController.handleSize,
                Vector3.one * 0.1f,
                Handles.SphereHandleCap
            );

            // 如果位置发生变化，更新路径点
            if (newPosition != pathController.pathPoints[i])
            {
                pathController.selectedPointIndex = i;
                Undo.RecordObject(this, "移动路径点");
                pathController.pathPoints[i] = newPosition;
                pathController.isDragging = true;
                EditorUtility.SetDirty(this);
            }
            else if (i == pathController.selectedPointIndex && Event.current.type == EventType.MouseUp)
            {
                pathController.selectedPointIndex = -1;
                // 当鼠标释放时，结束拖动状态
                pathController.isDragging = false;
            }

            // 处理点击选择
            if (Handles.Button(pathController.pathPoints[i] - Vector3.right * 0.5f, Quaternion.identity, pathController.handleSize / 5, pathController.handleSize / 5, Handles.DotHandleCap))
            {
                pathController.selectedPointIndex = i;
                pathController.isDragging = false; // 重置拖动状态
            }
        }

        // 为选中的点显示箭头手柄（方向控制）

        if (pathController.selectedPointIndex >= 0)
            ShowDirectionHandle(pathController.selectedPointIndex);



        Event evt = Event.current;        // 处理鼠标离开场景视图的情况
        if (evt.type == EventType.MouseLeaveWindow)
        {
            pathController.isDragging = false;
        }
    }

    // 显示方向控制手柄
    private void ShowDirectionHandle(int index)
    {
        Vector3 currentPoint = pathController.pathPoints[index];
        Handles.color = pathController.directionColor;
        float arrowSize = 0.8f;
        // 绘制箭头
        Handles.ArrowHandleCap(0, currentPoint, Quaternion.LookRotation(Vector3.up), arrowSize, EventType.Repaint);
        Handles.color = Color.red;
        Handles.ArrowHandleCap(1, currentPoint, Quaternion.LookRotation(Vector3.right), arrowSize, EventType.Repaint);
    }

    // 添加路径点
    private void AddPathPoint(object positionObj)
    {
        Vector3 position = (Vector3)positionObj;
        Undo.RecordObject(this, "添加路径点");
        pathController.pathPoints.Add(position);
        pathController.selectedPointIndex = pathController.pathPoints.Count - 1;
        pathController.isDragging = false;
        EditorUtility.SetDirty(this);
    }

    // 删除选中的路径点
    public void RemoveSelectedPoint()
    {
        if (pathController.selectedPointIndex >= 0 && pathController.selectedPointIndex < pathController.pathPoints.Count)
        {
            Undo.RecordObject(this, "删除路径点");
            pathController.pathPoints.RemoveAt(pathController.selectedPointIndex);
            pathController.selectedPointIndex = Mathf.Clamp(pathController.selectedPointIndex - 1, -1, pathController.pathPoints.Count - 1);
            pathController.isDragging = false;
            EditorUtility.SetDirty(this);
        }
    }
}