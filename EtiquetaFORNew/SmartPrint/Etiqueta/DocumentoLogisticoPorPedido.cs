using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace EtiquetaFORNew
{
    public class VendaCompleta
    {
        public bool Sucesso { get; set; }
        public string MensagemErro { get; set; }
        public JToken JsonCompleto { get; set; }
        public JObject Data { get; set; }
    }

    public class DistribuidoraDocumentoPedidoResult
    {
        public bool Sucesso { get; set; }
        public string MensagemErro { get; set; }
        public EtiquetaDistribuidora Etiqueta { get; set; }
        public int? QuantidadeVolumes { get; set; }

        public bool DeveSolicitarQuantidadeVolumes
        {
            get { return !QuantidadeVolumes.HasValue || QuantidadeVolumes.Value <= 0; }
        }

        public List<EtiquetaDistribuidora> Etiquetas
        {
            get
            {
                var etiquetas = new List<EtiquetaDistribuidora>();

                if (Etiqueta == null || DeveSolicitarQuantidadeVolumes)
                    return etiquetas;

                for (int volume = 1; volume <= QuantidadeVolumes.Value; volume++)
                {
                    etiquetas.Add(new EtiquetaDistribuidora
                    {
                        Venda = Etiqueta.Venda,
                        Empresa = Etiqueta.Empresa,
                        Destinatario = Etiqueta.Destinatario,
                        Endereco = Etiqueta.Endereco,
                        Produtos = Etiqueta.Produtos,
                        Volume = volume,
                        VolumeTotal = QuantidadeVolumes.Value
                    });
                }

                return etiquetas;
            }
        }
    }
}
