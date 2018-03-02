﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class UnlitShaderGUI : ShaderGUI
{
	public enum RenderingMode
	{
		Opaque = 0,
		Cutout = 1,
		Transparent = 2,
	}

	[System.Flags]
	public enum MaterialFlags
	{
		NoMaskScaleOffset = 1 << 0,
	}

	public const string RenderingModePropName = "_RenderingMode";
	public const string MainTexPropName = "_MainTex";
	public const string MaskPropName = "_Mask";
	public const string ColorPropName = "_Color";
	public const string CutoffPropName = "_Cutoff";
	public const string SrcBlendPropName = "_SrcBlend";
	public const string DstBlendPropName = "_DstBlend";
	public const string ZWritePropName = "_ZWrite";
	public const string MaterialFlagsPropName = "_MaterialFlags";

	static readonly GUIContent s_noMaskScaleOffsetLabel = new GUIContent("Same Tiling/Offset as Texture");

	bool m_firstTime = false;

	public override void OnGUI (MaterialEditor materialEditor, MaterialProperty[] properties)
	{
		Material mat = materialEditor.target as Material;

		if(!m_firstTime)
		{
			m_firstTime = true;
			MaterialChanged(mat);
		}

		EditorGUI.BeginChangeCheck();
		DrawProperties (materialEditor, properties);
		if (EditorGUI.EndChangeCheck())
		{
			MaterialChanged(materialEditor.target as Material);
		}
	}

	static void DrawProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
	{
		materialEditor.SetDefaultGUIWidths();

		MaterialProperty materialFlagesProp = FindProperty(MaterialFlagsPropName, properties);
		MaterialFlags materialFlags = (MaterialFlags)materialFlagesProp.floatValue;

		MaterialProperty renderingModeProp = DrawProperty(RenderingModePropName, materialEditor, properties);
		DrawProperty(MainTexPropName, materialEditor, properties);

		bool noMaskScaleOffset = GetMaterialFlag(materialFlags, MaterialFlags.NoMaskScaleOffset);

		MaterialProperty maskProp = FindProperty(MaskPropName, properties);
		bool maskScaleOffset = !noMaskScaleOffset;
		materialEditor.TextureProperty(maskProp, maskProp.displayName, maskScaleOffset);

		noMaskScaleOffset = EditorGUILayout.Toggle(s_noMaskScaleOffsetLabel, noMaskScaleOffset);
		materialFlags = SetMaterialFlag(materialFlags, MaterialFlags.NoMaskScaleOffset, noMaskScaleOffset);

		DrawProperty(ColorPropName, materialEditor, properties);

		if((RenderingMode)renderingModeProp.floatValue == RenderingMode.Cutout)
			DrawProperty(CutoffPropName, materialEditor, properties);

		materialFlagesProp.floatValue = (float)materialFlags;

		EditorGUILayout.Space();
		EditorGUILayout.Space();
		materialEditor.RenderQueueField();
		materialEditor.EnableInstancingField();
		materialEditor.DoubleSidedGIField();
	}

	static MaterialProperty DrawProperty(string propertyName, MaterialEditor materialEditor, MaterialProperty[] properties)
	{
		MaterialProperty property = FindProperty(propertyName, properties);
		materialEditor.ShaderProperty(property, property.displayName);
		return property;
	}

	void MaterialChanged(Material mat)
	{
		MaterialFlags materialFlags = (MaterialFlags)mat.GetInt(MaterialFlagsPropName);
		
		bool noTexture = mat.GetTexture(MainTexPropName) == null;
		Vector2 textureScale = mat.GetTextureScale(MainTexPropName);
		Vector2 textureOffset = mat.GetTextureOffset(MainTexPropName);
		bool noMask = mat.GetTexture(MaskPropName) == null;
		Vector2 maskScale = mat.GetTextureScale(MaskPropName);
		Vector2 maskOffset = mat.GetTextureOffset(MaskPropName);
		bool sameMaskScale = maskScale == textureScale;
		bool sameMaskOffset = maskOffset == textureOffset;
		bool noMaskScaleOffsetFlag = GetMaterialFlag(materialFlags, MaterialFlags.NoMaskScaleOffset);
		bool noMaskScaleOffset = noMaskScaleOffsetFlag || sameMaskScale && sameMaskOffset;
		bool noColor = mat.GetColor(ColorPropName) == Color.white;
		EnableKeyword(mat, "_TEXTURE_OFF", noTexture);
		EnableKeyword(mat, "_MASK_OFF", noMask);
		EnableKeyword(mat, "_MASK_SCALE_OFFSET_OFF", noMaskScaleOffset && !noMask);
		EnableKeyword(mat, "_COLOR_OFF", noColor);

		RenderingMode renderingMode = (RenderingMode)mat.GetInt(RenderingModePropName);

		switch (renderingMode)
		{
			case RenderingMode.Opaque:
				mat.SetOverrideTag("RenderType", "");
				mat.SetInt(SrcBlendPropName, (int)UnityEngine.Rendering.BlendMode.One);
				mat.SetInt(DstBlendPropName, (int)UnityEngine.Rendering.BlendMode.Zero);
				mat.SetInt(ZWritePropName, 1);
				EnableKeyword(mat, "_ALPHATEST_ON", false);
				mat.renderQueue = -1;
				break;

			case RenderingMode.Cutout:
				mat.SetOverrideTag("RenderType", "TransparentCutout");
				mat.SetInt(SrcBlendPropName, (int)UnityEngine.Rendering.BlendMode.One);
				mat.SetInt(DstBlendPropName, (int)UnityEngine.Rendering.BlendMode.Zero);
				mat.SetInt(ZWritePropName, 1);
				bool alphaTest = mat.GetFloat(CutoffPropName) > 0f;
				EnableKeyword(mat, "_ALPHATEST_ON", alphaTest);
				mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
				break;

			case RenderingMode.Transparent:
				mat.SetOverrideTag("RenderType", "Transparent");
				mat.SetInt(SrcBlendPropName, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				mat.SetInt(DstBlendPropName, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				mat.SetInt(ZWritePropName, 0);
				EnableKeyword(mat, "_ALPHATEST_ON", false);
				mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
				break;
		}
	}

	static bool GetMaterialFlag(MaterialFlags allFlags, MaterialFlags flag)
	{
		return (allFlags & flag) != 0;
	}

	static MaterialFlags SetMaterialFlag(MaterialFlags allFlags, MaterialFlags flag, bool enable)
	{
		if(enable)
			return allFlags | flag;
		else
			return allFlags & ~flag;
	}

	static void EnableKeyword(Material material, string keyword, bool enable)
	{
		if(enable)
			material.EnableKeyword(keyword);
		else
			material.DisableKeyword(keyword);
	}
}
