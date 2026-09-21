using System;
using System.Collections.Generic;
using System.Linq;
using KusakaFactory.Declavatar2.Data;
using KusakaFactory.Declavatar2.Ndmf;
using KusakaFactory.Declavatar2.Runtime;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using Avatar = KusakaFactory.Declavatar2.Data.Avatar;
using AnimatorCondition = KusakaFactory.Declavatar2.Data.AnimatorCondition;
using AnimatorState = KusakaFactory.Declavatar2.Data.AnimatorState;
using AnimatorTransition = KusakaFactory.Declavatar2.Data.AnimatorTransition;
using Keyframe = KusakaFactory.Declavatar2.Data.Keyframe;
using MenuItem = KusakaFactory.Declavatar2.Data.MenuItem;
using Motion = KusakaFactory.Declavatar2.Data.Motion;
using Object = UnityEngine.Object;

namespace KusakaFactory.Declavatar2.Tests
{
    public sealed class GenerationTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private GameObject _root;
        private BuildContext _build;
        private bool _servicesActive;

        [SetUp]
        public void SetUp()
        {
            _root = Track(new GameObject("Avatar"));
            _root.AddComponent<Animator>();
            var descriptor = _root.AddComponent<VRCAvatarDescriptor>();
            descriptor.customizeAnimationLayers = true;
            descriptor.baseAnimationLayers = new[]
            {
                new VRCAvatarDescriptor.CustomAnimLayer
                {
                    type = VRCAvatarDescriptor.AnimLayerType.FX,
                    animatorController = Track(new AnimatorController()),
                    isDefault = false,
                },
            };
            descriptor.specialAnimationLayers = Array.Empty<VRCAvatarDescriptor.CustomAnimLayer>();
            _build = new BuildContext(_root, null);
            StartServices();
        }

        [TearDown]
        public void TearDown()
        {
            StopServices();
            foreach (var obj in _objects.Where(obj => obj != null)) Object.DestroyImmediate(obj);
            _objects.Clear();
        }

        [TestCase(1f, 0.5f)]
        [TestCase(25f, 1f)]
        [TestCase(200f, 0.5f)]
        [TestCase(200f, -0.5f)]
        [TestCase(200f, 1.5f)]
        public void FixedBlendShapeUsesMaximumFrameWeight(float maximum, float ratio)
        {
            var face = CreateFace(maximum);
            var generation = Context(face, AvatarWithPaths(""));
            var motion = new Motion.InlineClip(new InlineAnimation.Fixed(new[]
            {
                new FixedEntry(Shape(), new AnimatedValue.Float(ratio)),
            }));
            var clip = (VirtualClip)new MotionGenerator(generation, PathMode.Relative, AssetClones()).Generate(motion, "Shape");
            var binding = clip.GetFloatCurveBindings().Single();
            Assert.That(binding.path, Is.Empty);
            Assert.That(binding.propertyName, Is.EqualTo("blendShape.Smile"));
            Assert.That(clip.GetFloatCurve(binding).Evaluate(0), Is.EqualTo(maximum * ratio).Within(0.0001f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void KeyedBlendShapeScalesValuesAndTangents(bool bezier)
        {
            var face = CreateFace(200f);
            var generation = Context(face, AvatarWithPaths(""));
            var interpolation = bezier ? (Interpolation)new Interpolation.Bezier(0.25, 0.1, 0.75, 0.9) : new Interpolation.Linear();
            var curve = new Curve(new Keyframe(0, new AnimatedValue.Float(0.25)), new[]
            {
                new Segment(interpolation, new Keyframe(1, new AnimatedValue.Float(0.75))),
            });
            var motion = new Motion.InlineClip(new InlineAnimation.Keyed(new ClipAttributes(2, false, false, 0), new[]
            {
                new KeyedEntry(Shape(), curve),
            }));
            var clip = (VirtualClip)new MotionGenerator(generation, PathMode.Relative, AssetClones()).Generate(motion, "Shape");
            var generated = clip.GetFloatCurve(clip.GetFloatCurveBindings().Single());
            Assert.That(generated.Evaluate(0), Is.EqualTo(50f));
            Assert.That(generated.Evaluate(1), Is.EqualTo(100f).Within(0.001f));
            Assert.That(generated.Evaluate(2), Is.EqualTo(150f));
            Assert.That(generated.keys[0].outTangent, Is.EqualTo(bezier ? 20f : 50f).Within(0.001f));
        }

        [Test]
        public void MergeAnimatorsRebaseInlineAndSharedExternalClipsIndependently()
        {
            var clip = Track(new AnimationClip { name = "External" });
            clip.SetCurve("Hat", typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0, 1, 1));
            clip.SetCurve("", typeof(Animator), "Weight", AnimationCurve.Constant(0, 1, 0.5f));
            var material = Track(new Material(Shader.Find("Standard")));
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("Hat", typeof(MeshRenderer), "m_Materials.Array.data[0]"), new[]
            {
                new ObjectReferenceKeyframe { time = 0, value = material },
            });
            var dictionary = Track(ScriptableObject.CreateInstance<DeclavatarAssetDictionary>());
            dictionary.Entries = new[] { new DeclavatarAssetDictionary.Entry { Name = "External", Asset = clip } };
            foreach (var name in new[] { "A", "B" })
            {
                var root = GenerationContext.CreateChild(_root, name);
                GenerationContext.CreateChild(root, "Hat");
                var inline = new Motion.InlineClip(new InlineAnimation.Fixed(new[]
                {
                    new FixedEntry(new AnimatedTarget.GameObject(new ObjectPathIndex(0), GameObjectProperty.Active), new AnimatedValue.Bool(true)),
                }));
                var controller = Controller(name, PathMode.Relative, inline, new Motion.ExternalClip(new AssetIndex(0)));
                var avatar = AvatarWithPaths("Hat", controller, new AssetLocator.Named(typeof(AnimationClip).FullName, "External"));
                var holder = name == "A" ? root : GenerationContext.CreateChild(_root, "DeclarationB");
                var declaration = holder.AddComponent<DeclavatarDeclaration>();
                declaration.RelativePathRoot = root;
                declaration.AssetDictionaries = new[] { dictionary };
                new ControllerGenerator(new GenerationContext(_build, declaration, avatar)).Generate();
            }

            for (var activation = 0; activation < 2; activation++)
            {
                var services = _build.Extension<AnimatorServicesContext>();
                foreach (var merge in _root.GetComponentsInChildren<ModularAvatarMergeAnimator>())
                {
                    Assert.That(merge.pathMode, Is.EqualTo(MergeAnimatorPathMode.Absolute));
                    if (activation == 0) Assert.That(((AnimatorController)merge.animator).layers, Is.Empty);
                    var generated = services.ControllerContext.Controllers[merge];
                    var clips = generated.Layers.Single().StateMachine.States.Select(state => (VirtualClip)state.State.Motion).ToArray();
                    var expected = generated.Layers.Single().Name + "/Hat";
                    Assert.That(clips[0].GetFloatCurveBindings().Single().path, Is.EqualTo(expected));
                    Assert.That(clips[1].GetFloatCurveBindings().Single(b => b.type == typeof(GameObject)).path, Is.EqualTo(expected));
                    Assert.That(clips[1].GetObjectCurveBindings().Single().path, Is.EqualTo(expected));
                    Assert.That(clips[1].GetFloatCurveBindings().Single(b => b.type == typeof(Animator)).path, Is.Empty);
                }
                StopServices();
                StartServices();
            }
            Assert.That(AnimationUtility.GetCurveBindings(clip).Single(b => b.type == typeof(GameObject)).path, Is.EqualTo("Hat"));
            Assert.That(AnimationUtility.GetObjectReferenceCurveBindings(clip).Single().path, Is.EqualTo("Hat"));
        }

        [Test]
        public void MergeAnimatorCarriesControllerOptions()
        {
            var root = GenerationContext.CreateChild(_root, "Outfit");
            var context = Context(root, AvatarWithPaths("", new PlayableController(
                PlayableLayer.Gesture, MergeMode.Replace, 42, PathMode.Relative, null,
                Array.Empty<AnimatorParameter>(), Array.Empty<AnimatorLayer>())));
            context.Declaration.MatchAvatarWriteDefaults = true;
            new ControllerGenerator(context).Generate();
            var merge = root.GetComponent<ModularAvatarMergeAnimator>();
            Assert.That(merge.layerType, Is.EqualTo(VRCAvatarDescriptor.AnimLayerType.Gesture));
            Assert.That(merge.mergeAnimatorMode, Is.EqualTo(MergeAnimatorMode.Replace));
            Assert.That(merge.layerPriority, Is.EqualTo(42));
            Assert.That(merge.pathMode, Is.EqualTo(MergeAnimatorPathMode.Absolute));
            Assert.That(merge.relativePathRoot.Get(_root.transform), Is.SameAs(root));
            Assert.That(merge.matchAvatarWriteDefaults, Is.True);
            Assert.That(merge.deleteAttachedAnimator, Is.False);
        }

        [Test]
        public void ModularAvatarRenamesGeneratedControllerAndMenuTogether()
        {
            _root.AddComponent<ModularAvatarParameters>().parameters.Add(new ParameterConfig
            {
                nameOrPrefix = "Hat",
                remapTo = "OutfitHat",
                syncType = ParameterSyncType.NotSynced,
            });
            var root = GenerationContext.CreateChild(_root, "Outfit");
            GenerationContext.CreateChild(root, "Hat");
            var motion = new Motion.InlineClip(new InlineAnimation.Fixed(new[]
            {
                new FixedEntry(new AnimatedTarget.GameObject(new ObjectPathIndex(0), GameObjectProperty.Active), new AnimatedValue.Bool(true)),
            }));
            var states = new[]
            {
                new AnimatorState("Off", motion, 1, null, null, false, Array.Empty<Behavior>()),
                new AnimatorState("On", motion, 1, null, null, false, Array.Empty<Behavior>()),
            };
            var transitions = new[]
            {
                new AnimatorTransition(TransitionSource.State(0), TransitionTarget.State(1), 0, new[] { new AnimatorCondition.If("Hat") }),
            };
            var controller = new PlayableController(PlayableLayer.Fx, MergeMode.Append, 0, PathMode.Relative, null,
                new[] { new AnimatorParameter("Hat", new AnimatorParameterKind.Bool(false)) },
                new[] { new AnimatorLayer("Hat", 0, states, transitions) });
            var avatar = new Avatar(AvatarWithPaths("Hat").Externals,
                new[] { new ExpressionParameter("Hat", new ExpressionParameterKind.Bool(false), false, true) },
                new[] { controller }, new[] { new MenuItem.Toggle("Hat", "Hat", new AnimatedValue.Bool(true)) });
            var generation = Context(root, avatar);
            new ParameterGenerator(generation).Generate();
            new ControllerGenerator(generation).Generate();
            new MenuGenerator(generation).Generate();
            Object.DestroyImmediate(generation.Declaration);
            StopServices();

            using (new OverrideTemporaryDirectoryScope(null)) AvatarProcessor.ProcessAvatar(_root);

            var descriptor = _root.GetComponent<VRCAvatarDescriptor>();
            var generated = (AnimatorController)descriptor.baseAnimationLayers.Single(l => l.type == VRCAvatarDescriptor.AnimLayerType.FX).animatorController;
            Assert.That(generated.parameters.Select(p => p.name), Does.Contain("OutfitHat").And.Not.Contain("Hat"));
            var state = generated.layers.Single(l => l.name == "Hat").stateMachine.defaultState;
            Assert.That(state.transitions.Single().conditions.Single().parameter, Is.EqualTo("OutfitHat"));
            Assert.That(descriptor.expressionParameters.parameters.Select(p => p.name), Does.Contain("OutfitHat").And.Not.Contain("Hat"));
            Assert.That(descriptor.expressionsMenu.controls.Single(c => c.name == "Hat").parameter.name, Is.EqualTo("OutfitHat"));
        }

        [Test]
        public void DictionaryRegistrationDefersRelativePrefixUntilReactivation()
        {
            var root = GenerationContext.CreateChild(_root, "Outfit");
            GenerationContext.CreateChild(root, "Hat");
            var merge = root.AddComponent<ModularAvatarMergeAnimator>();
            merge.animator = Track(new AnimatorController());
            merge.pathMode = MergeAnimatorPathMode.Relative;
            merge.relativePathRoot.Set(root);
            var controllers = _build.Extension<VirtualControllerContext>();
            var controller = VirtualAnimatorController.Create(controllers.CloneContext);
            var layer = controller.AddLayer(LayerPriority.Default, "Test");
            var clip = VirtualClip.Create("Test");
            clip.SetFloatCurve(EditorCurveBinding.FloatCurve("Hat", typeof(GameObject), "m_IsActive"), AnimationCurve.Constant(0, 1, 1));
            layer.StateMachine.DefaultState = layer.StateMachine.AddState("Test", clip);
            controllers.Controllers[merge] = controller;

            Assert.That(controllers.Controllers[merge], Is.SameAs(controller));
            Assert.That(clip.GetFloatCurveBindings().Single().path, Is.EqualTo("Hat"));
            Assert.That(merge.pathMode, Is.EqualTo(MergeAnimatorPathMode.Relative));

            StopServices();
            StartServices();

            var reactivated = _build.Extension<VirtualControllerContext>().Controllers[merge];
            var reactivatedClip = (VirtualClip)reactivated.Layers.Single().StateMachine.DefaultState.Motion;
            Assert.That(reactivatedClip.GetFloatCurveBindings().Single().path, Is.EqualTo("Outfit/Hat"));
            Assert.That(merge.pathMode, Is.EqualTo(MergeAnimatorPathMode.Absolute));
        }

        private void StartServices()
        {
            _build.ActivateExtensionContext<VirtualControllerContext>();
            _build.ActivateExtensionContext<AnimatorServicesContext>();
            _servicesActive = true;
        }

        private void StopServices()
        {
            if (!_servicesActive) return;
            _build.DeactivateExtensionContext<AnimatorServicesContext>();
            _build.DeactivateExtensionContext<VirtualControllerContext>();
            _servicesActive = false;
        }

        private CloneContext AssetClones()
        {
            return new CloneContext(_build.Extension<VirtualControllerContext>().PlatformBindings);
        }

        private GameObject CreateFace(float maximum)
        {
            var face = GenerationContext.CreateChild(_root, "Face");
            var mesh = Track(new Mesh { vertices = new[] { Vector3.zero } });
            mesh.AddBlendShapeFrame("Smile", maximum / 2, new[] { Vector3.up / 2 }, null, null);
            mesh.AddBlendShapeFrame("Smile", maximum, new[] { Vector3.up }, null, null);
            face.AddComponent<SkinnedMeshRenderer>().sharedMesh = mesh;
            return face;
        }

        private GenerationContext Context(GameObject root, Avatar avatar)
        {
            return new GenerationContext(_build, root.AddComponent<DeclavatarDeclaration>(), avatar);
        }

        private static AnimatedTarget Shape()
        {
            return new AnimatedTarget.Renderer(new ObjectPathIndex(0), typeof(SkinnedMeshRenderer).FullName, new RendererProperty.BlendShape("Smile"));
        }

        private static Avatar AvatarWithPaths(string path, PlayableController controller = null, AssetLocator asset = null)
        {
            var externals = new Externals(new[] { new ExternEntry<string>(path, Array.Empty<SourceLocation>()) },
                Array.Empty<ExternEntry<string>>(),
                asset == null ? Array.Empty<ExternEntry<AssetLocator>>() : new[] { new ExternEntry<AssetLocator>(asset, Array.Empty<SourceLocation>()) }, true);
            return new Avatar(externals, Array.Empty<ExpressionParameter>(),
                controller == null ? Array.Empty<PlayableController>() : new[] { controller }, Array.Empty<MenuItem>());
        }

        private static PlayableController Controller(string name, PathMode mode, params Motion[] motions)
        {
            var states = motions.Select((motion, i) => new AnimatorState(i.ToString(), motion, 1, null, null, false, Array.Empty<Behavior>())).ToArray();
            var layer = new AnimatorLayer(name, 0, states, Array.Empty<AnimatorTransition>());
            return new PlayableController(PlayableLayer.Fx, MergeMode.Append, 0, mode, null,
                new[] { new AnimatorParameter("Weight", new AnimatorParameterKind.Float(1)) }, new[] { layer });
        }

        private T Track<T>(T obj) where T : Object
        {
            _objects.Add(obj);
            return obj;
        }
    }
}
