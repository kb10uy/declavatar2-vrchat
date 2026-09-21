using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using KusakaFactory.Declavatar2.Data;
using KusakaFactory.Declavatar2.Resolution;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace KusakaFactory.Declavatar2.Ndmf
{
    internal sealed class BehaviorGenerator
    {
        private readonly GenerationContext _context;

        public BehaviorGenerator(GenerationContext context)
        {
            _context = context;
        }

        public ImmutableList<StateMachineBehaviour> Generate(IReadOnlyList<Behavior> behaviors)
        {
            var result = new List<StateMachineBehaviour>();
            VRCAvatarParameterDriver driver = null;
            foreach (var behavior in behaviors)
            {
                switch (behavior)
                {
                    case Behavior.ParameterDrive drive:
                        if (driver == null)
                        {
                            driver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                            driver.parameters = new List<VRC_AvatarParameterDriver.Parameter>();
                            result.Add(driver);
                        }
                        driver.parameters.Add(Convert(drive.Target));
                        break;
                    case Behavior.TrackingControl tracking:
                        result.Add(Convert(tracking));
                        break;
                    case Behavior.Generic generic:
                        var created = Convert(generic);
                        if (created != null) result.Add(created);
                        break;
                    default:
                        throw new InvalidOperationException($"unknown behavior {behavior}");
                }
            }
            return result.ToImmutableList();
        }

        private static VRC_AvatarParameterDriver.Parameter Convert(ParameterDriveTarget target)
        {
            switch (target)
            {
                case ParameterDriveTarget.Set set:
                    return new VRC_AvatarParameterDriver.Parameter
                    {
                        type = VRC_AvatarParameterDriver.ChangeType.Set,
                        name = set.Parameter,
                        value = Values.Scalar(set.Value),
                    };
                case ParameterDriveTarget.Add add:
                    return new VRC_AvatarParameterDriver.Parameter
                    {
                        type = VRC_AvatarParameterDriver.ChangeType.Add,
                        name = add.Parameter,
                        value = Values.Scalar(add.Value),
                    };
                case ParameterDriveTarget.RandomInt randomInt:
                    return new VRC_AvatarParameterDriver.Parameter
                    {
                        type = VRC_AvatarParameterDriver.ChangeType.Random,
                        name = randomInt.Parameter,
                        valueMin = randomInt.Min,
                        valueMax = randomInt.Max,
                    };
                case ParameterDriveTarget.RandomBool randomBool:
                    return new VRC_AvatarParameterDriver.Parameter
                    {
                        type = VRC_AvatarParameterDriver.ChangeType.Random,
                        name = randomBool.Parameter,
                        chance = (float)randomBool.Chance,
                    };
                case ParameterDriveTarget.RandomFloat randomFloat:
                    return new VRC_AvatarParameterDriver.Parameter
                    {
                        type = VRC_AvatarParameterDriver.ChangeType.Random,
                        name = randomFloat.Parameter,
                        valueMin = (float)randomFloat.Min,
                        valueMax = (float)randomFloat.Max,
                    };
                case ParameterDriveTarget.Copy copy:
                    return new VRC_AvatarParameterDriver.Parameter
                    {
                        type = VRC_AvatarParameterDriver.ChangeType.Copy,
                        source = copy.From,
                        name = copy.To,
                    };
                case ParameterDriveTarget.RangedCopy rangedCopy:
                    return new VRC_AvatarParameterDriver.Parameter
                    {
                        type = VRC_AvatarParameterDriver.ChangeType.Copy,
                        source = rangedCopy.From,
                        name = rangedCopy.To,
                        convertRange = true,
                        sourceMin = (float)rangedCopy.FromMin,
                        sourceMax = (float)rangedCopy.FromMax,
                        destMin = (float)rangedCopy.ToMin,
                        destMax = (float)rangedCopy.ToMax,
                    };
                default:
                    throw new InvalidOperationException($"unknown parameter drive {target}");
            }
        }

        private static VRCAnimatorTrackingControl Convert(Behavior.TrackingControl tracking)
        {
            var control = ScriptableObject.CreateInstance<VRCAnimatorTrackingControl>();
            control.trackingHead = Convert(tracking[TrackingControlTarget.Head]);
            control.trackingLeftHand = Convert(tracking[TrackingControlTarget.LeftHand]);
            control.trackingRightHand = Convert(tracking[TrackingControlTarget.RightHand]);
            control.trackingHip = Convert(tracking[TrackingControlTarget.Hip]);
            control.trackingLeftFoot = Convert(tracking[TrackingControlTarget.LeftFoot]);
            control.trackingRightFoot = Convert(tracking[TrackingControlTarget.RightFoot]);
            control.trackingLeftFingers = Convert(tracking[TrackingControlTarget.LeftFingers]);
            control.trackingRightFingers = Convert(tracking[TrackingControlTarget.RightFingers]);
            control.trackingEyes = Convert(tracking[TrackingControlTarget.Eyes]);
            control.trackingMouth = Convert(tracking[TrackingControlTarget.Mouth]);
            return control;
        }

        private static VRC_AnimatorTrackingControl.TrackingType Convert(TrackingControlMode mode)
        {
            switch (mode)
            {
                case TrackingControlMode.NoChange: return VRC_AnimatorTrackingControl.TrackingType.NoChange;
                case TrackingControlMode.Tracking: return VRC_AnimatorTrackingControl.TrackingType.Tracking;
                case TrackingControlMode.Animation: return VRC_AnimatorTrackingControl.TrackingType.Animation;
                default: throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        private StateMachineBehaviour Convert(Behavior.Generic generic)
        {
            var type = TypeIndex.FindStateMachineBehaviour(generic.TypeName);
            if (type == null)
            {
                _context.Report(ErrorSeverity.Error, "declavatar2.generate.behaviour_type", generic.TypeName);
                return null;
            }

            var instance = (StateMachineBehaviour)ScriptableObject.CreateInstance(type);
            var serialized = new SerializedObject(instance);
            var writer = new GenericValueWriter(_context, generic.TypeName);
            foreach (var field in generic.Fields)
            {
                var property = serialized.FindProperty(field.Key);
                if (property == null)
                {
                    _context.Report(ErrorSeverity.Error, "declavatar2.generate.behaviour_field", generic.TypeName, field.Key);
                    continue;
                }
                writer.Write(property, field.Value);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return instance;
        }
    }
}
