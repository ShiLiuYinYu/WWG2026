using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

namespace WWG.Editor
{
    /// <summary>One-time, idempotent layout migration. Existing authored objects are never recreated.</summary>
    public static class Temp1UiMigration
    {
        private static Sprite frame;
        private static Sprite button;
        private static Sprite dialoguePaper;

        [MenuItem("WWG/UI/Apply temp1 Layout")]
        public static void Apply()
        {
            PrepareSprite("Assets/Resources/UI/选择框.png", new Vector4(28, 28, 28, 28));
            PrepareSprite("Assets/Resources/S0/选项按钮.png", new Vector4(20, 20, 20, 20));
            PrepareSprite("Assets/Resources/UI/黄皮纸4_横.png", Vector4.zero);
            foreach (string name in new[] { "YuanQi", "TangChe", "XuYuan", "主角", "失踪女生" })
                PrepareSprite("Assets/Resources/Characters/" + name + ".png", Vector4.zero);
            frame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/选择框.png");
            button = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/S0/选项按钮.png");
            dialoguePaper = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/黄皮纸4_横.png");
            EditPrefab("Assets/Prefabs/UI/GameplayOverlay.prefab", EditOverlay);
            EditPrefab("Assets/Resources/Dialogue/WwgDialogueHost.prefab", EditDialogue);
            EditPrefab("Assets/Resources/UI/SaveSlotPanel.prefab", Skin);
            EditPrefab("Assets/Resources/UI/SettingsPanel.prefab", Skin);
            EditTitleScene();
            foreach (string scene in new[] { "BridgeOdd4F", "BridgeSpan4F", "BridgeEven4F", "ClubRoom" })
                EditS2Scene("Assets/Scenes/Locations/" + scene + ".unity");
            foreach (string scene in new[] { "S0", "S0/S0_ClubRoom", "S0/S0_Corridor", "S0/S0_Bridge",
                                            "S1", "S1/S1_Corridor", "S1/S1_BridgeEntrance" })
                EditChapterScene("Assets/Scenes/" + scene + ".unity");
            var cast = AssetDatabase.LoadAssetAtPath<DialogueCast>("Assets/Resources/Dialogue/WwgDialogueCast.asset");
            if (cast != null)
            {
                foreach (var entry in cast.entries)
                {
                    string file = entry.speaker == "袁祁" ? "YuanQi" : entry.speaker == "唐澈" ? "TangChe" : entry.speaker == "许圆" ? "XuYuan" : null;
                    if (file == null) continue;
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Characters/" + file + ".png");
                    if (sprite != null) entry.portrait = sprite;
                }
                AddCastIfMissing(cast, "我", "主角");
                AddCastIfMissing(cast, "林未", "失踪女生");
                AddCastIfMissing(cast, "失踪女生", "失踪女生");
                EditorUtility.SetDirty(cast);
            }
            AssetDatabase.SaveAssets();
        }

        private static void PrepareSprite(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteBorder == border) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        private static void AddCastIfMissing(DialogueCast cast, string speaker, string spriteName)
        {
            if (cast.Find(speaker) != null) return;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Characters/" + spriteName + ".png");
            if (sprite != null) cast.entries.Add(new DialogueCast.Entry { speaker = speaker, portrait = sprite });
        }

        private static void EditPrefab(string path, Action<GameObject> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try { edit(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static Transform Find(Transform root, string path) => root.Find(path);

        private static RectTransform Place(Transform parent, string name, Vector2 min, Vector2 max,
            Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var child = parent.Find(name);
            bool created = child == null;
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
                child = go.transform;
                child.SetParent(parent, false);
            }
            var rect = (RectTransform)child;
            if (created)
            {
                rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot;
                rect.anchoredPosition = pos; rect.sizeDelta = size;
            }
            return rect;
        }

        private static Image Panel(Transform parent, string name, Vector2 min, Vector2 max,
            Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var rect = Place(parent, name, min, max, pivot, pos, size);
            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = frame; image.type = Image.Type.Simple; image.color = Color.white;
            return image;
        }

        private static Text Label(Transform parent, string name, string value, Vector2 pos, Vector2 size, int fontSize = 24)
        {
            var rect = Place(parent, name, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(0, 1), pos, size);
            var text = rect.GetComponent<Text>() ?? rect.gameObject.AddComponent<Text>();
            text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize; text.color = new Color(.95f, .88f, .72f);
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            return text;
        }

        private static Button Button(Transform parent, string name, string label, Vector2 min,
            Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var rect = Place(parent, name, min, max, pivot, pos, size);
            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = button; image.type = Image.Type.Simple; image.color = Color.white;
            var result = rect.GetComponent<Button>() ?? rect.gameObject.AddComponent<Button>();
            Label(rect, "Label", label, Vector2.zero, size);
            return result;
        }

        private static void Skin(GameObject root)
        {
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.GetComponent<Button>() != null)
                {
                    image.sprite = button; image.type = Image.Type.Simple; image.color = Color.white;
                    foreach (var label in image.GetComponentsInChildren<Text>(true))
                        label.color = new Color(.95f, .88f, .72f);
                }
                else if (image.transform == root.transform || image.name.IndexOf("panel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         image.name.IndexOf("card", StringComparison.OrdinalIgnoreCase) >= 0 || image.name.IndexOf("frame", StringComparison.OrdinalIgnoreCase) >= 0)
                { image.sprite = frame; image.type = Image.Type.Simple; image.color = Color.white; }
            }
        }

        private static void EditOverlay(GameObject root)
        {
            var t = root.transform;
            if (Find(t, "Inventory panel") != null)
            {
                Skin(root);
                EnsureGhost(t);
                Find(t, "Top bar/Record button/Label").GetComponent<Text>().text = "备忘录";
                Find(t, "Inventory panel/Title").GetComponent<Text>().color = new Color(.95f, .88f, .72f);
                Find(t, "Inventory panel/Selection").GetComponent<Text>().color = new Color(.95f, .88f, .72f);
                var back = Find(t, "Item detail/Close/Label")?.GetComponent<Text>();
                if (back != null) back.text = "返回";
                return;
            }
            var top = Find(t, "Top bar");
            var topImage = top.GetComponent<Image>();
            if (topImage != null) topImage.color = Color.clear;
            var left = Panel(top, "Left card", new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(0, 1), Vector2.zero, new Vector2(520, 136));
            left.transform.SetAsFirstSibling();
            Find(top, "Top rule")?.gameObject.SetActive(false);
            var pause = Find(top, "Pause button");
            ((RectTransform)pause).anchoredPosition = new Vector2(18, -28);
            ((RectTransform)pause).sizeDelta = new Vector2(178, 66);
            pause.Find("Label").GetComponent<Text>().text = "暂停";
            ((RectTransform)Find(top, "Time")).anchoredPosition = new Vector2(215, -17);
            ((RectTransform)Find(top, "Objective")).anchoredPosition = new Vector2(545, -24);
            ((RectTransform)Find(top, "Record button")).anchoredPosition = new Vector2(215, -65);
            ((RectTransform)Find(top, "Record button")).sizeDelta = new Vector2(220, 48);

            var oldDock = Find(t, "Inventory dock");
            var bag = Button(t, "Inventory button", "背包", new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(1, 0), new Vector2(-28, 26), new Vector2(158, 70));
            bag.transform.SetSiblingIndex(oldDock.GetSiblingIndex());
            var panel = Panel(t, "Inventory panel", new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(1, 0), new Vector2(-28, 108), new Vector2(650, 670)).transform;
            panel.SetSiblingIndex(bag.transform.GetSiblingIndex() + 1);
            Label(panel, "Title", "背包", new Vector2(32, -22), new Vector2(500, 48), 32);
            var grid = Place(panel, "Grid", new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(0, 1), new Vector2(32, -96), new Vector2(500, 500));
            var photo = Find(oldDock, "Photo slot");
            var badge = Find(oldDock, "Badge slot");
            photo.SetParent(grid, false); badge.SetParent(grid, false);
            for (int i = 0; i < 25; i++)
            {
                string name = i == 0 ? "Photo slot" : i == 1 ? "Badge slot" : "Empty " + i.ToString("00");
                Transform cell = i == 0 ? photo : i == 1 ? badge : grid.Find(name);
                if (cell == null)
                {
                    cell = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).transform;
                    cell.SetParent(grid, false);
                }
                var rect = (RectTransform)cell;
                rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2((i % 5) * 102, -(i / 5) * 102);
                rect.sizeDelta = new Vector2(92, 92);
                var image = cell.GetComponent<Image>() ?? cell.gameObject.AddComponent<Image>();
                image.sprite = null; image.color = new Color(0, 0, 0, .55f);
                if (i < 2)
                {
                    var icon = cell.Find("Icon");
                    if (icon != null) icon.gameObject.SetActive(false);
                    var label = cell.Find("Label") as RectTransform;
                    label.anchorMin = Vector2.zero; label.anchorMax = Vector2.one;
                    label.offsetMin = new Vector2(5, 5); label.offsetMax = new Vector2(-5, -5);
                    var text = label.GetComponent<Text>();
                    text.alignment = TextAnchor.MiddleCenter; text.fontSize = 19;
                    text.color = Color.white; text.text = i == 0 ? "旧照片" : "旧工牌";
                }
            }
            Label(panel, "Selection", "暂无物品", new Vector2(33, -609), new Vector2(490, 36), 19);
            Button(panel, "Close", "返回", new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(1, 1), new Vector2(-22, -20), new Vector2(100, 52));
            var tip = Panel(panel, "Tooltip", new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(1, 1), new Vector2(-16, -96), new Vector2(270, 195)).transform;
            Label(tip, "Name", "物品", new Vector2(16, -15), new Vector2(238, 54), 21);
            Button(tip, "Use", "使用", new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(0, 1), new Vector2(16, -90), new Vector2(110, 58));
            Button(tip, "Inspect", "查看", new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(0, 1), new Vector2(142, -90), new Vector2(110, 58));
            tip.gameObject.SetActive(false);
            UnityEngine.Object.DestroyImmediate(oldDock.gameObject);
            Skin(root);
            var shade = Find(t, "Shade").GetComponent<Image>();
            shade.sprite = null; shade.color = new Color(0, 0, 0, .45f);
            Find(t, "Item detail/Use").gameObject.SetActive(false);
            Find(t, "Item detail/Inspect").gameObject.SetActive(false);
            EnsureGhost(t);
        }

        private static void EnsureGhost(Transform root)
        {
            if (root.Find("Inventory ghost") != null) return;
            var ghost = Place(root, "Inventory ghost", new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero, new Vector2(72, 72));
            var image = ghost.GetComponent<Image>() ?? ghost.gameObject.AddComponent<Image>();
            image.sprite = null; image.color = new Color(.08f, .08f, .08f, .78f);
            image.raycastTarget = false;
            var label = Label(ghost, "Name", "", new Vector2(4, -4), new Vector2(64, 64), 17);
            label.color = Color.white; label.raycastTarget = false;
            ghost.gameObject.SetActive(false);
        }

        private static void EditDialogue(GameObject root)
        {
            var panel = Find(root.transform, "Dialogue root/Dialogue panel") as RectTransform;
            var image = panel.GetComponent<Image>();
            var paper = dialoguePaper != null ? dialoguePaper : frame;
            // 只在还是旧皮肤时改矩形；已经换成黄皮纸的对话框保留人工调整过的位置与尺寸。
            if (image.sprite == null || image.sprite == frame)
            {
                panel.anchorMin = new Vector2(0, 0); panel.anchorMax = new Vector2(1, 0);
                panel.pivot = new Vector2(.5f, 0); panel.anchoredPosition = Vector2.zero;
                panel.sizeDelta = new Vector2(-44, 268);
            }
            image.sprite = paper; image.type = Image.Type.Simple; image.color = Color.white;
            var portrait = Find(panel, "Speaker portrait").GetComponent<Image>();
            portrait.color = new Color(.06f, .055f, .05f, 1f);
            portrait.preserveAspect = true;
            var portraitRect = portrait.rectTransform;
            if (Mathf.Approximately(portraitRect.anchoredPosition.y, 75f))
                portraitRect.anchoredPosition = new Vector2(portraitRect.anchoredPosition.x, 305f);
            var presenter = root.GetComponent<WwgDialoguePresenter>();
            if (presenter != null)
            {
                var serialized = new SerializedObject(presenter);
                var indent = serialized.FindProperty("textLeftWithPortrait");
                if (indent != null && Mathf.Approximately(indent.floatValue, 290f))
                { indent.floatValue = 36f; serialized.ApplyModifiedPropertiesWithoutUndo(); }
            }
            Skin(Find(root.transform, "Document panel/Document card").gameObject);
        }

        private static void EditS2Scene(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                GameObject canvasObject = null;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var candidate in root.GetComponentsInChildren<Canvas>(true))
                        if (candidate.name == "S2 Canvas") canvasObject = candidate.gameObject;
                if (canvasObject == null) throw new InvalidOperationException("Missing S2 Canvas: " + path);
                var canvas = canvasObject.transform;
                var authoredTop = Find(canvas, "Top bar");
                if (authoredTop != null)
                {
                    authoredTop.gameObject.SetActive(true);
                    foreach (string name in new[] { "Pause", "Time", "Record" })
                    {
                        var control = authoredTop.Find(name);
                        if (control != null) control.gameObject.SetActive(true);
                    }
                }
                var duplicateLocation = Find(canvas, "Location");
                if (duplicateLocation != null) duplicateLocation.gameObject.SetActive(false);
                foreach (Transform child in canvas)
                    if (child.name == "Inventory" && child.GetComponent<Button>() == null && child.Find("Grid") != null)
                    {
                        string[] existingIds = { StoryIds.Item.Photo, StoryIds.Item.Badge, StoryIds.Item.MachineNote, StoryIds.Item.TeamSheet };
                        var existingGrid = child.Find("Grid");
                        for (int i = 0; i < existingIds.Length; i++)
                        {
                            var cell = existingGrid.Find("Cell " + i.ToString("00"));
                            if (cell == null) continue;
                            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(cell.gameObject);
                            var slots = cell.GetComponents<S2InventorySlot>();
                            var slot = slots.Length == 0 ? cell.gameObject.AddComponent<S2InventorySlot>() : slots[0];
                            for (int j = 1; j < slots.Length; j++) UnityEngine.Object.DestroyImmediate(slots[j]);
                            slot.item = existingIds[i];
                        }
                        Skin(child.gameObject);
                        var memoExisting = Find(canvas, "Task record");
                        if (memoExisting != null) { Skin(memoExisting.gameObject); MakeScrollable(memoExisting, "Record"); }
                        var infoExisting = Find(canvas, "Inspect"); if (infoExisting != null) Skin(infoExisting.gameObject);
                        var infoBack = Find(canvas, "Inspect/Close/Caption")?.GetComponent<Text>();
                        if (infoBack != null) infoBack.text = "返回";
                        var codeExisting = Find(canvas, "Four digit cabinet"); if (codeExisting != null) Skin(codeExisting.gameObject);
                        foreach (var uiButton in Find(canvas, "Top bar").GetComponentsInChildren<Button>(true))
                            Skin(uiButton.gameObject);
                        foreach (Transform uiObject in canvas)
                            if (uiObject.name == "Inventory" && uiObject.GetComponent<Button>() != null)
                                Skin(uiObject.gameObject);
                        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                        return;
                    }
                var top = Find(canvas, "Top bar");
                var topImage = top.GetComponent<Image>();
                if (topImage != null) topImage.color = Color.clear;
                var card = Panel(top, "Left card", new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(0, 1), Vector2.zero, new Vector2(530, 138));
                card.transform.SetAsFirstSibling();
                var pause = Find(canvas, "Pause");
                if (pause != null && pause.GetComponent<Button>() != null)
                {
                    pause.SetParent(top, false);
                    var rt = (RectTransform)pause;
                    rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 1); rt.anchoredPosition = new Vector2(20, -26);
                    rt.sizeDelta = new Vector2(175, 64);
                    var caption = pause.Find("Caption")?.GetComponent<Text>();
                    if (caption != null) caption.text = "暂停";
                }
                var time = Find(canvas, "Time") as RectTransform;
                if (time != null)
                {
                    time.SetParent(top, false);
                    time.anchorMin = time.anchorMax = new Vector2(0, 1);
                    time.pivot = new Vector2(0, 1);
                    time.anchoredPosition = new Vector2(215, -19); time.sizeDelta = new Vector2(285, 45);
                }
                var record = Find(canvas, "Record");
                if (record != null && record.GetComponent<Button>() != null)
                {
                    record.SetParent(top, false);
                    var rt = (RectTransform)record;
                    rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 1); rt.anchoredPosition = new Vector2(215, -74);
                    rt.sizeDelta = new Vector2(220, 48);
                    var caption = record.Find("Caption")?.GetComponent<Text>();
                    if (caption != null) caption.text = "备忘录";
                }
                var objective = Find(canvas, "Objective") as RectTransform;
                if (objective != null) objective.anchoredPosition = new Vector2(555, -24);

                var bagButton = Find(canvas, "Inventory");
                if (bagButton != null && bagButton.GetComponent<Button>() != null)
                {
                    var rt = (RectTransform)bagButton;
                    rt.anchorMin = rt.anchorMax = new Vector2(1, 0);
                    rt.pivot = new Vector2(1, 0);
                    rt.anchoredPosition = new Vector2(-28, 26); rt.sizeDelta = new Vector2(158, 70);
                    var caption = bagButton.Find("Caption")?.GetComponent<Text>();
                    if (caption != null) caption.text = "背包";
                }
                var panel = Find(canvas, "Inventory");
                if (panel == null || panel.GetComponent<Button>() != null)
                    foreach (Transform child in canvas)
                        if (child.name == "Inventory" && child.GetComponent<Button>() == null) panel = child;
                if (panel == null) throw new InvalidOperationException("Missing inventory panel: " + path);
                var panelRect = (RectTransform)panel;
                panelRect.anchorMin = panelRect.anchorMax = new Vector2(1, 0);
                panelRect.pivot = new Vector2(1, 0);
                panelRect.anchoredPosition = new Vector2(-28, 110);
                panelRect.sizeDelta = new Vector2(650, 670);
                var items = panel.Find("Items");
                if (items != null) items.gameObject.SetActive(false);
                var grid = Place(panel, "Grid", new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(0, 1), new Vector2(32, -96), new Vector2(500, 500));
                string[] ids = { StoryIds.Item.Photo, StoryIds.Item.Badge, StoryIds.Item.MachineNote, StoryIds.Item.TeamSheet };
                for (int i = 0; i < 25; i++)
                {
                    var rect = Place(grid, "Cell " + i.ToString("00"), new Vector2(0, 1), new Vector2(0, 1),
                        new Vector2(0, 1), new Vector2((i % 5) * 102, -(i / 5) * 102), new Vector2(92, 92));
                    var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
                    image.sprite = null; image.color = new Color(0, 0, 0, .55f);
                    if (i < ids.Length)
                    {
                        var slot = rect.GetComponent<S2InventorySlot>() ?? rect.gameObject.AddComponent<S2InventorySlot>();
                        slot.item = ids[i];
                        var label = Label(rect, "Name", S2InventoryGrid.ItemName(ids[i]), new Vector2(5, -5), new Vector2(82, 82), 17);
                        label.color = Color.white;
                    }
                }
                var close = panel.Find("Close") as RectTransform;
                if (close != null)
                {
                    close.anchorMin = close.anchorMax = new Vector2(1, 1);
                    close.pivot = new Vector2(1, 1);
                    close.anchoredPosition = new Vector2(-18, -18);
                    close.sizeDelta = new Vector2(110, 55);
                }
                var tip = Panel(panel, "Tooltip", new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(1, 1), new Vector2(-16, -96), new Vector2(270, 195)).transform;
                Label(tip, "Name", "物品", new Vector2(16, -15), new Vector2(238, 54), 21);
                Button(tip, "Use", "使用", new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(0, 1), new Vector2(16, -90), new Vector2(110, 58));
                Button(tip, "Inspect", "查看", new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(0, 1), new Vector2(142, -90), new Vector2(110, 58));
                tip.gameObject.SetActive(false);
                if (panel.GetComponent<S2InventoryGrid>() == null) panel.gameObject.AddComponent<S2InventoryGrid>();
                Skin(panel.gameObject);
                var memo = Find(canvas, "Task record");
                if (memo != null)
                {
                    Skin(memo.gameObject);
                    var title = memo.Find("Title")?.GetComponent<Text>();
                    if (title != null) title.text = "备忘录";
                    MakeScrollable(memo, "Record");
                }
                var info = Find(canvas, "Inspect"); if (info != null) Skin(info.gameObject);
                var code = Find(canvas, "Four digit cabinet"); if (code != null) Skin(code.gameObject);
                var pausePanel = Find(canvas, "Pause");
                if (pausePanel != null && pausePanel.GetComponent<Button>() == null) Skin(pausePanel.gameObject);
                foreach (var buttonObject in new[] { pause, record, bagButton })
                {
                    if (buttonObject == null || buttonObject.GetComponent<Button>() == null) continue;
                    var image = buttonObject?.GetComponent<Image>();
                    if (image != null) { image.sprite = button; image.type = Image.Type.Simple; image.color = Color.white; }
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        private static void MakeScrollable(Transform panel, string textName)
        {
            var textTransform = panel.Find(textName) as RectTransform;
            if (textTransform == null)
            {
                var existingViewport = panel.Find("Viewport");
                var existing = existingViewport?.Find(textName) as RectTransform;
                if (existing != null) existing.sizeDelta = new Vector2(0, existing.sizeDelta.y);
                if (existingViewport != null) EnsureScrollRaycast(existingViewport);
                return;
            }
            var viewport = Place(panel, "Viewport", new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(0, 1), textTransform.anchoredPosition, textTransform.sizeDelta);
            if (viewport.GetComponent<RectMask2D>() == null) viewport.gameObject.AddComponent<RectMask2D>();
            EnsureScrollRaycast(viewport);
            textTransform.SetParent(viewport, false);
            textTransform.anchorMin = new Vector2(0, 1); textTransform.anchorMax = new Vector2(1, 1);
            textTransform.pivot = new Vector2(0, 1); textTransform.anchoredPosition = Vector2.zero;
            textTransform.sizeDelta = new Vector2(0, textTransform.sizeDelta.y);
            var fitter = textTransform.GetComponent<ContentSizeFitter>() ?? textTransform.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.GetComponent<ScrollRect>() ?? viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = textTransform;
            scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 30;
        }

        private static void EnsureScrollRaycast(Transform viewport)
        {
            var image = viewport.GetComponent<Image>() ?? viewport.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
        }

        private static void EditChapterScene(string path)
        {
            var active = EditorSceneManager.GetActiveScene();
            bool isActive = active.path == path;
            var scene = isActive ? active : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    var s0 = root.GetComponentInChildren<S0Controller>(true);
                    if (s0 != null)
                    {
                        Skin(s0.memoPanel);
                        Skin(s0.infoPanel);
                        Skin(s0.completionPanel);
                        MakeScrollable(s0.memoPanel.transform, s0.memoText.name);
                    }
                    var s1 = root.GetComponentInChildren<S1Controller>(true);
                    if (s1 != null)
                    {
                        Skin(s1.memoPanel);
                        Skin(s1.infoPanel);
                        Skin(s1.photoPanel);
                        Skin(s1.completionPanel);
                        MakeScrollable(s1.memoPanel.transform, s1.memoText.name);
                    }
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (!isActive) EditorSceneManager.CloseScene(scene, true); }
        }

        private static void EditTitleScene()
        {
            const string path = "Assets/Scenes/Title.unity";
            var active = EditorSceneManager.GetActiveScene();
            bool isActive = active.path == path;
            var scene = isActive ? active : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                        Skin(canvas.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (!isActive) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}

