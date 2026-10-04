using UnityEditor;
using UnityEngine;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport;

namespace VC5PvE.Editor
{
    [InitializeOnLoad]
    public static class PrototypeMcpStartup
    {
        static PrototypeMcpStartup()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += async () =>
            {
                if (MCPServiceLocator.TransportManager.IsRunning(TransportMode.Http)) return;
                try { await MCPServiceLocator.TransportManager.StartAsync(TransportMode.Http); }
                catch (System.Exception e) { Debug.LogWarning("VC5PvE MCP connection: " + e.Message); }
            };
        }
    }
}
