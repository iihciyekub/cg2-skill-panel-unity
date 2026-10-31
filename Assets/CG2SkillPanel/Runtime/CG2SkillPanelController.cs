using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace CG2SkillPanel
{
    [Serializable] public class SkillLearningInfo
    {
        public string npcName = "", mapName = "", position = "", requirements = "";
    }
    [Serializable] public class SkillRecord
    {
        public string code, kind, name, cost, group, skin;
        public float confidence;
        public int rowIndex;
        public bool reviewed;
        public SkillLearningInfo learning = new SkillLearningInfo();
    }
    [Serializable] public class SkillScreenshot
    {
        public string category, occupation, sourceImage, frame;
        public int pageNumber;
        public SkillRecord[] skills;
    }
    [Serializable] public class SkillDatabase
    {
        public int schemaVersion;
        public string notice;
        public SkillScreenshot[] pages;
    }
    /// <summary>Source-extracted chrome with independent data-backed UGUI controls.</summary>
    public class CG2SkillPanelController : MonoBehaviour
    {
        public TextAsset skillData;
        public Font uiFont;
        public Sprite rowRose, rowGreen, rowTeal, rowGold, rowBlue, rowSelected;
        public Text sortLabel;
        public Text categoryLabel, occupationLabel, listTitleLabel, pageLabel, selectedTitle, selectedBody;
        public Button categoryButton, categoryPreviousButton, categoryNextButton;
        public Button groupButton, groupPreviousButton, groupNextButton;
        public Button sortButton, sortPreviousButton, sortNextButton;
        public Button previousButton, nextButton, firstButton, lastButton, closeButton;
        public Button resetButton, detailCloseButton;
        public GameObject detailsPanel;
        public RectTransform professionsContainer, kindsContainer, rowsContainer;
        public GameObject professionItemPrefab, kindItemPrefab, skillRowPrefab;
        private SkillDatabase db;
        private Font runtimeFont;
        private readonly List<string> families = new List<string>();
        private string family, occupation, group = "戰鬥系", kindFilter = "全部";
        private int pageIndex, sortIndex;
        private Image selectedImage;
        private Color selectedColor;
        private const int PageSize = 18;
        private class Entry { public SkillRecord skill; public SkillScreenshot source; }
        private static readonly string[] Groups = { "戰鬥系", "其他", "製作系" };
        private static readonly string[] JobOrder = { "士兵", "戰士", "騎士", "格鬥士", "狂戰士", "弓箭手", "劍士", "槍術士" };
        private static readonly string[][] BattleKinds = {
            new[] { "全部", "徒手", "劍", "斧", "槍", "杖", "弓", "小刀", "投擲武器" },
            new[] { "近戰", "遠程", "魔法攻擊", "魔法回復", "魔法輔助", "魔法妨礙", "特殊技能", "強化" }
        };
        private static readonly string[][] OtherKinds = {
            new[] { "全部", "採伐", "採集", "狩獵", "挖掘" },
            new[] { "料理", "調配", "加工", "開拓", "治療", "鑑定", "其他" }
        };
        private static readonly string[][] ProductionKinds = {
            new[] { "全部", "製作" },
            new string[0]
        };
        private void Awake()
        {
            if (skillData == null) skillData = Resources.Load<TextAsset>("cg2-skills");
            db = skillData != null ? JsonUtility.FromJson<SkillDatabase>(skillData.text) : null;
            if (db == null || db.pages == null || db.pages.Length == 0)
            { Debug.LogError("CG2: missing Resources/cg2-skills.json"); enabled = false; return; }
            foreach (var p in db.pages) if (!families.Contains(p.category)) families.Add(p.category);
            family = families.Contains("物理系職業") ? "物理系職業" : families[0];
            occupation = Jobs().Contains("劍士") ? "劍士" : Jobs().First();
            ApplyRuntimeFont();
            Listen(categoryButton, () => CycleFamily(1));
            Listen(categoryPreviousButton, () => CycleFamily(-1));
            Listen(categoryNextButton, () => CycleFamily(1));
            Listen(groupButton, () => CycleGroup(1));
            Listen(groupPreviousButton, () => CycleGroup(-1));
            Listen(groupNextButton, () => CycleGroup(1));
            Listen(sortButton, () => CycleSort(1));
            Listen(sortPreviousButton, () => CycleSort(-1));
            Listen(sortNextButton, () => CycleSort(1));
            Listen(previousButton, () => SetPage(pageIndex-1));
            Listen(nextButton, () => SetPage(pageIndex+1));
            Listen(firstButton, () => SetPage(0));
            Listen(lastButton, () => SetPage(PageCount()-1));
            Listen(closeButton, () => gameObject.SetActive(false));
            Listen(detailCloseButton, CloseDetail);
            Listen(resetButton, () => { kindFilter = "全部"; sortIndex = pageIndex = 0; Refresh(); });
            detailsPanel.SetActive(false);
            Refresh();
        }
        private static void Listen(Button b, UnityEngine.Events.UnityAction action)
        { if (b != null) b.onClick.AddListener(action); }
        private void ApplyRuntimeFont()
        {
            try {
                runtimeFont = uiFont != null ? uiFont : Font.CreateDynamicFontFromOSFont(new[] { "PMingLiU", "MingLiU", "Songti TC", "SimSun", "Microsoft YaHei", "PingFang SC" }, 13);
                foreach (var t in GetComponentsInChildren<Text>(true)) if (runtimeFont != null) t.font = runtimeFont;
            } catch (Exception ex) { Debug.LogWarning("CG2 font fallback: " + ex.Message); }
        }
        private IEnumerable<string> Jobs()
        {
            var a = db.pages.Where(p => p.category == family).Select(p => p.occupation).Distinct();
            return family == "物理系職業" ? a.OrderBy(j => Array.IndexOf(JobOrder,j)) : a;
        }
        private bool Matches(SkillRecord r)
        {
            if (kindFilter == "全部") return true;
            if (kindFilter == "近戰") return new[] { "徒手", "劍", "斧", "槍", "杖" }.Contains(r.kind);
            if (kindFilter == "遠程") return new[] { "弓", "小刀", "投擲武器" }.Contains(r.kind);
            return r.kind == kindFilter;
        }
        private List<Entry> CurrentRows()
        {
            var a = db.pages.Where(p => p.category == family && p.occupation == occupation)
                .SelectMany(p => (p.skills ?? Array.Empty<SkillRecord>()).Select(s => new Entry { skill=s, source=p }))
                .Where(e => (string.IsNullOrEmpty(e.skill.group) ? "戰鬥系" : e.skill.group) == group && Matches(e.skill));
            if (sortIndex == 1) a = a.OrderBy(e => e.skill.name, StringComparer.Create(CultureInfo.GetCultureInfo("zh-TW"),false));
            if (sortIndex == 2) a = a.OrderBy(e => NumericCost(e.skill));
            if (sortIndex == 3) a = a.OrderBy(e => NumericCost(e.skill) == int.MaxValue).ThenByDescending(e => NumericCost(e.skill));
            return a.ToList();
        }
        private static int NumericCost(SkillRecord s)
        {
            var m = System.Text.RegularExpressions.Regex.Match(s.cost ?? "", @"\d+");
            int n; return m.Success && int.TryParse(m.Value,out n) ? n : int.MaxValue;
        }
        private int PageCount() => (CurrentRows().Count+PageSize-1)/PageSize;
        private static void Clear(RectTransform parent)
        { foreach (Transform child in parent) { child.gameObject.SetActive(false); Destroy(child.gameObject); } }
        private void ItemFont(GameObject item)
        { if (runtimeFont != null) foreach (var t in item.GetComponentsInChildren<Text>(true)) t.font = runtimeFont; }
        private static void Place(RectTransform rt, float x, float y, float w, float h)
        { rt.anchorMin = rt.anchorMax = new Vector2(0,1); rt.pivot = new Vector2(0,1); rt.anchoredPosition = new Vector2(x,-y); rt.sizeDelta = new Vector2(w,h); }
        private void CycleFamily(int delta)
        { family = families[(families.IndexOf(family)+delta+families.Count)%families.Count]; occupation=Jobs().First(); kindFilter="全部";pageIndex=0;Refresh(); }
        private void CycleGroup(int delta)
        { group=Groups[(Array.IndexOf(Groups,group)+delta+Groups.Length)%Groups.Length];kindFilter="全部";pageIndex=0;Refresh(); }
        private void CycleSort(int delta)
        { sortIndex=(sortIndex+delta+4)%4;pageIndex=0;RefreshRows(); }
        private void SetPage(int index)
        { pageIndex=Mathf.Clamp(index,0,Mathf.Max(0,PageCount()-1));RefreshRows(); }
        public void Reopen() { gameObject.SetActive(true); }
        public void Refresh()
        {
            if (db == null) return;
            categoryLabel.text=family=="物理系職業"?"戰鬥系職業":family;
            occupationLabel.text=group;
            Clear(professionsContainer);int i=0;
            foreach (var job in Jobs()) {
                string value=job;var item=Instantiate(professionItemPrefab,professionsContainer);item.SetActive(true);ItemFont(item);
                Place((RectTransform)item.transform,0,i++*24,116,24);
                var label=item.GetComponentInChildren<Text>(true);label.text=job;
                label.color=job==occupation?new Color32(255,225,125,255):new Color32(248,242,218,255);
                item.GetComponent<Button>().onClick.AddListener(()=>{occupation=value;kindFilter="全部";pageIndex=0;Refresh();});
            }
            Clear(kindsContainer);var columns=group=="其他"?OtherKinds:group=="製作系"?ProductionKinds:BattleKinds;
            for (int c=0;c<columns.Length;c++) for (int r=0;r<columns[c].Length;r++) {
                string value=columns[c][r];var item=Instantiate(kindItemPrefab,kindsContainer);item.SetActive(true);ItemFont(item);
                Place((RectTransform)item.transform,c*88,r*24,85,24);
                var label=item.GetComponentInChildren<Text>(true);label.text=value;
                label.color=value==kindFilter?new Color32(255,225,125,255):new Color32(248,242,218,255);
                item.GetComponent<Button>().onClick.AddListener(()=>{kindFilter=value;pageIndex=0;Refresh();});
            }
            RefreshRows();
        }
        private Sprite Skin(SkillRecord skill)
        {
            switch (skill.skin) {
                case "RowGreen": return rowGreen;
                case "RowTeal": return rowTeal;
                case "RowGold": return rowGold;
                case "RowBlue": return rowBlue != null?rowBlue:rowTeal;
                default: return rowRose;
            }
        }
        private void CloseDetail()
        { if (selectedImage != null) selectedImage.color=selectedColor;selectedImage=null;detailsPanel.SetActive(false); }
        private void RefreshRows()
        {
            if (sortLabel != null) sortLabel.text=new[] { "類別", "名稱", "MP ↑", "MP ↓" }[sortIndex];
            CloseDetail();Clear(rowsContainer);var rows=CurrentRows();int pages=(rows.Count+PageSize-1)/PageSize;
            pageIndex=Mathf.Clamp(pageIndex,0,Mathf.Max(0,pages-1));
            pageLabel.text=(pages==0?0:pageIndex+1)+" / "+pages;listTitleLabel.text=group+" - "+kindFilter;
            firstButton.interactable=previousButton.interactable=pageIndex>0;
            lastButton.interactable=nextButton.interactable=pageIndex<pages-1;
            int i=0;
            foreach (var entry in rows.Skip(pageIndex*PageSize).Take(PageSize)) {
                var data=entry;var item=Instantiate(skillRowPrefab,rowsContainer);item.SetActive(true);ItemFont(item);
                Place((RectTransform)item.transform,0,i++*18,312,18);
                foreach (var t in item.GetComponentsInChildren<Text>(true)) {
                    if (t.name=="Code")t.text=data.skill.code;
                    if (t.name=="Kind")t.text=data.skill.kind;
                    if (t.name=="Name")t.text=data.skill.name;
                    if (t.name=="Cost")t.text=data.skill.cost;
                }
                var img=item.GetComponent<Image>();img.sprite=Skin(data.skill);
                item.GetComponent<Button>().onClick.AddListener(()=>{
                    CloseDetail();selectedImage=img;selectedColor=img.color;img.color=new Color(1f,.91f,.65f);
                    selectedTitle.text=data.skill.name;
                    var learn=data.skill.learning ?? new SkillLearningInfo();
                    selectedBody.text=data.skill.code+"  "+data.skill.kind+"  "+data.skill.cost+"\n"+
                        "NPC："+LearningValue(learn.npcName)+"\n"+
                        "地圖："+LearningValue(learn.mapName)+"\n"+
                        "坐標："+LearningValue(learn.position)+"\n"+
                        "學習條件："+LearningValue(learn.requirements);
                    detailsPanel.SetActive(true);
                });
            }
        }
        private static string LearningValue(string value) => string.IsNullOrWhiteSpace(value) ? "待補充" : value;
        private void Update() { if (Input.GetKeyDown(KeyCode.Escape) && detailsPanel.activeSelf) CloseDetail(); }
    }
}
