using UnityEngine;

[ExecuteInEditMode] // 让我们在编辑模式下也能运行
public class TerrainDataFixer : MonoBehaviour
{
    [Header("1. 把你的地形物体拖进来")]
    public Terrain targetTerrain;

    [Header("2. 把你复制好的新数据文件拖进来")]
    public TerrainData newTerrainData;

    
    [ContextMenu("执行赋值 (Assign Data)")] // 在组件菜单里添加按钮
    void AssignData()
    {
        if (targetTerrain == null || newTerrainData == null)
        {
            Debug.LogError("请先在 Inspector 里把 Terrain 和 TerrainData 都拖进去！");
            return;
        }

        // 1. 强制修改地形组件的数据
        targetTerrain.terrainData = newTerrainData;

        // 2. 强制修改碰撞体的数据
        TerrainCollider collider = targetTerrain.GetComponent<TerrainCollider>();
        if (collider != null)
        {
            collider.terrainData = newTerrainData;
        }

        Debug.Log($"<color=green>成功！已将地形 {targetTerrain.name} 的数据替换为 {newTerrainData.name}</color>");
    }
}