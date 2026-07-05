/// <summary>
/// 配置済みの通路(Corridor)GameObject群をマテリアルごとに結合し、描画コール数を削減する。
/// 通路メッシュが複数サブメッシュ(複数マテリアル)を持つ場合でも、サブメッシュ単位で
/// 対応マテリアルに振り分けて結合する。
/// 1メッシュの頂点数が16bitインデックス上限(65535)を超えるとジオメトリが破損するため、
/// マテリアルごとに頂点数の上限でチャンク分割しながら結合する。
/// 元の通路GameObjectはColliderをNavMeshベイク用に残すため、MeshFilter/MeshRendererのみ破棄する。
/// </summary>
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class CorridorMeshCombiner
{
    private const string kCombinedRootName = "CombinedCorridors";

    // 1チャンクあたりの頂点数上限。16bit上限(65535)に安全マージンを取る。
    private const int kMaxVerticesPerChunk = 60000;
    private const float kWeldPositionTolerance = 1e-4f;

    public void Combine(Transform corridorParent)
    {
        var meshFilters = corridorParent.GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0) return;

        var combinedRoot = new GameObject(kCombinedRootName);
        combinedRoot.transform.SetParent(corridorParent, false);
        var rootWorldToLocal = combinedRoot.transform.worldToLocalMatrix;

        // マテリアル単位でCombineInstanceをグルーピングする。
        var groups = new Dictionary<Material, List<CombineInstance>>();

        foreach (var meshFilter in meshFilters)
        {
            if (!meshFilter.TryGetComponent<MeshRenderer>(out var renderer)) continue;

            var mesh = meshFilter.sharedMesh;
            if (mesh == null) continue;

            var materials = renderer.sharedMaterials;
            if (materials.Length == 0) continue;

            var localToRoot = rootWorldToLocal * meshFilter.transform.localToWorldMatrix;

            // 各サブメッシュを、対応するマテリアルのグループに振り分ける。
            // 単一サブメッシュ・複数サブメッシュ(Blender統合メッシュ)のどちらも正しく扱える。
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                var material = s < materials.Length ? materials[s] : materials[materials.Length - 1];
                if (material == null) continue;

                if (!groups.TryGetValue(material, out var list))
                {
                    list = new List<CombineInstance>();
                    groups[material] = list;
                }

                list.Add(new CombineInstance
                {
                    mesh = mesh,
                    subMeshIndex = s,
                    transform = localToRoot
                });
            }
        }

        if (groups.Count == 0)
        {
            Object.Destroy(combinedRoot);
            return;
        }

        foreach (var (material, instances) in groups)
        {
            CombineGroup(material, instances, combinedRoot.transform);
        }

        // 結合済みの元メッシュコンポーネントを破棄する（Colliderは残す）。
        foreach (var meshFilter in meshFilters)
        {
            if (meshFilter.TryGetComponent<MeshRenderer>(out var renderer))
                Object.Destroy(renderer);
            Object.Destroy(meshFilter);
        }
    }

    /// <summary>
    /// 1マテリアル分のCombineInstanceを、頂点数上限を超えないようチャンク分割して結合する。
    /// </summary>
    private void CombineGroup(Material material, List<CombineInstance> instances, Transform parent)
    {
        var chunk = new List<CombineInstance>();
        int chunkVertexCount = 0;
        int chunkIndex = 0;

        foreach (var instance in instances)
        {
            int vertexCount = GetSubMeshVertexCount(instance);

            // 追加すると上限を超える場合は、現在のチャンクを先に確定する。
            if (chunk.Count > 0 && chunkVertexCount + vertexCount > kMaxVerticesPerChunk)
            {
                CreateCombinedObject(material, chunk, parent, chunkIndex++);
                chunk.Clear();
                chunkVertexCount = 0;
            }

            chunk.Add(instance);
            chunkVertexCount += vertexCount;
        }

        if (chunk.Count > 0)
            CreateCombinedObject(material, chunk, parent, chunkIndex);
    }

    /// <summary>
    /// チャンク分割用に、対象サブメッシュのおおよその頂点数を返す。
    /// サブメッシュの頂点数が取得できない場合はメッシュ全体の頂点数で安全側に見積もる。
    /// </summary>
    private int GetSubMeshVertexCount(CombineInstance instance)
    {
        int vertexCount = instance.mesh.GetSubMesh(instance.subMeshIndex).vertexCount;
        return vertexCount > 0 ? vertexCount : instance.mesh.vertexCount;
    }

    /// <summary>
    /// 1チャンク分のCombineInstanceを結合し、MeshFilter/MeshRendererを持つ子GameObjectを生成する。
    /// </summary>
    private void CreateCombinedObject(Material material, List<CombineInstance> chunk, Transform parent, int chunkIndex)
    {
        var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
        mesh.CombineMeshes(chunk.ToArray(), true, true);
        WeldVertices(mesh, kWeldPositionTolerance);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        var go = new GameObject($"Combined_{material.name}_{chunkIndex}");
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    /// <summary>
    /// 位置がほぼ同一（回転による浮動小数点誤差の範囲内）の頂点を1つに統合する。
    /// </summary>
    private void WeldVertices(Mesh mesh,float tolerance)
    {
        var vertices = mesh.vertices;
        var uvs = mesh.uv;
        var boneWeights = mesh.boneWeights;

        var remap= new int[vertices.Length];
        var weldedPositions = new List<Vector3>();
        var weldedUVs = new List<Vector2>();
        var keyToIndex = new Dictionary<(long, long, long), int>();

        float inv = 1f / tolerance;

        for (int i = 0; i < vertices.Length; i++)
        {
            var p = vertices[i];
            var key = (
            (long)Mathf.Round(p.x * inv),
            (long)Mathf.Round(p.y * inv),
            (long)Mathf.Round(p.z * inv));

            if (!keyToIndex.TryGetValue(key, out var weldedIndex)) {
                weldedIndex = weldedPositions.Count;
                keyToIndex[key] = weldedIndex;
                weldedPositions.Add(p);
                if (uvs.Length > i) weldedUVs.Add(uvs[i]);
            }
        
            remap[i] = weldedIndex;
        }
        
        var subMeshCount = mesh.subMeshCount;
        var newTriangles = new int[subMeshCount][];
        for (int s = 0; s < subMeshCount; s++){
           var tris = mesh.GetTriangles(s);
           for (int t = 0; t < tris.Length; t++)
           tris[t] = remap[tris[t]];
           newTriangles[s] = tris;
        }
        
        mesh.Clear();
        mesh.SetVertices(weldedPositions);
        if (weldedUVs.Count == weldedPositions.Count) mesh.SetUVs(0, weldedUVs);
        mesh.subMeshCount = subMeshCount;
                for (int s = 0; s < subMeshCount; s++)
                    mesh.SetTriangles(newTriangles[s], s);
    }
}
