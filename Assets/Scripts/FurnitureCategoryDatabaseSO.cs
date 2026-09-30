using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "FurnitureCategoryDatabase", menuName = "DP/Furniture Category Database")]
public class FurnitureCategoryDatabaseSO : ScriptableObject {
    [Header("List of all required categories")]
    [Tooltip("Add category names here. The Editor script will generate .asset files for them.")]
    public List<string> categoryNames = new List<string> {
        "Armchair",
        "Bed",
        "Bedside Table",
        "Bookcase",
        "Cabinet",
        "Coffee Table",
        "Couch",
        "Desk",
        "Dining Chair",
        "Dining Table",
        "Lamp",
        "Office Chair",
        "TEST"
    };
}