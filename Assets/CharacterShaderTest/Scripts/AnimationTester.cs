using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// 애니메이션 테스트: 클립 하나를 여러 캐릭터(원본·새 쉐이더)에 똑같이 재생한다.
// Animator Controller 없이 Playable로 직접 재생 → 일시정지·속도·시간 이동(스크럽)이 자유롭다
public class AnimationTester
{
    class Track
    {
        public Animator animator;
        public Vector3 homePosition;      // 루트 모션으로 움직였다가 멈추면 돌아올 자리
        public Quaternion homeRotation;
        public PlayableGraph graph;
        public AnimationClipPlayable clipPlayable;
    }

    readonly List<Track> tracks = new();

    public AnimationClip Clip { get; private set; }
    public bool Playing { get; private set; }
    public bool Loop { get; set; } = true;
    public bool RootMotion { get; private set; }
    public float Speed { get; private set; } = 1f;
    public float Time => tracks.Count > 0 && Clip != null ? (float)tracks[0].clipPlayable.GetTime() : 0f;
    public float Length => Clip != null ? Clip.length : 0f;

    public AnimationTester(IEnumerable<Animator> animators)
    {
        foreach (var a in animators)
            if (a != null)
                tracks.Add(new Track { animator = a, homePosition = a.transform.localPosition, homeRotation = a.transform.localRotation });
    }

    public void Play(AnimationClip clip)
    {
        Stop();
        Clip = clip;
        if (clip == null)
            return;
        foreach (var t in tracks)
        {
            t.graph = PlayableGraph.Create($"ShaderLab Anim ({t.animator.name})");
            t.graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            t.clipPlayable = AnimationClipPlayable.Create(t.graph, clip);
            var output = AnimationPlayableOutput.Create(t.graph, "Animation", t.animator);
            output.SetSourcePlayable(t.clipPlayable);
            t.graph.Play();
        }
        Playing = true;
        SetSpeed(Speed);
    }

    // 클립을 멈추고 기본 자세(바인드 포즈)로 되돌린다
    public void Stop()
    {
        foreach (var t in tracks)
        {
            if (t.graph.IsValid())
                t.graph.Destroy();
            if (t.animator != null)
            {
                t.animator.Rebind();
                t.animator.Update(0f);
                t.animator.transform.SetLocalPositionAndRotation(t.homePosition, t.homeRotation);
            }
        }
        Clip = null;
        Playing = false;
    }

    public void SetPlaying(bool playing)
    {
        Playing = playing && Clip != null;
        ApplySpeed();
    }

    public void SetSpeed(float speed)
    {
        Speed = speed;
        ApplySpeed();
    }

    void ApplySpeed()
    {
        foreach (var t in tracks)
            if (t.clipPlayable.IsValid())
                t.clipPlayable.SetSpeed(Playing ? Speed : 0f);
    }

    // 원하는 시간으로 바로 이동 (일시정지 중에도 그 자세가 바로 보인다)
    public void Seek(float time)
    {
        if (Clip == null)
            return;
        time = Mathf.Clamp(time, 0f, Length);
        foreach (var t in tracks)
        {
            if (!t.clipPlayable.IsValid())
                continue;
            t.clipPlayable.SetTime(time);
            t.graph.Evaluate();
        }
    }

    // 프레임 단위로 앞뒤 이동
    public void StepFrames(int frames)
    {
        if (Clip == null)
            return;
        SetPlaying(false);
        float frameRate = Clip.frameRate > 0f ? Clip.frameRate : 30f;
        Seek(Time + frames / frameRate);
    }

    public void SetRootMotion(bool on)
    {
        RootMotion = on;
        foreach (var t in tracks)
        {
            if (t.animator == null)
                continue;
            t.animator.applyRootMotion = on;
            if (!on)
                t.animator.transform.SetLocalPositionAndRotation(t.homePosition, t.homeRotation);
        }
    }

    // 매 프레임: 반복 재생 / 끝에서 멈춤 처리
    public void Tick()
    {
        if (!Playing || Clip == null || Length <= 0f)
            return;
        float time = Time;
        if (time < Length)
            return;
        if (Loop)
            Seek(time % Length);
        else
        {
            Seek(Length);
            SetPlaying(false);
        }
    }

    public void Dispose() => Stop();
}
