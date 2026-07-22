using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using System.IO; // Necessário para Path.GetDirectoryName

namespace EtiquetaFORNew
{
    public partial class FormListaConfiguracoes : Form
    {
        // ⭐ Propriedade alterada para refletir a seleção de Configuração
        public string ConfiguracaoSelecionada { get; private set; }
        // ⭐ Nome do ListBox alterado (se você estiver usando code-behind, use o nome que definiu no Designer)
        private ListBox lstConfiguracoes;

        public FormListaConfiguracoes()
        {
            InitializeComponent();
            CarregarLista();
        }
               

        private void CarregarLista()
        {
            lstConfiguracoes.Items.Clear();
            // ⭐ MUDANÇA: Usa o método do Gerenciador para listar os nomes
            var configuracoes = GerenciadorConfiguracoesEtiqueta.ListarNomesConfiguracoes();

            if (configuracoes == null || configuracoes.Count == 0)
            {
                lstConfiguracoes.Items.Add("(Nenhuma configuração salva)");
                return;
            }

            foreach (var config in configuracoes)
            {
                lstConfiguracoes.Items.Add(config);
            }

            if (lstConfiguracoes.Items.Count > 0)
            {
                lstConfiguracoes.SelectedIndex = 0;
            }
        }

        private void CarregarSelecionado()
        {
            if (lstConfiguracoes.SelectedItem == null ||
                lstConfiguracoes.SelectedItem.ToString() == "(Nenhuma configuração salva)")
            {
                MessageBox.Show("Selecione uma configuração!", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            // ⭐ Atribui à nova propriedade ConfiguracaoSelecionada
            ConfiguracaoSelecionada = lstConfiguracoes.SelectedItem.ToString();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnExcluir_Click(object sender, EventArgs e)
        {
            if (lstConfiguracoes.SelectedItem == null ||
                lstConfiguracoes.SelectedItem.ToString() == "(Nenhuma configuração salva)")
            {
                MessageBox.Show("Selecione uma configuração para excluir!", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string nomeConfig = lstConfiguracoes.SelectedItem.ToString();

            if (MessageBox.Show($"Deseja realmente excluir a configuração '{nomeConfig}'?", // ⭐ Texto
                "Confirmar Exclusão", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // ⭐ MUDANÇA: Usa o método do Gerenciador para excluir
                if (GerenciadorConfiguracoesEtiqueta.ExcluirConfiguracao(nomeConfig))
                {
                    MessageBox.Show("Configuração excluída com sucesso!", "Sucesso",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    CarregarLista();
                }
                else
                {
                    MessageBox.Show("Erro ao tentar excluir a configuração.", "Erro",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}