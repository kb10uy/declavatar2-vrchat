using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using KusakaFactory.Declavatar2.Data;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf.animator;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using AnimatorCondition = KusakaFactory.Declavatar2.Data.AnimatorCondition;
using AnimatorLayer = KusakaFactory.Declavatar2.Data.AnimatorLayer;
using AnimatorTransition = KusakaFactory.Declavatar2.Data.AnimatorTransition;
using UnityCondition = UnityEditor.Animations.AnimatorCondition;
using UnityBlendingMode = UnityEditor.Animations.AnimatorLayerBlendingMode;
using UnityConditionMode = UnityEditor.Animations.AnimatorConditionMode;
using UnityController = UnityEditor.Animations.AnimatorController;

namespace KusakaFactory.Declavatar2.Ndmf
{
    internal sealed class ControllerGenerator
    {
        private readonly GenerationContext _context;

        public ControllerGenerator(GenerationContext context)
        {
            _context = context;
        }

        public void Generate()
        {
            var layers = new List<VirtualLayer[]>();
            var layerControls = new List<(VRCAnimatorLayerControl Control, LayerRef Layer)>();
            foreach (var controller in _context.Avatar.Controllers) layers.Add(GenerateController(controller, layerControls));
            foreach (var (control, layer) in layerControls) control.layer = layers[layer.Controller][layer.Layer].VirtualLayerIndex;
        }

        private VirtualLayer[] GenerateController(PlayableController controller, List<(VRCAnimatorLayerControl, LayerRef)> layerControls)
        {
            var controllers = _context.Services.ControllerContext;
            var target = VirtualAnimatorController.Create(controllers.CloneContext, $"Declavatar {controller.Playable}");
            target.Parameters = controller.Parameters.ToImmutableDictionary(parameter => parameter.Name, Convert);
            var assets = new CloneContext(controllers.PlatformBindings);
            var mask = controller.Mask is AssetIndex maskIndex ? assets.Clone(_context.Resolver.Asset<AvatarMask>(maskIndex)) : null;
            var motions = new MotionGenerator(_context, controller.PathMode, assets);
            var behaviors = new BehaviorGenerator(_context, controller, layerControls);
            var layers = controller.Layers
                .Select(layer => GenerateLayer(layer, motions, behaviors, MaskOf(layer, assets) ?? mask))
                .ToArray();
            foreach (var layer in layers) target.AddLayer(LayerPriority.Default, layer);
            new AnimationIndex(new[] { target }).RewritePaths(path => _context.VirtualPath(controller.PathMode, path));

            var merge = _context.Declaration.gameObject.AddComponent<ModularAvatarMergeAnimator>();
            merge.animator = new UnityController { name = target.Name };
            merge.layerType = LayerTypeOf(controller.Playable);
            merge.mergeAnimatorMode = controller.Mode == MergeMode.Replace ? MergeAnimatorMode.Replace : MergeAnimatorMode.Append;
            merge.layerPriority = controller.Priority;
            merge.pathMode = MergeAnimatorPathMode.Absolute;
            merge.relativePathRoot.Set(_context.RelativeRoot);
            merge.matchAvatarWriteDefaults = _context.Declaration.MatchAvatarWriteDefaults;
            merge.deleteAttachedAnimator = false;
            controllers.Controllers[merge] = target;
            return layers;
        }

        private VirtualLayer GenerateLayer(AnimatorLayer layer, MotionGenerator motions, BehaviorGenerator behaviors, VirtualAvatarMask mask)
        {
            var clone = _context.Services.ControllerContext.CloneContext;
            var root = VirtualStateMachine.Create(clone, layer.Name);
            var result = VirtualLayer.Create(clone, layer.Name);
            result.StateMachine = root;
            result.DefaultWeight = (float)layer.Settings.Weight;
            result.BlendingMode = layer.Settings.Blending == LayerBlending.Additive ? UnityBlendingMode.Additive : UnityBlendingMode.Override;
            result.AvatarMask = mask;

            var machines = layer.Machines.Select(machine => VirtualStateMachine.Create(clone, machine.Name)).ToArray();
            var slots = new Dictionary<VirtualStateMachine, int>();
            var states = new VirtualState[layer.States.Count];
            for (var i = 0; i < states.Length; i++)
            {
                var source = layer.States[i];
                var holder = MachineOf(root, machines, source.Machine);
                var motion = motions.Generate(source.Motion, $"{layer.Name}/{source.Name}");
                var state = holder.AddState(source.Name, motion, PositionOf(NextSlot(slots, holder)));
                state.Speed = (float)source.Speed;
                state.SpeedParameter = source.SpeedBy;
                state.TimeParameter = source.TimeBy;
                state.WriteDefaultValues = source.WriteDefaults;
                state.Behaviours = behaviors.Generate(source.Behaviors);
                states[i] = state;
            }
            for (var i = 0; i < machines.Length; i++)
            {
                var source = layer.Machines[i];
                var parent = MachineOf(root, machines, source.Parent);
                var child = new VirtualStateMachine.VirtualChildStateMachine { StateMachine = machines[i], Position = PositionOf(NextSlot(slots, parent)) };
                parent.StateMachines = parent.StateMachines.Add(child);
                if (source.DefaultState is int machineDefault) machines[i].DefaultState = states[machineDefault];
            }
            if (layer.DefaultState is int defaultState) root.DefaultState = states[defaultState];
            foreach (var transition in layer.Transitions) AddTransition(layer, root, machines, states, transition);

            return result;
        }

        private VirtualAvatarMask MaskOf(AnimatorLayer layer, CloneContext assets)
        {
            return layer.Settings.Mask is AssetIndex index ? assets.Clone(_context.Resolver.Asset<AvatarMask>(index)) : null;
        }

        private static VirtualStateMachine MachineOf(VirtualStateMachine root, VirtualStateMachine[] machines, int? index)
        {
            return index is int machine ? machines[machine] : root;
        }

        private static int NextSlot(Dictionary<VirtualStateMachine, int> slots, VirtualStateMachine machine)
        {
            slots.TryGetValue(machine, out var slot);
            slots[machine] = slot + 1;
            return slot;
        }

        private static void AddTransition(
            AnimatorLayer layer,
            VirtualStateMachine root,
            VirtualStateMachine[] machines,
            VirtualState[] states,
            AnimatorTransition transition)
        {
            var conditions = transition.Conditions.Select(Convert).ToImmutableList();
            switch (transition.From.Kind)
            {
                case TransitionSourceKind.Entry:
                {
                    var holder = MachineOf(root, machines, transition.From.MachineIndex);
                    var entry = VirtualTransition.Create();
                    SetDestination(entry, machines, states, transition.To);
                    entry.Conditions = conditions;
                    holder.EntryTransitions = holder.EntryTransitions.Add(entry);
                    return;
                }
                case TransitionSourceKind.MachineExit:
                {
                    var exited = transition.From.MachineIndex.Value;
                    var holder = MachineOf(root, machines, layer.Machines[exited].Parent);
                    var leaving = VirtualTransition.Create();
                    SetDestination(leaving, machines, states, transition.To);
                    leaving.Conditions = conditions;
                    var existing = holder.StateMachineTransitions.TryGetValue(machines[exited], out var list) ? list : ImmutableList<VirtualTransition>.Empty;
                    holder.StateMachineTransitions = holder.StateMachineTransitions.SetItem(machines[exited], existing.Add(leaving));
                    return;
                }
            }

            var from = states[transition.From.StateIndex.Value];
            var result = VirtualStateTransition.Create();
            result.ExitTime = conditions.IsEmpty ? 1f : null;
            result.Duration = (float)transition.Duration;
            result.HasFixedDuration = true;
            result.CanTransitionToSelf = false;
            result.InterruptionSource = UnityEditor.Animations.TransitionInterruptionSource.None;
            SetDestination(result, machines, states, transition.To);
            result.Conditions = conditions;
            from.Transitions = from.Transitions.Add(result);
        }

        private static void SetDestination(VirtualTransitionBase transition, VirtualStateMachine[] machines, VirtualState[] states, TransitionTarget target)
        {
            switch (target.Kind)
            {
                case TransitionTargetKind.Exit:
                    transition.SetExitDestination();
                    break;
                case TransitionTargetKind.Machine:
                    transition.SetDestination(machines[target.MachineIndex.Value]);
                    break;
                default:
                    transition.SetDestination(states[target.StateIndex.Value]);
                    break;
            }
        }

        private static UnityCondition Convert(AnimatorCondition condition)
        {
            switch (condition)
            {
                case AnimatorCondition.If _: return Condition(UnityConditionMode.If, condition.Parameter, 0f);
                case AnimatorCondition.IfNot _: return Condition(UnityConditionMode.IfNot, condition.Parameter, 0f);
                case AnimatorCondition.Equal equal: return Condition(UnityConditionMode.Equals, condition.Parameter, equal.Value);
                case AnimatorCondition.NotEqual notEqual: return Condition(UnityConditionMode.NotEqual, condition.Parameter, notEqual.Value);
                case AnimatorCondition.Greater greater: return Condition(UnityConditionMode.Greater, condition.Parameter, (float)greater.Value);
                case AnimatorCondition.Less less: return Condition(UnityConditionMode.Less, condition.Parameter, (float)less.Value);
                default: throw new InvalidOperationException($"unknown condition {condition}");
            }
        }

        private static UnityCondition Condition(UnityConditionMode mode, string parameter, float threshold)
        {
            return new UnityCondition { mode = mode, parameter = parameter, threshold = threshold };
        }

        private static AnimatorControllerParameter Convert(AnimatorParameter parameter)
        {
            var result = new AnimatorControllerParameter { name = parameter.Name };
            switch (parameter.Kind)
            {
                case AnimatorParameterKind.Bool b:
                    result.type = AnimatorControllerParameterType.Bool;
                    result.defaultBool = b.Default ?? false;
                    break;
                case AnimatorParameterKind.Int i:
                    result.type = AnimatorControllerParameterType.Int;
                    result.defaultInt = i.Default ?? 0;
                    break;
                case AnimatorParameterKind.Float f:
                    result.type = AnimatorControllerParameterType.Float;
                    result.defaultFloat = f.Default ?? 0f;
                    break;
            }
            return result;
        }

        private static Vector3 PositionOf(int index)
        {
            return new Vector3(300f * (index % 4), 80f * (index / 4) + 80f, 0f);
        }

        private static VRCAvatarDescriptor.AnimLayerType LayerTypeOf(PlayableLayer playable)
        {
            switch (playable)
            {
                case PlayableLayer.Base: return VRCAvatarDescriptor.AnimLayerType.Base;
                case PlayableLayer.Additive: return VRCAvatarDescriptor.AnimLayerType.Additive;
                case PlayableLayer.Gesture: return VRCAvatarDescriptor.AnimLayerType.Gesture;
                case PlayableLayer.Action: return VRCAvatarDescriptor.AnimLayerType.Action;
                case PlayableLayer.Fx: return VRCAvatarDescriptor.AnimLayerType.FX;
                case PlayableLayer.Sitting: return VRCAvatarDescriptor.AnimLayerType.Sitting;
                case PlayableLayer.TPose: return VRCAvatarDescriptor.AnimLayerType.TPose;
                case PlayableLayer.IkPose: return VRCAvatarDescriptor.AnimLayerType.IKPose;
                default: throw new ArgumentOutOfRangeException(nameof(playable), playable, null);
            }
        }
    }
}
