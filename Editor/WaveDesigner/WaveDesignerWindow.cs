using System.Collections.Generic;
using System.Linq;
using BounceHeroes.Data;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BounceHeroes.EditorTools
{
    /// <summary>
    /// 웨이브의 스폰 스텝 시퀀스를 9x13 격자 위에서 시각적으로 저작하는 에디터 윈도우입니다.
    /// 스텝을 선택하고 격자를 클릭해 몬스터를 배치/제거하며, 스텝은 순서대로 실행됩니다.
    /// </summary>
    public sealed class WaveDesignerWindow : EditorWindow
    {
        private const int Columns = 9;
        private const int Rows = 13;

        // 맨 위 행은 몬스터가 등장(드롭-인)하는 공간이라 스폰 배치를 금지한다.
        // 실제 몬스터는 두 번째 칸(row 1)부터 배치된다.
        private const int TopBanRows = 1;

        private const float CellPixelSize = 34f;
        private const string FolderPath = "Assets/Scripts/Editor/WaveDesigner/";

        // 도메인 리로드/창 재직렬화 시에도 저작 상태가 유지되도록 SerializeField로 보관한다.
        [SerializeField] private WaveTableData _waveTable;
        [SerializeField] private int _waveIndex;
        [SerializeField] private int _stepIndex;
        [SerializeField] private MonsterData _brush;
        [SerializeField] private int _hpOverrideBrush;
        [SerializeField] private int _brushRotation;
        [SerializeField] private bool _accumulatePreview;

        private enum DragMode
        {
            None,
            Place,
            Erase
        }

        private MonsterData[] _monsterPool;
        private DragMode _dragMode = DragMode.None;
        private int _lastDragRow = -1;
        private int _lastDragCol = -1;

        private VisualElement _stepListContainer;
        private VisualElement _paletteContainer;
        private VisualElement _gridContainer;
        private VisualElement _overlayLayer;
        private VisualElement _hoverPreview;
        private readonly List<VisualElement> _cellElements = new List<VisualElement>();
        private IntegerField _waveIndexField;
        private IntegerField _killRequirementField;
        private IntegerField _hpOverrideField;
        private Label _rotationLabel;

        [MenuItem("BounceHeroes/Wave Designer")]
        public static void Open()
        {
            WaveDesignerWindow window = GetWindow<WaveDesignerWindow>();
            window.titleContent = new GUIContent("Wave Designer");
            window.minSize = new Vector2(920, 620);
        }

        private WaveData CurrentWave => _waveTable != null && _waveIndex >= 0 && _waveIndex < _waveTable.WaveCount
            ? _waveTable.GetWave(_waveIndex)
            : null;

        private SpawnStep CurrentStep
        {
            get
            {
                WaveData wave = CurrentWave;

                if (wave?.Steps == null || _stepIndex < 0 || _stepIndex >= wave.Steps.Length)
                    return null;

                return wave.Steps[_stepIndex];
            }
        }

        public void CreateGUI()
        {
            VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(FolderPath + "WaveDesignerWindow.uxml");
            StyleSheet uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(FolderPath + "WaveDesignerWindow.uss");

            uxml.CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(uss);

            _monsterPool = LoadMonsterPool();

            BindToolbar();
            BuildGrid();
            BuildPalette();
            RefreshAll();
        }

        private static MonsterData[] LoadMonsterPool()
        {
            string[] guids = AssetDatabase.FindAssets("t:MonsterData");
            return guids
                .Select(g => AssetDatabase.LoadAssetAtPath<MonsterData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m => m != null)
                .OrderBy(m => m.DisplayName)
                .ToArray();
        }

        private void BindToolbar()
        {
            ObjectField waveTableField = rootVisualElement.Q<ObjectField>("wave-designer__wave-table-field");
            waveTableField.objectType = typeof(WaveTableData);
            waveTableField.tooltip = "이 스테이지의 전체 웨이브(페이즈) 데이터가 담긴 에셋입니다.\n" +
                "이 하나의 에셋 안에 12웨이브가 모두 들어 있습니다.";
            waveTableField.RegisterValueChangedCallback(evt =>
            {
                _waveTable = evt.newValue as WaveTableData;
                _waveIndex = 0;
                _stepIndex = 0;
                RefreshAll();
            });

            _waveIndexField = rootVisualElement.Q<IntegerField>("wave-designer__wave-index-field");
            _waveIndexField.tooltip = "지금 편집할 웨이브(페이즈) 번호입니다. 1부터 시작합니다.\n" +
                "값을 바꾸면 다른 웨이브의 스텝 목록으로 전환되고, 선택된 스텝은 그 웨이브의 1번으로 초기화됩니다.";
            _waveIndexField.RegisterValueChangedCallback(evt =>
            {
                if (_waveTable == null)
                    return;

                _waveIndex = Mathf.Clamp(evt.newValue - 1, 0, _waveTable.WaveCount - 1);
                _stepIndex = 0;
                RefreshAll();
            });

            _killRequirementField = rootVisualElement.Q<IntegerField>("wave-designer__kill-requirement-field");
            _killRequirementField.tooltip = "이 웨이브에서 처치 수가 이 값에 도달하면 3택지 스킬 선택 UI가 뜹니다.\n" +
                "예: 6으로 설정하면 이 웨이브에서 몬스터를 6마리 잡는 순간 선택지가 뜹니다.";
            _killRequirementField.RegisterValueChangedCallback(evt =>
            {
                if (CurrentWave == null)
                    return;

                CurrentWave.KillRequirement = Mathf.Max(0, evt.newValue);
                MarkDirtyAndSave();
            });

            _hpOverrideField = rootVisualElement.Q<IntegerField>("wave-designer__hp-override-field");
            _hpOverrideField.tooltip = "새로 배치하는 몬스터에 강제 적용할 체력입니다.\n" +
                "0이면 몬스터의 기본 체력 × GameBalanceData 배율 값을 그대로 사용합니다.\n" +
                "이미 배치된 몬스터의 체력은 바뀌지 않고, 이후 새로 놓는 것부터 적용됩니다.";
            _hpOverrideField.SetValueWithoutNotify(_hpOverrideBrush);
            _hpOverrideField.RegisterValueChangedCallback(evt => _hpOverrideBrush = Mathf.Max(0, evt.newValue));

            _rotationLabel = rootVisualElement.Q<Label>("wave-designer__rotation-label");
            RefreshRotationLabel();

            Button rotateBrushButton = rootVisualElement.Q<Button>("wave-designer__rotate-brush");
            rotateBrushButton.tooltip = "다음에 배치할 브러시의 회전각을 90도씩 순환합니다. (0° → 90° → 180° → 270°)\n" +
                "이미 배치된 몬스터의 회전은 바뀌지 않고, 이후 새로 놓는 것부터 적용됩니다.";
            rotateBrushButton.clicked += () =>
            {
                _brushRotation = (_brushRotation + 90) % 360;
                RefreshRotationLabel();
            };

            Toggle accumulateToggle = rootVisualElement.Q<Toggle>("wave-designer__accumulate-toggle");
            accumulateToggle.tooltip = "이전 스텝들의 배치를 흐리게(고스트) 겹쳐서 함께 보여줍니다.\n" +
                "스텝끼리 위치가 겹치지 않는지 확인할 때 켜두면 편합니다.";
            accumulateToggle.SetValueWithoutNotify(_accumulatePreview);
            accumulateToggle.RegisterValueChangedCallback(evt =>
            {
                _accumulatePreview = evt.newValue;
                RefreshGridDisplay();
            });

            waveTableField.SetValueWithoutNotify(_waveTable);

            VisualElement toolbar = rootVisualElement.Q<VisualElement>("wave-designer__toolbar");

            Button newTableButton = new Button(CreateNewWaveTable) { text = "New Table" };
            newTableButton.tooltip = "새 Wave Table 에셋을 생성하고(빈 웨이브 1개 포함) 즉시 편집 대상으로 연결합니다.";
            toolbar.Add(newTableButton);

            Button addWaveButton = new Button(AddWave) { text = "+ Wave" };
            addWaveButton.tooltip = "이 테이블에 빈 웨이브(페이즈)를 추가하고 그 웨이브로 전환합니다.";
            toolbar.Add(addWaveButton);

            Button removeWaveButton = new Button(RemoveWave) { text = "- Wave" };
            removeWaveButton.tooltip = "현재 편집 중인 웨이브를 삭제합니다.";
            toolbar.Add(removeWaveButton);

            Button addStepButton = rootVisualElement.Q<Button>("wave-designer__add-step");
            addStepButton.tooltip = "빈 스텝을 맨 뒤에 추가하고 그 스텝으로 전환합니다.";
            addStepButton.clicked += AddStep;

            Button duplicateStepButton = rootVisualElement.Q<Button>("wave-designer__duplicate-step");
            duplicateStepButton.tooltip = "현재 스텝의 배치를 그대로 복사한 새 스텝을 바로 뒤에 추가합니다.";
            duplicateStepButton.clicked += DuplicateStep;

            Button removeStepButton = rootVisualElement.Q<Button>("wave-designer__remove-step");
            removeStepButton.tooltip = "현재 선택된 스텝을 삭제합니다.";
            removeStepButton.clicked += RemoveStep;

            Button moveUpButton = rootVisualElement.Q<Button>("wave-designer__move-up");
            moveUpButton.tooltip = "현재 스텝을 바로 이전 순서와 맞바꿉니다. (스폰 순서가 당겨짐)";
            moveUpButton.clicked += () => MoveStep(-1);

            Button moveDownButton = rootVisualElement.Q<Button>("wave-designer__move-down");
            moveDownButton.tooltip = "현재 스텝을 바로 다음 순서와 맞바꿉니다. (스폰 순서가 밀림)";
            moveDownButton.clicked += () => MoveStep(1);
        }

        private void BuildGrid()
        {
            _gridContainer = rootVisualElement.Q<VisualElement>("wave-designer__grid");
            _gridContainer.Clear();
            _cellElements.Clear();
            _gridContainer.tooltip = "빈 칸을 누른 채 끌면 계속 배치되고, 배치된 칸을 누른 채 끌면 계속 지워집니다.";

            for (int row = 0; row < Rows; row++)
            {
                VisualElement rowElement = new VisualElement();
                rowElement.AddToClassList("wave-designer__grid-row");

                for (int col = 0; col < Columns; col++)
                {
                    VisualElement cell = new VisualElement();
                    cell.AddToClassList("wave-designer__cell");

                    if (row < TopBanRows)
                    {
                        // 스폰 금지 행: 시각적으로 어둡게 잠근다.
                        cell.style.backgroundColor = new Color(0.08f, 0.08f, 0.10f, 0.9f);
                        cell.tooltip = "스폰 금지 행 — 몬스터 등장(드롭-인) 공간입니다.";
                    }

                    rowElement.Add(cell);
                    _cellElements.Add(cell);
                }

                _gridContainer.Add(rowElement);
            }

            // 배치된 몬스터 아이콘은 칸 하나에 눌러 넣지 않고, footprint 크기만큼 실제로 늘어난
            // 별도 오버레이 레이어 위에 절대좌표로 그린다. 1x1부터 임의의 NxM까지 동일하게 동작한다.
            _overlayLayer = new VisualElement();
            _overlayLayer.AddToClassList("wave-designer__overlay-layer");
            _overlayLayer.pickingMode = PickingMode.Ignore;
            _gridContainer.Add(_overlayLayer);

            _hoverPreview = new VisualElement();
            _hoverPreview.AddToClassList("wave-designer__hover-preview");
            _hoverPreview.pickingMode = PickingMode.Ignore;
            _hoverPreview.style.display = DisplayStyle.None;
            _gridContainer.Add(_hoverPreview);

            // 클릭 한 번은 물론, 마우스를 누른 채로 끄는 드래그로도 연속 배치/제거가 되도록
            // 클릭 판정을 셀 단위가 아니라 그리드 전체에서 포인터 좌표로 직접 계산한다.
            _gridContainer.RegisterCallback<PointerDownEvent>(OnGridPointerDown);
            _gridContainer.RegisterCallback<PointerMoveEvent>(OnGridPointerMove);
            _gridContainer.RegisterCallback<PointerUpEvent>(OnGridPointerUp);
            _gridContainer.RegisterCallback<PointerLeaveEvent>(_ => HideHoverPreview());
        }

        private void OnGridPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || !TryGetCellFromLocal(evt.localPosition, out int row, out int col))
                return;

            SpawnStep step = CurrentStep;

            if (step == null)
                return;

            // 처음 누른 칸이 비어 있으면 드래그 내내 "배치", 이미 배치되어 있으면 드래그 내내 "제거"로 고정한다.
            // 칸마다 상태가 달라 배치/제거가 오락가락하지 않도록 방지한다.
            bool occupied = FindPlacementIndex(step, row, col) >= 0;

            if (!occupied && _brush == null)
            {
                Debug.LogWarning("팔레트에서 몬스터를 먼저 선택하세요.");
                return;
            }

            _dragMode = occupied ? DragMode.Erase : DragMode.Place;
            _lastDragRow = -1;
            _lastDragCol = -1;

            _gridContainer.CapturePointer(evt.pointerId);
            ApplyDrag(row, col);
        }

        private void OnGridPointerMove(PointerMoveEvent evt)
        {
            bool inGrid = TryGetCellFromLocal(evt.localPosition, out int row, out int col);

            if (_dragMode != DragMode.None)
            {
                if (inGrid)
                    ApplyDrag(row, col);

                HideHoverPreview();
                return;
            }

            if (_brush == null || !inGrid)
            {
                HideHoverPreview();
                return;
            }

            PlacedMonster.GetRotatedFootprint(_brush, _brushRotation, out int width, out int height);
            bool inBounds = row >= TopBanRows && row + height <= Rows && col + width <= Columns;

            PositionOverlay(_hoverPreview, row, col, width, height);
            _hoverPreview.EnableInClassList("wave-designer__hover-preview--invalid", !inBounds);
            _hoverPreview.style.display = DisplayStyle.Flex;
        }

        private void OnGridPointerUp(PointerUpEvent evt)
        {
            if (_dragMode == DragMode.None)
                return;

            _gridContainer.ReleasePointer(evt.pointerId);
            _dragMode = DragMode.None;
            _lastDragRow = -1;
            _lastDragCol = -1;
        }

        private void ApplyDrag(int row, int col)
        {
            if (row == _lastDragRow && col == _lastDragCol)
                return;

            _lastDragRow = row;
            _lastDragCol = col;

            if (_dragMode == DragMode.Place)
                PlaceAt(row, col);
            else if (_dragMode == DragMode.Erase)
                EraseAt(row, col);
        }

        private static bool TryGetCellFromLocal(Vector2 local, out int row, out int col)
        {
            col = Mathf.FloorToInt(local.x / CellPixelSize);
            row = Mathf.FloorToInt(local.y / CellPixelSize);
            return row >= 0 && row < Rows && col >= 0 && col < Columns;
        }

        private void HideHoverPreview()
        {
            _hoverPreview.style.display = DisplayStyle.None;
        }

        private void RefreshRotationLabel()
        {
            if (_rotationLabel != null)
                _rotationLabel.text = _brushRotation + "°";
        }

        /// <summary>
        /// 격자 요소를 footprint(가로x세로) 크기에 맞춰 지정한 앵커 칸 위치로 절대배치합니다.
        /// 1x1부터 임의의 NxM 몬스터까지 동일한 계산식으로 처리되도록 크기 확장에 대비합니다.
        /// </summary>
        /// <param name="element">배치할 오버레이 요소</param>
        /// <param name="row">앵커(좌상단) 행</param>
        /// <param name="col">앵커(좌상단) 열</param>
        /// <param name="width">가로 칸 수</param>
        /// <param name="height">세로 칸 수</param>
        private static void PositionOverlay(VisualElement element, int row, int col, int width, int height)
        {
            element.style.left = col * CellPixelSize;
            element.style.top = row * CellPixelSize;
            element.style.width = width * CellPixelSize;
            element.style.height = height * CellPixelSize;
        }

        private void BuildPalette()
        {
            _paletteContainer = rootVisualElement.Q<VisualElement>("wave-designer__palette");
            _paletteContainer.Clear();

            foreach (MonsterData monster in _monsterPool)
            {
                VisualElement item = new VisualElement();
                item.AddToClassList("wave-designer__palette-item");
                item.tooltip = $"{monster.DisplayName}\n" +
                    $"기본 체력 {monster.BaseHp} (실제 스폰 체력 = 이 값 × 현재 GameBalanceData의 배율)\n" +
                    $"공격력 {monster.AttackDamage} · Footprint {monster.FootprintWidth}x{monster.FootprintHeight}\n" +
                    "클릭해서 브러시로 선택하세요.";

                VisualElement icon = new VisualElement();
                icon.AddToClassList("wave-designer__palette-icon");

                if (monster.Sprite != null)
                    icon.style.backgroundImage = new StyleBackground(monster.Sprite);

                VisualElement info = new VisualElement();
                info.AddToClassList("wave-designer__palette-info");

                Label nameLabel = new Label(monster.DisplayName);
                nameLabel.AddToClassList("wave-designer__palette-name");

                // 기본 HP를 몰라서 오버라이드 값을 얼마로 줘야 할지 알 수 없다는 문제 해결: 항상 함께 보여준다.
                Label hpLabel = new Label($"기본 체력 {monster.BaseHp}");
                hpLabel.AddToClassList("wave-designer__palette-hp");

                info.Add(nameLabel);
                info.Add(hpLabel);

                item.Add(icon);
                item.Add(info);
                item.RegisterCallback<ClickEvent>(_ =>
                {
                    _brush = monster;
                    RefreshPaletteSelection();
                });

                _paletteContainer.Add(item);
            }

            RefreshPaletteSelection();
        }

        private void RefreshPaletteSelection()
        {
            for (int i = 0; i < _paletteContainer.childCount; i++)
            {
                VisualElement item = _paletteContainer[i];
                item.EnableInClassList("wave-designer__palette-item--selected", _monsterPool[i] == _brush);
            }
        }

        /// <summary>
        /// 지정한 칸에 현재 브러시로 몬스터를 배치합니다. 이미 그 칸을 덮는 배치가 있으면 아무 일도 하지 않습니다.
        /// </summary>
        /// <param name="row">배치할 앵커 행</param>
        /// <param name="col">배치할 앵커 열</param>
        private void PlaceAt(int row, int col)
        {
            SpawnStep step = CurrentStep;

            if (step == null || _brush == null)
                return;

            // 스폰 금지 행에는 배치하지 않는다.
            if (row < TopBanRows)
                return;

            if (FindPlacementIndex(step, row, col) >= 0)
                return;

            List<PlacedMonster> placements = new List<PlacedMonster>(step.Placements ?? new PlacedMonster[0]);
            placements.Add(new PlacedMonster
            {
                Monster = _brush,
                Row = row,
                Col = col,
                HpOverride = _hpOverrideBrush,
                Rotation = _brushRotation
            });

            step.Placements = placements.ToArray();
            MarkDirtyAndSave();
            RefreshGridDisplay();
        }

        /// <summary>
        /// 지정한 칸을 덮는 배치를 제거합니다. footprint가 여러 칸이면 그중 어느 칸을 지정해도 제거됩니다.
        /// </summary>
        /// <param name="row">지울 대상이 걸쳐 있는 행</param>
        /// <param name="col">지울 대상이 걸쳐 있는 열</param>
        private void EraseAt(int row, int col)
        {
            SpawnStep step = CurrentStep;

            if (step == null)
                return;

            int index = FindPlacementIndex(step, row, col);

            if (index < 0)
                return;

            List<PlacedMonster> placements = new List<PlacedMonster>(step.Placements);
            placements.RemoveAt(index);

            step.Placements = placements.ToArray();
            MarkDirtyAndSave();
            RefreshGridDisplay();
        }

        private static int FindPlacementIndex(SpawnStep step, int row, int col)
        {
            if (step.Placements == null)
                return -1;

            for (int i = 0; i < step.Placements.Length; i++)
            {
                if (IsCellCoveredByPlacement(step.Placements[i], row, col))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// 새 <see cref="WaveTableData"/> 에셋을 생성해 즉시 편집 대상으로 연결합니다.
        /// 갓 만든 에셋은 waves가 비어(null) 있어 스텝을 못 넣으므로, 빈 웨이브 1개를 미리 넣어 준다.
        /// </summary>
        private void CreateNewWaveTable()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "New Wave Table", "SO_WaveTable", "asset", "생성할 웨이브 테이블 에셋의 저장 위치를 선택하세요.");

            if (string.IsNullOrEmpty(path))
                return;

            WaveTableData table = CreateInstance<WaveTableData>();
            AppendEmptyWave(table);

            AssetDatabase.CreateAsset(table, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _waveTable = table;
            _waveIndex = 0;
            _stepIndex = 0;

            rootVisualElement.Q<ObjectField>("wave-designer__wave-table-field")?.SetValueWithoutNotify(table);
            RefreshAll();
        }

        /// <summary>현재 테이블에 빈 웨이브를 추가하고 그 웨이브로 전환합니다.</summary>
        private void AddWave()
        {
            if (_waveTable == null)
                return;

            AppendEmptyWave(_waveTable);

            _waveIndex = _waveTable.WaveCount - 1;
            _stepIndex = 0;
            MarkDirtyAndSave();
            RefreshAll();
        }

        /// <summary>현재 편집 중인 웨이브를 삭제합니다.</summary>
        private void RemoveWave()
        {
            if (_waveTable == null || _waveTable.WaveCount == 0)
                return;

            SerializedObject so = new SerializedObject(_waveTable);
            SerializedProperty wavesProp = so.FindProperty("waves");

            if (_waveIndex < 0 || _waveIndex >= wavesProp.arraySize)
                return;

            wavesProp.DeleteArrayElementAtIndex(_waveIndex);
            so.ApplyModifiedProperties();

            _waveIndex = Mathf.Clamp(_waveIndex, 0, Mathf.Max(0, _waveTable.WaveCount - 1));
            _stepIndex = 0;
            MarkDirtyAndSave();
            RefreshAll();
        }

        /// <summary>
        /// 대상 테이블의 private <c>waves</c> 배열에 빈 웨이브(Steps 없음, KillRequirement=3) 하나를 추가합니다.
        /// <see cref="WaveTableData"/>는 런타임 데이터라 waves를 조작할 공개 API가 없으므로 SerializedObject로 다룬다.
        /// </summary>
        private static void AppendEmptyWave(WaveTableData table)
        {
            SerializedObject so = new SerializedObject(table);
            SerializedProperty wavesProp = so.FindProperty("waves");

            int newIndex = wavesProp.arraySize;
            wavesProp.arraySize = newIndex + 1;

            SerializedProperty wave = wavesProp.GetArrayElementAtIndex(newIndex);
            wave.FindPropertyRelative("Steps").arraySize = 0;
            wave.FindPropertyRelative("KillRequirement").intValue = 3;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void AddStep()
        {
            if (_waveTable == null)
                return;

            // 갓 만든/비어 있는 테이블이면 웨이브부터 만들어 스텝을 넣을 수 있게 한다.
            if (_waveTable.WaveCount == 0)
            {
                AppendEmptyWave(_waveTable);
                _waveIndex = 0;
            }

            WaveData wave = CurrentWave;

            if (wave == null)
                return;

            List<SpawnStep> steps = new List<SpawnStep>(wave.Steps ?? new SpawnStep[0]);
            steps.Add(new SpawnStep { Placements = new PlacedMonster[0] });
            wave.Steps = steps.ToArray();
            _stepIndex = steps.Count - 1;

            MarkDirtyAndSave();
            RefreshStepList();
            RefreshGridDisplay();
        }

        private void DuplicateStep()
        {
            WaveData wave = CurrentWave;
            SpawnStep step = CurrentStep;

            if (wave == null || step == null)
                return;

            List<SpawnStep> steps = new List<SpawnStep>(wave.Steps);
            PlacedMonster[] clonedPlacements = step.Placements
                .Select(p => new PlacedMonster { Monster = p.Monster, Row = p.Row, Col = p.Col, HpOverride = p.HpOverride, Rotation = p.Rotation })
                .ToArray();

            steps.Insert(_stepIndex + 1, new SpawnStep { Placements = clonedPlacements });
            wave.Steps = steps.ToArray();
            _stepIndex++;

            MarkDirtyAndSave();
            RefreshStepList();
            RefreshGridDisplay();
        }

        private void RemoveStep()
        {
            WaveData wave = CurrentWave;

            if (wave?.Steps == null || wave.Steps.Length == 0)
                return;

            List<SpawnStep> steps = new List<SpawnStep>(wave.Steps);
            steps.RemoveAt(_stepIndex);
            wave.Steps = steps.ToArray();
            _stepIndex = Mathf.Clamp(_stepIndex, 0, steps.Count - 1);

            MarkDirtyAndSave();
            RefreshStepList();
            RefreshGridDisplay();
        }

        private void MoveStep(int direction)
        {
            WaveData wave = CurrentWave;

            if (wave?.Steps == null)
                return;

            int targetIndex = _stepIndex + direction;

            if (targetIndex < 0 || targetIndex >= wave.Steps.Length)
                return;

            (wave.Steps[_stepIndex], wave.Steps[targetIndex]) = (wave.Steps[targetIndex], wave.Steps[_stepIndex]);
            _stepIndex = targetIndex;

            MarkDirtyAndSave();
            RefreshStepList();
            RefreshGridDisplay();
        }

        private void RefreshAll()
        {
            bool hasWave = CurrentWave != null;

            _waveIndexField.SetValueWithoutNotify(_waveTable != null ? _waveIndex + 1 : 1);
            _killRequirementField.SetValueWithoutNotify(hasWave ? CurrentWave.KillRequirement : 0);

            RefreshStepList();
            RefreshGridDisplay();
        }

        private void RefreshStepList()
        {
            _stepListContainer = rootVisualElement.Q<ScrollView>("wave-designer__step-list");
            _stepListContainer.Clear();

            WaveData wave = CurrentWave;

            if (wave?.Steps == null)
                return;

            for (int i = 0; i < wave.Steps.Length; i++)
            {
                int capturedIndex = i;
                SpawnStep step = wave.Steps[i];
                int count = step.Placements?.Length ?? 0;

                Label item = new Label($"Step {i + 1}  ({count}마리)");
                item.AddToClassList("wave-designer__step-item");
                item.EnableInClassList("wave-designer__step-item--selected", i == _stepIndex);
                item.tooltip = i == 0
                    ? "이 스텝은 웨이브 시작과 동시에 스폰됩니다."
                    : $"Step {i}의 몬스터 전원이 기준 행을 통과하거나 죽어야 이 스텝이 스폰됩니다.";

                item.RegisterCallback<ClickEvent>(_ =>
                {
                    _stepIndex = capturedIndex;
                    RefreshStepList();
                    RefreshGridDisplay();
                });

                _stepListContainer.Add(item);
            }
        }

        private void RefreshGridDisplay()
        {
            foreach (VisualElement cell in _cellElements)
            {
                cell.RemoveFromClassList("wave-designer__cell--filled");
            }

            _overlayLayer.Clear();

            WaveData wave = CurrentWave;

            if (wave?.Steps == null)
                return;

            if (_accumulatePreview)
            {
                for (int i = 0; i < _stepIndex; i++)
                    PaintStep(wave.Steps[i], ghost: true);
            }

            SpawnStep current = CurrentStep;

            if (current != null)
                PaintStep(current, ghost: false);
        }

        private void PaintStep(SpawnStep step, bool ghost)
        {
            if (step?.Placements == null)
                return;

            foreach (PlacedMonster placement in step.Placements)
            {
                if (placement.Monster == null)
                    continue;

                PlacedMonster.GetRotatedFootprint(placement.Monster, placement.Rotation, out int width, out int height);

                for (int dr = 0; dr < height; dr++)
                {
                    int row = placement.Row + dr;

                    if (row < 0 || row >= Rows)
                        continue;

                    for (int dc = 0; dc < width; dc++)
                    {
                        int col = placement.Col + dc;

                        if (col < 0 || col >= Columns)
                            continue;

                        _cellElements[row * Columns + col].AddToClassList("wave-designer__cell--filled");
                    }
                }

                _overlayLayer.Add(CreatePlacementIcon(placement, ghost));
            }
        }

        /// <summary>
        /// 배치 하나에 대한 아이콘 오버레이를 footprint 크기 그대로 생성합니다.
        /// 회전이 있으면 원본 비율의 박스를 중심 고정으로 돌려 실제 점유 영역과 맞춥니다.
        /// </summary>
        /// <param name="placement">그릴 배치</param>
        /// <param name="ghost">누적 미리보기(이전 스텝) 여부</param>
        /// <returns>절대배치된 아이콘 요소</returns>
        private VisualElement CreatePlacementIcon(PlacedMonster placement, bool ghost)
        {
            int baseWidth = Mathf.Max(1, placement.Monster.FootprintWidth);
            int baseHeight = Mathf.Max(1, placement.Monster.FootprintHeight);

            VisualElement icon = new VisualElement();
            icon.AddToClassList("wave-designer__placement-icon");
            icon.pickingMode = PickingMode.Ignore;
            icon.EnableInClassList("wave-designer__cell--ghost", ghost);

            if (placement.Monster.Sprite != null)
                icon.style.backgroundImage = new StyleBackground(placement.Monster.Sprite);

            PositionRotatedIcon(icon, placement.Row, placement.Col, baseWidth, baseHeight, placement.Rotation);

            return icon;
        }

        /// <summary>
        /// 아이콘을 원본(회전 전) 크기로 두고 회전각만큼 돌려서, 회전 후 실제로 점유하는
        /// 칸 영역의 중심에 맞춰 배치합니다. 90/270도에서 가로세로가 뒤바뀌어도 스프라이트가
        /// 찌그러지지 않고 그 자리에서 회전한 것처럼 보이게 하기 위함입니다.
        /// </summary>
        private static void PositionRotatedIcon(VisualElement element, int row, int col, int baseWidth, int baseHeight, int rotationDegrees)
        {
            PlacedMonster.GetRotatedFootprint(baseWidth, baseHeight, rotationDegrees, out int occupiedWidth, out int occupiedHeight);

            float centerX = (col + occupiedWidth * 0.5f) * CellPixelSize;
            float centerY = (row + occupiedHeight * 0.5f) * CellPixelSize;
            float iconWidth = baseWidth * CellPixelSize;
            float iconHeight = baseHeight * CellPixelSize;

            element.style.left = centerX - iconWidth * 0.5f;
            element.style.top = centerY - iconHeight * 0.5f;
            element.style.width = iconWidth;
            element.style.height = iconHeight;

            if (rotationDegrees != 0)
                element.style.rotate = new Rotate(new Angle(rotationDegrees, AngleUnit.Degree));
        }

        /// <summary>
        /// 지정한 격자 칸이 배치의 footprint(가로x세로)에 포함되는지 확인합니다.
        /// </summary>
        /// <param name="placement">확인할 배치</param>
        /// <param name="row">확인할 행</param>
        /// <param name="col">확인할 열</param>
        /// <returns>배치의 footprint에 포함되면 true</returns>
        private static bool IsCellCoveredByPlacement(PlacedMonster placement, int row, int col)
        {
            if (placement.Monster == null)
                return placement.Row == row && placement.Col == col;

            PlacedMonster.GetRotatedFootprint(placement.Monster, placement.Rotation, out int width, out int height);

            return row >= placement.Row && row < placement.Row + height
                && col >= placement.Col && col < placement.Col + width;
        }

        private void MarkDirtyAndSave()
        {
            if (_waveTable == null)
                return;

            EditorUtility.SetDirty(_waveTable);
            AssetDatabase.SaveAssets();
        }
    }
}
