using UnityEngine;
using System.Collections.Generic;

public class ColorLists : MonoBehaviour
{
    [System.Serializable]
    public class OrganMaterials
    {
        public List<Material> materials = new List<Material>();
    }

    public List<OrganMaterials> organMaterials; // На каждый орган - список его материалов (в том же порядке, что и в ObjectLists.organs)
    public Texture2D greenTexture;              // Зеленая текстура для правильного органа
    public Texture2D redTexture;                // Красная текстура для неправильного органа

    private static readonly int BaseMapID = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexID = Shader.PropertyToID("_MainTex");

    // Изначальные (Base) Map каждого материала
    private readonly Dictionary<Material, Texture> baseMaps = new Dictionary<Material, Texture>();

    private void Awake()
    {
        for(int i = 0; i < organMaterials.Count; i++) {
            List<Material> materials = organMaterials[i].materials;
            for(int j = 0; j < materials.Count; j++) {
                Material material = materials[j];
                if(material == null || baseMaps.ContainsKey(material)) continue;

                Texture baseMap = null;
                if(material.HasProperty(BaseMapID)) baseMap = material.GetTexture(BaseMapID);
                else if(material.HasProperty(MainTexID)) baseMap = material.GetTexture(MainTexID);
                baseMaps[material] = baseMap;
            }
        }
    }

    private void SetBaseMap(Material material, Texture texture)
    {
        if(material == null || texture == null) return;

        if(material.HasProperty(BaseMapID)) material.SetTexture(BaseMapID, texture);
        else if(material.HasProperty(MainTexID)) material.SetTexture(MainTexID, texture);
    }

    // Подсветить орган: true - зеленый (правильный), false - красный (неправильный)
    public void SetOrganColor(int index, bool correct)
    {
        if(index < 0 || index >= organMaterials.Count) return;

        Texture2D texture = correct ? greenTexture : redTexture;
        List<Material> materials = organMaterials[index].materials;
        for(int j = 0; j < materials.Count; j++) {
            SetBaseMap(materials[j], texture);
        }
    }

    // Вернуть материалы органа к изначальному виду
    public void RestoreOrgan(int index)
    {
        if(index < 0 || index >= organMaterials.Count) return;

        List<Material> materials = organMaterials[index].materials;
        for(int j = 0; j < materials.Count; j++) {
            Material material = materials[j];
            if(material != null && baseMaps.TryGetValue(material, out Texture baseMap)) {
                SetBaseMap(material, baseMap);
            }
        }
    }

    public void RestoreAll()
    {
        for(int i = 0; i < organMaterials.Count; i++) {
            RestoreOrgan(i);
        }
    }
}
