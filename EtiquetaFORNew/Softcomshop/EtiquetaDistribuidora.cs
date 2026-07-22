using System;
using System.Collections.Generic;

namespace EtiquetaFORNew
{
    public class EtiquetaDistribuidora
    {
        public DadosVendaDistribuidora Venda { get; set; }
        public DadosEmpresaDistribuidora Empresa { get; set; }
        public DadosDestinatarioEtiquetaDistribuidora Destinatario { get; set; }
        public DadosEnderecoEtiquetaDistribuidora Endereco { get; set; }
        public List<ProdutoVendaDistribuidora> Produtos { get; set; }
        public int Volume { get; set; }
        public int VolumeTotal { get; set; }

        public EtiquetaDistribuidora()
        {
            Venda = new DadosVendaDistribuidora();
            Empresa = new DadosEmpresaDistribuidora();
            Destinatario = new DadosDestinatarioEtiquetaDistribuidora();
            Endereco = new DadosEnderecoEtiquetaDistribuidora();
            Produtos = new List<ProdutoVendaDistribuidora>();
        }
    }

    public class DadosVendaDistribuidora
    {
        public long Id { get; set; }
        public long ClienteId { get; set; }
        public long EmpresaId { get; set; }
        public string NumeroDocumento { get; set; }
        public string NumeroNf { get; set; }
        public DateTime? DataEmissao { get; set; }
        public string Observacao { get; set; }
        public long NfeId { get; set; }
    }

    public class DadosEmpresaDistribuidora
    {
        public long Id { get; set; }
        public string Nome { get; set; }
        public string Fantasia { get; set; }
        public string RazaoSocial { get; set; }
        public string Cnpj { get; set; }
    }

    public class DadosDestinatarioEtiquetaDistribuidora
    {
        public long Id { get; set; }
        public string Nome { get; set; }
        public string RazaoSocial { get; set; }
        public string Documento { get; set; }
    }

    public class DadosEnderecoEtiquetaDistribuidora
    {
        public string Endereco { get; set; }
        public string Numero { get; set; }
        public string Complemento { get; set; }
        public string Bairro { get; set; }
        public string Cidade { get; set; }
        public string Uf { get; set; }
        public string Cep { get; set; }
    }

    public class ProdutoVendaDistribuidora
    {
        public long VendaId { get; set; }
        public long ProdutoId { get; set; }
        public decimal Quantidade { get; set; }
        public decimal Preco { get; set; }
        public decimal Peso { get; set; }
    }

    public class DistribuidoraDocumentoLogisticoResult
    {
        public bool Sucesso { get; set; }
        public string MensagemErro { get; set; }
        public EtiquetaDistribuidora Etiqueta { get; set; }

        public List<EtiquetaDistribuidora> Etiquetas
        {
            get
            {
                if (Etiqueta == null)
                    return new List<EtiquetaDistribuidora>();

                int totalEtiquetas = CalcularTotalEtiquetas(Etiqueta);
                var etiquetas = new List<EtiquetaDistribuidora>();

                for (int volume = 1; volume <= totalEtiquetas; volume++)
                {
                    etiquetas.Add(CriarEtiquetaVolume(Etiqueta, volume, totalEtiquetas));
                }

                return etiquetas;
            }
        }

        private static int CalcularTotalEtiquetas(EtiquetaDistribuidora etiqueta)
        {
            if (etiqueta.Produtos == null || etiqueta.Produtos.Count == 0)
                return 1;

            int total = 0;

            foreach (var produto in etiqueta.Produtos)
            {
                if (produto == null || produto.Quantidade <= 0)
                    continue;

                total += (int)Math.Ceiling(produto.Quantidade);
            }

            return Math.Max(1, total);
        }

        private static EtiquetaDistribuidora CriarEtiquetaVolume(EtiquetaDistribuidora origem, int volume, int totalEtiquetas)
        {
            return new EtiquetaDistribuidora
            {
                Venda = origem.Venda,
                Empresa = origem.Empresa,
                Destinatario = origem.Destinatario,
                Endereco = origem.Endereco,
                Produtos = origem.Produtos,
                Volume = volume,
                VolumeTotal = totalEtiquetas
            };
        }
    }
}
