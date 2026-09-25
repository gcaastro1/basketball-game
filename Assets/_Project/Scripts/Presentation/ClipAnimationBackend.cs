using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Basket.Characters;

namespace Basket.Presentation
{
    // Real clips (when a CharacterVisualDefinition provides them) through the Playables API:
    // an idle/run blend by speed, with the current pose's clip crossfaded on top. No
    // Animator Controller asset: the pose -> clip mapping is data, the blending is code.
    public sealed class ClipAnimationBackend : IDisposable
    {
        private const float CrossfadeSeconds = 0.15f;

        private readonly CharacterAnimationClips clips;
        private PlayableGraph graph;
        private readonly AnimationMixerPlayable root;
        private readonly AnimationMixerPlayable locomotion;
        private AnimationClipPlayable action;
        private AnimationClip actionClip;
        private float actionWeight;
        private AnimPose lastPose;

        public ClipAnimationBackend(Animator animator, CharacterAnimationClips clips)
        {
            if (clips == null || !clips.HasLocomotion) throw new ArgumentException("Clip backend needs idle and run clips.");
            this.clips = clips;
            graph = PlayableGraph.Create("CharacterAnimation");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var output = AnimationPlayableOutput.Create(graph, "Animation", animator);

            locomotion = AnimationMixerPlayable.Create(graph, 2);
            graph.Connect(AnimationClipPlayable.Create(graph, clips.idle), 0, locomotion, 0);
            graph.Connect(AnimationClipPlayable.Create(graph, clips.run), 0, locomotion, 1);

            root = AnimationMixerPlayable.Create(graph, 2);
            graph.Connect(locomotion, 0, root, 0);
            root.SetInputWeight(0, 1f);
            output.SetSourcePlayable(root);
            graph.Play();
        }

        public void Update(AnimPose pose, float speedRatio, float dt)
        {
            locomotion.SetInputWeight(0, 1f - speedRatio);
            locomotion.SetInputWeight(1, speedRatio);

            AnimationClip wanted = ClipFor(pose);
            if (wanted != null && wanted != actionClip)
            {
                if (action.IsValid())
                {
                    graph.Disconnect(root, 1);
                    action.Destroy();
                }
                actionClip = wanted;
                action = AnimationClipPlayable.Create(graph, wanted);
                graph.Connect(action, 0, root, 1);
            }
            else if (wanted != null && pose != lastPose && action.IsValid())
            {
                action.SetTime(0); // same clip, new action (e.g. a second jump shot)
            }
            lastPose = pose;
            // Back to locomotion: the last clip fades out instead of cutting.
            float target = wanted != null ? 1f : 0f;
            actionWeight = Mathf.MoveTowards(actionWeight, target, dt / CrossfadeSeconds);
            root.SetInputWeight(0, 1f - actionWeight);
            if (action.IsValid()) root.SetInputWeight(1, actionWeight);
        }

        private AnimationClip ClipFor(AnimPose pose) => pose switch
        {
            AnimPose.Dribble => clips.dribble,
            AnimPose.HoldBall => clips.holdBall,
            AnimPose.Defense => clips.defense,
            AnimPose.JumpShot => clips.jumpShot,
            AnimPose.Layup => clips.layup,
            AnimPose.Dunk => clips.dunk,
            AnimPose.Pass => clips.pass,
            AnimPose.Block => clips.block,
            AnimPose.Airborne => clips.airborne,
            AnimPose.Celebrate => clips.celebrate,
            _ => null
        };

        public void Dispose()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}
