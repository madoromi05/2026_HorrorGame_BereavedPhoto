﻿/// <summary>
/// �_���W���������̑S�̃t���[�𓝊�����N���X
/// �ePlacer��Builder��g�ݗ��ď��ɌĂяo��
/// </summary>
using DungeonSystem;
using UnityEngine;
using static UnityEngine.Rendering.CoreUtils;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private FieldBluePrint _bluePrint;
    [SerializeField] private RoomDataBase _roomDataBase;
    [SerializeField] private CorridorDataBase _corridorDataBase;

    // �����E�ʘH��Hierarchy��ŕ����ĊǗ����邽�߂̐e�I�u�W�F�N�g
    [SerializeField] private Transform _roomParent;
    [SerializeField] private Transform _corridorParent;

    [Header("Debug")]
    [SerializeField] private bool _isDebugMode;
    [SerializeField] private Transform _debugParent;

    private DungeonGridBuilder _gridBuilder;
    private DungeonDebugVisualizer _debugVisualizer;
    private SectionPlacer _sectionPlacer;
    private CorridorPlacer _corridorPlacer;

    private void Start()
    {
        Generate();
    }

    /// <summary>
    /// �_���W�����𐶐�����
    /// �O���b�h�\�z �� �����z�u �� �ʘH�z�u�̏��Ɏ��s����
    /// </summary>
    public void Generate()
    {
        Debug.Log("[Generator] Generate() called");
        Initialize();

        var (grid, sections) = _gridBuilder.Build(_bluePrint, _roomDataBase);
        Debug.Log($"[Generator] sections[0].RoomGridPosition={sections[0].RoomGridPosition}");  // 追加

        _sectionPlacer.Place(sections, _roomParent);
        _corridorPlacer.Place(grid, _corridorParent);

        if (_isDebugMode && _debugParent != null)
            _debugVisualizer.Visualize(grid, sections, _debugParent);
    }

    private void Initialize()
    {
        _gridBuilder = new DungeonGridBuilder();
        _sectionPlacer = new SectionPlacer(_roomDataBase, _bluePrint.OneGridSize);
        _corridorPlacer = new CorridorPlacer(_corridorDataBase, _bluePrint.OneGridSize);
        _debugVisualizer = new DungeonDebugVisualizer(_bluePrint.OneGridSize);
    }
}