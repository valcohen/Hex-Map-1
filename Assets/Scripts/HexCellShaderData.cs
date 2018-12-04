using UnityEngine;

public class HexCellShaderData : MonoBehaviour {
    Texture2D cellTexture;

    public void Initalize(int x, int z) {
        if (cellTexture) {
            cellTexture.Resize(x, z);
        }
        else {
            cellTexture = new Texture2D(
                x, z, TextureFormat.RGBA32,
                false   /* mipmaps */,
                true    /* linear color space */
            );
            cellTexture.filterMode = FilterMode.Point;
            cellTexture.wrapMode   = TextureWrapMode.Clamp;
        }
    }
}
