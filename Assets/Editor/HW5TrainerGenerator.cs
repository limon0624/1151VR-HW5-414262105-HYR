// HW5 訓練家產生器 (Trainer generator for 1151VR-HW5)
// 放在 Assets/Editor/ 資料夾；另外要把 TrainerAnimator.cs 放在 Assets/Scripts/
// 選單：Tools > HW5 > 3. 產生訓練家
// 會產生 Assets/HW5_Generated/Prefabs/Trainer.prefab，並放一個到場景裡

using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

public static class HW5TrainerGenerator
{
    const string Root      = "Assets/HW5_Generated";
    const string MatDir    = Root + "/Materials";
    const string PrefabDir = Root + "/Prefabs";

    [MenuItem("Tools/HW5/3. 產生訓練家 (Generate Trainer)")]
    static void Generate()
    {
        EnsureFolder(MatDir);
        EnsureFolder(PrefabDir);

        var root = new GameObject("Trainer");
        var mats = new Dictionary<string, Material>();

        // ---- 材質 ----
        mats["skin"] = MakeMat("Trainer_skin", new Color(1f, 0.82f, 0.68f), 0.25f);
        mats["hair"] = MakeMat("Trainer_hair", new Color(0.22f, 0.13f, 0.08f), 0.35f);
        mats["cap"] = MakeMat("Trainer_cap", new Color(0.98f, 0.76f, 0.18f), 0.1f);
        mats["brim"] = MakeMat("Trainer_brim", new Color(0.12f, 0.2f, 0.35f), 0.1f);
        mats["jacket"] = MakeMat("Trainer_jacket", new Color(0.1f, 0.55f, 0.58f), 0.1f);
        mats["trim"] = MakeMat("Trainer_trim", new Color(0.98f, 0.8f, 0.25f), 0.1f);
        mats["shirt"] = MakeMat("Trainer_shirt", new Color(0.95f, 0.95f, 0.95f), 0.1f);
        mats["pants"] = MakeMat("Trainer_pants", new Color(0.16f, 0.2f, 0.32f), 0.1f);
        mats["shoe"] = MakeMat("Trainer_shoe", new Color(0.9f, 0.35f, 0.25f), 0.1f);
        mats["sole"] = MakeMat("Trainer_sole", new Color(0.97f, 0.97f, 0.97f), 0.1f);
        mats["bag"] = MakeMat("Trainer_bag", new Color(0.95f, 0.55f, 0.2f), 0.1f);
        mats["bagflap"] = MakeMat("Trainer_bagflap", new Color(0.8f, 0.4f, 0.15f), 0.1f);
        mats["eye"] = MakeMat("Trainer_eye", new Color(0.12f, 0.08f, 0.06f), 0.85f);
        mats["white"] = MakeMat("Trainer_white", new Color(1f, 1f, 1f), 0.9f);
        mats["mouth"] = MakeMat("Trainer_mouth", new Color(0.6f, 0.2f, 0.2f), 0.1f);
        mats["blush"] = MakeMat("Trainer_blush", new Color(1f, 0.6f, 0.6f), 0.1f);

        // ---- 關節 ----
        var J = new Dictionary<string, Transform>();
        J["ROOT"] = root.transform;
        J["Hips"] = Joint("Hips", J["ROOT"], new Vector3(0f, 0.82f, 0f), new Vector3(0f, 0f, 0f), 1f);
        J["Spine"] = Joint("Spine", J["Hips"], new Vector3(0f, 0.03f, 0f), new Vector3(0f, 0f, 0f), 1f);
        J["Head"] = Joint("Head", J["Spine"], new Vector3(0f, 0.5f, 0f), new Vector3(0f, 0f, 0f), 1.12f);
        J["ShoulderR"] = Joint("ShoulderR", J["Spine"], new Vector3(0.2f, 0.43f, 0f), new Vector3(0f, 0f, 8f), 1f);
        J["ElbowR"] = Joint("ElbowR", J["ShoulderR"], new Vector3(0f, -0.29f, 0f), new Vector3(0f, 0f, 0f), 1f);
        J["HandR"] = Joint("HandR", J["ElbowR"], new Vector3(0f, -0.27f, 0f), new Vector3(0f, 0f, 0f), 1f);
        J["ShoulderL"] = Joint("ShoulderL", J["Spine"], new Vector3(-0.2f, 0.43f, 0f), new Vector3(0f, 0f, -8f), 1f);
        J["ElbowL"] = Joint("ElbowL", J["ShoulderL"], new Vector3(0f, -0.29f, 0f), new Vector3(0f, 0f, 0f), 1f);
        J["LegR"] = Joint("LegR", J["Hips"], new Vector3(0.085f, -0.02f, 0f), new Vector3(0f, 0f, 0f), 1f);
        J["KneeR"] = Joint("KneeR", J["LegR"], new Vector3(0f, -0.39f, 0f), new Vector3(0f, 0f, 0f), 1f);
        J["LegL"] = Joint("LegL", J["Hips"], new Vector3(-0.085f, -0.02f, 0f), new Vector3(0f, 0f, 0f), 1f);
        J["KneeL"] = Joint("KneeL", J["LegL"], new Vector3(0f, -0.39f, 0f), new Vector3(0f, 0f, 0f), 1f);

        // ---- 身體各部位 ----
        Part("Jacket", J["Spine"], PrimitiveType.Capsule, new Vector3(0f, 0.23f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0.34f, 0.27f, 0.23f), mats["jacket"], true);
        Part("Shirt", J["Spine"], PrimitiveType.Capsule, new Vector3(0f, 0.25f, 0.1f), new Vector3(0f, 0f, 0f), new Vector3(0.11f, 0.17f, 0.04f), mats["shirt"], true);
        Part("TrimR", J["Spine"], PrimitiveType.Cube, new Vector3(0.062f, 0.25f, 0.11f), new Vector3(0f, 0f, 0f), new Vector3(0.016f, 0.38f, 0.022f), mats["trim"], true);
        Part("TrimL", J["Spine"], PrimitiveType.Cube, new Vector3(-0.062f, 0.25f, 0.11f), new Vector3(0f, 0f, 0f), new Vector3(0.016f, 0.38f, 0.022f), mats["trim"], true);
        Part("StrapR", J["Spine"], PrimitiveType.Capsule, new Vector3(0.115f, 0.28f, 0.1f), new Vector3(0f, 0f, 0f), new Vector3(0.04f, 0.15f, 0.03f), mats["bagflap"], true);
        Part("StrapL", J["Spine"], PrimitiveType.Capsule, new Vector3(-0.115f, 0.28f, 0.1f), new Vector3(0f, 0f, 0f), new Vector3(0.04f, 0.15f, 0.03f), mats["bagflap"], true);
        Part("Backpack", J["Spine"], PrimitiveType.Capsule, new Vector3(0f, 0.22f, -0.165f), new Vector3(0f, 0f, 0f), new Vector3(0.27f, 0.17f, 0.14f), mats["bag"], true);
        Part("BackpackFlap", J["Spine"], PrimitiveType.Sphere, new Vector3(0f, 0.32f, -0.17f), new Vector3(0f, 0f, 0f), new Vector3(0.27f, 0.14f, 0.145f), mats["bagflap"], true);
        Part("Neck", J["Spine"], PrimitiveType.Cylinder, new Vector3(0f, 0.52f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0.09f, 0.05f, 0.09f), mats["skin"], true);
        Part("PantsTop", J["Hips"], PrimitiveType.Capsule, new Vector3(0f, 0.02f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0.32f, 0.11f, 0.22f), mats["pants"], true);
        Part("HeadBall", J["Head"], PrimitiveType.Sphere, new Vector3(0f, 0.17f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0.28f, 0.3f, 0.27f), mats["skin"], true);
        Part("HairBack", J["Head"], PrimitiveType.Sphere, new Vector3(0f, 0.185f, -0.025f), new Vector3(0f, 0f, 0f), new Vector3(0.3f, 0.31f, 0.29f), mats["hair"], true);
        Part("BangR", J["Head"], PrimitiveType.Sphere, new Vector3(0.06f, 0.235f, 0.112f), new Vector3(0f, 0f, -15f), new Vector3(0.09f, 0.06f, 0.05f), mats["hair"], true);
        Part("BangM", J["Head"], PrimitiveType.Sphere, new Vector3(0f, 0.24f, 0.118f), new Vector3(0f, 0f, 0f), new Vector3(0.09f, 0.065f, 0.05f), mats["hair"], true);
        Part("BangL", J["Head"], PrimitiveType.Sphere, new Vector3(-0.06f, 0.235f, 0.112f), new Vector3(0f, 0f, 15f), new Vector3(0.09f, 0.06f, 0.05f), mats["hair"], true);
        Part("CapDome", J["Head"], PrimitiveType.Sphere, new Vector3(0f, 0.29f, -0.005f), new Vector3(0f, 0f, 0f), new Vector3(0.305f, 0.2f, 0.295f), mats["cap"], true);
        Part("CapBrim", J["Head"], PrimitiveType.Cylinder, new Vector3(0f, 0.27f, 0.2f), new Vector3(8f, 0f, 0f), new Vector3(0.22f, 0.008f, 0.17f), mats["brim"], true);
        Part("CapButton", J["Head"], PrimitiveType.Sphere, new Vector3(0f, 0.39f, -0.005f), new Vector3(0f, 0f, 0f), new Vector3(0.035f, 0.02f, 0.035f), mats["brim"], true);
        Part("EyeR", J["Head"], PrimitiveType.Sphere, new Vector3(0.055f, 0.16f, 0.121f), new Vector3(0f, 0f, 0f), new Vector3(0.045f, 0.062f, 0.02f), mats["eye"], false);
        Part("EyeShineR", J["Head"], PrimitiveType.Sphere, new Vector3(0.063f, 0.172f, 0.131f), new Vector3(0f, 0f, 0f), new Vector3(0.016f, 0.016f, 0.006f), mats["white"], false);
        Part("BrowR", J["Head"], PrimitiveType.Cube, new Vector3(0.058f, 0.208f, 0.116f), new Vector3(0f, 0f, -8f), new Vector3(0.045f, 0.009f, 0.01f), mats["hair"], false);
        Part("BlushR", J["Head"], PrimitiveType.Sphere, new Vector3(0.085f, 0.122f, 0.098f), new Vector3(0f, 38f, 0f), new Vector3(0.04f, 0.018f, 0.01f), mats["blush"], false);
        Part("EyeL", J["Head"], PrimitiveType.Sphere, new Vector3(-0.055f, 0.16f, 0.121f), new Vector3(0f, 0f, 0f), new Vector3(0.045f, 0.062f, 0.02f), mats["eye"], false);
        Part("EyeShineL", J["Head"], PrimitiveType.Sphere, new Vector3(-0.047f, 0.172f, 0.131f), new Vector3(0f, 0f, 0f), new Vector3(0.016f, 0.016f, 0.006f), mats["white"], false);
        Part("BrowL", J["Head"], PrimitiveType.Cube, new Vector3(-0.058f, 0.208f, 0.116f), new Vector3(0f, 0f, 8f), new Vector3(0.045f, 0.009f, 0.01f), mats["hair"], false);
        Part("BlushL", J["Head"], PrimitiveType.Sphere, new Vector3(-0.085f, 0.122f, 0.098f), new Vector3(0f, -38f, 0f), new Vector3(0.04f, 0.018f, 0.01f), mats["blush"], false);
        Part("Mouth", J["Head"], PrimitiveType.Sphere, new Vector3(0f, 0.096f, 0.115f), new Vector3(0f, 0f, 0f), new Vector3(0.04f, 0.012f, 0.01f), mats["mouth"], false);
        Part("UpperArmR", J["ShoulderR"], PrimitiveType.Capsule, new Vector3(0f, -0.14f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0.11f, 0.16f, 0.11f), mats["jacket"], true);
        Part("ForearmR", J["ElbowR"], PrimitiveType.Capsule, new Vector3(0f, -0.11f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0.095f, 0.13f, 0.095f), mats["jacket"], true);
        Part("CuffR", J["ElbowR"], PrimitiveType.Cylinder, new Vector3(0f, -0.2f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0.1f, 0.015f, 0.1f), mats["trim"], true);
        Part("HandR", J["ElbowR"], PrimitiveType.Sphere, new Vector3(0f, -0.27f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0.085f, 0.1f, 0.075f), mats["skin"], true);
        Part("UpperArmL", J["ShoulderL"], PrimitiveType.Capsule, new Vector3(0f, -0.14f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0.11f, 0.16f, 0.11f), mats["jacket"], true);
        Part("ForearmL", J["ElbowL"], PrimitiveType.Capsule, new Vector3(0f, -0.11f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0.095f, 0.13f, 0.095f), mats["jacket"], true);
        Part("CuffL", J["ElbowL"], PrimitiveType.Cylinder, new Vector3(0f, -0.2f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0.1f, 0.015f, 0.1f), mats["trim"], true);
        Part("HandL", J["ElbowL"], PrimitiveType.Sphere, new Vector3(0f, -0.27f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0.085f, 0.1f, 0.075f), mats["skin"], true);
        Part("ThighR", J["LegR"], PrimitiveType.Capsule, new Vector3(0f, -0.19f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0.165f, 0.21f, 0.165f), mats["pants"], true);
        Part("ShinR", J["KneeR"], PrimitiveType.Capsule, new Vector3(0f, -0.17f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0.145f, 0.2f, 0.145f), mats["pants"], true);
        Part("ShoeR", J["KneeR"], PrimitiveType.Sphere, new Vector3(0f, -0.36f, 0.04f), new Vector3(0f, 0f, 0f), new Vector3(0.13f, 0.1f, 0.25f), mats["shoe"], true);
        Part("SoleR", J["KneeR"], PrimitiveType.Cube, new Vector3(0f, -0.395f, 0.04f), new Vector3(0f, 0f, 0f), new Vector3(0.135f, 0.03f, 0.25f), mats["sole"], true);
        Part("ThighL", J["LegL"], PrimitiveType.Capsule, new Vector3(0f, -0.19f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0.165f, 0.21f, 0.165f), mats["pants"], true);
        Part("ShinL", J["KneeL"], PrimitiveType.Capsule, new Vector3(0f, -0.17f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0.145f, 0.2f, 0.145f), mats["pants"], true);
        Part("ShoeL", J["KneeL"], PrimitiveType.Sphere, new Vector3(0f, -0.36f, 0.04f), new Vector3(0f, 0f, 0f), new Vector3(0.13f, 0.1f, 0.25f), mats["shoe"], true);
        Part("SoleL", J["KneeL"], PrimitiveType.Cube, new Vector3(0f, -0.395f, 0.04f), new Vector3(0f, 0f, 0f), new Vector3(0.135f, 0.03f, 0.25f), mats["sole"], true);

        // ---- 動畫程式 ----
        var anim = root.AddComponent<TrainerAnimator>();
        anim.hips      = J["Hips"];
        anim.spine     = J["Spine"];
        anim.head      = J["Head"];
        anim.shoulderR = J["ShoulderR"];
        anim.elbowR    = J["ElbowR"];
        anim.handR     = J["HandR"];
        anim.shoulderL = J["ShoulderL"];
        anim.elbowL    = J["ElbowL"];
        anim.legR      = J["LegR"];
        anim.kneeR     = J["KneeR"];
        anim.legL      = J["LegL"];
        anim.kneeL     = J["KneeL"];

        // ---- 存成 Prefab ----
        string path = PrefabDir + "/Trainer.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        // ---- 放一個到場景：平地中心往左 4 公尺，面向右邊 (+X) ----
        Vector3 center = Vector3.zero;
        Terrain t = Terrain.activeTerrain;
        if (t != null)
            center = t.transform.position + new Vector3(t.terrainData.size.x * 0.5f, 0f, t.terrainData.size.z * 0.5f);

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        inst.transform.position = center + new Vector3(-4f, 0f, 0f);
        inst.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        Undo.RegisterCreatedObjectUndo(inst, "Create Trainer");
        Selection.activeGameObject = inst;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
        EditorSceneManager.MarkSceneDirty(inst.scene);

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("HW5",
            "訓練家產生完成！\n\nPrefab：" + path +
            "\n場景裡已經放了一個（位置 " + inst.transform.position + "，面向右邊）。\n\n" +
            "按 Play：1.5 秒後會做一次丟球動作，之後按空白鍵可以再丟。\n記得 Ctrl+S 存檔。", "OK");
    }

    // ------------------------------------------------------------------
    static Transform Joint(string name, Transform parent, Vector3 pos, Vector3 euler, float scale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = euler;
        go.transform.localScale = Vector3.one * scale;
        return go.transform;
    }

    static void Part(string name, Transform parent, PrimitiveType type, Vector3 pos, Vector3 euler,
                     Vector3 scale, Material mat, bool castShadow)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());   // 不要碰撞體，免得擋到精靈球
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = euler;
        go.transform.localScale = scale;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        if (!castShadow) r.shadowCastingMode = ShadowCastingMode.Off;
    }

    static Material MakeMat(string name, Color c, float smooth)
    {
        Shader sh = null;
        if (GraphicsSettings.currentRenderPipeline != null) sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh) { color = c };
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        string path = MatDir + "/" + name + ".mat";
        var old = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (old != null) { old.shader = sh; old.CopyPropertiesFromMaterial(m); EditorUtility.SetDirty(old); return old; }
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
