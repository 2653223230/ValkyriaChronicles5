using UnityEngine;
using UnityEngine.UI;
using TcgEngine;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// 主菜单局域网对战：主机走 HostP2P，加入方填 IP 走 Multiplayer（与 TestP2P / Netcode 流程一致）。
    /// 需在场景中挂 CanvasGroup（UIPanel 要求）、拖引用：本机 IP 文本、可选错误提示 Text、Join 输入框。
    /// </summary>
    public class Vc5LanBattlePanel : UIPanel
    {
        [Header("Optional UI")]
        public InputField join_ip_field;
        public Text local_ip_text;
        public Text hint_error;

        private static Vc5LanBattlePanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            if (hint_error != null)
                hint_error.text = "";
            RefreshLocalIp();
        }

        public void RefreshLocalIp()
        {
            if (local_ip_text == null)
                return;
            string ip = NetworkTool.GetLocalIp();
            local_ip_text.text = string.IsNullOrEmpty(ip) ? "(未检测到局域网 IPv4)" : ip;
        }

        public void OnClickHostLan()
        {
            if (hint_error != null)
                hint_error.text = "";
            MainMenu menu = MainMenu.Get();
            string err = "";
            if (menu == null)
                err = "未找到 MainMenu。";
            else if (!menu.TryStartLanBattleHost(out err))
                { /* leave err */ }
            else
            {
                Hide(true);
                return;
            }

            if (hint_error != null)
                hint_error.text = string.IsNullOrEmpty(err) ? "无法开战。" : err;
            else if (!string.IsNullOrEmpty(err))
                Debug.LogWarning(err);
        }

        public void OnClickJoinLan()
        {
            if (hint_error != null)
                hint_error.text = "";
            string raw = join_ip_field != null ? join_ip_field.text : "";
            MainMenu menu = MainMenu.Get();
            string err = "";
            if (menu == null)
                err = "未找到 MainMenu。";
            else if (!menu.TryStartLanBattleJoin(raw, out err))
                { }
            else
            {
                Hide(true);
                return;
            }

            if (hint_error != null)
                hint_error.text = string.IsNullOrEmpty(err) ? "无法加入。" : err;
            else if (!string.IsNullOrEmpty(err))
                Debug.LogWarning(err);
        }

        /// <summary>关闭本面板并打开房号对战（Matchmaking code）。</summary>
        public void OnClickOpenPlayWithCode()
        {
            Hide(true);
            JoinCodePanel jp = JoinCodePanel.Get();
            if (jp != null)
                jp.Show();
        }

        public void OnClickClosePanel()
        {
            Hide();
        }

        public static Vc5LanBattlePanel Get()
        {
            return instance;
        }
    }
}
