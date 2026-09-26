using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Basket.Characters;

namespace Basket.Presentation
{
    // Real clips (when a CharacterVisualDefinition provides them) through the Playables API.
    // No Animator Controller asset: the pose -> clip mapping is data, the blending is code.
    //
    //   layer 0  full body: locomotion (idle / walk / run / backpedal by speed), with a
    //            full-body action clip (defense, jump, ...) crossfaded over it
    //   layer 1  upper body only (avatar mask): dribble / hold-ball over whatever the
    //            legs are doing, so a dribbler keeps running
    //
    // Poses with no clip are left to the procedural animator (see PlayerAnimationDriver).
    public sealed class ClipAnimationBackend : IDisposable
    {
        private const float CrossfadeSeconds = 0.15f;
        private const int LocoIdle = 0, LocoWalk = 1, LocoRun = 2, LocoBack = 3;

        private readonly CharacterAnimationClips clips;
        private PlayableGraph graph;
        private readonly AnimationLayerMixerPlayable layers;
        private readonly AnimationMixerPlayable fullBody;
        private readonly AnimationMixerPlayable locomotion;
        private readonly AnimationClipPlayable[] locoClips = new AnimationClipPlayable[4];
        private readonly ActionSlot fullAction, upperAction;

        public ClipAnimationBackend(Animator animator, CharacterAnimationClips clips)
        {
            if (clips == null || !clips.HasLocomotion) throw new ArgumentException("Clip backend needs idle and run clips.");
            this.clips = clips;
            graph = PlayableGraph.Create("CharacterAnimation");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var output = AnimationPlayableOutput.Create(graph, "Animation", animator);

            locomotion = AnimationMixerPlayable.Create(graph, 4);
            ConnectLoco(LocoIdle, clips.idle);
            ConnectLoco(LocoWalk, clips.walk);
            ConnectLoco(LocoRun, clips.run);
            ConnectLoco(LocoBack, clips.runBackward);

            fullBody = AnimationMixerPlayable.Create(graph, 2);
            graph.Connect(locomotion, 0, fullBody, 0);
            fullBody.SetInputWeight(0, 1f);

            layers = AnimationLayerMixerPlayable.Create(graph, 2);
            graph.Connect(fullBody, 0, layers, 0);
            layers.SetInputWeight(0, 1f);
            layers.SetLayerMaskFromAvatarMask(1, UpperBodyMask());

            fullAction = new ActionSlot(graph, fullBody, 1);
            upperAction = new ActionSlot(graph, layers, 1);
            output.SetSourcePlayable(layers);
            graph.Play();
        }

        // Which clip, if any, plays this pose (and on which layer).
        public bool HasClipFor(AnimPose pose) => UpperClip(pose) != null || FullClip(pose) != null;

        // speed: horizontal m/s; forward: its component along the body's facing.
        public void Update(AnimPose pose, float speed, float forward, float dt)
        {
            LocomotionWeights w = LocomotionBlend.Compute(speed, forward, clips.walkSpeed, clips.runSpeed,
                clips.walk != null, clips.runBackward != null);
            SetLoco(LocoIdle, w.Idle, 1f);
            SetLoco(LocoWalk, w.Walk, w.WalkRate);
            SetLoco(LocoRun, w.Run, w.RunRate);
            SetLoco(LocoBack, w.Back, w.BackRate);

            fullAction.Update(FullClip(pose), pose, dt);
            upperAction.Update(UpperClip(pose), pose, dt);
            fullBody.SetInputWeight(0, 1f - fullAction.Weight);
        }

        private AnimationClip UpperClip(AnimPose pose) => pose switch
        {
            AnimPose.Dribble => clips.dribble,
            AnimPose.HoldBall => clips.holdBall,
            _ => null
        };

        private AnimationClip FullClip(AnimPose pose) => pose switch
        {
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

        private void ConnectLoco(int slot, AnimationClip clip)
        {
            if (clip == null) return;
            locoClips[slot] = AnimationClipPlayable.Create(graph, clip);
            graph.Connect(locoClips[slot], 0, locomotion, slot);
        }

        private void SetLoco(int slot, float weight, float rate)
        {
            if (!locoClips[slot].IsValid()) return;
            locomotion.SetInputWeight(slot, weight);
            locoClips[slot].SetSpeed(rate);
        }

        // Arms, spine and head; the legs and hips stay with the full-body layer.
        private static AvatarMask UpperBodyMask()
        {
            var mask = new AvatarMask();
            for (var part = AvatarMaskBodyPart.Root; part < AvatarMaskBodyPart.LastBodyPart; part++)
                mask.SetHumanoidBodyPartActive(part, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            return mask;
        }

        public void Dispose()
        {
            if (graph.IsValid()) graph.Destroy();
        }

        // One crossfaded action input of a mixer: swaps the clip when the pose changes and
        // restarts it for a new action of the same kind (e.g. a second jump shot).
        private sealed class ActionSlot
        {
            private readonly PlayableGraph graph;
            private readonly Playable mixer;
            private readonly int input;
            private AnimationClipPlayable playable;
            private AnimationClip clip;
            private AnimPose lastPose;

            public float Weight { get; private set; }

            public ActionSlot(PlayableGraph graph, Playable mixer, int input)
            {
                this.graph = graph;
                this.mixer = mixer;
                this.input = input;
            }

            public void Update(AnimationClip wanted, AnimPose pose, float dt)
            {
                if (wanted != null && wanted != clip)
                {
                    if (playable.IsValid())
                    {
                        graph.Disconnect(mixer, input);
                        playable.Destroy();
                    }
                    clip = wanted;
                    playable = AnimationClipPlayable.Create(graph, wanted);
                    graph.Connect(playable, 0, mixer, input);
                }
                else if (wanted != null && pose != lastPose && playable.IsValid() && !wanted.isLooping)
                {
                    playable.SetTime(0);
                }
                lastPose = pose;
                // Back to what is underneath: the last clip fades out instead of cutting.
                Weight = MoveTowards(Weight, wanted != null ? 1f : 0f, dt / CrossfadeSeconds);
                if (playable.IsValid()) mixer.SetInputWeight(input, Weight);
            }

            private static float MoveTowards(float from, float to, float step) =>
                Mathf.Abs(to - from) <= step ? to : from + Mathf.Sign(to - from) * step;
        }
    }
}
