using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 导出的图片数据
/// </summary>
public class AtlasImageData 
{
	public string name;
	public int x;
	public int y;
	public int w;
	public int h;

	public int borderLeft;
	public int borderBottom;
	public int borderRight;
	public int borderTop;
	public string ToString(int i)
	{
		return string.Format("{5},name:{0},x:{1},y:{2},w:{3},h:{4},left:{6},bottom:{7},right:{8},top:{9}", 
			name, x, y, w, h,i,borderLeft, borderBottom, borderRight, borderTop);
	}
}

/// <summary>
/// 初始化后计算后的 图片数据
/// </summary>
public class ImageData
{
	public Texture2D texture;
	public Sprite sprite;
	public AtlasImageData data;
	public string name;
	public int x;
	public int y;
	public int width;
	public int height;
	public int number;
	public bool temporaryTexture;

	public int paddingLeft;
	public int paddingBottom;
	public int paddingRight;
	public int paddingTop;

	public AtlasImageData ConvertAtlasData(int tex_h)
	{
		return new AtlasImageData()
		{
			name = name,
			x = x,
			y = tex_h - (y + height),
			w = width,
			h = height
		};
	}

	public void SetTexture(Color32[] newPixels, int newWidth, int newHeight)
	{
		temporaryTexture = true;

		texture = new Texture2D(newWidth, newHeight);
		texture.name = name;
		texture.SetPixels32(newPixels);
		width = newWidth;
		height = newHeight;
		texture.Apply();
	}

	public void SetPadding(int left, int bottom, int right, int top)
	{
		paddingLeft = left;
		paddingBottom = bottom;
		paddingRight = right;
		paddingTop = top;
	}
}

public class AtlasPage
{
	public string atlasFileName;
	public string jsonDataFileName;
	public string lineDataFileName;
	public List<ImageData> pageData;
	public int width;
	public int height;
	public AtlasPage()
	{
		pageData = new List<ImageData>();
	}
}

public enum AtlasMode
{
	Freedom,
	Fixed
}


