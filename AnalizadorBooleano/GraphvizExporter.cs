using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace AnalizadorBooleano
{
    // GraphvizExporter: clase para exportar el árbol sintáctico a imagen usando Graphviz
    public static class GraphvizExporter
    {
        public static string ExportarComoImagen(Nodo raiz, string rutaBinDot, string rutaSalidaSinExtension)
        {
            var dotCode = GenerarDot(raiz);
            var dotFile = rutaSalidaSinExtension + ".dot";
            File.WriteAllText(dotFile, dotCode);

            var dotExe = Path.Combine(rutaBinDot, "dot.exe");
            if (!File.Exists(dotExe))
                throw new FileNotFoundException("No se encontró dot.exe en: " + dotExe);

            var proceso = new Process();
            proceso.StartInfo.FileName = dotExe;
            proceso.StartInfo.Arguments = $"-Tpng \"{dotFile}\" -o \"{rutaSalidaSinExtension}.png\"";
            proceso.StartInfo.CreateNoWindow = true;
            proceso.StartInfo.UseShellExecute = false;
            proceso.Start();
            proceso.WaitForExit();

            return rutaSalidaSinExtension + ".png";
        }

        private static string GenerarDot(Nodo raiz)
        {
            var sb = new StringBuilder();
            sb.AppendLine("digraph G {");
            sb.AppendLine("node [shape=box];");

            int idCounter = 0;
            GenerarNodos(raiz, ref idCounter, sb, null);

            sb.AppendLine("}");
            return sb.ToString();
        }

        private static int GenerarNodos(Nodo nodo, ref int id, StringBuilder sb, string padreId)
        {
            string miId = "n" + id++;
            string etiqueta = nodo.Valor != null ? $"{nodo.Tipo}: {nodo.Valor}" : nodo.Tipo;
            sb.AppendLine($"{miId} [label=\"{etiqueta}\"];");

            if (padreId != null)
                sb.AppendLine($"{padreId} -> {miId};");

            foreach (var hijo in nodo.Hijos)
            {
                GenerarNodos(hijo, ref id, sb, miId);
            }

            return id;
        }
    }
}
