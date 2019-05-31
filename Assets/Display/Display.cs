using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Display : MonoBehaviour
{
    [SerializeField]
    Image _displayImage;
    [SerializeField]
    SpownData _spownData;

    public void DoDisplay()
    {
        /*
         *  获取图片并在Image上显示
         */
        _displayImage.sprite = GetMapSprite();
    }

    Sprite GetMapSprite()
    {
        /*
         *  获取地图
         *  获取图片
         *  绘制
         *  转为Sprite返回
         */
        Map map = new DungeonSpowner().SpownMap(_spownData, GetEntrancePosition());

        Texture2D texture = GetTexture();

        DrawTexture(texture, map);

        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
    }

    Texture2D GetTexture()
    {
        Texture2D texture = new Texture2D(_spownData.width, _spownData.height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    Vector2 GetEntrancePosition()
    {
        return new Vector2(Random.Range(1, _spownData.width - 1), Random.Range(1, _spownData.height - 1));
    }

    void DrawTexture(Texture2D texture, Map map)
    {
        for (int x = 0; x < texture.width; x++)
            for (int y = 0; y < texture.height; y++)
                texture.SetPixel(x, y, GetQuadColor(map.GetQuadType(x, y)));
        texture.Apply();
    }

    Color GetQuadColor(QuadType quadType)
    {
        switch (quadType)
        {
            case QuadType.WALL:
                return Color.black;

            case QuadType.FLOOR:
                return Color.white;

            default:
                return Color.red;
        }
    }
}
