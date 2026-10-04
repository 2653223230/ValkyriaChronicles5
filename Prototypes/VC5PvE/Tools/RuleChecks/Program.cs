using System;
using System.Reflection;
using NUnit.Framework;
using VC5PvE.Tests;

internal static class Program
{
    private static int Main()
    {
        var instance = new RulesAcceptanceTests();
        var passed = 0;
        var failed = 0;
        foreach (var method in typeof(RulesAcceptanceTests).GetMethods(BindingFlags.Instance | BindingFlags.Public))
        {
            if (Attribute.IsDefined(method, typeof(TestAttribute)))
            {
                try { method.Invoke(instance, null); Console.WriteLine("PASS " + method.Name); passed++; }
                catch (Exception ex) { Console.WriteLine("FAIL " + method.Name + ": " + (ex.InnerException == null ? ex.Message : ex.InnerException.Message)); failed++; }
            }
        }
        Console.WriteLine("RESULT passed=" + passed + " failed=" + failed);
        return failed == 0 ? 0 : 1;
    }
}
