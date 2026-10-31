#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CG2SkillPanel.Editor
{
    public static class CG2SkillPanelBuilder
    {
        private const string Base = "Assets/CG2SkillPanel/";
        private static Font cachedFont;

        private static Font DefaultFont()
        {
            if (cachedFont != null) return cachedFont;
            try { cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { }
            if (cachedFont == null)
            {
                try { cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                catch { }
            }
            return cachedFont;
        }

        private static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static GameObject Ui(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, x, y, w, h);
            return go;
        }

        private static Sprite Sprite(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(Base + "Art/" + name + ".png");

        private static Image AddImage(GameObject go, Sprite sprite = null,
                                      Color? tint = null)
        {
            go.AddComponent<CanvasRenderer>();
            var image = go.AddComponent<Image>();
            if (sprite != null) image.sprite = sprite;
            image.color = tint ?? Color.white;
            image.raycastTarget = true;
            return image;
        }

        private static Text Text(
            string name, Transform parent, float x, float y, float w, float h,
            string content, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var go = Ui(name, parent, x, y, w, h);
            go.AddComponent<CanvasRenderer>();
            var text = go.AddComponent<Text>();
            text.font = DefaultFont();
            text.fontSize = size;
            text.text = content;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static Button Button(string name, Transform parent,
            float x, float y, float w, float h, Sprite background = null)
        {
            var go = Ui(name, parent, x, y, w, h);
            var img = AddImage(go, background, background == null ? new Color(1,1,1,0) : Color.white);
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.ColorTint;
            var cb = button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1, 0.96f, 0.70f);
            cb.pressedColor = new Color(0.8f, 0.77f, 0.7f);
            cb.selectedColor = Color.white;
            cb.fadeDuration = 0.08f;
            button.colors = cb;
            return button;
        }

        private static void SetupSprites()
        {
            foreach (var file in Directory.GetFiles(Base + "Art", "*.png"))
            {
                var importer = AssetImporter.GetAtPath(file) as TextureImporter;
                if (importer == null) continue;
                if (importer.textureType != TextureImporterType.Sprite ||
                    importer.filterMode != FilterMode.Point || importer.mipmapEnabled ||
                    importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 100;
                    importer.filterMode = FilterMode.Point;
                    importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }
        }

        private static GameObject BuildProfessionItem()
        {
            var b = Button("ProfessionItem", null, 0, 0, 116, 24);
            Text("Label", b.transform, 4, 0, 112, 24, "劍士", 13, new Color32(248,242,218,255));
            return b.gameObject;
        }
        private static GameObject BuildKindItem()
        {
            var b = Button("KindItem", null, 0, 0, 85, 24);
            Text("Label", b.transform, 3, 0, 82, 24, "全部", 13, new Color32(248,242,218,255));
            return b.gameObject;
        }
        private static GameObject BuildSkillRow()
        {
            var b = Button("SkillRow", null, 0, 0, 312, 18, Sprite("RowRose"));
            Text("Code", b.transform, 0, 0, 44, 18, "【010】", 12, Color.white, TextAnchor.MiddleCenter);
            Text("Kind", b.transform, 47, 0, 51, 18, "徒手", 12, Color.white);
            Text("Name", b.transform, 101, 0, 161, 18, "氣功波", 12, Color.white);
            Text("Cost", b.transform, 262, 0, 45, 18, "MP 10", 12, Color.white, TextAnchor.MiddleRight);
            return b.gameObject;
        }
        private static GameObject BuildSkillDetail()
        {
            var obj = Ui("SkillDetail", null, 0, 0, 320, 138);
            AddImage(obj, Sprite("DetailBackdrop"));
            var outline = obj.AddComponent<Outline>(); outline.effectColor = new Color32(160,115,47,255);
            outline.effectDistance = new Vector2(2,-2);
            Text("Title", obj.transform, 12, 8, 274, 22, "技能資訊", 14, new Color32(73,51,30,255));
            var body = Text("Body", obj.transform, 12, 35, 296, 96, "技能資料", 11,
                new Color32(57,44,32,255), TextAnchor.UpperLeft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            var close = Button("DetailClose", obj.transform, 293, 5, 22, 22);
            Text("Label", close.transform, 0, 0, 22, 22, "×", 17, new Color32(73,51,30,255), TextAnchor.MiddleCenter);
            return obj;
        }

        private static GameObject SaveTemplate(GameObject go, string path)
        {
            var asset = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return asset;
        }

        private static GameObject BuildPanel(
            GameObject professionPrefab, GameObject kindPrefab,
            GameObject rowPrefab, GameObject detailPrefab)
        {
            var root = Ui("CG2SkillPanel", null, 0, 0, 687, 403);
            var rt = (RectTransform)root.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            AddImage(root, Sprite("PanelChrome")).raycastTarget = true;

            // The base contains the original title, borders, sort label, close and arrows.
            // These transparent controls cover only their extracted native rectangles.
            var category = Button("CategorySelector", root.transform, 43, 36, 78, 12);
            var categoryLabel = Text("Label", category.transform, 0, 0, 78, 12,
                "戰鬥系職業", 11, Color.white, TextAnchor.MiddleCenter);
            var categoryPrevious = Button("CategoryPrevious", root.transform, 29,36,12,12);
            var categoryNext = Button("CategoryNext", root.transform, 124,36,12,12);
            var group = Button("SkillGroup", root.transform, 212,36,77,12);
            var groupLabel = Text("Label", group.transform, 0,0,77,12,"戰鬥系",11,Color.white,TextAnchor.MiddleCenter);
            var groupPrevious = Button("GroupPrevious", root.transform,198,36,12,12);
            var groupNext = Button("GroupNext", root.transform,292,36,12,12);
            var professions = (RectTransform)Ui("ProfessionList", root.transform,20,55,116,315).transform;
            var kinds = (RectTransform)Ui("SkillKindList", root.transform,157,55,186,315).transform;
            var listTitle = Text("SkillListTitle", root.transform,369,24,200,18,
                "戰鬥系 - 全部",13,new Color32(69,59,32,255));
            var sort = Button("Sort", root.transform,586,25,78,14,Sprite("SortLabelBlank"));
            var sortLabel = Text("Label",sort.transform,0,0,78,14,"類別",12,Color.white,TextAnchor.MiddleCenter);
            var sortPrevious = Button("SortPrevious",root.transform,572,25,12,14);
            var sortNext = Button("SortNext",root.transform,666,25,14,14);
            var close = Button("Close", root.transform,155,381,55,14);
            var rows = (RectTransform)Ui("SkillRows",root.transform,368,46,312,324).transform;
            var first = Button("First",root.transform,450,374,19,20);
            var prev = Button("Previous",root.transform,474,374,19,20);
            var page = Text("PageCounter",root.transform,499,375,47,20,"1 / 4",12,
                new Color32(87,72,40,255),TextAnchor.MiddleCenter);
            var next = Button("Next",root.transform,554,374,19,20);
            var last = Button("Last",root.transform,579,374,19,20);

            var detail = (GameObject)PrefabUtility.InstantiatePrefab(detailPrefab);
            detail.transform.SetParent(root.transform, false);
            Place((RectTransform)detail.transform, 363, 160, 320, 138);
            detail.name = "SelectedSkillDetail";
            var selectedTitle = detail.transform.Find("Title").GetComponent<Text>();
            var selectedBody = detail.transform.Find("Body").GetComponent<Text>();
            var detailClose = detail.transform.Find("DetailClose").GetComponent<Button>();
            detail.SetActive(false);

            var controller = root.AddComponent<CG2SkillPanelController>();
            controller.skillData = AssetDatabase.LoadAssetAtPath<TextAsset>(Base + "Resources/cg2-skills.json");
            controller.rowRose = Sprite("RowRose");
            controller.rowGreen = Sprite("RowGreen");
            controller.rowTeal = Sprite("RowTeal");
            controller.rowGold = Sprite("RowGold");
            controller.rowSelected = Sprite("RowSelected");
            controller.rowBlue = Sprite("RowBlue");
            controller.categoryLabel = categoryLabel;
            controller.occupationLabel = groupLabel;
            controller.listTitleLabel = listTitle;
            controller.pageLabel = page;
            controller.selectedTitle = selectedTitle;
            controller.selectedBody = selectedBody;
            controller.categoryButton = category;
            controller.categoryPreviousButton = categoryPrevious;
            controller.categoryNextButton = categoryNext;
            controller.groupButton = group;
            controller.groupPreviousButton = groupPrevious;
            controller.groupNextButton = groupNext;
            controller.sortPreviousButton = sortPrevious;
            controller.sortNextButton = sortNext;
            controller.sortButton = sort;
            controller.sortLabel = sortLabel;
            controller.previousButton = prev;
            controller.nextButton = next;
            controller.firstButton = first;
            controller.lastButton = last;
            controller.closeButton = close;
            controller.detailCloseButton = detailClose;
            controller.detailsPanel = detail;
            controller.professionsContainer = professions;
            controller.kindsContainer = kinds;
            controller.rowsContainer = rows;
            controller.professionItemPrefab = professionPrefab;
            controller.kindItemPrefab = kindPrefab;
            controller.skillRowPrefab = rowPrefab;
            return root;
        }

        [MenuItem("Tools/CG2/Generate Interactive Skill Panel Prefabs")]
        public static void Generate()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SetupSprites();
            Directory.CreateDirectory(Base + "Prefabs");
            Directory.CreateDirectory(Base + "Scenes");
            AssetDatabase.Refresh();
            var professions = SaveTemplate(BuildProfessionItem(), Base + "Prefabs/ProfessionItem.prefab");
            var kinds = SaveTemplate(BuildKindItem(), Base + "Prefabs/SkillKindItem.prefab");
            var rows = SaveTemplate(BuildSkillRow(), Base + "Prefabs/SkillRow.prefab");
            var detail = SaveTemplate(BuildSkillDetail(), Base + "Prefabs/SkillDetail.prefab");
            var panel = SaveTemplate(BuildPanel(professions, kinds, rows, detail),
                                     Base + "Prefabs/CG2SkillPanel.prefab");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = canvas.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.pixelPerfect = true;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1;
            scaler.referenceResolution = new Vector2(687, 403);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(panel);
            instance.transform.SetParent(canvas.transform, false);
            ((RectTransform)instance.transform).anchoredPosition = Vector2.zero;
            var reopen = Button("ReopenPanel", canvas.transform, 12, 12, 100, 26, Sprite("TabGold"));
            Text("Label",reopen.transform,0,0,100,26,"技能資訊",12,Color.white,TextAnchor.MiddleCenter);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(reopen.onClick,
                instance.GetComponent<CG2SkillPanelController>().Reopen);
            EditorSceneManager.SaveScene(scene, Base + "Scenes/CG2SkillPanelDemo.unity");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CG2: generated five Prefabs and demo Scene under " + Base);
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("CG2 技能面板",
                "已生成 CG2SkillPanel、SkillRow、SkillKindItem、ProfessionItem、SkillDetail Prefab 和演示 Scene。", "确定");
        }
    }
}
#endif
