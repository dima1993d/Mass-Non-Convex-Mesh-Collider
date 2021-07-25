#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
public class ColliderGeneratorEditor : EditorWindow
{
    private int boxesPerEdge = 20;
    private bool isTrigger = false;
    private PhysicMaterial physicsMaterial;
    void OnGUI()
    {
        GameObject[] objects = Selection.gameObjects;
        EditorGUILayout.LabelField("Generate Colliders for " + objects.Length + " Prefabs");
        boxesPerEdge = EditorGUILayout.IntField("Number of Boxes PerEdge:", boxesPerEdge);
        boxesPerEdge = Mathf.Clamp(boxesPerEdge, 1, 50);
        isTrigger = EditorGUILayout.Toggle("Is Trigger", isTrigger);
        physicsMaterial = (PhysicMaterial)EditorGUILayout.ObjectField("Material", physicsMaterial, typeof(PhysicMaterial), true);

        if (objects.Length == 0)
        {
            EditorGUILayout.HelpBox("No objects selected", MessageType.Warning);
            GUI.enabled = false;
        }
        if (isTrigger)
        {
            EditorGUILayout.HelpBox("BoxColliders will be set to Trigger", MessageType.Warning);
        }
        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Colliders"))
            OptimizedColliderGenerator.GetAllUniquePrefabs(boxesPerEdge, isTrigger, physicsMaterial);
        EditorGUILayout.Space();
        if (GUILayout.Button("Destroy All Mesh Colliders In Prefabs"))
            OptimizedColliderGenerator.DestroyAllMeshColliders();
        EditorGUILayout.Space();
        if (GUILayout.Button("Destroy All Child Objects Of Colliders GameObject"))
            OptimizedColliderGenerator.DestroyAllColliders();
        GUI.enabled = true;


    }
    public static ColliderGeneratorEditor Instance
    {
        get { return GetWindow<ColliderGeneratorEditor>(); }
    }

    [MenuItem("GameObject/Generate Colliders", false, -1), MenuItem("Assets/Generate Colliders", false, 51)]
    static void CreateWindow()
    {
        ColliderGeneratorEditor window;
        if (Instance == null)
        {
            window = new ColliderGeneratorEditor();
        }
        else
        {
            window = Instance;
        }

        window.ShowUtility();
    }
}

public class OptimizedColliderGenerator
{

    public MeshRenderer[] childRenderers;
    public GameObject[] uniquePrefabs;
    public static OptimizedColliderGenerator instance;
    public static void GetAllUniquePrefabs(int boxesPerEdge = 20, bool isTrigger = false, PhysicMaterial physicsMaterial = null, bool destroyMeshColliders = true)
    {
        GameObject[] assetRoot = Selection.gameObjects;
        for (int i = 0; i < assetRoot.Length; i++)
        {
            using (var editScope = new EditPrefabAssetScope(GetPath(assetRoot[i])))
            {
                if (destroyMeshColliders)
                {
                    MeshCollider[] meshColliders = editScope.prefabRoot.GetComponentsInChildren<MeshCollider>();
                    for (int t = 0; t < meshColliders.Length; t++)
                    {
                        UnityEngine.Object.DestroyImmediate(meshColliders[t]);
                    }
                }

                MeshRenderer[] childRenderers = editScope.prefabRoot.GetComponentsInChildren<MeshRenderer>();
                Transform colliderParentTransform = editScope.prefabRoot.transform.Find("Colliders");

                for (int t = 0; t < childRenderers.Length; t++)
                {
                    AddComponentAndCalculate(childRenderers[t].gameObject, colliderParentTransform, boxesPerEdge, isTrigger, physicsMaterial);
                    if (assetRoot.Length >= 10)
                    {
                        if (i % 10 == 0)
                        {
                            float num = i;
                            EditorUtility.DisplayProgressBar("Collider Generaton Progress", "Generating Colliders for: " + assetRoot[i].gameObject.name, num / assetRoot.Length);
                        }
                    }
                }
            }
        }
        if (assetRoot.Length >= 10)
            EditorUtility.ClearProgressBar();
    }
    [MenuItem("Assets/GenerateColliders", true)]
    private static bool GenerateCollidersMenuOptionValidation()
    {
        // This returns true when the selected object is a GameObject (the menu item will be disabled otherwise).
        return Selection.activeObject.GetType() == typeof(GameObject);
    }
    static void AddComponentAndCalculate(GameObject t, Transform colliderParentTransform, int boxesPerEdge = 20, bool isTrigger = false, PhysicMaterial physicsMaterial = null)
    {
        NonConvexMeshCollider nonConvexMeshCollider = t.GetComponent<NonConvexMeshCollider>();
        if (!nonConvexMeshCollider)
        {
            nonConvexMeshCollider = t.AddComponent<NonConvexMeshCollider>();
        }
        nonConvexMeshCollider.colliderParentTransform = colliderParentTransform;
        nonConvexMeshCollider.BoxesPerEdge = boxesPerEdge;
        nonConvexMeshCollider.isTrigger = isTrigger;
        nonConvexMeshCollider.physicsMaterial = physicsMaterial;
        nonConvexMeshCollider.Calculate();
    }
    private static string GetPath(GameObject obj)
    {
        GameObject parentObject;
        if (PrefabUtility.GetPrefabInstanceStatus(obj) == PrefabInstanceStatus.NotAPrefab)
        {
            parentObject = obj;
        }
        else
        {
            parentObject = PrefabUtility.GetCorrespondingObjectFromSource(obj);
        }
        return AssetDatabase.GetAssetPath(parentObject);
    }

    public static void DestroyAllMeshColliders()
    {
        GameObject[] assetRoot = Selection.gameObjects;
        for (int i = 0; i < assetRoot.Length; i++)
        {
            using (var editScope = new EditPrefabAssetScope(GetPath(assetRoot[i])))
            {
                MeshCollider[] meshColliders = editScope.prefabRoot.GetComponentsInChildren<MeshCollider>();
                for (int t = 0; t < meshColliders.Length; t++)
                {
                    UnityEngine.Object.DestroyImmediate(meshColliders[t]);
                }
            }
        }
    }
    public static void DestroyAllColliders()
    {
        GameObject[] assetRoot = Selection.gameObjects;
        for (int i = 0; i < assetRoot.Length; i++)
        {
            using (var editScope = new EditPrefabAssetScope(GetPath(assetRoot[i])))
            {
                Transform colliders = editScope.prefabRoot.transform.Find("Colliders");
                foreach (Transform item in colliders)
                {
                    UnityEngine.Object.DestroyImmediate(item.gameObject);
                }
            }
        }
    }

}

public class EditPrefabAssetScope : IDisposable
{

    public readonly string assetPath;
    public readonly GameObject prefabRoot;

    public EditPrefabAssetScope(string assetPath)
    {
        this.assetPath = assetPath;
        prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
    }

    public void Dispose()
    {
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }
}
#endif

