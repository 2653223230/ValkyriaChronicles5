using System;

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Method)] public sealed class TestAttribute : Attribute { }

    public static class Assert
    {
        public static void AreEqual(object expected, object actual)
        { AreEqual(expected, actual, null); }
        public static void AreEqual(object expected, object actual, string message)
        {
            if (!object.Equals(expected, actual)) throw new Exception((message ?? "") + " expected <" + expected + "> but was <" + actual + ">");
        }
        public static void IsTrue(bool value) { IsTrue(value, null); }
        public static void IsTrue(bool value, string message)
        { if (!value) throw new Exception(message ?? "expected true"); }
        public static void IsFalse(bool value) { IsFalse(value, null); }
        public static void IsFalse(bool value, string message)
        { if (value) throw new Exception(message ?? "expected false"); }
    }
}
