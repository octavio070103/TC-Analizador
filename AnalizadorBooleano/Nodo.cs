using System.Collections.Generic;
using System.Text;

namespace AnalizadorBooleano
{
    public class Nodo
    {
        public string Tipo { get; }
        public string Valor { get; }
        public List<Nodo> Hijos { get; }

        public Nodo(string tipo, string valor = null, List<Nodo> hijos = null)
        {
            Tipo = tipo;
            Valor = valor;
            Hijos = hijos ?? new List<Nodo>();
        }

        public string Mostrar(int nivel = 0)
        {
            var indent = new string(' ', nivel * 2);
            var sb = new StringBuilder();
            // Terminal con valor
            if (Hijos.Count == 0 && Valor != null)
                sb.AppendLine($"{indent}{Tipo}: {Valor}");
            else
            {
                // No terminal
                sb.AppendLine($"{indent}{Tipo}");
                foreach (var h in Hijos)
                    sb.Append(h.Mostrar(nivel + 1));
            }
            return sb.ToString();
        }
    }
}
