namespace UnityEditor {
  [System.AttributeUsage(System.AttributeTargets.Method)] public class MenuItem : System.Attribute { public MenuItem(string p){} }
  public class SerializedProperty { public UnityEngine.Object objectReferenceValue; public int intValue; public float floatValue; public bool boolValue; public int arraySize; public SerializedProperty GetArrayElementAtIndex(int i)=>null; public UnityEngine.Vector3 vector3Value; public int enumValueIndex; }
  public class SerializedObject { public SerializedObject(UnityEngine.Object o){} public SerializedProperty FindProperty(string n)=>null; public bool ApplyModifiedPropertiesWithoutUndo()=>true; }
  public static class AssetDatabase { public static T LoadAssetAtPath<T>(string p) where T: UnityEngine.Object => null; public static UnityEngine.Object[] LoadAllAssetsAtPath(string p) => null; public static AssetImporter GetImporter(string p) => null; public static void CreateAsset(UnityEngine.Object o, string p){} public static void SaveAssets(){} public static bool IsValidFolder(string p)=>true; public static string CreateFolder(string a, string b)=>""; }
  public class EditorBuildSettingsScene { public string path; public EditorBuildSettingsScene(string p, bool e){} }
  public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes; }
  public class SceneAsset : UnityEngine.Object { }
  public class AssetImporter : UnityEngine.Object { public string assetPath; public static AssetImporter GetAtPath(string p) => null; }
  public enum ModelImporterAnimationType { None, Legacy, Generic, Human }
  public enum ModelImporterAvatarSetup { NoAvatar, CreateFromThisModel, CopyFromOther }
  public class ModelImporter : AssetImporter { public ModelImporterAnimationType animationType; public ModelImporterAvatarSetup avatarSetup; }
  public class AssetPostprocessor { public string assetPath; public AssetImporter assetImporter; }
  public static class EditorUtility { public static void SetDirty(UnityEngine.Object o){} }
  public static class GameObjectUtility { public static void SetStaticEditorFlags(UnityEngine.GameObject g, int f){} }
}
namespace UnityEditor.SceneManagement {
  public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
  public enum NewSceneMode { Single, Additive }
  public static class EditorSceneManager { public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup s, NewSceneMode m)=>default; public static bool SaveScene(UnityEngine.SceneManagement.Scene s, string p)=>true; }
}
