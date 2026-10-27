using UnityEditor;

// VARCO 3D로 만든 모델·텍스처 가져오기 규칙 (Models/Varco, Textures/Varco)
// - 캐릭터·애니메이션: Humanoid 리그 (뼈 이름이 Hips·Spine·LeftArm…이라 Unity가 자동으로 맞춘다) → 클립 하나를 세 캐릭터가 같이 쓴다
// - VARCO 모델은 키가 1m로 나와서 1.7배로 키운다. 무기는 실제 길이에 맞춘다
// - ORM·메탈릭 맵은 색이 아니라 데이터 → sRGB 끔, 노멀은 Normal Map
public class VarcoAssetImporter : AssetPostprocessor
{
    const string ModelDir = "Assets/CharacterShaderTest/Models/Varco/";
    const string TextureDir = "Assets/CharacterShaderTest/Textures/Varco/";
    public const float CharacterScale = 1.7f;

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(ModelDir))
            return;
        var importer = (ModelImporter)assetImporter;
        string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        bool animation = assetPath.StartsWith(ModelDir + "Animations/");
        bool weapon = file is "Sword" or "Bow" or "Staff";

        importer.materialImportMode = ModelImporterMaterialImportMode.None;   // 머티리얼은 장면 메뉴가 만든다
        importer.globalScale = file switch
        {
            "Sword" => 1f,
            "Bow" => 1.15f,
            "Staff" => 1.6f,
            _ => CharacterScale,
        };

        if (weapon)
        {
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            return;
        }

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = animation;
    }

    // 클립 이름 = 파일 이름. 대기·걷기·달리기는 반복, 제자리에서 재생되게 루트 위치·회전은 고정
    void OnPreprocessAnimation()
    {
        if (!assetPath.StartsWith(ModelDir + "Animations/"))
            return;
        var importer = (ModelImporter)assetImporter;
        string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips)
        {
            clip.name = file;
            clip.loopTime = file is "Idle" or "Walk" or "Run";
            clip.lockRootHeightY = true;
            clip.keepOriginalPositionY = true;
            clip.lockRootRotation = true;
            clip.keepOriginalOrientation = true;
        }
        importer.clipAnimations = clips;
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(TextureDir))
            return;
        var importer = (TextureImporter)assetImporter;
        if (assetPath.EndsWith("_Normal.png"))
            importer.textureType = TextureImporterType.NormalMap;
        else if (assetPath.EndsWith("_ORM.png") || assetPath.EndsWith("_MetallicSmoothness.png"))
            importer.sRGBTexture = false;
    }
}
