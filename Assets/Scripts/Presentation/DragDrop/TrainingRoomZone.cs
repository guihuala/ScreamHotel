using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Domain;

public class TrainingRoomZone : MonoBehaviour, IDropZone
{
    [Header("Visual")]
    public MeshRenderer plate;
    public Color canColor  = new Color(0.3f, 1f, 0.3f, 1f);
    public Color fullColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("Slots")]
    [Tooltip("五个训练槽位锚点（世界空间或相对本物体）")]
    public Transform[] slotAnchors = new Transform[5];

    [Header("VFX/Prefabs")]
    public ParticleSystem trainingVfxPrefab;
    public GameObject remainTextPrefab;

    private Game _game;
    private Color _origColor;
    private readonly List<string> _slotGhostIds = new(); // 长度<=5，对应 slotAnchors
    private readonly Dictionary<string, (ParticleSystem vfx, TextMesh remain)> _slotVfx = new();

    void Awake()
    {
        _game = FindObjectOfType<Game>();
        if (plate) _origColor = plate.material.color;

        // 统一长度/初始化
        if (slotAnchors == null || slotAnchors.Length != 5)
        {
            var arr = new Transform[5];
            for (int i = 0; i < 5; i++)
            {
                arr[i] = (slotAnchors != null && i < slotAnchors.Length && slotAnchors[i] != null)
                         ? slotAnchors[i]
                         : CreateAnchor(i);
            }
            slotAnchors = arr;
        }
        _slotGhostIds.Clear();
        _slotGhostIds.AddRange(Enumerable.Repeat<string>(null, 5));
    }

    private Transform CreateAnchor(int i)
    {
        var t = new GameObject($"TrainSlot{i}").transform;
        t.SetParent(transform, false);
        t.localPosition = new Vector3(-0.8f + i * 0.4f, 0.55f, 0);
        return t;
    }

    // ==== 拖拽反馈（与 RoomDropZone 接口风格一致） ====
    public void ShowHoverFeedback(string ghostId)
    {
        if (plate == null) return;
        var c = CanAccept(ghostId, true) ? canColor : fullColor;
        plate.material.color = Color.Lerp(plate.material.color, c, 0.5f);
    }

    public void ClearFeedback()
    {
        if (plate == null) return;
        plate.material.color = _origColor;
    }

    public bool TryDrop(string ghostId, bool isGhost, out Transform targetAnchor)
    {
        targetAnchor = null;
        if (!CanAccept(ghostId, isGhost))
        {
            Flash(fullColor);
            return false;
        }

        int slot = FirstEmptySlot();
        if (slot < 0) { Flash(fullColor); return false; }

        // 锁定：占位 + 返回锚点给表现层
        _slotGhostIds[slot] = ghostId;
        targetAnchor = slotAnchors[slot];

        // 弹出“选择恐惧属性”的小面板
        OpenPickFearUI(ghostId, slot);

        Flash(canColor);
        return true;
    }

    public bool CanAccept(string ghostId, bool isGhost)
    {
        if (_game == null || string.IsNullOrEmpty(ghostId)) return false;

        var g = _game.World.Ghosts.FirstOrDefault(x => x.Id == ghostId);
        if (g == null) return false;

        // 训练中不可重复放置；槽满不可放
        if (g.State == GhostState.Training) return false;
        if (FirstEmptySlot() < 0) return false;

        return true;
    }

    private int FirstEmptySlot() => _slotGhostIds.FindIndex(id => string.IsNullOrEmpty(id));

    private void Flash(Color c)
    {
        if (!plate) return;
        plate.material.color = c;
        CancelInvoke(nameof(Revert)); Invoke(nameof(Revert), 0.25f);
    }
    private void Revert() { if (plate) plate.material.color = _origColor; }

    // === 选择恐惧属性 UI ===
    private void OpenPickFearUI(string ghostId, int slotIndex)
    {
        var ui = new GameObject("PickFearUI").AddComponent<PickFearUI>();
        ui.transform.SetParent(transform, false);
        ui.Init((FearTag picked) =>
        {
            StartTraining(ghostId, picked, slotIndex);
        });
    }

    // === 启动训练 ===
    private void StartTraining(string ghostId, FearTag tag, int slotIndex)
    {
        var g = _game.World.Ghosts.FirstOrDefault(x => x.Id == ghostId);
        if (g == null) return;

        // 将目标训练属性临时记在 ghost 的一个扩展字段（或你也可以在 GhostTrainer 里用字典管理）
        g.State = GhostState.Training;
        g.TrainingDays = 0;
        g.Sub = tag; // 训练完成后获得/覆盖 Sub，也可先缓存，完成时再赋值

        // 视觉：开训练环/粒子 + 文本
        if (trainingVfxPrefab && slotAnchors[slotIndex])
        {
            var v = Instantiate(trainingVfxPrefab, slotAnchors[slotIndex].position, Quaternion.identity, slotAnchors[slotIndex]);
            v.Play();
            TextMesh t = null;
            if (remainTextPrefab)
            {
                var go = Instantiate(remainTextPrefab, slotAnchors[slotIndex]);
                go.transform.localPosition = new Vector3(0, 0.65f, 0);
                t = go.GetComponent<TextMesh>();
            }
            _slotVfx[ghostId] = (v, t);
            UpdateRemainDaysUI(ghostId); // 初次刷新
        }

        // 通知 GhostTrainer
        var trainer = FindObjectOfType<GhostTrainer>();
        if (trainer) trainer.StartTraining(g); // 内部会置 Training 状态并清天数
    }

    public void OnTrainingDayAdvanced() // 供 GhostTrainer 每天调用刷新一次 UI
    {
        foreach (var id in _slotGhostIds.Where(s => !string.IsNullOrEmpty(s)))
            UpdateRemainDaysUI(id);
    }
    
    public void ShowHoverFeedback(string id, bool isGhost)
    {
        ShowHoverFeedback(id);
    }

    public void OnTrainingComplete(Ghost ghost) // 供 GhostTrainer 回调
    {
        // 清 FX/文字 + 释放槽位
        if (_slotVfx.TryGetValue(ghost.Id, out var pack))
        {
            if (pack.vfx) Destroy(pack.vfx.gameObject);
            if (pack.remain) Destroy(pack.remain.gameObject);
        }
        _slotVfx.Remove(ghost.Id);

        int idx = _slotGhostIds.FindIndex(x => x == ghost.Id);
        if (idx >= 0) _slotGhostIds[idx] = null;

        // 闪一下表示完成
        Flash(canColor);
    }

    private void UpdateRemainDaysUI(string ghostId)
    {
        if (!_slotVfx.TryGetValue(ghostId, out var pack)) return;
        var g = _game.World.Ghosts.FirstOrDefault(x => x.Id == ghostId);
        if (g == null) return;

        int need = _game.World.Config.Rules.ghostTrainingTimeDays;
        int remain = Mathf.Max(0, need - g.TrainingDays);
        if (pack.remain) pack.remain.text = $"剩余 {remain} 天";
    }
}
