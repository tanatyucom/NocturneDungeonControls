using UnityEngine;

namespace NocturneModernController
{
    internal static class ExplorationCursorController
    {
        private static bool _controlling;
        private static bool _visibleBeforeControl;

        internal static void Update(bool explorationActive)
        {
            if (explorationActive)
            {
                if (!_controlling)
                {
                    _visibleBeforeControl = Cursor.visible;
                    _controlling = true;
                }

                // The synthetic vertical mouse input makes Unity reveal its
                // cursor again, so enforce this every exploration frame.
                Cursor.visible = false;
                return;
            }

            if (_controlling)
            {
                Cursor.visible = _visibleBeforeControl;
                _controlling = false;
            }
        }

        internal static void Restore()
        {
            if (_controlling)
            {
                Cursor.visible = _visibleBeforeControl;
                _controlling = false;
            }
        }
    }
}
