using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/*
 * 1,图集大小根据计算规则改变尺寸
 * 2,图集尺寸都是边长为2的n次方
 * 3，图集数据会根据打包算法，收集每一张图片的数据
 * 4，图片像素化，打包成图集图片
 */
public sealed class AtlasCore
{

	public static AtlasCore ins { get; } = new AtlasCore();
	/* 图集规则
	 * 1,图集放置图片，是在0点坐标开始放置。
	 * 2，x坐标水平放置
	 * 3，y坐标水平放置
	 * 4，先放置大图片图集，在放置小图片图集。
	 * 5，根据上一张图片的，来计算下一张图片的位置。
	 */
	private string m_AtlasFileName;

	private const int MAXWIDTH = 4096;
	private const int MAXHEIGHT = 4096;
	private const string EXT_IMAGE = ".png";

	//文件数据目录	
	private readonly string saveFilePathDir = Application.dataPath;
	

	/* 打包顺序
	 * 1，先初始化图片数据
	 * 2，开始计算打包数据，图片在图集中的位置。
	 * 3，开始打包：根据图片数据，像素化数据打包成一张图片。
	 */
	//导出png格式的图集
	public void ExportAtlasImage(List<Texture2D> texture2Ds, string dirName)
	{
		List<ImageData>  m_loadDatas = CreateSprites(texture2Ds);
		Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);

		List<AtlasImageData> atlasImageDatas = new List<AtlasImageData>();
		List<string> arrayLst = new List<string>();

		if (PackTextures(tex, m_loadDatas))
		{
			int length = m_loadDatas.Count;

			for (int i = 0; i < length; i++)
			{
				ImageData img = m_loadDatas[i];

				var convertData = img.ConvertAtlasData(tex.height );

				atlasImageDatas.Add(convertData);

				arrayLst.Add(convertData.ToString(i));
			}

			//将图片转化为 图集
			byte[] buffer = GetImageExtend(tex);//

			string assetPath = saveFilePathDir + "/" + dirName + EXT_IMAGE;
			File.WriteAllBytes(assetPath, buffer);

			string saveFilePath = saveFilePathDir + "/" + dirName+ ".json";
			string jsonText = UnityEngine.JsonUtility.ToJson(atlasImageDatas);
			File.WriteAllText(saveFilePath, jsonText, System.Text.Encoding.UTF8);

			File.WriteAllLines(saveFilePathDir + "/" + dirName + ".txt", arrayLst.ToArray(), System.Text.Encoding.UTF8);

#if UNITY_EDITOR
			AssetDatabase.SaveAssets();

			AssetDatabase.Refresh();

			assetPath = assetPath.Replace("\\", "/").Replace(Application.dataPath, "Assets");

			TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

			if (textureImporter)
			{
				textureImporter.textureType = TextureImporterType.Sprite;

				textureImporter.mipmapEnabled = false;

				textureImporter.isReadable = true;

				textureImporter.alphaIsTransparency = true;

				textureImporter.sRGBTexture = false;

				textureImporter.SaveAndReimport();
			}
			
			AssetDatabase.Refresh();
#endif
			Debug.Log("fininsh! "+ length);
		}


	}

	private byte[] GetImageExtend(Texture2D tex)
	{
		if (EXT_IMAGE == ".png")
		{
			return tex.EncodeToPNG();
		}
		else if (EXT_IMAGE == ".tga")
		{
			return tex.EncodeToTGA();
		}
		else
		{
			return tex.EncodeToJPG();
		}
	}

	
	public  bool PackTextures(Texture2D tex, List<ImageData> sprites)
	{
		Texture2D[] textures = new Texture2D[sprites.Count];

		Rect[] rects;

#if UNITY_ANDROID || UNITY_IPHONE || UNITY_EDITOR
		int maxSize = MAXHEIGHT;
#endif
		if (TexturePacker.unityPacking)
		{
			for (int i = 0; i < sprites.Count; ++i) textures[i] = sprites[i].texture;

			rects = tex.PackTextures(textures, 1, maxSize);
		}
		else
		{
			sprites.Sort(Compare);
			for (int i = 0; i < sprites.Count; ++i)
			{
				textures[i] = sprites[i].texture;
			}

			rects = TexturePacker.PackTextures(tex, textures, 4, 4, 1, maxSize);
		}

		for (int i = 0; i < sprites.Count; ++i)
		{
			Rect rect = ConvertToPixels(rects[i], tex.width, tex.height, true);

			// Apparently Unity can take the liberty of destroying temporary textures without any warning
			if (textures[i] == null) return false;

			// Make sure that we don't shrink the textures
			if (Mathf.RoundToInt(rect.width) != textures[i].width) return false;

			ImageData se = sprites[i];

			se.x = Mathf.RoundToInt(rect.x);

			se.y = Mathf.RoundToInt(rect.y);

			se.width = Mathf.RoundToInt(rect.width);

			se.height = Mathf.RoundToInt(rect.height);
		}
		return true;
	}

	/// <summary>
	/// 这个方法和<see cref="Texture2D.GenerateAtlas"/>方法功能一样
	/// </summary>
	/// <param name="tex"></param>
	/// <param name="inputTextureList"></param>
	/// <returns></returns>
	public bool PackTextures(out TexturePacker.RectInfo tex, List<TexturePacker.RectInfo> inputTextureList)
	{
		TexturePacker.RectInfo[] textures = new TexturePacker.RectInfo[inputTextureList.Count];

		Rect[] rects;

		inputTextureList.Sort(Compare);

		for (int i = 0; i < inputTextureList.Count; ++i)
		{
			textures[i] = inputTextureList[i];
		}

		rects = TexturePacker.PackTextures(out tex, textures, 4, 4, 0);

		if (rects == null) 
			return false;

		for (int i = 0; i < inputTextureList.Count; ++i)
		{
			Rect rect = ConvertToPixels(rects[i], tex.width, tex.height, true);

			if (Mathf.RoundToInt(rect.width) != textures[i].width) return false;

			var se = inputTextureList[i];

			se.x = Mathf.RoundToInt(rect.x);

			se.y = Mathf.RoundToInt(rect.y);

			se.width = Mathf.RoundToInt(rect.width);

			se.height = Mathf.RoundToInt(rect.height);

			se.y = tex.height - (se.y + se.height);

			inputTextureList[i] = se;
		}

		return true;
	}

	static  Rect ConvertToPixels(Rect rect, int width, int height, bool round)
	{
		Rect final = rect;

		if (round)
		{
			final.xMin = Mathf.RoundToInt(rect.xMin * width);

			final.xMax = Mathf.RoundToInt(rect.xMax * width);

			final.yMin = Mathf.RoundToInt((1f - rect.yMax) * height);

			final.yMax = Mathf.RoundToInt((1f - rect.yMin) * height);
		}
		else
		{
			final.xMin = rect.xMin * width;

			final.xMax = rect.xMax * width;

			final.yMin = (1f - rect.yMax) * height;

			final.yMax = (1f - rect.yMin) * height;
		}
		return final;
	}
	static int Compare(ImageData a, ImageData b)
	{
		// A is null b is not b is greater so put it at the front of the list
		if (a == null && b != null) return 1;

		// A is not null b is null a is greater so put it at the front of the list
		if (a != null && b == null) return -1;

		// Get the total pixels used for each sprite
		int aPixels = a.width * a.height;
		int bPixels = b.width * b.height;

		if (aPixels > bPixels) return -1;
		else if (aPixels < bPixels) return 1;
		return 0;
	}

	static int Compare(TexturePacker.RectInfo a, TexturePacker.RectInfo b)
	{
		// Get the total pixels used for each sprite
		int aPixels = a.width * a.height;
		int bPixels = b.width * b.height;

		if (aPixels > bPixels) return -1;
		else if (aPixels < bPixels) return 1;
		return 0;
	}

	public List<ImageData> CreateSprites(List<Texture2D> textures)
	{
		List<ImageData> list = new List<ImageData>();

		foreach (var tex in textures)
		{
			Texture2D oldTex = tex;
			Color32[] pixels = oldTex.GetPixels32();

			int xmin = oldTex.width;
			int xmax = 0;
			int ymin = oldTex.height;
			int ymax = 0;
			int oldWidth = oldTex.width;
			int oldHeight = oldTex.height;

			// Find solid pixels

			for (int y = 0, yw = oldHeight; y < yw; ++y)
			{
				for (int x = 0, xw = oldWidth; x < xw; ++x)
				{
					Color32 c = pixels[y * xw + x];

					if (c.a != 0)
					{
						if (y < ymin) ymin = y;
						if (y > ymax) ymax = y;
						if (x < xmin) xmin = x;
						if (x > xmax) xmax = x;
					}
				}
			}


			int newWidth = (xmax - xmin) + 1;
			int newHeight = (ymax - ymin) + 1;

			if (newWidth > 0 && newHeight > 0)
			{
				ImageData sprite = new ImageData();
				sprite.x = 0;
				sprite.y = 0;
				sprite.width = oldTex.width;
				sprite.height = oldTex.height;

				// If the dimensions match, then nothing was actually trimmed
				if (newWidth == oldWidth && newHeight == oldHeight)
				{
					sprite.texture = oldTex;
					sprite.name = oldTex.name;
					sprite.temporaryTexture = false;
				}
				else
				{
					// Copy the non-trimmed texture data into a temporary buffer
					Color32[] newPixels = new Color32[newWidth * newHeight];

					for (int y = 0; y < newHeight; ++y)
					{
						for (int x = 0; x < newWidth; ++x)
						{
							int newIndex = y * newWidth + x;
							int oldIndex = (ymin + y) * oldWidth + (xmin + x);

							newPixels[newIndex] = pixels[oldIndex];
						}
					}

					// Create a new texture
					sprite.name = oldTex.name;
					sprite.SetTexture(newPixels, newWidth, newHeight);

					// Remember the padding offset
					sprite.SetPadding(xmin, ymin, oldWidth - newWidth - xmin, oldHeight - newHeight - ymin);
				}
				list.Add(sprite);
			}
		}
		return list;
	}
}

