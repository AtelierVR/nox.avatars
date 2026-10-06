// À placer dans Assets/Editor/CreateTPoseClip.cs
// Sélectionne ton perso (instance du FBX dans la scène, avec Animator + Avatar Humanoid)
// puis : Tools > Create T-Pose Clip From Selected Humanoid
// Le script lit la vraie T-pose de l'Avatar (celle de "Enforce T-Pose") sur une COPIE temporaire,
// puis écrit les vraies valeurs de muscles (et non des 0) dans le clip.
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class CreateTPoseClip
{
    [MenuItem("Tools/Create T-Pose Clip From Selected Humanoid")]
    static void Create()
    {
        var src = Selection.activeGameObject;
        var srcAnimator = src != null ? src.GetComponent<Animator>() : null;
        if (srcAnimator == null || srcAnimator.avatar == null || !srcAnimator.avatar.isHuman)
        {
            Debug.LogError("Sélectionne un GameObject avec un Animator dont l'Avatar est Humanoid.");
            return;
        }

        var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(srcAnimator.avatar)) as ModelImporter;
        if (importer == null || importer.animationType != ModelImporterAnimationType.Human)
        {
            Debug.LogError("L'Avatar doit venir d'un modèle importé (FBX) en Animation Type = Humanoid.");
            return;
        }

        // Copie temporaire pour ne pas toucher à ton perso
        var copy = Object.Instantiate(src);
        copy.hideFlags = HideFlags.HideAndDontSave;
        copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var animator = copy.GetComponent<Animator>();

        // Applique la T-pose stockée dans l'Avatar (= ce que fait "Enforce T-Pose")
        var all = copy.GetComponentsInChildren<Transform>(true);
        foreach (var sb in importer.humanDescription.skeleton)
        {
            foreach (var t in all)
            {
                if (t == copy.transform || t.name != sb.name) continue;
                t.localPosition = sb.position;
                t.localRotation = sb.rotation;
                t.localScale = sb.scale;
                break;
            }
        }

        var handler = new HumanPoseHandler(animator.avatar, animator.transform);
        var pose = new HumanPose();
        handler.GetHumanPose(ref pose);
        handler.Dispose();
        Object.DestroyImmediate(copy);

        var clip = new AnimationClip { frameRate = 30f, name = "Humanoid_TPose_Generated" };
        void Set(string attr, float v)
        {
            var c = new AnimationCurve(new Keyframe(0f, v), new Keyframe(1f / 30f, v));
            clip.SetCurve("", typeof(Animator), attr, c);
        }

        Set("RootT.x", pose.bodyPosition.x);
        Set("RootT.y", pose.bodyPosition.y);
        Set("RootT.z", pose.bodyPosition.z);
        Set("RootQ.x", pose.bodyRotation.x);
        Set("RootQ.y", pose.bodyRotation.y);
        Set("RootQ.z", pose.bodyRotation.z);
        Set("RootQ.w", pose.bodyRotation.w);

        for (int i = 0; i < HumanTrait.MuscleCount; i++)
        {
            // "Left Thumb 1 Stretched" -> "LeftHand.Thumb.1 Stretched"
            string n = Regex.Replace(HumanTrait.MuscleName[i],
                @"^(Left|Right) (Thumb|Index|Middle|Ring|Little) (.*)$", "$1Hand.$2.$3");
            Set(n, pose.muscles[i]);
        }

        const string path = "Assets/Humanoid_TPose_Generated.anim";
        AssetDatabase.CreateAsset(clip, path);
        AssetDatabase.SaveAssets();
        Debug.Log("Clip créé : " + path);
    }
}