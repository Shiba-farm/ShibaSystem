using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace Hong.VertexBlend
{
    public class BlendPbrInspector : ShaderGUI
    {
        MaterialEditor materialEditor;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            this.materialEditor = materialEditor;
            Material material = this.materialEditor.target as Material;
        
            // new GUIStyle
            GUIStyle boldLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 15,
                wordWrap = true,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            GUI.contentColor = new Color(0, 0.9f, 1f);
            CultureInfo cultureInfo = CultureInfo.CurrentUICulture;
            if(cultureInfo.TwoLetterISOLanguageName != "zh")
                GUILayout.Label("兼容Unity地形纹理规则", boldLabelStyle);
            else
                GUILayout.Label("Compatible with Unity terrain Layer texture rules", boldLabelStyle);
            GUI.contentColor = Color.yellow;
            GUILayout.Label("Layer 0 For Black VertexColor");
            GUI.contentColor = Color.white;

            // 显示基础属性
            MaterialProperty layer0Color = FindProperty("_Layer0_Color", properties);
            MaterialProperty layer0Texture = FindProperty("_Layer0_Texture", properties);
            MaterialProperty layer0Tiling = FindProperty("_Layer0_Tiling", properties);
        
            MaterialProperty layer0Normal = FindProperty("_Layer0_Normal", properties);
            MaterialProperty layer0NormalIntensity = FindProperty("_Layer0_Normal_Intensity", properties);
            MaterialProperty layer0Mask = FindProperty("_Layer0_Mask", properties);
            MaterialProperty layer0Metallic = FindProperty("_Layer0_Metallic", properties);
            MaterialProperty layer0Smoothness = FindProperty("_Layer0_Smoothness", properties);
            MaterialProperty layer0Ao = FindProperty("_Layer0_Ao_Intensity", properties);
            materialEditor.TexturePropertySingleLine(new GUIContent("Layer0 Texture"), layer0Texture, layer0Color);
            Vector4 layer0T = material.GetVector("_Layer0_Tiling");
            Vector2 layer0T2 = new Vector2(layer0T.x, layer0T.y);
            layer0T2 = EditorGUILayout.Vector2Field(new GUIContent("Layer0 Tiling"), layer0T2);
            material.SetVector("_Layer0_Tiling", layer0T2);
            materialEditor.TexturePropertyTwoLines(new GUIContent("Layer0 Normal"), layer0Normal, layer0NormalIntensity, new GUIContent("Layer0 Normal Intensity"), null);
            materialEditor.TexturePropertySingleLine(new GUIContent("Layer0 Mask"), layer0Mask);
            materialEditor.RangeProperty(layer0Metallic, "Layer0 MetalIntensity");
            materialEditor.RangeProperty(layer0Smoothness, "Layer0 Smoothness");
            materialEditor.RangeProperty(layer0Ao, "Layer0 AoIntensity");

            EditorGUILayout.Space();
            GUI.contentColor = new Color(1, 0.25f, 0.25f);
            GUILayout.Label("Layer 1 For Red VertexColor");
            GUI.contentColor = Color.white;

            MaterialProperty layer1Color = FindProperty("_Layer1_Color", properties);
            MaterialProperty layer1Texture = FindProperty("_Layer1_Texture", properties);
            MaterialProperty layer1Tiling = FindProperty("_Layer1_Tiling", properties);
            MaterialProperty layer1Normal = FindProperty("_Layer1_Normal", properties);
            MaterialProperty layer1NormalIntensity = FindProperty("_Layer1_Normal_Intensity", properties);
            MaterialProperty layer1Mask = FindProperty("_Layer1_Mask", properties);
            MaterialProperty layer1MetallicIntensity = FindProperty("_Layer1_Metallic", properties);
            MaterialProperty layer1Smoothness = FindProperty("_Layer1_Smoothness", properties);
            MaterialProperty layer1Ao = FindProperty("_Layer1_Ao_Intensity", properties);
            materialEditor.TexturePropertySingleLine(new GUIContent("Layer1 Texture"), layer1Texture, layer1Color);
            Vector4 layer1T = material.GetVector("_Layer1_Tiling");
            Vector2 layer1T2 = new Vector2(layer1T.x,layer1T.y);
            layer1T2 = EditorGUILayout.Vector2Field(new GUIContent("Layer1 Tiling"), layer1T2);
            material.SetVector("_Layer1_Tiling", layer1T2);
            materialEditor.TexturePropertyTwoLines(new GUIContent("Layer1 Normal"), layer1Normal, layer1NormalIntensity, new GUIContent("Layer1 Normal Intensity"), null);
            materialEditor.TexturePropertySingleLine(new GUIContent("Layer1 Mask"), layer1Mask);
            materialEditor.RangeProperty(layer1MetallicIntensity, "Layer1 MetalIntensity");
            materialEditor.RangeProperty(layer1Smoothness, "Layer1 Smoothness");
            materialEditor.RangeProperty(layer1Ao, "Layer1 AoIntensity");

            EditorGUILayout.Space();
            GUI.contentColor = Color.green;
            GUILayout.Label("Layer 2 For Green VertexColor");
            GUI.contentColor = Color.white;

            MaterialProperty layer2Color = FindProperty("_Layer2_Color", properties);
            MaterialProperty layer2Texture = FindProperty("_Layer2_Texture", properties);
            MaterialProperty layer2Tiling = FindProperty("_Layer2_Tiling", properties);
            MaterialProperty layer2Normal = FindProperty("_Layer2_Normal", properties);
            MaterialProperty layer2NormalIntensity = FindProperty("_Layer2_Normal_Intensity", properties);
            MaterialProperty layer2Mask = FindProperty("_Layer2_Mask", properties);
            MaterialProperty layer2MetallicIntensity = FindProperty("_Layer2_Metallic", properties);
            MaterialProperty layer2Smoothness = FindProperty("_Layer2_Smoothness", properties);
            MaterialProperty layer2Ao = FindProperty("_Layer2_Ao_Intensity", properties);
            materialEditor.TexturePropertySingleLine(new GUIContent("Layer2 Texture"), layer2Texture, layer2Color);
            Vector4 layer2T = material.GetVector("_Layer2_Tiling");
            Vector2 layer2T2 = new Vector2(layer2T.x, layer2T.y);
            layer2T2 = EditorGUILayout.Vector2Field(new GUIContent("Layer2 Tiling"), layer2T2);
            material.SetVector("_Layer2_Tiling", layer2T2);
            materialEditor.TexturePropertyTwoLines(new GUIContent("Layer2 Normal"), layer2Normal, layer2NormalIntensity, new GUIContent("Layer2 Normal Intensity"), null);
            materialEditor.TexturePropertySingleLine(new GUIContent("Layer2 Mask"), layer2Mask);
            materialEditor.RangeProperty(layer2MetallicIntensity, "Layer2 MetalIntensity");
            materialEditor.RangeProperty(layer2Smoothness, "Layer2 Smoothness");
            materialEditor.RangeProperty(layer2Ao, "Layer2 AoIntensity");

            EditorGUILayout.Space();
            GUI.contentColor = new Color(0,0.5f,1);
            GUILayout.Label("Layer 3 For Blue VertexColor");
            GUI.contentColor = Color.white;

            MaterialProperty layer3Color = FindProperty("_Layer3_Color", properties);
            MaterialProperty layer3Texture = FindProperty("_Layer3_Texture", properties);
            MaterialProperty layer3Tiling = FindProperty("_Layer3_Tiling", properties);
            MaterialProperty layer3Normal = FindProperty("_Layer3_Normal", properties);
            MaterialProperty layer3NormalIntensity = FindProperty("_Layer3_Normal_Intensity", properties);
            MaterialProperty layer3Mask = FindProperty("_Layer3_Mask", properties);
            MaterialProperty layer3MetallicIntensity = FindProperty("_Layer3_Metallic", properties);
            MaterialProperty layer3Smoothness = FindProperty("_Layer3_Smoothness", properties);
            MaterialProperty layer3Ao = FindProperty("_Layer3_Ao_Intensity", properties);
            materialEditor.TexturePropertySingleLine(new GUIContent("Layer3 Texture"), layer3Texture, layer3Color);
            Vector4 layer3T = material.GetVector("_Layer3_Tiling");
            Vector2 layer3T2 = new Vector2(layer3T.x, layer3T.y);
            layer3T2 = EditorGUILayout.Vector2Field(new GUIContent("Layer3 Tiling"), layer3T2);
            material.SetVector("_Layer3_Tiling", layer3T2);
            materialEditor.TexturePropertyTwoLines(new GUIContent("Layer3 Normal"), layer3Normal, layer3NormalIntensity, new GUIContent("Layer2 Normal Intensity"), null);
            materialEditor.TexturePropertySingleLine(new GUIContent("Layer3 Mask"), layer3Mask);
            materialEditor.RangeProperty(layer3MetallicIntensity, "Layer3 MetalIntensity");
            materialEditor.RangeProperty(layer3Smoothness, "Layer3 Smoothness");
            materialEditor.RangeProperty(layer3Ao, "Layer3 AoIntensity");

            try
            {
                EditorGUILayout.Space();
                MaterialProperty splatTexture = FindProperty("_Splat", properties);
                GUI.contentColor = new Color(1,0.5f,.8f);
                GUILayout.Label("Splat");
                GUI.contentColor = Color.white;
                materialEditor.TexturePropertySingleLine(new GUIContent("Splat Texture"), splatTexture);
            }
            catch (Exception e) {}
        
            // 折叠组示例
            // bool showAdvanced = EditorGUILayout.Foldout(true, "Advanced Settings");
            // if (showAdvanced)
            // {
            //     MaterialProperty smoothness = FindProperty("_Layer0_Smoothness", properties);
            //     materialEditor.ShaderProperty(smoothness, "Smoothness");
            // }
        
            // 渲染默认的其余属性
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
        }
    }
}
