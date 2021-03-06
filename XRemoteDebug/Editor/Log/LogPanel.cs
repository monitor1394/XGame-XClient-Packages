using UnityEngine;
using XCommon.Editor;

namespace XRemoteDebug
{
    internal class LogPanel : IRemoteDebugPanel
    {
        private RemoteDebugWindow m_Window;

        public LogPanel(RemoteDebugWindow window)
        {
            m_Window = window;
        }

        public ITianGlyphPanel GetStatusPanel()
        {
            return null;
        }

        public void OnEnable()
        {
        }

        public void Update()
        {
        }

        public void OnGUI(Rect rect)
        {
            GUI.Label(rect, "log");
        }

        public void Reload()
        {
        }
    }
}