using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IdleTafang.Gameplay.Combat
{
    /// <summary>
    /// 刷怪点布局模式。
    /// <para><b>圆环（annulus / full ring）</b>：围绕基地一整圈 360° 都可刷怪；若只关心「同一半径上一圈点」，即整条圆环。</para>
    /// <para><b>圆弧（arc）</b>：只用圆周上的一段角度区间；本项目的「按扇区分组」即为每个扇区一条圆弧，互不重叠，与战斗扇区一致。</para>
    /// </summary>
    public enum CombatSpawnLayoutMode
    {
        /// <summary>在子物体上手动放置 <see cref="CombatSpawnPoint"/>（忽略 GeneratedSpawns）。</summary>
        ManualChildren,

        /// <summary>运行时（及 Editor 下改参时）在 GeneratedSpawns 下按扇区圆弧自动生成点。</summary>
        ProceduralSectorArcs
    }

    [DefaultExecutionOrder(50)]
    public sealed class CombatPath : MonoBehaviour
    {
        private const string GeneratedRootName = "GeneratedSpawns";

        [Header("Layout")]
        [SerializeField] private CombatSpawnLayoutMode layoutMode = CombatSpawnLayoutMode.ManualChildren;
        [SerializeField] private CombatArena arena;

        [Header("Procedural (sector arcs)")]
        [Tooltip("须与战斗扇区数量一致（默认 3）。")]
        [SerializeField] private int sectorCount = 3;

        [Tooltip("每个扇区内均匀分布几个刷怪槽位（ cycle 轮流使用）。")]
        [SerializeField] private int spawnsPerSector = 3;

        [Tooltip("相对 CombatArena.CenterPoint 的水平距离（XZ），对齐你在相机外摆放的大致半径。")]
        [SerializeField] private float spawnRadius = 22f;

        [SerializeField] private float spawnHeightOffset;

        [Tooltip("每条扇区圆弧两端向内收缩的角度（度），避免点压在扇区交界线上。")]
        [SerializeField] private float sectorAngleInsetDegrees = 4f;

        [SerializeField] private CombatSpawnPoint[] spawnPoints = Array.Empty<CombatSpawnPoint>();

#if UNITY_EDITOR
        [NonSerialized] private bool editorDeferredRegeneratePending;
#endif

        public CombatSpawnPoint[] SpawnPoints => spawnPoints;

        public CombatPathLogic Logic
        {
            get
            {
                if (spawnPoints == null || spawnPoints.Length == 0)
                {
                    return new CombatPathLogic(null);
                }

                CombatPoint[] points = new CombatPoint[spawnPoints.Length];
                for (int i = 0; i < spawnPoints.Length; i++)
                {
                    points[i] = spawnPoints[i].LogicPosition;
                }

                return new CombatPathLogic(points);
            }
        }

        private void OnValidate()
        {
            sectorCount = Mathf.Max(1, sectorCount);
            spawnsPerSector = Mathf.Max(1, spawnsPerSector);
            spawnRadius = Mathf.Max(0.5f, spawnRadius);
            sectorAngleInsetDegrees = Mathf.Max(0f, sectorAngleInsetDegrees);

#if UNITY_EDITOR
            // OnValidate 内同步改层级常与序列化打架，删不干净会一直叠 GeneratedSpawns；推迟到下一帧 Editor 更新。
            if (!Application.isPlaying && layoutMode == CombatSpawnLayoutMode.ProceduralSectorArcs)
            {
                QueueEditorDeferredRegenerateSpawns();
                return;
            }
#endif
            RebuildSpawnPoints();
        }

#if UNITY_EDITOR
        private void OnDestroy()
        {
            if (Application.isPlaying || !editorDeferredRegeneratePending)
            {
                return;
            }

            EditorApplication.delayCall -= EditorDeferredRegenerateSpawns;
            editorDeferredRegeneratePending = false;
        }

        private void QueueEditorDeferredRegenerateSpawns()
        {
            if (editorDeferredRegeneratePending)
            {
                return;
            }

            editorDeferredRegeneratePending = true;
            EditorApplication.delayCall += EditorDeferredRegenerateSpawns;
        }

        private void EditorDeferredRegenerateSpawns()
        {
            EditorApplication.delayCall -= EditorDeferredRegenerateSpawns;
            editorDeferredRegeneratePending = false;

            if (this == null)
            {
                return;
            }

            if (Application.isPlaying || layoutMode != CombatSpawnLayoutMode.ProceduralSectorArcs)
            {
                return;
            }

            RegenerateProceduralSpawnTransforms();
            RebuildSpawnPoints();
        }
#endif

        private void Awake()
        {
            if (layoutMode == CombatSpawnLayoutMode.ProceduralSectorArcs)
            {
                RegenerateProceduralSpawnTransforms();
            }

            RebuildSpawnPoints();
        }

        /// <summary>F1：与 RunConfig 对齐扇区数量（仅写字段；程序化布局在 Awake 中再生）。须早于本组件 Awake 调用。</summary>
        public void ApplyRuntimeSectorCount(int count)
        {
            sectorCount = Mathf.Max(1, count);
        }

        /// <summary>手动刷新程序化点（例如运行时改波次规则前调用）。</summary>
        public void RegenerateProceduralSpawnPoints()
        {
            if (layoutMode != CombatSpawnLayoutMode.ProceduralSectorArcs)
            {
                return;
            }

            RegenerateProceduralSpawnTransforms();
            RebuildSpawnPoints();
        }

        public void RebuildSpawnPoints()
        {
            if (layoutMode == CombatSpawnLayoutMode.ProceduralSectorArcs)
            {
                if (!TryFindGeneratedSpawnRoot(out Transform root))
                {
                    spawnPoints = Array.Empty<CombatSpawnPoint>();
                    return;
                }

                spawnPoints = root.GetComponentsInChildren<CombatSpawnPoint>(true);
                return;
            }

            CombatSpawnPoint[] all = GetComponentsInChildren<CombatSpawnPoint>(true);
            int kept = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && !IsUnderGeneratedSpawnSubtree(all[i].transform))
                {
                    kept++;
                }
            }

            var manual = new CombatSpawnPoint[kept];
            int write = 0;
            for (int i = 0; i < all.Length; i++)
            {
                CombatSpawnPoint sp = all[i];
                if (sp == null || IsUnderGeneratedSpawnSubtree(sp.transform))
                {
                    continue;
                }

                manual[write++] = sp;
            }

            spawnPoints = manual;
        }

        private void RegenerateProceduralSpawnTransforms()
        {
            if (!TryResolveArena(out CombatArena resolved) || resolved.CenterPoint == null)
            {
                return;
            }

            // 删净程序化根节点（含 Unity 自动加的 GeneratedSpawns (n)）再建新树。
            DestroyAllGeneratedSpawnRoots();

            GameObject rootGo = new GameObject(GeneratedRootName);
            rootGo.transform.SetParent(transform, false);
            rootGo.transform.localPosition = Vector3.zero;
            rootGo.transform.localRotation = Quaternion.identity;
            rootGo.transform.localScale = Vector3.one;
            Transform root = rootGo.transform;

            Vector3 center = resolved.CenterPoint.position;
            int sectors = Mathf.Max(1, sectorCount);
            int per = Mathf.Max(1, spawnsPerSector);

            for (int s = 0; s < sectors; s++)
            {
                for (int j = 0; j < per; j++)
                {
                    float angleDeg = CombatSpawnSectorLayout.AngleDegreesForSlot(
                        s,
                        j,
                        sectors,
                        per,
                        sectorAngleInsetDegrees);

                    Vector3 world = CombatSpawnSectorLayout.WorldPositionFromArena(
                        center,
                        spawnHeightOffset,
                        spawnRadius,
                        angleDeg);

                    GameObject go = new GameObject($"Spawn_S{s}_{j}");
                    go.transform.SetParent(root, worldPositionStays: false);
                    go.transform.position = world;
                    go.AddComponent<CombatSpawnPoint>();
                }
            }
        }

        private void DestroyAllGeneratedSpawnRoots()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (!IsGeneratedSpawnRootName(child.name))
                {
                    continue;
                }

                DestroyImmediate(child.gameObject);
            }
        }

        /// <summary>精确名或 Unity 自动重命名的副本（如 GeneratedSpawns (1)）。</summary>
        private static bool IsGeneratedSpawnRootName(string objectName)
        {
            if (objectName == GeneratedRootName)
            {
                return true;
            }

            return objectName.StartsWith(GeneratedRootName + " (", StringComparison.Ordinal)
                   && objectName.EndsWith(")", StringComparison.Ordinal);
        }

        private bool TryFindGeneratedSpawnRoot(out Transform root)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (IsGeneratedSpawnRootName(child.name))
                {
                    root = child;
                    return true;
                }
            }

            root = null;
            return false;
        }

        private bool IsUnderGeneratedSpawnSubtree(Transform t)
        {
            Transform walk = t;
            while (walk != null && walk != transform)
            {
                if (walk.parent == transform && IsGeneratedSpawnRootName(walk.name))
                {
                    return true;
                }

                walk = walk.parent;
            }

            return false;
        }

        private bool TryResolveArena(out CombatArena resolved)
        {
            resolved = arena;
            if (resolved != null)
            {
                return true;
            }

            resolved = FindObjectOfType<CombatArena>();
            return resolved != null;
        }

    }
}
