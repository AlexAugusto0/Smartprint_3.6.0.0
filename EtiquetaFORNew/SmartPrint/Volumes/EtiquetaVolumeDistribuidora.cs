using System;
using System.Collections.Generic;

namespace EtiquetaFORNew
{
    public class EtiquetaVolumeDistribuidora
    {
        public DadosNotaDistribuidora DadosNota { get; set; }
        public DadosDestinatarioDistribuidora DadosDestinatario { get; set; }
        public DadosEnderecoDistribuidora DadosEndereco { get; set; }
        public List<ItemNotaDistribuidora> Itens { get; set; }
        public List<VolumeDistribuidora> Volumes { get; set; }
        public VolumeDistribuidora Volume { get; set; }

        public EtiquetaVolumeDistribuidora()
        {
            DadosNota = new DadosNotaDistribuidora();
            DadosDestinatario = new DadosDestinatarioDistribuidora();
            DadosEndereco = new DadosEnderecoDistribuidora();
            Itens = new List<ItemNotaDistribuidora>();
            Volumes = new List<VolumeDistribuidora>();
            Volume = new VolumeDistribuidora();
        }
    }

    public class DadosNotaDistribuidora
    {
        public string NumeroDocumento { get; set; }
        public string NumeroNFe { get; set; }
        public string Serie { get; set; }
        public string Modelo { get; set; }
        public DateTime? DataEmissao { get; set; }
        public long ClienteId { get; set; }
        public decimal? ValorTotal { get; set; }
        public string Observacao { get; set; }
        public string ChaveAcesso { get; set; }
    }

    public class DadosDestinatarioDistribuidora
    {
        public string RazaoSocial { get; set; }
        public string NomeFantasia { get; set; }
        public string Documento { get; set; }
        public string Telefone { get; set; }
        public string Email { get; set; }
    }

    public class DadosEnderecoDistribuidora
    {
        public string Logradouro { get; set; }
        public string Numero { get; set; }
        public string Complemento { get; set; }
        public string Cep { get; set; }
        public long BairroId { get; set; }
        public string Bairro { get; set; }
        public string Cidade { get; set; }
        public string Uf { get; set; }
    }

    public class ItemNotaDistribuidora
    {
        public string Codigo { get; set; }
        public string Descricao { get; set; }
        public decimal Quantidade { get; set; }
        public int QuantidadeVolumes { get; set; }
        public decimal? Peso { get; set; }
    }

    public class VolumeDistribuidora
    {
        public int VolumeAtual { get; set; }
        public int TotalVolumes { get; set; }
        public string CodigoVolume { get; set; }
        public decimal? Peso { get; set; }
        public DateTime DataImpressao { get; set; }
        public string Operador { get; set; }
        public string EmpresaEmitente { get; set; }
    }

    public class DistribuidoraNFeVolumesResult
    {
        public bool Sucesso { get; set; }
        public string MensagemErro { get; set; }
        public DadosNotaDistribuidora DadosNota { get; set; }
        public DadosDestinatarioDistribuidora DadosDestinatario { get; set; }
        public DadosEnderecoDistribuidora DadosEndereco { get; set; }
        public List<ItemNotaDistribuidora> Itens { get; set; }
        public List<VolumeDistribuidora> Volumes { get; set; }
        public List<EtiquetaVolumeDistribuidora> Etiquetas { get; set; }

        public int TotalVolumes => Volumes == null ? 0 : Volumes.Count;

        public DistribuidoraNFeVolumesResult()
        {
            Itens = new List<ItemNotaDistribuidora>();
            Volumes = new List<VolumeDistribuidora>();
            Etiquetas = new List<EtiquetaVolumeDistribuidora>();
        }
    }
}
