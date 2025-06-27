using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO.Compression;
using System.Windows.Input;
using System.Windows.Media;

namespace AnalizadorBooleano
{
    public partial class MainWindow : Window
    {
        private Nodo nodoActual;
        private string expresionActual;
        private string imagenActualPath;

        // Constructor de la ventana principal
        public MainWindow()
        {
            InitializeComponent();
            imgArbol.MouseWheel += ImgArbol_MouseWheel;
        }

        private void Analizar_Click(object sender, RoutedEventArgs e)
        {
            txtResultado.Text = "";
            imgArbol.Source = null;
        
            // Limpiar imágenes temporales previas
            foreach (var file in Directory.GetFiles(Path.GetTempPath(), "arbol_sintactico_*.png"))
            {
                try { File.Delete(file); } catch { }
            }

            try
            {
                var nodo = MiParser.Parsear(txtEntrada.Text);
                txtResultado.Text = nodo.Mostrar();

                //string basePath = Path.Combine(Path.GetTempPath(), "arbol_sintactico");
                string uniqueId = DateTime.Now.Ticks.ToString(); // o Guid.NewGuid().ToString()
                string tempDir = Path.GetTempPath();
                string basePath = Path.Combine(tempDir, "arbol_sintactico_" + uniqueId);
                string rutaDot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "graphviz_bin");

                string imagenPath = GraphvizExporter.ExportarComoImagen(nodo, rutaDot, basePath);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagenPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                imgArbol.Source = bitmap;

                //guardar los datos para exportar con el metodo ExportarResultado()
                nodoActual = nodo;
                expresionActual = txtEntrada.Text;
                imagenActualPath = imagenPath;

            }
            catch (Exception ex)
            {
                txtResultado.Text = " Error: " + ex.Message;
            }
        }

        private void ExportarResultado(Nodo raiz, string expresion, string resultadoJerarquico, string imagenPath)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Archivo ZIP (*.zip)|*.zip",
                FileName = "analisis_arbol_booleano"
            };

            if (dialog.ShowDialog() == true)
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "export_" + Guid.NewGuid());
                Directory.CreateDirectory(tempDir);

                // Guardar archivos individuales
                File.WriteAllText(Path.Combine(tempDir, "expresion.txt"), expresion);
                File.WriteAllText(Path.Combine(tempDir, "estructura.txt"), resultadoJerarquico);
                File.Copy(imagenPath, Path.Combine(tempDir, "arbol.png"));

                // Crear el ZIP con los datos
                string zipPath = dialog.FileName;
                if (File.Exists(zipPath)) File.Delete(zipPath);
                System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, zipPath);

                MessageBox.Show("Exportación completada con éxito.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Exportar_Click(object sender, RoutedEventArgs e)
        {
            if (nodoActual == null || string.IsNullOrEmpty(imagenActualPath))
            {
                MessageBox.Show("Primero analiza una expresión antes de exportar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ExportarResultado(nodoActual, expresionActual, nodoActual.Mostrar(), imagenActualPath);
        }

        //eventos para manejo de texto en el TextBox
        private void txtEntrada_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                e.Handled = true; // evitar salto de línea
                Analizar_Click(null, null);
            }
        }

        private void txtEntrada_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtEntrada.Text == "Escribe tu consulta booleana aquí...")
            {
                txtEntrada.Text = "";
                txtEntrada.Foreground = Brushes.Black;
            }
        }

        private void txtEntrada_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtEntrada.Text))
            {
                txtEntrada.Text = "Escribe tu consulta booleana aquí...";
                txtEntrada.Foreground = Brushes.Gray;
            }
        }

        private void ImgArbol_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomFactor = e.Delta > 0 ? 1.1 : 1 / 1.1; // zoom in/out

            scaleTransform.ScaleX *= zoomFactor;
            scaleTransform.ScaleY *= zoomFactor;
        }
    }
}
