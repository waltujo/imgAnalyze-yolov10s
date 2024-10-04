using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using YoloDotNet.Enums;
using YoloDotNet.Models;
using YoloDotNet;
using Yolov5Net.Scorer;
using Yolov8Net;
using SkiaSharp;

namespace VerImage
{
    public class Program
    {
        static void Main(string[] args)
        {
            string directoryPath = @"CAMINHO DA PASTA COM AS FOTOS";
            var varredura = new Varredura();
            varredura.verificaYolo10(directoryPath);
        }
    }

    public class Varredura
    {
        public void verificaYolo10(string directoryPath)
        {
            List<ObjectDetection> results = new List<ObjectDetection>();

            // Instantiate a new Yolo object
            using var yolo = new Yolo(new YoloOptions
            {
                OnnxModel = @"CAMINHO PARA O YOLO\yolov10s.onnx", // Your Yolov8 or Yolov10 model in onnx format
                ModelType = ModelType.ObjectDetection, // Model type
                Cuda = false, // Use CPU or CUDA for GPU accelerated inference. Default = true
                GpuId = 0, // Select Gpu by id. Default = 0
                PrimeGpu = false // Pre-allocate GPU before first. Default = false
            });

            // Cria a pasta para fotos sem pessoas, com pessoas e icones
            string semPessoasPath = Path.Combine(directoryPath, "semPessoas");
            string iconsPath = Path.Combine(directoryPath, "icons");
            string comPessoasPath = Path.Combine(directoryPath, "comPessoas");

            if (!Directory.Exists(semPessoasPath)) Directory.CreateDirectory(semPessoasPath);
            if (!Directory.Exists(iconsPath)) Directory.CreateDirectory(iconsPath);
            if (!Directory.Exists(comPessoasPath)) Directory.CreateDirectory(comPessoasPath);

            // Percorre todas as imagens nas subpastas
            foreach (var filePath in Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories))
            {
                try
                {
                    if (filePath.StartsWith(semPessoasPath) || filePath.StartsWith(comPessoasPath) || filePath.StartsWith(iconsPath)) continue;

                    // Load image
                    using var img = SKImage.FromEncodedData(filePath);

                    try
                    {
                        // Run inference and get the results
                        results = yolo.RunObjectDetection(img, confidence: 0.25, iou: 0.7);
                    }
                    //SÓ ENTRA NESSA EXCEPTION CASO A IMAGEM ESTEJA CORROMPIDA
                    catch (NullReferenceException)
                    {
                        File.Delete(filePath);
                    }
                   
                    // Verifica se alguma predição contém uma pessoa
                    bool containsPerson = results.Any(x => x.Label.Name == "person");

                    var scoreImg = results.Select(x => x.Confidence);

                    //ImageAnalysisResult imgAnalyze = AnalyzeImage(img);
                    //imgAnalyze.Predictions = predictions.ToList();

                    if (!containsPerson)
                    {
                        string semFilePath = Path.Combine(semPessoasPath, Path.GetFileName(filePath));
                        File.Move(filePath, semFilePath);
                        //SaveAnalysisToFile(imgAnalyze, semFilePath);
                        Console.WriteLine($"Imagem {filePath} movida para 'semPessoas' (NÃO CONTÉM PESSOAS).");
                    }
                    //else if (hasLowColorVariation)
                    //{
                    //    Console.WriteLine("Imagem com baixa variação de cor detectada ou é um ícone.");
                    //    string iconFilePath = Path.Combine(iconsPath, Path.GetFileName(filePath));
                    //    File.Move(filePath, iconFilePath);
                    //    SaveAnalysisToFile(imgAnalyze, iconFilePath);
                    //    Console.WriteLine($"Imagem {filePath} movida para 'icons'.");
                    //}
                    else
                    {
                        string comFilePath = Path.Combine(comPessoasPath, Path.GetFileName(filePath));
                        File.Move(filePath, comFilePath);
                        //SaveAnalysisToFile(imgAnalyze, comFilePath);
                        Console.WriteLine($"Imagem {filePath} mantida (CONTÉM PESSOAS).");
                    }
                }
                catch (Exception ex)
                {
                    // Se a imagem não puder ser carregada, é considerada corrompida e será movida para uma pasta
                    File.Move(filePath, iconsPath);
                    Console.WriteLine($"Imagem {filePath} excluída (corrompida). Erro: {ex.Message}");
                }
            }
        }
        public void VerificarImagensAsync(string directoryPath)
        {
            // Caminho para o modelo YOLO
            string modelPath = @"C:CAMINHO PARA O YOLO\yolov8m.onnx";

            // Carrega o modelo YOLO
            //using var scorer = new YoloScorer<YoloCocoP5Model>(modelPath);
            using var yolo = YoloV8Predictor.Create(modelPath);

            // Cria a pasta para fotos sem pessoas, com pessoas e icones
            string semPessoasPath = Path.Combine(directoryPath, "semPessoas");
            string iconsPath = Path.Combine(directoryPath, "icons");
            string comPessoasPath = Path.Combine(directoryPath, "comPessoas");

            if (!Directory.Exists(semPessoasPath)) Directory.CreateDirectory(semPessoasPath);
            if (!Directory.Exists(iconsPath)) Directory.CreateDirectory(iconsPath);
            if (!Directory.Exists(comPessoasPath)) Directory.CreateDirectory(comPessoasPath);

            // Percorre todas as imagens nas subpastas
            foreach (var filePath in Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories))
            {
                try
                {
                    if (filePath.StartsWith(semPessoasPath) || filePath.StartsWith(comPessoasPath) || filePath.StartsWith(iconsPath)) continue;

                    // Tenta carregar a imagem para verificar se está corrompida
                    using var img = Image.Load<Rgba32>(filePath);

                    if (img.Width > 416 || img.Height > 416)
                    {
                        img.Mutate(x => x.Resize(new Size(416, 416)));
                    }

                    // Pré-processamento: Redimensiona, ajusta contraste e brilho
                    img.Mutate(x => x
                        .Contrast(1.2f)
                        .Brightness(1.1f)
                        .GaussianBlur(0.5f));

                    // Realiza a predição com o YOLO
                    var predictions = yolo.Predict(img);

                    // Verifica se alguma predição contém uma pessoa
                    bool containsPerson = predictions.Any(p => p.Label?.Name == "person" && p.Score > 0.3);

                    var scoreImg = predictions.Select(x => x.Score);
                   
                    bool hasLowColorVariation = HasLowColorVariation(img);

                    ImageAnalysisResult imgAnalyze = AnalyzeImage(img);

                    imgAnalyze.Predictions = predictions.ToList();
                    
                    if (!containsPerson) 
                    {
                        string semFilePath = Path.Combine(semPessoasPath, Path.GetFileName(filePath));
                        //File.Move(filePath, semFilePath);
                        SaveAnalysisToFile(imgAnalyze, semFilePath);
                        Console.WriteLine($"Imagem {filePath} movida para 'semPessoas' (NÃO CONTÉM PESSOAS).");
                    }
                    else if (hasLowColorVariation)
                    {
                        Console.WriteLine("Imagem com baixa variação de cor detectada ou é um ícone.");
                        string iconFilePath = Path.Combine(iconsPath, Path.GetFileName(filePath));
                        //File.Move(filePath, iconFilePath);
                        SaveAnalysisToFile(imgAnalyze, iconFilePath);
                        Console.WriteLine($"Imagem {filePath} movida para 'icons'.");
                    }
                    else
                    {
                        string comFilePath = Path.Combine(comPessoasPath, Path.GetFileName(filePath));
                        //File.Move(filePath, comFilePath);
                        SaveAnalysisToFile(imgAnalyze, comFilePath);
                        Console.WriteLine($"Imagem {filePath} mantida (CONTÉM PESSOAS).");
                    }
                }
                catch (Exception ex)
                {
                    // Se a imagem não puder ser carregada, é considerada corrompida e será excluída
                    //File.Delete(filePath);
                    Console.WriteLine($"Imagem {filePath} excluída (corrompida). Erro: {ex.Message}");
                }
            }
        }
        public ImageAnalysisResult AnalyzeImage(Image<Rgba32> img)
        {
            // Agrupa as cores da imagem e conta a frequência de cada cor
            var colorGroups = img.GetPixelMemoryGroup()
                .SelectMany(memory => memory.ToArray())
                .GroupBy(pixel => pixel)
                .ToDictionary(g => g.Key, g => g.Count());

            // Conta o número de cores distintas
            int distinctColorCount = colorGroups.Count;

            // Ordena as cores por frequência (da mais comum para a menos comum)
            var dominantColors = colorGroups
                .OrderByDescending(kv => kv.Value)
                .Select(kv => kv.Key)
                .Take(5) // Limita para as 5 cores mais dominantes
                .ToList();

            // Determina a classificação com base no número de cores distintas
            string classification = distinctColorCount switch
            {
                < 5 => "até 5 cores", // Poucas cores distintas, ícone básico
                >= 5 and < 15 => "entre 5 e 15 cores", // Algumas cores, ainda um ícone ou imagem simples
                >= 15 and < 30 => "foto entre 15 e 30 cores", // Variação limitada, mas não um ícone
                _ => "foto" // Alta variação de cor, normalmente uma foto ou imagem complexa
            };

            return new ImageAnalysisResult
            {
                DistinctColorCount = distinctColorCount,
                ColorFrequencies = colorGroups,
                DominantColors = dominantColors,
                Classification = classification
            };
        }
        public bool HasLowColorVariation(Image<Rgba32> img)
        {
            // Acessa os pixels da imagem
            var pixelGroups = img.CloneAs<Rgba32>().GetPixelMemoryGroup();

            // Inicializa variáveis para calcular as médias das cores
            double totalPixels = 0;
            double sumRed = 0, sumGreen = 0, sumBlue = 0;

            // Calcula a média de cada canal de cor
            foreach (var memory in pixelGroups)
            {
                foreach (var pixel in memory.Span)
                {
                    sumRed += pixel.R;
                    sumGreen += pixel.G;
                    sumBlue += pixel.B;
                    totalPixels++;
                }
            }

            double redMean = sumRed / totalPixels;
            double greenMean = sumGreen / totalPixels;
            double blueMean = sumBlue / totalPixels;

            // Inicializa variáveis para calcular o desvio padrão
            double sumSquaredRedDiff = 0, sumSquaredGreenDiff = 0, sumSquaredBlueDiff = 0;

            // Calcula o desvio padrão de cada canal de cor
            foreach (var memory in pixelGroups)
            {
                foreach (var pixel in memory.Span)
                {
                    sumSquaredRedDiff += Math.Pow(pixel.R - redMean, 2);
                    sumSquaredGreenDiff += Math.Pow(pixel.G - greenMean, 2);
                    sumSquaredBlueDiff += Math.Pow(pixel.B - blueMean, 2);
                }
            }

            double redStdDev = Math.Sqrt(sumSquaredRedDiff / totalPixels);
            double greenStdDev = Math.Sqrt(sumSquaredGreenDiff / totalPixels);
            double blueStdDev = Math.Sqrt(sumSquaredBlueDiff / totalPixels);

            // Verifica se a imagem tem baixa variação de cor
            double colorThreshold = 10.0;
            return (redStdDev < colorThreshold) && (greenStdDev < colorThreshold) && (blueStdDev < colorThreshold);
           
        }
        public void SaveAnalysisToFile(ImageAnalysisResult analysisResult, string filePath)
        {
            var txtFilePath = $"{filePath}.txt";
            using (var writer = new StreamWriter(txtFilePath))
            {
                writer.WriteLine($"Número de cores distintas: {analysisResult.DistinctColorCount}");
                writer.WriteLine($"Classificação: {analysisResult.Classification}");
                writer.WriteLine("Cores dominantes:");
                foreach (var color in analysisResult.DominantColors)
                {
                    writer.WriteLine($"Cor: {color}");
                }
                writer.WriteLine("\nFrequência das cores:");
                foreach (var kvp in analysisResult.ColorFrequencies.OrderByDescending(kvp => kvp.Value).Take(10))
                {
                    writer.WriteLine($"Cor: {kvp.Key}, Frequência: {kvp.Value}");
                }

                writer.WriteLine("\nDados de predição:");
                foreach (var prediction in analysisResult.Predictions)
                {
                    writer.WriteLine($"Objeto: {prediction.Label?.Name}, Confiança: {prediction.Score}, Caixa delimitadora: {prediction.Rectangle}");
                }
            }
        }
        public class ImageAnalysisResult
        {
            public int DistinctColorCount { get; set; }
            public Dictionary<Rgba32, int>? ColorFrequencies { get; set; }
            public List<Rgba32>? DominantColors { get; set; }
            public string? Classification { get; set; }
            public List<Prediction>? Predictions { get; set; }
        }
    }
}