using UnityEngine;

public static class RuntimeSpriteUtility
{
    static Sprite whiteSprite;

    public static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite == null)
            {
                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            }

            return whiteSprite;
        }
    }
}
