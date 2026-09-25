namespace UnityEngine.TestTools {
  [System.AttributeUsage(System.AttributeTargets.Method)] public class UnityTestAttribute : System.Attribute {}
  public static class LogAssert { public static void Expect(UnityEngine.LogType t, string m){} public static bool ignoreFailingMessages; }
}
namespace UnityEngine.TestTools {
  [System.AttributeUsage(System.AttributeTargets.Method)] public class UnityTearDownAttribute : System.Attribute {}
  [System.AttributeUsage(System.AttributeTargets.Method)] public class UnitySetUpAttribute : System.Attribute {}
}
