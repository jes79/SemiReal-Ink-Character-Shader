using UnityEditor;
using UnityEngine;

// 강의 모드 담당자 캐릭터(Lecture/Resources/Lecture/*.png)는 작은 픽셀아트라
// 기본 임포트(Bilinear·압축·밉맵·2의 거듭제곱 맞춤)로 들어오면 흐릿하게 번진다.
// 이 폴더의 텍스처만 점 필터 · 무압축 · 원본 크기 그대로 · 밉맵 없음으로 들여온다.
public class LectureSpriteImporter : AssetPostprocessor
{
    const string Folder = "Assets/CharacterShaderTest/Lecture/Resources/Lecture/";

    // 설정을 바꾸면 숫자를 올린다 → 이 폴더의 텍스처가 자동으로 다시 임포트됨
    public override uint GetVersion() => 1;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Point;                          // 픽셀 경계를 그대로
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;               // 96x180 같은 원본 크기 유지
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
    }
}
