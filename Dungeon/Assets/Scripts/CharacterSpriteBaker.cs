using System.IO;
using UnityEditor;
using UnityEngine;

public class CharacterSpriteBaker : EditorWindow
{
	[Header("Input")]
	public GameObject characterPrefab;

	[Header("Output")]
	public string outputFolder = "Assets/Art/Characters/Baked/LivingBrother";
	public string outputName = "LivingBrother";

	[Header("Render Settings")]
	public int width = 512;
	public int height = 512;
	public float orthographicSize = 1.6f;
	public Vector3 cameraPosition = new Vector3(0f, 1.8f, -5f);
	public Vector3 cameraEuler = new Vector3(20f, 0f, 0f);
	public Color backgroundColor = new Color(0f, 0f, 0f, 0f);

	[Header("Sprite Settings")]
	public float pixelsPerUnit = 128f;
	public bool createCombinedSpriteSheet = true;

	private readonly string[] _directionNames =
	{
		"South",
		"West",
		"North",
		"East"
	};

	// Adjust these if your model faces another direction by default.
	private readonly float[] _yRotations =
	{
		180f, // South / front
        90f,  // West
        0f,   // North / back
        270f  // East
    };

	[MenuItem("Tools/Character Sprite Baker")]
	public static void Open()
	{
		GetWindow<CharacterSpriteBaker>("Character Sprite Baker");
	}

	private void OnGUI()
	{
		GUILayout.Label("Character Sprite Baker", EditorStyles.boldLabel);

		characterPrefab = (GameObject)EditorGUILayout.ObjectField(
			"Character Prefab",
			characterPrefab,
			typeof(GameObject),
			false
		);

		outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
		outputName = EditorGUILayout.TextField("Output Name", outputName);

		EditorGUILayout.Space();

		width = EditorGUILayout.IntField("Width", width);
		height = EditorGUILayout.IntField("Height", height);
		orthographicSize = EditorGUILayout.FloatField("Orthographic Size", orthographicSize);
		cameraPosition = EditorGUILayout.Vector3Field("Camera Position", cameraPosition);
		cameraEuler = EditorGUILayout.Vector3Field("Camera Rotation", cameraEuler);
		backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);

		EditorGUILayout.Space();

		pixelsPerUnit = EditorGUILayout.FloatField("Pixels Per Unit", pixelsPerUnit);
		createCombinedSpriteSheet = EditorGUILayout.Toggle("Create Sprite Sheet", createCombinedSpriteSheet);

		EditorGUILayout.Space();

		if (GUILayout.Button("Bake 4 Directions"))
		{
			Bake();
		}
	}

	private void Bake()
	{
		if (characterPrefab == null)
		{
			Debug.LogError("Character prefab is not assigned.");
			return;
		}

		if (!Directory.Exists(outputFolder))
		{
			Directory.CreateDirectory(outputFolder);
			AssetDatabase.Refresh();
		}

		var renderTextures = new Texture2D[4];

		GameObject instance = null;
		Camera bakeCamera = null;
		Light keyLight = null;
		Light fillLight = null;

		try
		{
			instance = (GameObject)PrefabUtility.InstantiatePrefab(characterPrefab);
			instance.name = characterPrefab.name + "_BakeInstance";
			instance.transform.position = Vector3.zero;
			instance.transform.rotation = Quaternion.identity;

			SetLayerRecursive(instance, LayerMask.NameToLayer("CharacterBake"));

			bakeCamera = CreateBakeCamera();
			keyLight = CreateDirectionalLight("Key Light", new Vector3(45f, -30f, 0f), 1.0f);
			fillLight = CreateDirectionalLight("Fill Light", new Vector3(25f, 140f, 0f), 0.35f);

			for (int i = 0; i < 4; i++)
			{
				instance.transform.rotation = Quaternion.Euler(0f, _yRotations[i], 0f);

				Texture2D frame = RenderFrame(bakeCamera);
				renderTextures[i] = frame;

				string fileName = $"{outputName}_{_directionNames[i]}.png";
				string path = Path.Combine(outputFolder, fileName);

				File.WriteAllBytes(path, frame.EncodeToPNG());
				Debug.Log($"Saved: {path}");
			}

			AssetDatabase.Refresh();

			for (int i = 0; i < 4; i++)
			{
				string fileName = $"{outputName}_{_directionNames[i]}.png";
				string path = Path.Combine(outputFolder, fileName);
				ImportAsSprite(path, SpriteImportMode.Single);
			}

			if (createCombinedSpriteSheet)
			{
				CreateHorizontalSpriteSheet(renderTextures);
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			Debug.Log("Character sprite bake complete.");
		}
		finally
		{
			if (instance != null) DestroyImmediate(instance);
			if (bakeCamera != null) DestroyImmediate(bakeCamera.gameObject);
			if (keyLight != null) DestroyImmediate(keyLight.gameObject);
			if (fillLight != null) DestroyImmediate(fillLight.gameObject);

			foreach (Texture2D tex in renderTextures)
			{
				if (tex != null) DestroyImmediate(tex);
			}
		}
	}

	private Camera CreateBakeCamera()
	{
		var cameraObject = new GameObject("Bake Camera");
		var cam = cameraObject.AddComponent<Camera>();

		cam.transform.position = cameraPosition;
		cam.transform.rotation = Quaternion.Euler(cameraEuler);

		cam.orthographic = true;
		cam.orthographicSize = orthographicSize;
		cam.clearFlags = CameraClearFlags.SolidColor;
		cam.backgroundColor = backgroundColor;
		cam.allowHDR = false;
		cam.allowMSAA = true;

		int layer = LayerMask.NameToLayer("CharacterBake");
		if (layer >= 0)
			cam.cullingMask = 1 << layer;

		return cam;
	}

	private Light CreateDirectionalLight(string name, Vector3 euler, float intensity)
	{
		var lightObject = new GameObject(name);
		var light = lightObject.AddComponent<Light>();

		light.type = LightType.Directional;
		light.transform.rotation = Quaternion.Euler(euler);
		light.intensity = intensity;
		light.color = Color.white;

		return light;
	}

	private Texture2D RenderFrame(Camera cam)
	{
		var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
		{
			antiAliasing = 4
		};

		cam.targetTexture = rt;

		RenderTexture previous = RenderTexture.active;
		RenderTexture.active = rt;

		cam.Render();

		var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
		texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
		texture.Apply();

		cam.targetTexture = null;
		RenderTexture.active = previous;

		rt.Release();
		DestroyImmediate(rt);

		return texture;
	}

	private void CreateHorizontalSpriteSheet(Texture2D[] frames)
	{
		int sheetWidth = width * 4;
		int sheetHeight = height;

		var sheet = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGBA32, false);

		Color clear = new Color(0f, 0f, 0f, 0f);
		var clearPixels = new Color[sheetWidth * sheetHeight];
		for (int i = 0; i < clearPixels.Length; i++)
			clearPixels[i] = clear;

		sheet.SetPixels(clearPixels);

		for (int i = 0; i < frames.Length; i++)
		{
			sheet.SetPixels(i * width, 0, width, height, frames[i].GetPixels());
		}

		sheet.Apply();

		string sheetPath = Path.Combine(outputFolder, $"{outputName}_4Directions.png");
		File.WriteAllBytes(sheetPath, sheet.EncodeToPNG());

		DestroyImmediate(sheet);

		AssetDatabase.Refresh();

		ImportAsMultipleSpriteSheet(sheetPath);
		Debug.Log($"Saved sprite sheet: {sheetPath}");
	}

	private void ImportAsSprite(string path, SpriteImportMode mode)
	{
		TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

		if (importer == null)
		{
			Debug.LogError($"Could not import texture at path: {path}");
			return;
		}

		importer.textureType = TextureImporterType.Sprite;
		importer.spriteImportMode = mode;
		importer.spritePixelsPerUnit = pixelsPerUnit;
		importer.alphaIsTransparency = true;
		importer.mipmapEnabled = false;
		importer.filterMode = FilterMode.Bilinear;
		importer.textureCompression = TextureImporterCompression.Uncompressed;

		EditorUtility.SetDirty(importer);
		importer.SaveAndReimport();
	}

	private void ImportAsMultipleSpriteSheet(string path)
	{
		TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

		if (importer == null)
		{
			Debug.LogError($"Could not import sprite sheet at path: {path}");
			return;
		}

		importer.textureType = TextureImporterType.Sprite;
		importer.spriteImportMode = SpriteImportMode.Multiple;
		importer.spritePixelsPerUnit = pixelsPerUnit;
		importer.alphaIsTransparency = true;
		importer.mipmapEnabled = false;
		importer.filterMode = FilterMode.Bilinear;
		importer.textureCompression = TextureImporterCompression.Uncompressed;

#pragma warning disable 0618
		var metadata = new SpriteMetaData[4];

		for (int i = 0; i < 4; i++)
		{
			metadata[i] = new SpriteMetaData
			{
				name = $"{outputName}_{_directionNames[i]}",
				rect = new Rect(i * width, 0, width, height),
				alignment = (int)SpriteAlignment.BottomCenter,
				pivot = new Vector2(0.5f, 0f)
			};
		}

		importer.spritesheet = metadata;
#pragma warning restore 0618

		EditorUtility.SetDirty(importer);
		importer.SaveAndReimport();
	}

	private void SetLayerRecursive(GameObject obj, int layer)
	{
		if (layer < 0)
		{
			Debug.LogWarning("Layer 'CharacterBake' does not exist. Create it in Project Settings > Tags and Layers.");
			return;
		}

		obj.layer = layer;

		foreach (Transform child in obj.transform)
		{
			SetLayerRecursive(child.gameObject, layer);
		}
	}
}