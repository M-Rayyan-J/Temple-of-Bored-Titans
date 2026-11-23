using UnityEngine;

namespace TempleOfBoredTitans.Player
{
    /// <summary>Drives the imported Lumberjack legacy clips from the hunter's movement input.</summary>
    public sealed class PlayerAvatarLocomotion : MonoBehaviour
    {
        private Animation legacyAnimation;
        private string idleClip = "Idle";
        private string walkClip = "Walk";
        private string runClip = "Walk";
        private string activeClip;

        public void Configure(Animation animation, string idle, string walk, string run)
        {
            legacyAnimation = animation;
            if (!string.IsNullOrEmpty(idle)) idleClip = idle;
            if (!string.IsNullOrEmpty(walk)) walkClip = walk;
            if (!string.IsNullOrEmpty(run)) runClip = run;
            InitializeIdle();
        }

        private void Awake()
        {
            if (legacyAnimation == null) legacyAnimation = GetComponentInChildren<Animation>(true);
            InitializeIdle();
        }

        private void InitializeIdle()
        {
            if (legacyAnimation == null) return;
            legacyAnimation.playAutomatically = false;
            legacyAnimation.Stop();
            if (legacyAnimation.GetClip(idleClip) != null) Play(idleClip);
            else if (legacyAnimation.clip != null) Play(legacyAnimation.clip.name);
        }

        public void SetMovement(bool moving, bool running)
        {
            if (legacyAnimation == null) return;
            string wanted = !moving ? idleClip : (running ? runClip : walkClip);
            AnimationState state = legacyAnimation[wanted];
            if (state != null) state.speed = running ? 1.5f : 1f;
            if (wanted == activeClip || legacyAnimation.GetClip(wanted) == null) return;
            Play(wanted);
        }

        private void Play(string clipName)
        {
            if (legacyAnimation == null || legacyAnimation.GetClip(clipName) == null) return;
            legacyAnimation.CrossFade(clipName, .12f, PlayMode.StopSameLayer);
            activeClip = clipName;
        }
    }
}
