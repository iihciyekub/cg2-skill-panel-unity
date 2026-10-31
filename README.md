# CG2 Skill Panel · Unity 资源包

从游戏截图拆出的技能面板 UI，可生成五个 UGUI Prefab 并导入游戏项目。默认面板尺寸 **687 × 403**，支持职业/技能系别切换、类型筛选、名称与 MP 排序、18 行分分页，以及技能学习地点弹窗。

本仓库只包含 Unity 素材、运行脚本、Prefab 生成器、技能 JSON 与维护工具。没有网页预览、原始截图、OCR 中间文件、Unity 缓存或完整游戏工程。

## 合作伙伴如何导入

### 使用资源包

1. 从 [Releases](../../releases) 下载 `CG2SkillPanel-0.1.0.unitypackage`。
2. 在目标 Unity 项目选择 **Assets → Import Package → Custom Package**，导入全部内容。
3. 确认已启用 **Unity UI / com.unity.ugui**。示例使用旧版 Input Manager；**Active Input Handling** 使用 Input Manager 或 Both。
4. 选择 **Tools → CG2 → Generate Interactive Skill Panel Prefabs**。
5. 打开生成的 `Assets/CG2SkillPanel/Scenes/CG2SkillPanelDemo.unity`，按 Play。
6. 将 `Assets/CG2SkillPanel/Prefabs/CG2SkillPanel.prefab` 拖入自己游戏的 Canvas，即可继续对接。

生成器会先询问是否保存当前已修改的场景，再创建演示场景。重新生成会覆盖本包同名生成文件；自定义 Prefab 请另存名称。

### 从 GitHub 源码导入

克隆仓库，或下载源码 ZIP。将 `Assets/CG2SkillPanel` **连同 `.meta` 文件**复制到游戏项目的 `Assets` 中，然后执行相同生成菜单。无需复制 `tools` 到 Unity。

## 包含的资源

| 路径 | 用途 |
| --- | --- |
| `Assets/CG2SkillPanel/Art` | 原图取样拼接的去字底板、行皮肤、独立按钮贴图 |
| `Assets/CG2SkillPanel/Resources/cg2-skills.json` | schema v3 技能数据：43 个职业/宠物分类，3,134 条记录 |
| `Assets/CG2SkillPanel/Runtime/CG2SkillPanelController.cs` | UGUI 控制器、数据结构、学习地点占位逻辑 |
| `Assets/CG2SkillPanel/Editor/CG2SkillPanelBuilder.cs` | 生成完整面板、职业项、分类项、技能行和详情五个 Prefab，以及演示场景 |
| `tools/validate_assets.py` | 检查资源引用、稳定 GUID 和技能必填字段 |
| `tools/export_package.py` | 生成可分发的 `.unitypackage` 和 Assets ZIP |

`Prefabs/` 和 `Scenes/` 由 Unity 编辑器菜单生成，源码仓库不包含未经 Unity 生成的伪 Prefab。`.meta` 使用固定 GUID，合作开发时应一并提交，避免引用变化。

## NPC 学习地点：留给游戏项目填写

每条技能都有独立的 `learning` 对象，初始为空，不捏造 NPC 或位置：

```json
{
  "code": "【010】",
  "kind": "劍",
  "name": "刀背斬",
  "cost": "MP 10",
  "learning": {
    "npcName": "",
    "mapName": "",
    "position": "",
    "requirements": ""
  }
}
```

| 字段 | 填写内容 |
| --- | --- |
| `npcName` | 教授该技能的 NPC 名称或游戏内约定标识 |
| `mapName` | 所在地图/区域 |
| `position` | 坐标或位置描述，格式由游戏项目约定 |
| `requirements` | 学习条件，例如等级、职业、前置任务等 |

点击技能时，弹窗显示 **NPC、地图、坐标、学习条件**；空字符串显示“待補充”。同一技能在不同职业下可能有不同 MP 或学习条件，应编辑对应职业 `pages[].skills[]` 中的记录，不要全局覆盖同名技能。`sourceImage` 与 `rowIndex` 仅用于定位原始记录，不是资源加载依赖。

游戏运行时也可以通过 `SkillRecord.learning` 填写后再显示；游戏系统负责 NPC 交互、任务判断、扣费和真正学习技能。本包提供 UI 与数据占位。

## 对接现有游戏

- 默认数据通过 `Resources.Load<TextAsset>("cg2-skills")` 读取；也可在控制器的 **Skill Data** 中指定另一个 TextAsset。数据须保留 `pages` 结构。
- 在生成的完整面板控制器上指定 **Ui Font**，使用游戏项目已有、包含中文字符的 Font 资源。未指定时使用系统中文字体回退；移动平台或缺少字体的环境应配置自己的字体。
- 控制器 `Reopen()` 可绑定游戏自己的技能按钮。关闭面板会隐藏对象，演示场景另有打开按钮。
- Canvas 使用像素对齐，原生 1× 尺寸；贴图为 Point、无 mipmap、无压缩。集成到游戏时可按项目分辨率调整 Canvas Scaler。
- 分类按钮和两侧箭头用于轮转职业大类/技能系别/排序；职业和技能类型列表直接点击选择。

## 数据与验证状态

编号、类别、名称和 MP 已通过完整性检查，未保留空字段；不同职业 MP 独立保留。**字段完整不代表所有名称均已人工校对**，部分文字仍为 OCR 候选；`reviewed` 和相关来源/识别元数据保留在 JSON 中供伙伴继续校对。所有 `learning` 字段为待补充占位。

本地未安装 Unity Editor，因此尚未完成 Unity C# 编译、资源导入和 Play Mode 验证。已检查数据、资源引用、GUID 和资源包结构；合作方仍需在目标 Unity 版本实际导入运行。

## 校验与重新打包

只需要 Python 3 标准库，不依赖本项目之外的截图或 OCR 环境：

```bash
python3 tools/validate_assets.py
python3 tools/export_package.py
```

产物位于 `dist/`：

- `CG2SkillPanel-0.1.0.unitypackage`
- `CG2SkillPanel-0.1.0-Assets.zip`
- `SHA256SUMS`

`dist/` 不重复提交 Git，已发布版本通过 GitHub Releases 分发。
