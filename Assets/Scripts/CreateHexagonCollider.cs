using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public class CreateHexagonCollider : MonoBehaviour
{
    public float radius = 0.5f; // 六边形半径
    public float height = 0.01f; // 柱体高度
    void Awake()
    {
        Mesh mesh = new Mesh();
        
        // 顶点数组（14个顶点 = 底面6 + 顶面6 + 底面中心1 + 顶面中心1）
        Vector3[] vertices = new Vector3[14];
        int[] triangles = new int[72]; // 36(侧面) + 18(底面) + 18(顶面)

        // 生成底面和顶面的外圈顶点
        for (int i = 0; i < 6; i++)
        {
            float angle = 60f * i * Mathf.Deg2Rad;
            // 底面顶点（Z=0）
            vertices[i] = new Vector3(
                radius * Mathf.Cos(angle),
                radius * Mathf.Sin(angle),
                0
            );
            // 顶面顶点（Z=height）
            vertices[i + 6] = vertices[i] + new Vector3(0, 0, height);
        }

        // 添加底面和顶面的中心顶点
        vertices[12] = new Vector3(0, 0, 0);         // 底面中心
        vertices[13] = new Vector3(0, 0, height);    // 顶面中心

        // 生成侧面三角形（36个索引）
        for (int i = 0; i < 6; i++)
        {
            int a = i;
            int b = (i + 1) % 6;
            int c = a + 6;
            int d = b + 6;

            triangles[i * 6] = a;
            triangles[i * 6 + 1] = b;
            triangles[i * 6 + 2] = c;
            triangles[i * 6 + 3] = b;
            triangles[i * 6 + 4] = d;
            triangles[i * 6 + 5] = c;
        }

        // 生成底面三角形（18个索引）
        int triIndex = 36;
        for (int i = 0; i < 6; i++)
        {
            int current = i;
            int next = (i + 1) % 6;
            triangles[triIndex++] = 12;     // 底面中心
            triangles[triIndex++] = current;
            triangles[triIndex++] = next;
        }

        // 生成顶面三角形（18个索引，反向确保法线朝外）
        for (int i = 0; i < 6; i++)
        {
            int current = 6 + i;
            int next = 6 + ((i + 1) % 6);
            triangles[triIndex++] = 13;     // 顶面中心
            triangles[triIndex++] = next;    // 反向顶点顺序
            triangles[triIndex++] = current;
        }

        // 应用网格数据
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshCollider>().sharedMesh = mesh;
        GetComponent<MeshCollider>().convex = true; // 启用凸面碰撞
    }
}