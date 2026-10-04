using UnityEngine;

namespace VC5PvE
{
    [ExecuteAlways]
    public sealed class ScenicBackdrop : MonoBehaviour
    {
        private void LateUpdate()
        {
            var camera=GetComponentInParent<Camera>();
            if(camera==null) return;
            float height=camera.orthographicSize*2.02f;
            float aspect=Mathf.Max(camera.aspect,16f/9f);
            if(camera.aspect<16f/9f) height*=16f/9f/Mathf.Max(.1f,camera.aspect);
            transform.localScale=new Vector3(height*aspect,height,1);
        }
    }
}
