using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(SonorizacionRestaurante))]
public sealed class SonorizacionRestauranteEditor : Editor
{
    private static readonly string[] PointFields =
    {
        "label", "audioEvent", "position", "volumeDb", "radius", "aimRadius", "trigger", "secondaryEvent", "sinkNumber"
    };

    private ReorderableList points;
    private bool refreshHeights;

    private void OnEnable()
    {
        BuildList();
        EditorApplication.update += UpdatePreview;
        EditorApplication.playModeStateChanged += StopPreviewOnModeChange;
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdatePreview;
        EditorApplication.playModeStateChanged -= StopPreviewOnModeChange;
        if (target is SonorizacionRestaurante controller && controller != null)
            controller.StopPointPreview();
    }

    private void UpdatePreview()
    {
        if (target is SonorizacionRestaurante controller && controller != null)
            controller.UpdateEditPreview();
    }

    private void StopPreviewOnModeChange(PlayModeStateChange change)
    {
        if ((change == PlayModeStateChange.ExitingEditMode || change == PlayModeStateChange.ExitingPlayMode) &&
            target is SonorizacionRestaurante controller && controller != null)
            controller.StopPointPreview();
    }

    private void BuildList()
    {
        var selectedIndex = points?.index ?? -1;
        points = new ReorderableList(serializedObject, serializedObject.FindProperty("points"),
            draggable: true, displayHeader: true, displayAddButton: true, displayRemoveButton: true);
        points.index = selectedIndex;
        points.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Points");
        points.elementHeightCallback = GetPointHeight;
        points.drawElementCallback = DrawPoint;
        points.onChangedCallback = _ => SceneView.RepaintAll();
        points.onSelectCallback = _ => SceneView.RepaintAll();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        if (refreshHeights && Event.current.type == EventType.Layout)
        {
            BuildList();
            refreshHeights = false;
        }

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        EditorGUILayout.LabelField("Volumen de otros audios (dB)", EditorStyles.boldLabel);
        var hasDiningPoint = false;
        for (var index = 0; index < points.serializedProperty.arraySize; index++)
        {
            var point = points.serializedProperty.GetArrayElementAtIndex(index);
            if (point.FindPropertyRelative("audioEvent").stringValue == "AmbienteRestaurante")
            {
                hasDiningPoint = true;
                break;
            }
        }
        if (!hasDiningPoint)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("restaurantVolumeDb"), new GUIContent("Ambiente restaurante"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("orderVolumeDb"), new GUIContent("Aviso de nuevo pedido"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("driveThruVolumeDb"), new GUIContent("Aviso de drive thru"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("layeredFryingVolumeDb"), new GUIContent("Fritura en capas"));
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Transición de música", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("distanciaFundidoMusical"),
            new GUIContent("Distancia del fundido"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("segundosFundidoMusical"),
            new GUIContent("Tiempo de fundido (s)"));
        points.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }

    private float GetPointHeight(int index)
    {
        var point = points.serializedProperty.GetArrayElementAtIndex(index);
        var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
        if (!point.isExpanded)
            return height;

        foreach (var field in PointFields)
            height += EditorGUI.GetPropertyHeight(point.FindPropertyRelative(field)) +
                EditorGUIUtility.standardVerticalSpacing;

        return height + 2 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
    }

    private void DrawPoint(Rect rect, int index, bool isActive, bool isFocused)
    {
        var point = points.serializedProperty.GetArrayElementAtIndex(index);
        var line = EditorGUIUtility.singleLineHeight;
        var spacing = EditorGUIUtility.standardVerticalSpacing;
        rect.y += spacing;
        rect.height = line;
        var wasExpanded = point.isExpanded;
        point.isExpanded = EditorGUI.Foldout(rect, wasExpanded,
            point.FindPropertyRelative("label").stringValue, toggleOnLabelClick: true);
        if (point.isExpanded != wasExpanded)
        {
            refreshHeights = true;
            Repaint();
            return;
        }

        if (!point.isExpanded)
            return;

        using (new EditorGUI.IndentLevelScope())
        {
            rect.y += line + spacing;
            foreach (var field in PointFields)
            {
                var property = point.FindPropertyRelative(field);
                rect.height = EditorGUI.GetPropertyHeight(property);
                EditorGUI.PropertyField(rect, property);
                rect.y += rect.height + spacing;

                if (field == "position")
                {
                    rect.height = line;
                    using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                    {
                        if (GUI.Button(EditorGUI.IndentedRect(rect), "Mover en Scene"))
                        {
                            points.index = index;
                            var sceneView = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
                            sceneView.Focus();
                            sceneView.Frame(new Bounds(property.vector3Value, Vector3.one * 100f), false);
                            sceneView.Repaint();
                        }
                    }
                    rect.y += line + spacing;
                }
            }

            rect.height = line;
            var buttons = EditorGUI.IndentedRect(rect);
            var width = (buttons.width - spacing * 2f) / 3f;
            buttons.width = width;
            var controller = (SonorizacionRestaurante)target;
            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling))
            {
                if (GUI.Button(buttons, "Probar audio"))
                {
                    serializedObject.ApplyModifiedProperties();
                    controller.PreviewPoint(index, false);
                }
                buttons.x += width + spacing;
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(point.FindPropertyRelative("secondaryEvent").stringValue)))
                {
                    if (GUI.Button(buttons, "Probar secundario"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        controller.PreviewPoint(index, true);
                    }
                }
                buttons.x += width + spacing;
                if (GUI.Button(buttons, "Detener prueba"))
                    controller.StopPointPreview();
            }
        }
    }

    private void OnSceneGUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        serializedObject.Update();
        if (points.index < 0 || points.index >= points.serializedProperty.arraySize)
            return;

        var position = points.serializedProperty.GetArrayElementAtIndex(points.index)
            .FindPropertyRelative("position");
        EditorGUI.BeginChangeCheck();
        var moved = Handles.PositionHandle(position.vector3Value, Quaternion.identity);
        if (!EditorGUI.EndChangeCheck())
            return;

        position.vector3Value = moved;
        serializedObject.ApplyModifiedProperties();
    }
}
