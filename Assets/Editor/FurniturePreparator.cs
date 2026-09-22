using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class FurniturePreparator : EditorWindow {
    // List of raw 3D models dragged in by the user
    public List<GameObject> rawModels = new List<GameObject>();

    // Default export path
    private string exportPath = "Assets/FurniturePrefabs";

    [MenuItem("DP/Furniture Preparator")]
    public static void ShowWindow() {
        GetWindow<FurniturePreparator>("Furniture Preparator");
    }

    private void OnGUI() {
        GUILayout.Label("Furniture Asset Preparator", EditorStyles.boldLabel);
        GUILayout.Space(5);

        exportPath = EditorGUILayout.TextField("Export Path", exportPath);
        GUILayout.Space(10);

        // --- Drag and Drop Area ---
        GUIStyle dropAreaStyle = new GUIStyle(GUI.skin.box) {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        GUILayout.Box("Drag and Drop 3D Models Here", dropAreaStyle, GUILayout.ExpandWidth(true), GUILayout.Height(60));
        HandleDragAndDrop(GUILayoutUtility.GetLastRect());

        GUILayout.Space(10);

        // Display the List in the EditorWindow
        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty modelsProperty = so.FindProperty("rawModels");
        EditorGUILayout.PropertyField(modelsProperty, true);
        so.ApplyModifiedProperties();

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear List", GUILayout.Height(30))) {
            rawModels.Clear();
        }

        if (GUILayout.Button("Prepare and Save Prefabs", GUILayout.Height(30))) {
            ProcessModels();
        }
        GUILayout.EndHorizontal();
    }

    // Handles the logic for dragging and dropping assets into the specific Rect area
    private void HandleDragAndDrop(Rect dropArea) {
        Event currentEvent = Event.current;

        if (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform) {
            if (!dropArea.Contains(currentEvent.mousePosition))
                return;

            // Change cursor to indicate a valid drop zone
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (currentEvent.type == EventType.DragPerform) {
                DragAndDrop.AcceptDrag();

                foreach (Object draggedObject in DragAndDrop.objectReferences) {
                    // Only add GameObjects (models/prefabs) that are not already in the list
                    if (draggedObject is GameObject go && !rawModels.Contains(go)) {
                        rawModels.Add(go);
                    }
                }
                currentEvent.Use();
            }
        }
    }

    private void ProcessModels() {
        if (rawModels == null || rawModels.Count == 0) {
            Debug.LogWarning("Preparator: No models added to the list.");
            return;
        }

        // Create export directory if it does not exist
        if (!Directory.Exists(exportPath)) {
            try {
                Directory.CreateDirectory(exportPath);
                AssetDatabase.Refresh();
                Debug.Log($"Preparator: Created new directory at {exportPath}");
            }
            catch (System.Exception e) {
                Debug.LogError($"Preparator: Failed to create directory: {e.Message}");
                return;
            }
        }

        // Process each model
        foreach (GameObject model in rawModels) {
            if (model == null)
                continue;

            try {
                // Create a clean Root Object
                GameObject root = new GameObject(model.name + "_Prefab");
                // Ensure root is exactly at 0,0,0 during preparation
                root.transform.position = Vector3.zero;

                // Instantiate the raw graphics as a Child
                GameObject graphics = Instantiate(model, root.transform);
                graphics.name = model.name + "_Graphics";

                // Reset child transform to align with root initially
                graphics.transform.localPosition = Vector3.zero;
                graphics.transform.localRotation = Quaternion.identity;

                // Find all renderers to calculate exact physical bounds
                Renderer[] renderers = graphics.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) {
                    Debug.LogError($"Preparator: Model '{model.name}' has no Renderers. Skipping collider generation.");
                    DestroyImmediate(root);
                    continue;
                }

                // Combine bounds of all renderers
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer r in renderers) {
                    bounds.Encapsulate(r.bounds);
                }

                // --- PIVOT FIX LOGIC ---
                Vector3 bottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                graphics.transform.position -= bottomCenter;

                // Recalculate bounds after graphics offset
                bounds = renderers[0].bounds;
                foreach (Renderer r in renderers) {
                    bounds.Encapsulate(r.bounds);
                }

                // Convert global bounds to local space of the Root object
                Vector3 localCenter = root.transform.InverseTransformPoint(bounds.center);
                Vector3 localSize = bounds.size;

                // Add BoxCollider to Root
                BoxCollider boxCollider = root.AddComponent<BoxCollider>();
                boxCollider.center = localCenter;
                boxCollider.size = localSize;

                // Add FurnitureData to Root
                FurnitureData data = root.AddComponent<FurnitureData>();

                // Determine final file path
                string localPath = $"{exportPath}/{root.name}.prefab";
                localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

                // Save to project
                PrefabUtility.SaveAsPrefabAsset(root, localPath);
                Debug.Log($"Preparator: Successfully created prefab '{root.name}' at {localPath}");

                // Cleanup temporary object from the Scene
                DestroyImmediate(root);
            }
            catch (System.Exception e) {
                Debug.LogError($"Preparator: Critical error processing model '{model.name}': {e.Message}\n{e.StackTrace}");
            }
        }

        // Final refresh to ensure Project window updates
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}