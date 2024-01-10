// Created by 月北(ybwork-cn) https://github.com/ybwork-cn/

using UnityEditor;
using UnityEngine;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal.ShaderGUI;
using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ybwork.ECSVertexAnimation.Editor
{
    class LitDetailGUI
    {
        internal static class Styles
        {
            public static readonly GUIContent detailInputs = EditorGUIUtility.TrTextContent("Detail Inputs", "These settings define the surface details by tiling and overlaying additional maps on the surface.");

            public static readonly GUIContent detailMaskText = EditorGUIUtility.TrTextContent("Mask", "Select a mask for the Detail map. The mask uses the alpha channel of the selected texture. The Tiling and Offset settings have no effect on the mask.");

            public static readonly GUIContent detailAlbedoMapText = EditorGUIUtility.TrTextContent("Base Map", "Select the surface detail texture.The alpha of your texture determines surface hue and intensity.");

            public static readonly GUIContent detailNormalMapText = EditorGUIUtility.TrTextContent("Normal Map", "Designates a Normal Map to create the illusion of bumps and dents in the details of this Material's surface.");

            public static readonly GUIContent detailAlbedoMapScaleInfo = EditorGUIUtility.TrTextContent("Setting the scaling factor to a value other than 1 results in a less performant shader variant.");

            public static readonly GUIContent detailAlbedoMapFormatError = EditorGUIUtility.TrTextContent("This texture is not in linear space.");
        }

        public struct LitProperties
        {
            public MaterialProperty detailMask;

            public MaterialProperty detailAlbedoMapScale;

            public MaterialProperty detailAlbedoMap;

            public MaterialProperty detailNormalMapScale;

            public MaterialProperty detailNormalMap;

            public LitProperties(MaterialProperty[] properties)
            {
                detailMask = BaseShaderGUI.FindProperty("_DetailMask", properties, propertyIsMandatory: false);
                detailAlbedoMapScale = BaseShaderGUI.FindProperty("_DetailAlbedoMapScale", properties, propertyIsMandatory: false);
                detailAlbedoMap = BaseShaderGUI.FindProperty("_DetailAlbedoMap", properties, propertyIsMandatory: false);
                detailNormalMapScale = BaseShaderGUI.FindProperty("_DetailNormalMapScale", properties, propertyIsMandatory: false);
                detailNormalMap = BaseShaderGUI.FindProperty("_DetailNormalMap", properties, propertyIsMandatory: false);
            }
        }

        public static void DoDetailArea(LitProperties properties, MaterialEditor materialEditor)
        {
            materialEditor.TexturePropertySingleLine(Styles.detailMaskText, properties.detailMask);
            materialEditor.TexturePropertySingleLine(Styles.detailAlbedoMapText, properties.detailAlbedoMap, (properties.detailAlbedoMap.textureValue != null) ? properties.detailAlbedoMapScale : null);
            if (properties.detailAlbedoMapScale.floatValue != 1f)
            {
                EditorGUILayout.HelpBox(Styles.detailAlbedoMapScaleInfo.text, MessageType.Info, wide: true);
            }

            Texture2D texture2D = properties.detailAlbedoMap.textureValue as Texture2D;
            if (texture2D != null && GraphicsFormatUtility.IsSRGBFormat(texture2D.graphicsFormat))
            {
                EditorGUILayout.HelpBox(Styles.detailAlbedoMapFormatError.text, MessageType.Warning, wide: true);
            }

            materialEditor.TexturePropertySingleLine(Styles.detailNormalMapText, properties.detailNormalMap, (properties.detailNormalMap.textureValue != null) ? properties.detailNormalMapScale : null);
            materialEditor.TextureScaleOffsetProperty(properties.detailAlbedoMap);
        }

        public static void SetMaterialKeywords(Material material)
        {
            if (material.HasProperty("_DetailAlbedoMap") && material.HasProperty("_DetailNormalMap") && material.HasProperty("_DetailAlbedoMapScale"))
            {
                bool flag = material.GetFloat("_DetailAlbedoMapScale") != 1f;
                bool flag2 = (bool)material.GetTexture("_DetailAlbedoMap") || (bool)material.GetTexture("_DetailNormalMap");
                CoreUtils.SetKeyword(material, "_DETAIL_MULX2", !flag && flag2);
                CoreUtils.SetKeyword(material, "_DETAIL_SCALED", flag && flag2);
            }
        }
    }

    class VertexAnimationShaderEditor : BaseShaderGUI
    {
        private static readonly string[] workflowModeNames = Enum.GetNames(typeof(LitGUI.WorkflowMode));
        private LitGUI.LitProperties litProperties;
        private LitDetailGUI.LitProperties litDetailProperties;

        public static readonly GUIContent animMapTitle = EditorGUIUtility.TrTextContent("Anim Map");
        public static readonly GUIContent animMapNormalTitle = EditorGUIUtility.TrTextContent("Anim Map Normal");
        public static readonly string animLenTitle = "Anim Len";
        public static readonly string loopPropTitle = "Loop";
        public static readonly string currentTimeTitle = "Current Time";

        private MaterialProperty animMapProp;
        private MaterialProperty animMapNormalProp;
        private MaterialProperty animLenProp;
        private MaterialProperty currentTimeProp;
        private MaterialProperty loopProp;

        public override void FillAdditionalFoldouts(MaterialHeaderScopeList materialScopesList)
        {
            materialScopesList.RegisterHeaderScope(LitDetailGUI.Styles.detailInputs, Expandable.Details, delegate {
                LitDetailGUI.DoDetailArea(litDetailProperties, materialEditor);
            });
        }

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            litProperties = new LitGUI.LitProperties(properties);
            litDetailProperties = new LitDetailGUI.LitProperties(properties);
            animMapProp = FindProperty("_AnimMap", properties, propertyIsMandatory: false);
            animMapNormalProp = FindProperty("_AnimMapNormal", properties, propertyIsMandatory: false);
            animLenProp = FindProperty("_AnimLen", properties, propertyIsMandatory: false);
            loopProp = FindProperty("_Loop", properties, propertyIsMandatory: false);
            currentTimeProp = FindProperty("_CurrentTime", properties, propertyIsMandatory: false);
        }

        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material, LitGUI.SetMaterialKeywords, LitDetailGUI.SetMaterialKeywords);
        }

        public override void DrawSurfaceOptions(Material material)
        {
            EditorGUIUtility.labelWidth = 0f;
            if (litProperties.workflowMode != null)
            {
                DoPopup(LitGUI.Styles.workflowModeText, litProperties.workflowMode, workflowModeNames);
            }

            base.DrawSurfaceOptions(material);
        }

        public override void DrawSurfaceInputs(Material material)
        {
            materialEditor.TexturePropertySingleLine(animMapTitle, animMapProp);
            materialEditor.TexturePropertySingleLine(animMapNormalTitle, animMapNormalProp);
            materialEditor.ShaderProperty(animLenProp, animLenTitle);
            materialEditor.ShaderProperty(loopProp, loopPropTitle);
            materialEditor.ShaderProperty(currentTimeProp, currentTimeTitle);
            EditorGUILayout.Space();

            base.DrawSurfaceInputs(material);
            LitGUI.Inputs(litProperties, materialEditor, material);
            DrawEmissionProperties(material, keyword: true);
            DrawTileOffset(materialEditor, baseMapProp);
        }

        public override void DrawAdvancedOptions(Material material)
        {
            if (litProperties.reflections != null && litProperties.highlights != null)
            {
                materialEditor.ShaderProperty(litProperties.highlights, LitGUI.Styles.highlightsText);
                materialEditor.ShaderProperty(litProperties.reflections, LitGUI.Styles.reflectionsText);
            }

            base.DrawAdvancedOptions(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
            {
                throw new ArgumentNullException("material");
            }

            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialBlendMode(material);
                return;
            }

            SurfaceType surfaceType = SurfaceType.Opaque;
            BlendMode blendMode = BlendMode.Alpha;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                surfaceType = SurfaceType.Opaque;
                material.SetFloat("_AlphaClip", 1f);
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                surfaceType = SurfaceType.Transparent;
                blendMode = BlendMode.Alpha;
            }

            material.SetFloat("_Blend", (float)blendMode);
            material.SetFloat("_Surface", (float)surfaceType);
            if (surfaceType == SurfaceType.Opaque)
            {
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            if (oldShader.name.Equals("Standard (Specular setup)"))
            {
                material.SetFloat("_WorkflowMode", 0f);
                Texture texture = material.GetTexture("_SpecGlossMap");
                if (texture != null)
                {
                    material.SetTexture("_MetallicSpecGlossMap", texture);
                }
            }
            else
            {
                material.SetFloat("_WorkflowMode", 1f);
                Texture texture2 = material.GetTexture("_MetallicGlossMap");
                if (texture2 != null)
                {
                    material.SetTexture("_MetallicSpecGlossMap", texture2);
                }
            }
        }
    }
}