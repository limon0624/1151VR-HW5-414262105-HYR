
https://youtu.be/l_oRLf6K1ZI
https://github.com/limon0624/1151VR-HW5-414262105-HYR

製作流程
(1) 建立專案與 Repository
在 GitHub 建立 Public repository 1151VR-HW5-414262105-HYR，.gitignore 選 Unity
在 Unity Hub 建立 3D (URP) 專案
(2) 建立場景：樹、草地、起伏的山
新增 Terrain（GameObject → 3D Object → Terrain），大小設為 200 × 200，高度 60
用編輯器腳本 HW5NatureGenerator.cs 產生地面貼圖（草地、泥土、岩石）、草叢貼圖和 4 種低多邊形樹的 Prefab
用腳本自動布置地形：中間保留平地放角色，四周用 Perlin Noise 堆出起伏的山，陡坡自動鋪岩石貼圖，再種樹和草
把 Terrain 的位置設為 (-100, 0, -100)，讓平地中心對準世界原點
(3) 載入 3D 模型
訓練家：用編輯器腳本 HW5TrainerGenerator.cs，以 Unity 基本形狀（球體、膠囊、圓柱、方塊）組成 Q 版人物，並建立關節（肩膀、手肘、腿、膝蓋等）讓它可以做動作
寶可夢、精靈球：從 Poly Pizza 下載 GLB 模型，安裝 glTFast 套件（com.unity.cloud.gltfast）後匯入 Unity
調整三個模型的大小與位置：訓練家在左、寶可夢在右，攝影機從正前方拍攝
(4) 訓練家丟球動畫（TrainerAnimator.cs）
待機時有呼吸、手臂微微擺動的動畫
丟球動作分成四個姿勢：待機 → 蓄力（右手舉到頭後方、身體後轉、左腳踏出）→ 出手（右手往前甩）→ 收尾，用程式在姿勢之間平滑內插
出手的瞬間發出事件（Released），通知精靈球飛出
(5) 精靈球由左至右拋出（PokeballThrow.cs）
出手前，每一幀把精靈球貼在訓練家右手上
收到出手事件後，沿拋物線飛向寶可夢： 位置 = Lerp(起點, 終點, t) + 向上 × 拋物線高度 × 4t(1 − t)
飛行時持續旋轉；到達寶可夢時發出 Hit 事件
(6) 收服特效（CaptureEffect.cs）
精靈球打中後往回彈，停在半空中
寶可夢身上爆出閃光粒子，並打一道點光源
寶可夢換成紅色發光材質，紅白閃爍，一道光束把它縮小、吸進精靈球
精靈球掉到地上彈一下，再左右搖晃三下
星星粒子特效爆開，畫面上方顯示「收服成功！」

所有特效（粒子、光束、星星貼圖、文字）都由程式產生，不需要額外的素材。
