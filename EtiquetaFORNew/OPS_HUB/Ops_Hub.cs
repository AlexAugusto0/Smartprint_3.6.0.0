using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EtiquetaFORNew.OPS_HUB
{
    public class Ops_Hub
    {
        private Process _processoExterno = null;

        // APIs do Windows para mover a janela
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        /// <summary>
        /// Inicia o OPS_HUB.exe e o posiciona na mesma tela do formulário informado.
        /// </summary>
        public void IniciarProvedor(Form formularioPai)
        {
            string caminhoExe = Path.Combine(AppContext.BaseDirectory, "OPS_HUB.exe");

            if (!File.Exists(caminhoExe))
            {
                MessageBox.Show("Arquivo OPS_HUB.exe não encontrado.");
                return;
            }

            // Se já estiver rodando, apenas reposiciona
            if (_processoExterno != null && !_processoExterno.HasExited)
            {
                MoverParaMesmaTela(formularioPai);
                return;
            }

            try
            {
                _processoExterno = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = caminhoExe,
                        UseShellExecute = false
                    }
                };

                _processoExterno.Start();

                // Espera a janela carregar e posiciona
                _processoExterno.WaitForInputIdle(5000);
                System.Threading.Thread.Sleep(500);

                MoverParaMesmaTela(formularioPai);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar o processo: {ex.Message}");
            }
        }

        /// <summary>
        /// Fecha o processo externo se ele ainda estiver rodando.
        /// </summary>
        public void TerminarProcesso()
        {
            if (_processoExterno != null && !_processoExterno.HasExited)
            {
                try
                {
                    _processoExterno.Kill();
                    _processoExterno.Dispose();
                }
                catch { /* Ignora erros de fechamento */ }
            }
        }

        private void MoverParaMesmaTela(Form formularioPai)
        {
            if (_processoExterno == null || _processoExterno.MainWindowHandle == IntPtr.Zero) return;

            IntPtr handle = _processoExterno.MainWindowHandle;
            Screen telaAtual = Screen.FromControl(formularioPai);

            RECT rect;
            if (GetWindowRect(handle, out rect))
            {
                int largura = rect.Right - rect.Left;
                int altura = rect.Bottom - rect.Top;

                int novoX = telaAtual.Bounds.X + 50;
                int novoY = telaAtual.Bounds.Y + 50;

                MoveWindow(handle, novoX, novoY, largura, altura, true);
            }
        }
    }
}