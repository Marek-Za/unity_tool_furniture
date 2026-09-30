using UnityEngine;
using UnityEditor;

// This tells Unity to use this custom inspector for FurnitureCategoryDatabaseSO objects
[CustomEditor(typeof(FurnitureCategoryDatabaseSO))]
public class FurnitureCategoryDatabaseEditor : Editor {

    public override void OnInspectorGUI() {
        // Draw the default list of strings
        DrawDefaultInspector();

        FurnitureCategoryDatabaseSO database = (FurnitureCategoryDatabaseSO)target;

        GUILayout.Space(20);

        // Create the generation button
        if (GUILayout.Button("Generate Category Assets From Database", GUILayout.Height(40))) {
            GenerateCategories(database);
        }
    }

    private void GenerateCategories(FurnitureCategoryDatabaseSO database) {
        string folderPath = "Assets/FurnitureCategories";

        // Create the folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(folderPath)) {
            AssetDatabase.CreateFolder("Assets", "FurnitureCategories");
        }

        int createdCount = 0;

        // Iterate through the names and create missing assets
        foreach (string catName in database.categoryNames) {
            if (string.IsNullOrWhiteSpace(catName))
                continue;

            string safeName = catName.Trim();
            string assetPath = $"{folderPath}/{safeName}.asset";

            // Check if the asset already exists to prevent overwriting
            FurnitureCategorySO existingAsset = AssetDatabase.LoadAssetAtPath<FurnitureCategorySO>(assetPath);

            if (existingAsset == null) {
                // Create the actual ScriptableObject instance in memory
                FurnitureCategorySO newCategory = ScriptableObject.CreateInstance<FurnitureCategorySO>();

                // Save it physically to the disk
                AssetDatabase.CreateAsset(newCategory, assetPath);
                createdCount++;
            }
        }

        // Force Unity to save and refresh the Project window
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Category Generation Complete! Created {createdCount} new category files.");
    }
}