// HW5 自然素材產生器 (Nature asset generator for 1151VR-HW5)
// 放在 Assets/Editor/ 資料夾裡，Unity 上方選單會出現 Tools > HW5
//   1. 產生自然素材 : 產生草地/泥土/岩石貼圖、草叢貼圖、4 種樹的 Prefab
//   2. 自動布置地形 : 做出中間平地 + 四周起伏的山，鋪貼圖、種樹、種草
// 適用 Built-In Render Pipeline（也會盡量相容 URP）

using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

public static class HW5NatureGenerator
{
    const string Root      = "Assets/HW5_Generated";
    const string TexDir    = Root + "/Textures";
    const string MatDir    = Root + "/Materials";
    const string MeshDir   = Root + "/Meshes";
    const string PrefabDir = Root + "/Prefabs";
    const string LayerDir  = Root + "/TerrainLayers";

    static readonly string[] TreeNames = { "Pine_A", "Pine_B", "Round_A", "Round_B" };

    // ------------------------------------------------------------------
    //  選單 1：產生素材
    // ------------------------------------------------------------------
    [MenuItem("Tools/HW5/1. 產生自然素材 (Generate Nature Assets)")]
    static void GenerateAssetsMenu()
    {
        if (GenerateAssets(true))
        {
            EditorUtility.DisplayDialog("HW5",
                "素材產生完成！\n\n位置：Assets/HW5_Generated\n" +
                "・Textures：草地、泥土、岩石、草叢貼圖\n" +
                "・Prefabs：4 種樹\n" +
                "・TerrainLayers：可以直接用在 Terrain 的 Paint Texture", "OK");
        }
    }

    static bool GenerateAssets(bool askIfExists)
    {
        if (askIfExists && AssetDatabase.IsValidFolder(PrefabDir))
        {
            bool again = EditorUtility.DisplayDialog("HW5",
                "素材已經產生過了，要重新產生嗎？\n（重新產生後，地形上已經種的樹會失效，需要再執行一次「2. 自動布置地形」或重新種樹）",
                "重新產生", "取消");
            if (!again) return false;
        }

        try
        {
            EnsureFolder(TexDir); EnsureFolder(MatDir); EnsureFolder(MeshDir);
            EnsureFolder(PrefabDir); EnsureFolder(LayerDir);

            // ---------- 貼圖 ----------
            EditorUtility.DisplayProgressBar("HW5", "產生地面貼圖…", 0.1f);
            var grassGround = SaveTexture(MakeGround(512,
                new Color(0.20f, 0.38f, 0.12f), new Color(0.40f, 0.62f, 0.22f),
                new Color(0.62f, 0.75f, 0.30f), 11, 0.04f, 4, false), "Ground_Grass", false, TextureWrapMode.Repeat);

            var dirt = SaveTexture(MakeGround(512,
                new Color(0.30f, 0.21f, 0.13f), new Color(0.52f, 0.40f, 0.26f),
                new Color(0.65f, 0.58f, 0.48f), 23, 0.06f, 4, false), "Ground_Dirt", false, TextureWrapMode.Repeat);

            var rock = SaveTexture(MakeGround(512,
                new Color(0.30f, 0.30f, 0.31f), new Color(0.60f, 0.59f, 0.56f),
                new Color(0.22f, 0.22f, 0.22f), 37, 0.05f, 4, true), "Ground_Rock", false, TextureWrapMode.Repeat);

            EditorUtility.DisplayProgressBar("HW5", "產生草叢貼圖…", 0.3f);
            SaveTexture(MakeGrassBlades(256, 5), "Grass_Detail", true, TextureWrapMode.Clamp);

            // ---------- Terrain Layers ----------
            MakeLayer("Layer_Grass", grassGround, 8f);
            MakeLayer("Layer_Dirt", dirt, 8f);
            MakeLayer("Layer_Rock", rock, 12f);

            // ---------- 材質 ----------
            EditorUtility.DisplayProgressBar("HW5", "產生樹…", 0.6f);
            var bark       = MakeMat("Bark",          new Color(0.36f, 0.24f, 0.14f), 0.05f);
            var pineLeaf   = MakeMat("Leaves_Pine",   new Color(0.12f, 0.36f, 0.19f), 0.10f);
            var pineLeaf2  = MakeMat("Leaves_Pine2",  new Color(0.18f, 0.44f, 0.22f), 0.10f);
            var roundLeaf  = MakeMat("Leaves_Round",  new Color(0.30f, 0.55f, 0.18f), 0.10f);
            var roundLeaf2 = MakeMat("Leaves_Round2", new Color(0.45f, 0.62f, 0.20f), 0.10f);

            // ---------- 樹 ----------
            SaveTreePrefab("Pine_A",  BuildPine(101, 1.00f), bark, pineLeaf);
            SaveTreePrefab("Pine_B",  BuildPine(202, 1.25f), bark, pineLeaf2);
            SaveTreePrefab("Round_A", BuildRound(303, 1.00f), bark, roundLeaf);
            SaveTreePrefab("Round_B", BuildRound(404, 0.85f), bark, roundLeaf2);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ------------------------------------------------------------------
    //  選單 2：自動布置地形
    // ------------------------------------------------------------------
    [MenuItem("Tools/HW5/2. 自動布置地形 (Auto Build Terrain)")]
    static void BuildTerrainMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Pine_A.prefab") == null)
        {
            if (!GenerateAssets(false)) return;
        }

        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            int choice = EditorUtility.DisplayDialogComplex("HW5 地形",
                "場景裡已經有一個 Terrain（" + terrain.name + "）。\n\n" +
                "要覆蓋它（高度、貼圖、樹、草全部重做），還是另外建立一個新的？",
                "覆蓋現有地形", "取消", "建立新地形");
            if (choice == 1) return;
            if (choice == 2) terrain = CreateTerrain();
        }
        else
        {
            terrain = CreateTerrain();
        }

        try
        {
            BuildTerrain(terrain);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    static Terrain CreateTerrain()
    {
        var td = new TerrainData();
        td.heightmapResolution = 257;               // 先設解析度，再設大小
        td.size = new Vector3(200f, 60f, 200f);
        string path = AssetDatabase.GenerateUniqueAssetPath(Root + "/HW5_TerrainData.asset");
        EnsureFolder(Root);
        AssetDatabase.CreateAsset(td, path);

        GameObject go = Terrain.CreateTerrainGameObject(td);
        go.name = "HW5_Terrain";
        go.transform.position = new Vector3(-100f, 0f, -100f);   // 讓平地中心在 (0,0,0)
        Undo.RegisterCreatedObjectUndo(go, "Create HW5 Terrain");
        return go.GetComponent<Terrain>();
    }

    static void BuildTerrain(Terrain terrain)
    {
        TerrainData td = terrain.terrainData;

        // ---------- 1) 高度：中間平地，四周起伏的山 ----------
        EditorUtility.DisplayProgressBar("HW5", "堆山中…", 0.1f);
        int res = td.heightmapResolution;
        var h = new float[res, res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float u = x / (float)(res - 1), v = y / (float)(res - 1);
                float d = CenterDist(u, v);
                float mask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.95f, d));
                float n = Fbm(u * 3f + 17.3f, v * 3f + 4.1f, 5);
                float ridge = 1f - Mathf.Abs(Mathf.PerlinNoise(u * 6f + 50f, v * 6f + 20f) * 2f - 1f);
                h[y, x] = mask * Mathf.Clamp01(0.15f + 0.65f * n + 0.2f * ridge) * 0.85f;
            }
        }
        td.SetHeights(0, 0, h);

        // ---------- 2) 地面貼圖：草地 / 泥土 / 岩石（陡坡） ----------
        EditorUtility.DisplayProgressBar("HW5", "鋪草地…", 0.35f);
        var grassL = AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerDir + "/Layer_Grass.terrainlayer");
        var dirtL  = AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerDir + "/Layer_Dirt.terrainlayer");
        var rockL  = AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerDir + "/Layer_Rock.terrainlayer");
        td.alphamapResolution = 256;
        td.terrainLayers = new[] { grassL, dirtL, rockL };

        int ar = td.alphamapResolution;
        var map = new float[ar, ar, 3];
        for (int ay = 0; ay < ar; ay++)
        {
            for (int ax = 0; ax < ar; ax++)
            {
                float u = ax / (float)(ar - 1), v = ay / (float)(ar - 1);
                float steep = td.GetSteepness(u, v);
                float hgt = td.GetInterpolatedHeight(u, v) / td.size.y;

                float rock = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(26f, 40f, steep));
                rock = Mathf.Max(rock, Mathf.InverseLerp(0.55f, 0.75f, hgt));
                float patch = Mathf.PerlinNoise(u * 12f + 3f, v * 12f + 7f);
                float dirt = (1f - rock) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.58f, 0.72f, patch)) * 0.8f;
                float grass = Mathf.Max(0f, 1f - rock - dirt);

                float sum = grass + dirt + rock;
                map[ay, ax, 0] = grass / sum;
                map[ay, ax, 1] = dirt / sum;
                map[ay, ax, 2] = rock / sum;
            }
        }
        td.SetAlphamaps(0, 0, map);

        // ---------- 3) 種樹（避開中間平地與陡坡） ----------
        EditorUtility.DisplayProgressBar("HW5", "種樹…", 0.6f);
        var protos = new List<TreePrototype>();
        foreach (var name in TreeNames)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + name + ".prefab");
            if (prefab != null) protos.Add(new TreePrototype { prefab = prefab });
        }
        td.treePrototypes = protos.ToArray();
        td.RefreshPrototypes();

        var rng = new System.Random(2026);
        var trees = new List<TreeInstance>();
        int attempts = 0;
        while (trees.Count < 320 && attempts < 30000)
        {
            attempts++;
            float u = (float)rng.NextDouble(), v = (float)rng.NextDouble();
            if (u < 0.02f || u > 0.98f || v < 0.02f || v > 0.98f) continue;
            if (CenterDist(u, v) < 0.45f) continue;                         // 中間平地不種
            if (td.GetSteepness(u, v) > 32f) continue;                      // 太陡不種
            if (Mathf.PerlinNoise(u * 8f + 11f, v * 8f + 5f) < 0.42f) continue; // 讓樹成群

            float s = 0.8f + 0.6f * (float)rng.NextDouble();
            trees.Add(new TreeInstance
            {
                position = new Vector3(u, 0f, v),
                prototypeIndex = rng.Next(protos.Count),
                widthScale = s,
                heightScale = s * (0.9f + 0.2f * (float)rng.NextDouble()),
                rotation = (float)(rng.NextDouble() * Mathf.PI * 2.0),
                color = Color.white,
                lightmapColor = Color.white
            });
        }
        td.SetTreeInstances(trees.ToArray(), true);

        // ---------- 4) 種草 ----------
        EditorUtility.DisplayProgressBar("HW5", "種草…", 0.8f);
        var grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/Grass_Detail.png");
        var dp = new DetailPrototype
        {
            prototypeTexture = grassTex,
            usePrototypeMesh = false,
            renderMode = DetailRenderMode.Grass,
            minWidth = 0.8f, maxWidth = 1.4f,
            minHeight = 0.5f, maxHeight = 1.0f,
            healthyColor = new Color(0.80f, 0.95f, 0.70f),
            dryColor = new Color(0.90f, 0.88f, 0.60f),
            noiseSpread = 0.4f
        };
        td.detailPrototypes = new[] { dp };
        td.SetDetailResolution(256, 16);

        bool coverage = false;
#if UNITY_2022_2_OR_NEWER
        coverage = td.detailScatterMode == DetailScatterMode.CoverageMode;
#endif
        int dr = td.detailResolution;
        var layer = new int[dr, dr];
        for (int dy = 0; dy < dr; dy++)
        {
            for (int dx = 0; dx < dr; dx++)
            {
                float u = dx / (float)(dr - 1), v = dy / (float)(dr - 1);
                float density = Mathf.InverseLerp(0.30f, 0.70f, Mathf.PerlinNoise(u * 10f + 1f, v * 10f + 9f));
                if (CenterDist(u, v) < 0.42f) density *= 0.6f;   // 平地上草少一點，才看得到腳
                if (td.GetSteepness(u, v) > 28f) density = 0f;
                layer[dy, dx] = coverage ? Mathf.RoundToInt(density * 255f) : Mathf.RoundToInt(density * 3f);
            }
        }
        td.SetDetailLayer(0, 0, 0, layer);

        // ---------- 5) 顯示設定 ----------
        terrain.detailObjectDistance = 120f;
        terrain.detailObjectDensity = 1f;
        terrain.treeDistance = 2000f;
        terrain.treeBillboardDistance = 300f;
        terrain.Flush();

        EditorUtility.SetDirty(td);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
        Selection.activeGameObject = terrain.gameObject;

        Vector3 center = terrain.transform.position + new Vector3(td.size.x * 0.5f, 0f, td.size.z * 0.5f);
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.LookAt(center + Vector3.up * 3f, Quaternion.Euler(20f, 0f, 0f), 60f);

        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("HW5",
            "地形布置完成！\n\n中間平地的中心座標：" + center +
            "\n等一下訓練家、寶可夢、精靈球都放在這附近（地面高度 Y = " + center.y + "）。\n\n記得按 Ctrl+S 存檔。", "OK");
    }

    // ==================================================================
    //  貼圖產生
    // ==================================================================
    static Texture2D MakeGround(int size, Color dark, Color light, Color speck,
                                int seed, float speckAmount, int basePeriod, bool streaks)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float n = TileFbm(u, v, basePeriod, 5, seed);
                if (streaks)
                    n = n * 0.6f + 0.4f * TileNoise(u * 4f, v * 24f, 4, 24, seed + 500);
                float fine = TileNoise(x * 0.5f, y * 0.5f, size / 2, size / 2, seed + 999);

                Color c = Color.Lerp(dark, light, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.75f, n)));
                c *= 0.85f + 0.3f * fine;
                if (Hash(x, y, seed + 7) < speckAmount) c = Color.Lerp(c, speck, 0.6f);
                c.a = 1f;
                px[y * size + x] = c;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    // 透明背景上的一叢草（給 Terrain 的 Paint Details 用）
    static Texture2D MakeGrassBlades(int size, int seed)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];
        var bg = new Color(0.35f, 0.60f, 0.20f, 0f);   // 透明但帶綠色，避免邊緣出現黑邊
        for (int i = 0; i < px.Length; i++) px[i] = bg;

        var rng = new System.Random(seed);
        Color bottom = new Color(0.22f, 0.42f, 0.10f);
        for (int b = 0; b < 30; b++)
        {
            float baseX = size * (0.12f + 0.76f * (float)rng.NextDouble());
            float hgt   = size * (0.50f + 0.47f * (float)rng.NextDouble());
            float w     = size * (0.03f + 0.025f * (float)rng.NextDouble());
            float bend  = size * ((float)rng.NextDouble() - 0.5f) * 0.4f;
            Color tip = Color.Lerp(new Color(0.78f, 0.96f, 0.48f), new Color(0.50f, 0.82f, 0.28f), (float)rng.NextDouble());

            for (int y = 0; y < (int)hgt && y < size; y++)
            {
                float t = y / hgt;
                float cx = baseX + bend * t * t;
                float half = w * (1f - t) * 0.5f + 0.4f;
                int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - half - 1f));
                int x1 = Mathf.Min(size - 1, Mathf.CeilToInt(cx + half + 1f));
                for (int x = x0; x <= x1; x++)
                {
                    float a = Mathf.Clamp01(half + 0.5f - Mathf.Abs(x + 0.5f - cx));
                    if (a <= 0f) continue;
                    Color c = Color.Lerp(bottom, tip, t);
                    if (x + 0.5f < cx) c *= 0.85f;          // 葉片一側稍暗，有立體感
                    int i = y * size + x;
                    Color dst = px[i];
                    float outA = a + dst.a * (1f - a);
                    Color outC = (c * a + dst * (dst.a * (1f - a))) / Mathf.Max(outA, 1e-5f);
                    outC.a = outA;
                    px[i] = outC;
                }
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    // ==================================================================
    //  樹的模型（低多邊形）
    // ==================================================================
    class MeshBuilder
    {
        public readonly List<Vector3> verts = new List<Vector3>();
        public readonly List<int>[] subs;

        public MeshBuilder(int subCount)
        {
            subs = new List<int>[subCount];
            for (int i = 0; i < subCount; i++) subs[i] = new List<int>();
        }

        public void Tri(int sub, Vector3 a, Vector3 b, Vector3 c)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            subs[sub].Add(i); subs[sub].Add(i + 1); subs[sub].Add(i + 2);
        }

        public void Cylinder(int sub, Vector3 p, float r0, float r1, float h, int seg)
        {
            for (int i = 0; i < seg; i++)
            {
                float a0 = Mathf.PI * 2f * i / seg, a1 = Mathf.PI * 2f * (i + 1) / seg;
                Vector3 b0 = p + new Vector3(Mathf.Cos(a0) * r0, 0f, Mathf.Sin(a0) * r0);
                Vector3 b1 = p + new Vector3(Mathf.Cos(a1) * r0, 0f, Mathf.Sin(a1) * r0);
                Vector3 t0 = p + new Vector3(Mathf.Cos(a0) * r1, h, Mathf.Sin(a0) * r1);
                Vector3 t1 = p + new Vector3(Mathf.Cos(a1) * r1, h, Mathf.Sin(a1) * r1);
                Tri(sub, b0, t0, b1);
                Tri(sub, b1, t0, t1);
            }
        }

        public void Cone(int sub, Vector3 p, float r, float h, int seg, float rot, Vector3 tipOffset)
        {
            Vector3 apex = p + new Vector3(0f, h, 0f) + tipOffset;
            for (int i = 0; i < seg; i++)
            {
                float a0 = rot + Mathf.PI * 2f * i / seg, a1 = rot + Mathf.PI * 2f * (i + 1) / seg;
                Vector3 b0 = p + new Vector3(Mathf.Cos(a0) * r, 0f, Mathf.Sin(a0) * r);
                Vector3 b1 = p + new Vector3(Mathf.Cos(a1) * r, 0f, Mathf.Sin(a1) * r);
                Tri(sub, b0, apex, b1);   // 側面
                Tri(sub, p, b0, b1);      // 底面（朝下）
            }
        }

        public void Blob(int sub, Vector3 c, Vector3 radius, int lat, int lon, float jitter, System.Random rng)
        {
            var P = new Vector3[lat + 1, lon];
            for (int i = 0; i <= lat; i++)
            {
                float th = Mathf.PI * i / lat;
                for (int j = 0; j < lon; j++)
                {
                    float ph = Mathf.PI * 2f * j / lon;
                    float k = (i == 0 || i == lat) ? 1f : 1f + jitter * ((float)rng.NextDouble() * 2f - 1f);
                    Vector3 dir = (i == 0) ? Vector3.up : (i == lat) ? Vector3.down
                                : new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                    P[i, j] = c + Vector3.Scale(dir, radius) * k;
                }
            }
            for (int i = 0; i < lat; i++)
            {
                for (int j = 0; j < lon; j++)
                {
                    int jn = (j + 1) % lon;
                    Vector3 p00 = P[i, j], p01 = P[i, jn], p10 = P[i + 1, j], p11 = P[i + 1, jn];
                    if (i != lat - 1) Tri(sub, p10, p00, p11);
                    if (i != 0)       Tri(sub, p11, p00, p01);
                }
            }
        }

        public Mesh Build(string name)
        {
            var m = new Mesh { name = name };
            m.SetVertices(verts);
            m.subMeshCount = subs.Length;
            for (int i = 0; i < subs.Length; i++) m.SetTriangles(subs[i], i);
            m.RecalculateNormals();   // 頂點不共用 → 平面著色的低多邊形風格
            m.RecalculateBounds();
            return m;
        }
    }

    // 松樹：樹幹 + 4 層圓錐
    static Mesh BuildPine(int seed, float scale)
    {
        var rng = new System.Random(seed);
        var mb = new MeshBuilder(2);
        mb.Cylinder(0, new Vector3(0f, -0.3f, 0f), 0.28f * scale, 0.18f * scale, 2.4f * scale, 7);
        for (int k = 0; k < 4; k++)
        {
            float y = (1.2f + k * 1.25f) * scale;
            float r = (2.1f - k * 0.45f) * scale;
            float hh = (2.4f - k * 0.3f) * scale;
            Vector3 tip = new Vector3(((float)rng.NextDouble() - 0.5f) * 0.3f, 0f, ((float)rng.NextDouble() - 0.5f) * 0.3f) * scale;
            mb.Cone(1, new Vector3(0f, y, 0f), r, hh, 8, (float)rng.NextDouble() * Mathf.PI, tip);
        }
        return mb.Build("Pine_" + seed);
    }

    // 圓樹：樹幹 + 一大三小的樹冠
    static Mesh BuildRound(int seed, float scale)
    {
        var rng = new System.Random(seed);
        var mb = new MeshBuilder(2);
        mb.Cylinder(0, new Vector3(0f, -0.3f, 0f), 0.32f * scale, 0.2f * scale, 3.2f * scale, 7);
        mb.Blob(1, new Vector3(0f, 3.9f, 0f) * scale, new Vector3(2.1f, 1.8f, 2.1f) * scale, 5, 8, 0.15f, rng);
        for (int k = 0; k < 3; k++)
        {
            float ang = k * Mathf.PI * 2f / 3f + (float)rng.NextDouble();
            Vector3 c = new Vector3(Mathf.Cos(ang) * 1.4f, 3.4f + (float)rng.NextDouble() * 0.9f, Mathf.Sin(ang) * 1.4f) * scale;
            float r = (1.1f + (float)rng.NextDouble() * 0.4f) * scale;
            mb.Blob(1, c, new Vector3(r, r * 0.85f, r), 4, 6, 0.15f, rng);
        }
        return mb.Build("Round_" + seed);
    }

    // ==================================================================
    //  存檔工具
    // ==================================================================
    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static Texture2D SaveTexture(Texture2D tex, string name, bool alpha, TextureWrapMode wrap)
    {
        string path = TexDir + "/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.wrapMode = wrap;
        ti.alphaIsTransparency = alpha;
        ti.mipmapEnabled = true;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static TerrainLayer MakeLayer(string name, Texture2D tex, float tile)
    {
        var tl = new TerrainLayer { diffuseTexture = tex, tileSize = new Vector2(tile, tile) };
        AssetDatabase.CreateAsset(tl, LayerDir + "/" + name + ".terrainlayer");
        return tl;
    }

    static Material MakeMat(string name, Color c, float smooth)
    {
        Shader sh = null;
        if (GraphicsSettings.currentRenderPipeline != null) sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh) { color = c };
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        AssetDatabase.CreateAsset(m, MatDir + "/" + name + ".mat");
        return m;
    }

    static void SaveTreePrefab(string name, Mesh mesh, Material bark, Material leaves)
    {
        AssetDatabase.CreateAsset(mesh, MeshDir + "/" + name + ".asset");
        var go = new GameObject(name);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = new[] { bark, leaves };
        PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/" + name + ".prefab");
        Object.DestroyImmediate(go);
    }

    // ==================================================================
    //  雜訊
    // ==================================================================
    static float CenterDist(float u, float v)
    {
        float dx = (u - 0.5f) * 2f, dy = (v - 0.5f) * 2f;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    static float Fbm(float x, float y, int octaves)
    {
        float sum = 0f, amp = 0.5f, norm = 0f, f = 1f;
        for (int o = 0; o < octaves; o++)
        {
            sum += amp * Mathf.PerlinNoise(x * f, y * f);
            norm += amp; amp *= 0.5f; f *= 2f;
        }
        return sum / norm;
    }

    static int Mod(int a, int m) { int r = a % m; return r < 0 ? r + m : r; }

    static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + seed * 1442695041;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0x7fffffff) / (float)int.MaxValue;
        }
    }

    // 可無縫拼接的 value noise
    static float TileNoise(float x, float y, int periodX, int periodY, int seed)
    {
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;
        float sx = fx * fx * (3f - 2f * fx), sy = fy * fy * (3f - 2f * fy);
        int ax = Mod(x0, periodX), bx = Mod(x0 + 1, periodX);
        int ay = Mod(y0, periodY), by = Mod(y0 + 1, periodY);
        float a = Hash(ax, ay, seed), b = Hash(bx, ay, seed);
        float c = Hash(ax, by, seed), d = Hash(bx, by, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, sx), Mathf.Lerp(c, d, sx), sy);
    }

    static float TileFbm(float u, float v, int basePeriod, int octaves, int seed)
    {
        float sum = 0f, amp = 0.5f, norm = 0f;
        int p = basePeriod;
        for (int o = 0; o < octaves; o++)
        {
            sum += amp * TileNoise(u * p, v * p, p, p, seed + o * 31);
            norm += amp; amp *= 0.5f; p *= 2;
        }
        return sum / norm;
    }
}
