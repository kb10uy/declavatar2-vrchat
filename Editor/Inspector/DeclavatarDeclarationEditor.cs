using System;
using System.Collections.Generic;
using System.Linq;
using KusakaFactory.Declavatar2.Compilation;
using KusakaFactory.Declavatar2.Data;
using KusakaFactory.Declavatar2.Runtime;
using UnityEditor;
using UnityEngine;
using Avatar = KusakaFactory.Declavatar2.Data.Avatar;

namespace KusakaFactory.Declavatar2.Inspector
{
    [CustomEditor(typeof(DeclavatarDeclaration))]
    internal sealed class DeclavatarDeclarationEditor : Editor
    {
        private string _status;
        private MessageType _statusType;
        private readonly List<string> _details = new List<string>();

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var declaration = (DeclavatarDeclaration)target;
            EditorGUILayout.Space();
            if (GUILayout.Button("Recompile"))
            {
                DeclarationCache.Invalidate(declaration);
            }
            Refresh(declaration);

            EditorGUILayout.HelpBox(_status, _statusType);
            foreach (var detail in _details) EditorGUILayout.LabelField(detail, EditorStyles.wordWrappedLabel);
        }

        private void Refresh(DeclavatarDeclaration declaration)
        {
            _details.Clear();
            try
            {
                var compiled = DeclarationCache.GetOrCompile(declaration);
                if (compiled == null)
                {
                    SetStatus("No script is assigned.", MessageType.Info);
                }
                else if (compiled.Succeeded)
                {
                    Summarize(compiled.Avatar);
                }
                else
                {
                    var stage = compiled.Diagnostics.Stage == DiagnosticStage.Script ? "Script" : "Declaration";
                    SetStatus($"{stage} error ({compiled.Diagnostics.Items.Count})", MessageType.Error);
                    _details.AddRange(compiled.Diagnostics.Items.Select(item => item.ToString()));
                }
            }
            catch (Exception e)
            {
                SetStatus(e.Message, MessageType.Error);
            }
        }

        private void Summarize(Avatar avatar)
        {
            var layers = avatar.Controllers.Sum(c => c.Layers.Count);
            SetStatus($"Compiled: {avatar.ExpressionParameters.Count} expression parameters, {avatar.Controllers.Count} controllers ({layers} layers), {avatar.Menu.Count} menu items", MessageType.Info);
            var externals = avatar.Externals;
            if (externals.ObjectPaths.Count > 0) _details.Add("Objects: " + string.Join(", ", externals.ObjectPaths.Select(e => e.Value)));
            if (externals.ComponentTypes.Count > 0) _details.Add("Components: " + string.Join(", ", externals.ComponentTypes.Select(e => e.Value)));
            if (externals.Assets.Count > 0) _details.Add("Assets: " + string.Join(", ", externals.Assets.Select(e => Resolution.ExternalResolver.Describe(e.Value))));
            if (externals.NeedsRelativeRoot) _details.Add("Uses relative paths; the relative path root defaults to this object.");
        }

        private void SetStatus(string status, MessageType type)
        {
            _status = status;
            _statusType = type;
        }
    }
}
