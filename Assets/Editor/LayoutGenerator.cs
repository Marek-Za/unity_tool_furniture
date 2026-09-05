using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class LayoutGenerator : EditorWindow
{
    // Dimensions of our room (floor)
    
    private string roomName = "WIP Room";
    private GameObject currentlyWorkingOn;
    private Vector3 roomSize = Vector3.one;
    private Vector3 roomCenter = Vector3.zero + new Vector3(0, 1, 0);
    private GameObject parentFolderObject;
    private bool isVisualizationOn = false;

    #region Default values
    private string de_roomName = "WIP Room";
    private Vector3 de_roomSize = Vector3.one;
    private Vector3 de_roomCenter = Vector3.zero + new Vector3(0, 1, 0);
    #endregion

    // Location of this tool in Unity Editor menu
    [MenuItem("DP/LayoutGenerator")] 
    public static void ShowWindow() {
        // Creates the window or focuses it if it's already open
        GetWindow<LayoutGenerator>("Layout Generator");
    }

    private void Update() {
        if (isVisualizationOn && currentlyWorkingOn != null) {
            if (currentlyWorkingOn.transform.hasChanged) {
                roomCenter = currentlyWorkingOn.transform.position;
                roomSize = currentlyWorkingOn.transform.localScale;
                currentlyWorkingOn.transform.hasChanged = false;
                Repaint();
            }
        }
    }

    // Definition of the GUI window: label, button, slider, text field, int field, object field, etc
    // Renders the GUI window
    private void OnGUI() {
        if (isVisualizationOn && currentlyWorkingOn == null) {
            isVisualizationOn = false;
        }

        // Display a bold label for the section
        GUILayout.Label("Procedural Room Settings", EditorStyles.boldLabel);

        // Create a field to let the designer change the room size directly in the window
        #region WorkinOn
        if (currentlyWorkingOn != null) {
            GUILayout.BeginHorizontal();
            currentlyWorkingOn = EditorGUILayout.ObjectField("Working on", currentlyWorkingOn, typeof(GameObject), allowSceneObjects: true) as GameObject;
            GUILayout.Space(10);
            if (GUILayout.Button("End Work", GUILayout.MaxWidth(70))) {
                roomName = de_roomName;
                roomSize = de_roomSize;
                roomCenter = de_roomCenter;
                isVisualizationOn = false;
                currentlyWorkingOn =null;
            }
                GUILayout.EndHorizontal();
        }
        #endregion


        #region roomName
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        roomName = EditorGUILayout.TextField("Room name", roomName);
        GUILayout.Space(10);
        if (GUILayout.Button("Reset", GUILayout.MaxWidth(70)))
            roomName = de_roomName;
        GUILayout.EndHorizontal();
        #endregion

        GUILayout.Space(8);

        #region Room Size / Scale
        GUILayout.BeginHorizontal();
        GUILayout.Label("Room size / Scale");
        GUILayout.Space(10);
        if (GUILayout.Button("Reset", GUILayout.MaxWidth(70)))
            roomSize = de_roomSize;
        GUILayout.EndHorizontal();

        float x = EditorGUILayout.FloatField("  Width  (X)", roomSize.x, GUILayout.MaxWidth(500));
        float y = EditorGUILayout.FloatField("  Height (Y)", roomSize.y, GUILayout.MaxWidth(500));
        float z = EditorGUILayout.FloatField("  Length (Z)", roomSize.z, GUILayout.MaxWidth(500));
        roomSize = new Vector3(x, y, z);
        GUILayout.Space(10);

        #endregion

        GUILayout.Space(8);

        #region roomCenter
        GUILayout.BeginHorizontal();
        roomCenter = EditorGUILayout.Vector3Field("Room center pivot", roomCenter);
        GUILayout.Space(10);
        if (GUILayout.Button("Reset", GUILayout.MaxWidth(70)))
            roomCenter = de_roomCenter;
        GUILayout.EndHorizontal();
        #endregion

        GUILayout.Space(20);

        parentFolderObject = EditorGUILayout.ObjectField("Parent of this furniture", parentFolderObject, typeof(GameObject),allowSceneObjects:true) as GameObject;

        

        #region Buttons
        if (!isVisualizationOn) {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Visualize size of the room",GUILayout.MinWidth(200))) {
                VisualizeSizeOfRoom();
            }
            EditorGUILayout.HelpBox("Will create a ghost of the room in which the furniture will be placed", MessageType.None);
            GUILayout.EndHorizontal();
        }

        // Button for automatic filling up the room with the ghost object
        if (isVisualizationOn) {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Let the ghost fill the room")) {
                GhostFillRoom();
            }
            EditorGUILayout.HelpBox("Ghost will fill up the room", MessageType.None);
            GUILayout.EndHorizontal();
        }

        // Create a button that triggers the generation logic when clicked
        if (GUILayout.Button("Generate Random Furniture")) {
            
            GenerateFurniture();
        }
        if (GUILayout.Button("Generate 1000 Random Furnitures")) {
            for (int i = 0; i < 1000; i++) {
                GenerateFurniture();
            }
        }

        if (currentlyWorkingOn != null) {
            GUILayout.Space(10);
            if (GUILayout.Button("Delete visualization of the room")) {
                DestroyImmediate(currentlyWorkingOn);
                currentlyWorkingOn = null;
                isVisualizationOn = false;
            }
        }
        #endregion

        if (GUI.changed) {
            if (isVisualizationOn)
                VisualizeSizeOfRoom();
        }
    }

    private void VisualizeSizeOfRoom() {
        if (currentlyWorkingOn == null) {
            GameObject room = GameObject.CreatePrimitive(PrimitiveType.Cube);
            room.name = roomName;
            room.transform.position = roomCenter;
            room.transform.localScale = roomSize;
            MeshRenderer mr = room.GetComponent<MeshRenderer>();
            mr.material = CreateGhostMaterial(room);
            currentlyWorkingOn = room;
        }
        else {
            currentlyWorkingOn.name = roomName;
            currentlyWorkingOn.transform.position = roomCenter;
            currentlyWorkingOn.transform.localScale = roomSize;
        }
        isVisualizationOn = true;
    }

    private void GhostFillRoom() {
        Vector3[] VertexNormals = currentlyWorkingOn.GetComponent<MeshFilter>().sharedMesh.normals;
        Vector3[] VertexNormals_after = new Vector3[VertexNormals.Length];
        int vertexCounter = 0;

        foreach (Vector3 vertex in VertexNormals) {
            bool knownVertex = false;
            foreach (Vector3 vector in VertexNormals_after) {
                if (vertex == vector) {
                    knownVertex = true;
                    break;
                }
            }
            if (!knownVertex) {
                VertexNormals_after[vertexCounter] = vertex;
                vertexCounter++;
            }
        }
        System.Array.Resize(ref VertexNormals_after, vertexCounter);

        // Extract both extents and the local center offset of the mesh to handle arbitrary pivot points
        Bounds meshBounds = currentlyWorkingOn.GetComponent<MeshFilter>().sharedMesh.bounds;
        Vector3 originalExtents = meshBounds.extents;
        Vector3 localCenter = meshBounds.center;

        // Store the original collider state and disable it to prevent self-intersection during raycasting
        Collider selfCollider = currentlyWorkingOn.GetComponent<Collider>();
        bool wasColliderEnabled = false;
        if (selfCollider != null) {
            wasColliderEnabled = selfCollider.enabled;
            selfCollider.enabled = false;
        }

        float distPosX = float.MaxValue;
        float distNegX = float.MaxValue;
        float distPosY = float.MaxValue;
        float distNegY = float.MaxValue;
        float distPosZ = float.MaxValue;
        float distNegZ = float.MaxValue;

        foreach (Vector3 vertex in VertexNormals_after) {
            Vector3 globalDirection = currentlyWorkingOn.transform.TransformDirection(vertex);
            Ray ray = new Ray(roomCenter, globalDirection);
            float maxDistanceForRay = 100f;

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistanceForRay)) {
                Vector3 offsetToHit = hit.point - roomCenter;

                float dotX = Vector3.Dot(offsetToHit, currentlyWorkingOn.transform.right);
                float dotY = Vector3.Dot(offsetToHit, currentlyWorkingOn.transform.up);
                float dotZ = Vector3.Dot(offsetToHit, currentlyWorkingOn.transform.forward);

                float absX = Mathf.Abs(dotX);
                float absY = Mathf.Abs(dotY);
                float absZ = Mathf.Abs(dotZ);

                if (absX > absY && absX > absZ) {
                    if (dotX > 0)
                        distPosX = Mathf.Min(distPosX, absX);
                    else
                        distNegX = Mathf.Min(distNegX, absX);
                }
                else if (absY > absX && absY > absZ) {
                    if (dotY > 0)
                        distPosY = Mathf.Min(distPosY, absY);
                    else
                        distNegY = Mathf.Min(distNegY, absY);
                }
                else {
                    if (dotZ > 0)
                        distPosZ = Mathf.Min(distPosZ, absZ);
                    else
                        distNegZ = Mathf.Min(distNegZ, absZ);
                }
            }
        }

        // Restore the original state of the collider after all environmental calculations are finished
        if (selfCollider != null) {
            selfCollider.enabled = wasColliderEnabled;
        }

        Vector3 currentScale = currentlyWorkingOn.transform.localScale;
        if (distPosX == float.MaxValue)
            distPosX = originalExtents.x * currentScale.x;
        if (distNegX == float.MaxValue)
            distNegX = originalExtents.x * currentScale.x;
        if (distPosY == float.MaxValue)
            distPosY = originalExtents.y * currentScale.y;
        if (distNegY == float.MaxValue)
            distNegY = originalExtents.y * currentScale.y;
        if (distPosZ == float.MaxValue)
            distPosZ = originalExtents.z * currentScale.z;
        if (distNegZ == float.MaxValue)
            distNegZ = originalExtents.z * currentScale.z;

        Vector3 newScale = new Vector3(
            (distPosX + distNegX) / (2f * originalExtents.x),
            (distPosY + distNegY) / (2f * originalExtents.y),
            (distPosZ + distNegZ) / (2f * originalExtents.z)
        );

        // Calculate the absolute spatial center of the available space
        Vector3 targetWorldCenter = roomCenter +
            currentlyWorkingOn.transform.right * ((distPosX - distNegX) / 2f) +
            currentlyWorkingOn.transform.up * ((distPosY - distNegY) / 2f) +
            currentlyWorkingOn.transform.forward * ((distPosZ - distNegZ) / 2f);

        // Calculate the exact offset of the mesh center based on its internal pivot and the new scale
        Vector3 scaledCenterOffset =
            currentlyWorkingOn.transform.right * (localCenter.x * newScale.x) +
            currentlyWorkingOn.transform.up * (localCenter.y * newScale.y) +
            currentlyWorkingOn.transform.forward * (localCenter.z * newScale.z);

        // Assign final values to the required variables, compensating for the internal mesh offset
        roomSize = newScale;
        roomCenter = targetWorldCenter - scaledCenterOffset;
    }

    private void GenerateFurniture() {
        GameObject furniture = GameObject.CreatePrimitive(PrimitiveType.Cube);
        furniture.name = "Generated_Table";
        furniture.transform.parent = SetParent(furniture);

        float randomX = roomCenter.x + Random.Range(-roomSize.x / 2f + furniture.transform.localScale.x / 2f, roomSize.x / 2f - furniture.transform.localScale.x / 2f);
        float randomZ = roomCenter.z + Random.Range(-roomSize.z / 2f + furniture.transform.localScale.z / 2f, roomSize.z / 2f - furniture.transform.localScale.z / 2f);

        furniture.transform.position = new Vector3(randomX, 0.5f, randomZ);
    }

    private Transform SetParent(GameObject furniture) {
        if (parentFolderObject == null) {
            GameObject ParentObject = new GameObject("Furniture");
            ParentObject.transform.position = Vector3.zero;
            parentFolderObject = ParentObject;
        }
        return parentFolderObject.transform;
    }

    private Material CreateGhostMaterial(GameObject GhostObject) {
        // Retrieve the MeshRenderer component from the target object
        MeshRenderer mr = GhostObject.GetComponent<MeshRenderer>();

        // Create a new material and assign the Standard shader explicitly
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetFloat("_Surface", 1.0f);
        mat.SetFloat("_Blend", 0.0f);
        mat.SetFloat("_ReceiveShadows", 0.0f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        // Apply color using Color32 for the 0-255 value scale, including the alpha channel
        mat.color = new Color32(100, 140, 200, 80);

        // Apply the material to the renderer using sharedMaterial for Editor scripts
        mr.sharedMaterial = mat;

        return mat;
    }
}
