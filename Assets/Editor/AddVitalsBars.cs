using UnityEditor;
using UnityEngine;

namespace WWG.EditorTools
{
    /// <summary>把 VitalsBars 作为嵌套 prefab 挂到 GameplayOverlay 的 Top bar 下。</summary>
    public static class AddVitalsBars
    {
        private const string OverlayPath = "Assets/Prefabs/UI/GameplayOverlay.prefab";
        private const string VitalsPath = "Assets/Prefabs/UI/VitalsBars.prefab";

        [MenuItem("WWG/Add Vitals Bars")]
        public static void Add()
        {
            var root = PrefabUtility.LoadPrefabContents(OverlayPath);
            if (root == null) { Debug.LogError("[Vitals] 打不开 " + OverlayPath); return; }

            var topBar = root.transform.Find("Top bar");
            if (topBar == null) { Debug.LogError("[Vitals] 找不到 Top bar"); PrefabUtility.UnloadPrefabContents(root); return; }

            if (topBar.Find("Vitals") != null)
            {
                Debug.Log("[Vitals] Top bar/Vitals 已存在，跳过");
                PrefabUtility.UnloadPrefabContents(root);
                return;
            }

            var src = AssetDatabase.LoadAssetAtPath<GameObject>(VitalsPath);
            if (src == null) { Debug.LogError("[Vitals] 找不到 " + VitalsPath); PrefabUtility.UnloadPrefabContents(root); return; }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(src, topBar);
            inst.name = "Vitals";
            var rt = inst.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-24f, -14f);
                rt.sizeDelta = new Vector2(460f, 44f);
                rt.localScale = Vector3.one;
            }

            PrefabUtility.SaveAsPrefabAsset(root, OverlayPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[Vitals] 已把 Vitals 挂到 Top bar 下（anchoredPosition -24,-14，size 460x44）");
        }
    }
}
