using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class FurniturePreparator : EditorWindow {
    // List of raw 3D models dragged in by the user
    public List<GameObject> rawModels = new List<GameObject>();

    // Unified path matching the rest of the project structure
    private string exportPath = "Assets/LayoutGenerator/FurniturePrefabs";
    private string categoryPath = "Assets/LayoutGenerator/FurnitureCategories";

    // Flag to show completion warning message in the GUI
    private bool showCompletionMessage = false;

    [MenuItem("DP/Furniture Preparator")]
    public static void ShowWindow() {
        GetWindow<FurniturePreparator>("Furniture Preparator");
    }

    private void OnGUI() {
        //Category Setup Guidance
        #region Category_Setup_Guidance
        GUILayout.Label("Category Configuration", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("It is highly recommended to establish categories before generating furniture assets.", MessageType.Info);

        if (GUILayout.Button("Find and Open Category Database", GUILayout.Height(30))) {
            PingCategoryDatabase();
        }
        #endregion

        GUILayout.Space(20);

        GUILayout.Label("Furniture Asset Preparator", EditorStyles.boldLabel);
        GUILayout.Space(5);

        exportPath = EditorGUILayout.TextField("Export Path", exportPath);
        GUILayout.Space(10);

        //Drag and Drop Area
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
            showCompletionMessage = false;
        }

        if (GUILayout.Button("Prepare and Save Prefabs", GUILayout.Height(30))) {
            ProcessModels();
        }
        GUILayout.EndHorizontal();

        // Display completion message directly in the tool UI
        if (showCompletionMessage) {
            GUILayout.Space(15);
            EditorGUILayout.HelpBox("Preparation process finished. Automatic categorization might encounter ambiguous names. Manual verification of the generated prefabs is recommended", MessageType.Warning);
        }
    }

    private void PingCategoryDatabase() {
        // Locate the database object automatically in the project folder
        string[] guids = AssetDatabase.FindAssets("t:FurnitureCategoryDatabaseSO");
        if (guids.Length > 0) {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            Object databaseAsset = AssetDatabase.LoadAssetAtPath<Object>(path);

            // Highlight and display the asset in the Inspector
            Selection.activeObject = databaseAsset;
            EditorGUIUtility.PingObject(databaseAsset);
        }
        else {
            Debug.LogWarning("Preparator: No FurnitureCategoryDatabaseSO found in the project. Please create one.");
        }
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
        showCompletionMessage = false;

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

                // Attempt to auto-assign a category based on the model name
                AutoAssignCategory(data, model.name);

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

        // Trigger the GUI message and log final instruction
        showCompletionMessage = true;
        Debug.Log("Preparator: Batch processing finished. Please review the categories manually.");
    }

    private void AutoAssignCategory(FurnitureData data, string modelName) {
        if (!Directory.Exists(categoryPath))
            return;

        // Search for all category assets in the specified folder
        string[] assetGuids = AssetDatabase.FindAssets("t:FurnitureCategorySO", new[] { categoryPath });

        string lowerModelName = modelName.ToLower();
        List<FurnitureCategorySO> matchedCategories = new List<FurnitureCategorySO>();

        // Gather all categories that match the model name
        foreach (string guid in assetGuids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            FurnitureCategorySO category = AssetDatabase.LoadAssetAtPath<FurnitureCategorySO>(path);

            // Exclude Undefined from matching logic to prevent false positives
            if (category != null && category.name != "Undefined" && lowerModelName.Contains(category.name.ToLower())) {
                matchedCategories.Add(category);
            }
        }

        // Assign category based on the number of matches found
        if (matchedCategories.Count == 1) {
            data.category = matchedCategories[0];
            Debug.Log($"Preparator: Auto-assigned category '{matchedCategories[0].name}' to model '{modelName}'.");
        }
        else {
            string undefinedPath = $"{categoryPath}/Undefined.asset";
            FurnitureCategorySO undefinedCategory = AssetDatabase.LoadAssetAtPath<FurnitureCategorySO>(undefinedPath);

            if (undefinedCategory != null) {
                data.category = undefinedCategory;

                if (matchedCategories.Count > 1) {
                    Debug.LogWarning($"Preparator: Model '{modelName}' matched multiple categories ({matchedCategories.Count}). Assigned 'Undefined' to prevent conflicts.");
                }
                else {
                    Debug.Log($"Preparator: Assigned default category 'Undefined' to model '{modelName}'.");
                }
            }
            else {
                Debug.LogWarning($"Preparator: Default category 'Undefined' not found at {undefinedPath}. Please run the Category Generator.");
            }
        }
    }
}