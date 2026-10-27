using UnityEditor;

// 이름이 "_Mask.png"로 끝나는 텍스처는 색이 아니라 데이터(마스크)이므로 sRGB 변환을 끈다.
// (sRGB가 켜져 있으면 0.5 같은 중간값이 어둡게 바뀌어 재질 판별 경계가 틀어진다)
public class MaskTextureImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/CharacterShaderTest/") || !assetPath.EndsWith("_Mask.png"))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = false;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.mipmapEnabled = true;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
    }
}
