using IdleTafang.Gameplay.Combat;
using UnityEngine;

namespace IdleTafang.UI
{
    public sealed class SectorFocusPresenter : MonoBehaviour
    {
        [SerializeField] private SectorFocusCombatAdapter combatAdapter;
        [SerializeField] private SectorFocusHudView hudView;
        [SerializeField] private SectorFocusWorldView worldView;

        private bool visible;

        public void Initialize(
            SectorFocusCombatAdapter adapter,
            SectorFocusHudView hud,
            SectorFocusWorldView world)
        {
            if (combatAdapter != null)
            {
                combatAdapter.SnapshotChanged -= OnSnapshotChanged;
            }

            combatAdapter = adapter;
            hudView = hud;
            worldView = world;

            if (combatAdapter != null)
            {
                combatAdapter.SnapshotChanged += OnSnapshotChanged;
                OnSnapshotChanged(combatAdapter.Snapshot);
            }

            SetVisible(false);
        }

        public void SetVisible(bool isVisible)
        {
            visible = isVisible;
            hudView?.SetVisible(isVisible);
            worldView?.SetVisible(isVisible);

            if (visible && combatAdapter != null)
            {
                OnSnapshotChanged(combatAdapter.Snapshot);
            }
        }

        private void OnDestroy()
        {
            if (combatAdapter != null)
            {
                combatAdapter.SnapshotChanged -= OnSnapshotChanged;
            }
        }

        private void OnSnapshotChanged(SectorFocusSnapshot snapshot)
        {
            if (!visible)
            {
                return;
            }

            hudView?.Render(snapshot);
            worldView?.Render(snapshot);
        }
    }
}
