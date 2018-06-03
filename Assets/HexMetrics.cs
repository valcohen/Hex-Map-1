using UnityEngine;

public class HexMetrics {

    public const float outerRadius = 10f;
    public const float innerRadius = outerRadius * 0.866025404f; // .866 == sqrt(3) / 2
    public const float solidFactor = 0.75f; // 0 = all border, 1 = all hex
    public const float blendFactor = 1f - solidFactor;

    static Vector3[] corners = {
        new Vector3(0f,             0f,  outerRadius),
        new Vector3(innerRadius,    0f,  0.5f * outerRadius),
        new Vector3(innerRadius,    0f, -0.5f * outerRadius),
        new Vector3(0f,             0f, -outerRadius),
        new Vector3(-innerRadius,   0f, -0.5f * outerRadius),
        new Vector3(-innerRadius,   0f,  0.5f * outerRadius),
        new Vector3(0f,             0f,  outerRadius)
    };

    public static Vector3 GetFirstCorner (HexDirection direction) {
        return corners[(int)direction];
    }

    public static Vector3 GetSecondCorner (HexDirection direction) {
        return corners[(int)direction + 1];
    }

    public static Vector3 GetFirstSolidCorner (HexDirection direction) {
        return corners[(int)direction] * solidFactor;
    }

    public static Vector3 GetSecondSolidCorner (HexDirection direction) {
        return corners[(int)direction + 1] * solidFactor;
    }

    /*
     *   v3-+-+-+-v4
     *    \ |X|X| /
     *     v1-+-v2
     *      \ | /
     *       \|/
     *        v center
     */
    public static Vector3 GetBridge(HexDirection direction) {
        return (corners[(int)direction] + corners[(int)direction + 1]) *
            0.5f * blendFactor;
    }
}
