using UnityEngine;

// 무기를 손에 쥐는 자세: 손 뼈 기준 위치·회전.
// LabCharacter가 무기 칸을 바꿀 때 지금 입은 옷의 손 뼈에 이 자세로 붙인다
public class HandGrip : MonoBehaviour
{
    public HumanBodyBones bone = HumanBodyBones.RightHand;
    public Vector3 localPosition;
    public Quaternion localRotation = Quaternion.identity;
    public Vector3 localScale = Vector3.one;
}
