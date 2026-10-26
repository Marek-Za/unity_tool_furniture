using UnityEngine;

[CreateAssetMenu(fileName = "NewFurnitureCategory", menuName = "DP/Furniture Category")]
public class FurnitureCategorySO : ScriptableObject {
    [Tooltip("Optional description of this category.")]
    [TextArea]
    public string description;
}