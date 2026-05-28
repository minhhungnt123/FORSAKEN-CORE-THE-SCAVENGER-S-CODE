using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;

[InitializeOnLoad]
public class HierarchyDumper
{
    static HierarchyDumper()
    {
        EditorApplication.delayCall += DumpHierarchies;
    }

    [MenuItem("Tools/Dump Prefab Hierarchies")]
    public static void DumpHierarchies()
    {
        string[] prefabsToDump = new string[]
        {
            "Assets/_Scripts/_Perfabs/fighting_leg_1.prefab",
            "Assets/_Scripts/_Perfabs/fighting_arm_left_1.prefab",
            "Assets/Model_Robot/fighting_leg_1.fbx",
            "Assets/Model_Robot/fighting_arm_left_1.fbx"
        };

        StringBuilder sb = new StringBuilder();

        foreach (string path in prefabsToDump)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                sb.AppendLine($"Could not find prefab at: {path}");
                continue;
            }

            sb.AppendLine($"--- Hierarchy for {prefab.name} ---");
            DumpChild(prefab.transform, "", sb);
            sb.AppendLine();
        }

        File.WriteAllText("HierarchyDump.txt", sb.ToString());
        Debug.Log("Dumped to HierarchyDump.txt automatically.");
    }

    private static void DumpChild(Transform t, string indent, StringBuilder sb)
    {
        sb.AppendLine($"{indent}- {t.name}");
        for (int i = 0; i < t.childCount; i++)
        {
            DumpChild(t.GetChild(i), indent + "  ", sb);
        }
    }
}
