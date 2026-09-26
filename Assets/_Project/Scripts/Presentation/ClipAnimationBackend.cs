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
    //   layer 0  full body: three locomotion sets -- free / with the ball / in guard --
    //            crossfaded by state, each mixing idle, directional moves, run and turns
    //            (LocomotionBlend); a one-shot action clip (shots, block, ...) over them
    //   layer 1  upper body only (avatar mask): the dribble over running legs
    //
    // Every clip time is driven here (loop windows of long mocap takes, shots in step with
    // the gameplay shot). Poses with no clip are left to the procedural animator.
    public sealed class ClipAnimationBackend : IDisposable
    {
        private const float CrossfadeSeconds = 0.2f;

        private readonly CharacterAnimationClips clips;
        private PlayableGraph graph;
        private readonly AnimationLayerMixerPlayable layers;
        private readonly AnimationMixerPlayable fullBody;
        private readonly AnimationMixerPlayable sets;
        private readonly LocomotionLayer free, ball, guard;
        private readonly LoopPlayer upperDribble;
        private readonly ActionPlayer action;
        private float ballWeight, guardWeight, upperWeight;
        private AnimPose lastPose = AnimPose.Locomotion;
        private int jumpShotCount;
        private LocomotionMix freeMix, ballMix, guardMix;
        private float lastTempo = 1f;

        public ClipAnimationBackend(Animator animator, CharacterAnimationClips clips)
        {
            if (clips == null || !clips.HasLocomotion) throw new ArgumentException("Clip backend needs the free set's idle and run.");
            this.clips = clips;
            graph = PlayableGraph.Create("CharacterAnimation");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var output = AnimationPlayableOutput.Create(graph, "Animation", animator);

            sets = AnimationMixerPlayable.Create(graph, 3);
            free = new LocomotionLayer(graph, sets, 0, clips.free);
            ball = new LocomotionLayer(graph, sets, 1, clips.withBall);
            guard = new LocomotionLayer(graph, sets, 2, clips.guard);

            fullBody = AnimationMixerPlayable.Create(graph, 2);
            graph.Connect(sets, 0, fullBody, 0);
            fullBody.SetInputWeight(0, 1f);
            action = new ActionPlayer(graph, fullBody, 1);

            layers = AnimationLayerMixerPlayable.Create(graph, 2);
            graph.Connect(fullBody, 0, layers, 0);
            layers.SetInputWeight(0, 1f);
            layers.SetLayerMaskFromAvatarMask(1, UpperBodyMask());
            upperDribble = LoopPlayer.Create(graph, layers, 1, clips.dribbleUpperBody);

            output.SetSourcePlayable(layers);
            graph.Play();
        }

        // Whether this pose is animated by clips (else the procedural animator covers it).
        public bool HasClipFor(AnimPose pose)
        {
            switch (pose)
            {
                case AnimPose.Locomotion: return true;
                case AnimPose.Dribble:
                case AnimPose.HoldBall: return clips.withBall != null && clips.withBall.Any;
                case AnimPose.Defense: return clips.guard != null && clips.guard.Any;
                default: return ActionFor(pose).clip != null;
            }
        }

        // shotProgress: gameplay shot progress (0 start .. 1 release), negative when not shooting.
        // dribbleBounces: the gameplay dribble's bounce count (x.5 = ball in the hand), negative
        // when not dribbling; with dribbleBouncesInWindow set, the arm follows the real ball.
        public void Update(AnimPose pose, in LocomotionInput move, float dt, float shotProgress = -1f, float dribbleBounces = -1f)
        {
            float fade = dt / CrossfadeSeconds;
            bool withBall = (pose == AnimPose.Dribble || pose == AnimPose.HoldBall) && HasClipFor(AnimPose.Dribble);
            bool guarding = pose == AnimPose.Defense && HasClipFor(AnimPose.Defense);
            ballWeight = Mathf.MoveTowards(ballWeight, withBall ? 1f : 0f, fade);
            guardWeight = Mathf.MoveTowards(guardWeight, guarding ? 1f : 0f, fade);
            sets.SetInputWeight(0, Mathf.Max(0f, 1f - ballWeight - guardWeight));
            sets.SetInputWeight(1, ballWeight);
            sets.SetInputWeight(2, guardWeight);

            // Read every frame: playbackSpeed can be tuned in the Inspector while playing.
            float tempo = clips.playbackSpeed > 0.01f ? clips.playbackSpeed : 1f;
            float step = dt * tempo;
            lastTempo = tempo;
            freeMix = free.Update(move, step);
            ballMix = ball.Update(move, step);
            guardMix = guard.Update(move, step);

            // Dribbling (standing, walking or running): the arms dribble over whatever the legs
            // do, in time with the gameplay ball -- the window starts with the hand up on the
            // ball, bounce x.5 -- or free-running when the clip's bounce count is unknown.
            if (upperDribble != null)
            {
                float upper = withBall && pose == AnimPose.Dribble ? 1f : 0f;
                upperWeight = Mathf.MoveTowards(upperWeight, upper, fade);
                if (clips.dribbleBouncesInWindow > 0f && dribbleBounces >= 0f)
                    upperDribble.SetPhase((dribbleBounces - 0.5f) / clips.dribbleBouncesInWindow);
                else
                    upperDribble.Advance(ballMix.MoveRate * step);
                layers.SetInputWeight(1, upperWeight);
            }

            if (pose == AnimPose.JumpShot && lastPose != AnimPose.JumpShot) jumpShotCount++;
            ActionClip wanted = ActionFor(pose, move.Speed);
            bool isShot = pose == AnimPose.JumpShot || pose == AnimPose.Layup || pose == AnimPose.Dunk || pose == AnimPose.FreeThrow;
            action.Update(wanted, pose != lastPose, isShot ? shotProgress : -1f, step, fade);
            fullBody.SetInputWeight(0, 1f - action.Weight);
            lastPose = pose;
        }

        // What is playing, for the practice court's animation panel: each locomotion set with its
        // weight and the clips it mixes (weight, playback rate, window time), the upper-body
        // dribble and the one-shot action.
        public string Describe()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"tempo x{lastTempo:0.00}\n");
            float freeWeight = Mathf.Max(0f, 1f - ballWeight - guardWeight);
            free.Describe(sb, "free", freeWeight, freeMix);
            ball.Describe(sb, "withBall", ballWeight, ballMix);
            guard.Describe(sb, "guard", guardWeight, guardMix);
            if (upperDribble != null && upperWeight > 0.01f)
                sb.Append($"upper dribble {upperWeight:0.00}: {upperDribble.Describe()}\n");
            if (action.Weight > 0.01f) sb.Append($"action {action.Weight:0.00}: {action.Describe()}\n");
            return sb.ToString();
        }

        private ActionClip ActionFor(AnimPose pose, float speed = 0f)
        {
            switch (pose)
            {
                case AnimPose.JumpShot:
                    bool moving = clips.withBall != null && speed > clips.withBall.moveSpeed
                                  && clips.jumpShotsMoving != null && clips.jumpShotsMoving.Length > 0;
                    ActionClip[] pool = moving ? clips.jumpShotsMoving : clips.jumpShots;
                    return pool != null && pool.Length > 0 ? pool[jumpShotCount % pool.Length] : default;
                case AnimPose.Layup: return clips.layup;
                case AnimPose.Dunk: return clips.dunk;
                case AnimPose.FreeThrow: return clips.freeThrow;
                case AnimPose.Pass: return clips.pass;
                case AnimPose.Block: return clips.block;
                case AnimPose.Airborne: return clips.airborne;
                case AnimPose.Celebrate: return clips.celebrate;
                default: return default;
            }
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

        // A clip whose time is driven here: loops over its window (or holds one frame).
        private sealed class LoopPlayer
        {
            private readonly AnimationClipPlayable playable;
            private readonly LoopClip def;
            private readonly float length;
            private float phase;

            private LoopPlayer(AnimationClipPlayable playable, LoopClip def)
            {
                this.playable = playable;
                this.def = def;
                length = def.clip.length;
                playable.SetSpeed(0);
                playable.SetTime(def.From * length);
            }

            public static LoopPlayer Create(PlayableGraph graph, Playable mixer, int input, LoopClip def)
            {
                if (def.clip == null) return null;
                var p = AnimationClipPlayable.Create(graph, def.clip);
                graph.Connect(p, 0, mixer, input);
                return new LoopPlayer(p, def);
            }

            public string Describe() =>
                $"{def.clip.name} [{def.From * length:0.00}-{def.To * length:0.00}s]{(def.reverse ? " rev" : "")} @ {playable.GetTime():0.00}s";

            // Jumps to a point of the window: 0 = its start, 1 = its end (wraps).
            public void SetPhase(float windowPhase)
            {
                phase = windowPhase - Mathf.Floor(windowPhase);
                float span = (def.To - def.From) * length;
                float p = def.reverse ? 1f - phase : phase;
                playable.SetTime(def.From * length + p * span);
            }

            // Advances by `seconds` of clip time (already scaled by rate and tempo).
            public void Advance(float seconds)
            {
                float span = (def.To - def.From) * length;
                if (span <= 0.001f)
                {
                    playable.SetTime(def.From * length);
                    return;
                }
                phase += (def.reverse ? -seconds : seconds) / span;
                phase -= Mathf.Floor(phase);
                playable.SetTime(def.From * length + phase * span);
            }
        }

        // One locomotion set: eight clips mixed by LocomotionBlend.
        private sealed class LocomotionLayer
        {
            private const int Idle = 0, Forward = 1, Backward = 2, Left = 3, Right = 4, Run = 5, TurnLeft = 6, TurnRight = 7;
            private readonly LocomotionClipSet set;
            private readonly AnimationMixerPlayable mixer;
            private readonly LoopPlayer[] players = new LoopPlayer[8];

            public LocomotionLayer(PlayableGraph graph, AnimationMixerPlayable parent, int input, LocomotionClipSet set)
            {
                this.set = set ?? new LocomotionClipSet();
                mixer = AnimationMixerPlayable.Create(graph, 8);
                graph.Connect(mixer, 0, parent, input);
                players[Idle] = LoopPlayer.Create(graph, mixer, Idle, this.set.idle);
                players[Forward] = LoopPlayer.Create(graph, mixer, Forward, this.set.forward);
                players[Backward] = LoopPlayer.Create(graph, mixer, Backward, this.set.backward);
                players[Left] = LoopPlayer.Create(graph, mixer, Left, this.set.left);
                players[Right] = LoopPlayer.Create(graph, mixer, Right, this.set.right);
                players[Run] = LoopPlayer.Create(graph, mixer, Run, this.set.run);
                players[TurnLeft] = LoopPlayer.Create(graph, mixer, TurnLeft, this.set.runTurnLeft);
                players[TurnRight] = LoopPlayer.Create(graph, mixer, TurnRight, this.set.runTurnRight);
            }

            public LocomotionMix Update(in LocomotionInput move, float step)
            {
                LocomotionInput i = move;
                i.MoveSpeed = set.moveSpeed;
                i.RunSpeed = set.runSpeed;
                i.HasForward = players[Forward] != null;
                i.HasBackward = players[Backward] != null;
                i.HasLeft = players[Left] != null;
                i.HasRight = players[Right] != null;
                i.HasRun = players[Run] != null;
                i.HasTurnLeft = players[TurnLeft] != null;
                i.HasTurnRight = players[TurnRight] != null;
                LocomotionMix m = LocomotionBlend.Compute(i);
                // Without an idle clip the moving clips share the whole weight.
                bool hasIdle = players[Idle] != null;
                float scale = hasIdle || m.Idle >= 1f ? 1f : 1f / Mathf.Max(0.0001f, 1f - m.Idle);
                Set(Idle, m.Idle, 1f, step);
                Set(Forward, m.Forward * scale, m.MoveRate, step);
                Set(Backward, m.Backward * scale, m.MoveRate, step);
                Set(Left, m.Left * scale, m.MoveRate, step);
                Set(Right, m.Right * scale, m.MoveRate, step);
                Set(Run, m.Run * scale, m.RunRate, step);
                Set(TurnLeft, m.TurnLeft * scale, m.RunRate, step);
                Set(TurnRight, m.TurnRight * scale, m.RunRate, step);
                return m;
            }

            private static readonly string[] SlotNames = { "idle", "forward", "backward", "left", "right", "run", "turnL", "turnR" };

            public void Describe(System.Text.StringBuilder sb, string name, float setWeight, LocomotionMix m)
            {
                if (setWeight < 0.01f) return;
                sb.Append($"{name} set {setWeight:0.00} (move x{m.MoveRate:0.00}, run x{m.RunRate:0.00})\n");
                float[] w = { m.Idle, m.Forward, m.Backward, m.Left, m.Right, m.Run, m.TurnLeft, m.TurnRight };
                for (int i = 0; i < w.Length; i++)
                {
                    if (w[i] < 0.01f) continue;
                    string clip = players[i] != null ? players[i].Describe() : "(no clip)";
                    sb.Append($"  {SlotNames[i]} {w[i]:0.00}: {clip}\n");
                }
            }

            private void Set(int slot, float weight, float rate, float step)
            {
                if (players[slot] == null) return;
                mixer.SetInputWeight(slot, weight);
                players[slot].Advance(rate * step);
            }
        }

        // The one-shot action over locomotion, crossfaded in and out. Shots follow the gameplay
        // shot through their window (ActionClipTiming) and play on after the release; other
        // actions play once through their window and hold its end.
        private sealed class ActionPlayer
        {
            private readonly PlayableGraph graph;
            private readonly AnimationMixerPlayable mixer;
            private readonly int input;
            private AnimationClipPlayable playable;
            private ActionClip current;
            private float elapsed;

            public float Weight { get; private set; }

            public ActionPlayer(PlayableGraph graph, AnimationMixerPlayable mixer, int input)
            {
                this.graph = graph;
                this.mixer = mixer;
                this.input = input;
            }

            public string Describe() => current.clip == null ? "(none)"
                : $"{current.clip.name} [{current.window.start * current.clip.length:0.00}-{current.window.release * current.clip.length:0.00}s] @ {(playable.IsValid() ? playable.GetTime() : 0):0.00}s";

            public void Update(ActionClip wanted, bool newPose, float shotProgress, float step, float fade)
            {
                if (wanted.clip != null && (wanted.clip != current.clip || newPose))
                {
                    if (wanted.clip != current.clip)
                    {
                        if (playable.IsValid())
                        {
                            graph.Disconnect(mixer, input);
                            playable.Destroy();
                        }
                        playable = AnimationClipPlayable.Create(graph, wanted.clip);
                        playable.SetSpeed(0);
                        graph.Connect(playable, 0, mixer, input);
                    }
                    current = wanted;
                    elapsed = 0f;
                }
                if (playable.IsValid() && wanted.clip != null)
                {
                    float length = current.clip.length;
                    ClipWindow w = current.window;
                    if (ActionClipTiming.IsDriven(shotProgress))
                    {
                        playable.SetTime(ActionClipTiming.TimeFor(shotProgress, w.start, w.release, length));
                        elapsed = 0f;
                    }
                    else
                    {
                        elapsed += step;
                        bool afterShot = shotProgress >= 1f;
                        playable.SetTime(ActionClipTiming.Play(afterShot ? w.release : w.start, afterShot ? 1f : w.release, elapsed, length));
                    }
                }
                // Back to what is underneath: the last clip fades out instead of cutting.
                Weight = Mathf.MoveTowards(Weight, wanted.clip != null ? 1f : 0f, fade);
                if (playable.IsValid()) mixer.SetInputWeight(input, Weight);
            }
        }
    }
}
