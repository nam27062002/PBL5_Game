using System.Collections.Generic;
using Scripts.Utils;
using UnityEngine;

namespace Scripts.UI
{
    public class InputBlocker : MonoSingleton<InputBlocker>
    {
        private readonly List<Object> _guiBlockers = new List<Object>();

        public UICamera uiCam;

        protected override void Awake()
        {
            base.Awake();
            uiCam = GetComponent<UICamera>();
        }

        private void BlockGUI(Object blocker)
        {
            if (!_guiBlockers.Contains(blocker))
            {
                _guiBlockers.Add(blocker);
            }

            SetNgui(false);
        }

        private void UnblockGUI(Object blocker)
        {
            _guiBlockers.Remove(blocker);

            if (_guiBlockers.Count <= 0)
            {
                SetNgui(true);
            }
        }

        public void SetBlock(Object blocker, bool @on)
        {
            if (on) BlockGUI(blocker);
            else UnblockGUI(blocker);
        }

        private void SetNgui(bool enable)
        {
            if (uiCam && uiCam.enabled != enable)
            {
                uiCam.enabled = enable;
            }
        }

        private void OnGUI()
        {
            if (uiCam == null || uiCam.enabled) return;
            // All this jus to put the text at bottom middle of the screen.
            GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));

            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            GUI.color = Color.red;
            GUILayout.Label("Blocking NGUI");
            foreach (var guiBlocker in _guiBlockers)
            {
                GUILayout.Label(guiBlocker.GetType() + " - " + guiBlocker.name);
            }

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.EndArea();
        }

 
    }
}