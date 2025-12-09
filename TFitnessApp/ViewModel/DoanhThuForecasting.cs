using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;

namespace TFitnessApp
{
    // 1. Cấu trúc dữ liệu đầu vào
    public class DoanhThuInput
    {
        [LoadColumn(0)] public string Month { get; set; }
        [LoadColumn(1)] public float Revenue { get; set; }      
        [LoadColumn(5)] public float Revenue_Lag1 { get; set; } 
        [LoadColumn(6)] public float Revenue_Lag12 { get; set; } 
        [LoadColumn(7)] public float Month_Num { get; set; }    
    }

    // 2. Cấu trúc kết quả dự báo của Regression
    public class DoanhThuPrediction
    {
        [ColumnName("Score")]
        public float ForecastedRevenue { get; set; }
    }

    // 3. Cấu trúc kết quả dự báo của SSA
    public class SsaForecast
    {
        public float[] ForecastedRevenue { get; set; }
        public float[] LowerBoundRevenue { get; set; }
        public float[] UpperBoundRevenue { get; set; }
    }

    // 4. Class lưu kết quả so sánh
    public class ModelResult
    {
        public string ModelName { get; set; }
        public double Accuracy { get; set; } 
        public float ForecastT1 { get; set; } 
        public float ForecastT2 { get; set; } 
        public string Note { get; set; }
    }

    public class DoanhThuPredictor
    {
        private static MLContext mlContext = new MLContext(seed: 1);

        public static string TrainAndPredict(string csvPath)
        {
            try
            {
                if (!File.Exists(csvPath)) return "Lỗi: Không tìm thấy file dữ liệu.";

                // Load dữ liệu
                List<DoanhThuInput> dataList = File.ReadAllLines(csvPath)
                                                   .Skip(1) // Bỏ header
                                                   .Select(LineToObj)
                                                   .Where(x => x != null)
                                                   .ToList();

                if (dataList.Count < 24) return "Dữ liệu quá ít để so sánh các mô hình.";

                // Chia Train/Test (Lấy 12 tháng cuối làm Test)
                int totalRows = dataList.Count;
                int testSize = 12;
                int trainSize = totalRows - testSize;

                var trainDataView = mlContext.Data.LoadFromEnumerable(dataList.Take(trainSize));
                var testDataView = mlContext.Data.LoadFromEnumerable(dataList.Skip(trainSize));
                var fullDataView = mlContext.Data.LoadFromEnumerable(dataList);

                List<ModelResult> results = new List<ModelResult>();

                // --- 1. SSA (Time Series) ---
                results.Add(EvaluateSSA(trainDataView, testDataView, fullDataView, totalRows, trainSize));

                // Định nghĩa Pipeline cho Regression
                var featurePipeline = mlContext.Transforms.CopyColumns("Label", "Revenue")
                    .Append(mlContext.Transforms.Concatenate("Features", "Revenue_Lag1", "Revenue_Lag12", "Month_Num"));

                // --- 2. FastTree ---
                var fastTreePipeline = featurePipeline.Append(mlContext.Regression.Trainers.FastTree());
                results.Add(EvaluateRegression("FastTree", fastTreePipeline, trainDataView, testDataView, fullDataView, dataList));

                // --- 3. LightGBM ---
                var lightGbmPipeline = featurePipeline.Append(mlContext.Regression.Trainers.LightGbm());
                results.Add(EvaluateRegression("LightGBM", lightGbmPipeline, trainDataView, testDataView, fullDataView, dataList));

                // --- 4. SDCA ---
                var sdcaPipeline = featurePipeline.Append(mlContext.Regression.Trainers.Sdca());
                results.Add(EvaluateRegression("SDCA", sdcaPipeline, trainDataView, testDataView, fullDataView, dataList));

                return FormatOutput(results);
            }
            catch (Exception ex)
            {
                return $"Lỗi xử lý AI: {ex.Message}";
            }
        }

        // Parse CSV an toàn
        private static DoanhThuInput LineToObj(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;
            var cols = line.Split(',');
            if (cols.Length < 8) return null;

            try
            {
                return new DoanhThuInput
                {
                    Month = cols[0],
                    Revenue = float.Parse(cols[1], CultureInfo.InvariantCulture),
                    Revenue_Lag1 = float.Parse(cols[5], CultureInfo.InvariantCulture),
                    Revenue_Lag12 = float.Parse(cols[6], CultureInfo.InvariantCulture),
                    Month_Num = float.Parse(cols[7], CultureInfo.InvariantCulture)
                };
            }
            catch { return null; }
        }

        // Đánh giá mô hình SSA (ĐÃ SỬA LỖI THIẾU CỘT)
        private static ModelResult EvaluateSSA(IDataView trainSet, IDataView testSet, IDataView fullSet, int totalRows, int trainRows)
        {
            try
            {
                // 1. Train & Test
                // QUAN TRỌNG: Phải thêm confidenceLowerBoundColumn và confidenceUpperBoundColumn
                var pipeline = mlContext.Forecasting.ForecastBySsa(
                    outputColumnName: nameof(SsaForecast.ForecastedRevenue),
                    inputColumnName: nameof(DoanhThuInput.Revenue),
                    windowSize: 12, 
                    seriesLength: trainRows, 
                    trainSize: trainRows, 
                    horizon: 12,
                    confidenceLevel: 0.95f,
                    confidenceLowerBoundColumn: nameof(SsaForecast.LowerBoundRevenue), // Đã thêm
                    confidenceUpperBoundColumn: nameof(SsaForecast.UpperBoundRevenue)  // Đã thêm
                );

                var model = pipeline.Fit(trainSet);
                var engine = model.CreateTimeSeriesEngine<DoanhThuInput, SsaForecast>(mlContext);
                var forecast = engine.Predict(); 

                // Tính độ chính xác
                var actuals = testSet.GetColumn<float>("Revenue").ToArray();
                double totalError = 0;
                for (int i = 0; i < 12; i++)
                {
                    totalError += Math.Abs((actuals[i] - forecast.ForecastedRevenue[i]) / actuals[i]);
                }
                double accuracy = Math.Max(0, 100 * (1 - (totalError / 12)));

                // 2. Retrain Full Data (Dự báo tương lai)
                var finalPipeline = mlContext.Forecasting.ForecastBySsa(
                    outputColumnName: nameof(SsaForecast.ForecastedRevenue),
                    inputColumnName: nameof(DoanhThuInput.Revenue),
                    windowSize: 12, 
                    seriesLength: totalRows, 
                    trainSize: totalRows, 
                    horizon: 2,
                    confidenceLevel: 0.95f,
                    confidenceLowerBoundColumn: nameof(SsaForecast.LowerBoundRevenue), // Đã thêm
                    confidenceUpperBoundColumn: nameof(SsaForecast.UpperBoundRevenue)  // Đã thêm
                );

                var finalModel = finalPipeline.Fit(fullSet);
                var finalEngine = finalModel.CreateTimeSeriesEngine<DoanhThuInput, SsaForecast>(mlContext);
                var futureForecast = finalEngine.Predict();

                return new ModelResult
                {
                    ModelName = "SSA (Time Series)",
                    Accuracy = accuracy,
                    ForecastT1 = futureForecast.ForecastedRevenue[0],
                    ForecastT2 = futureForecast.ForecastedRevenue[1]
                };
            }
            catch (Exception ex)
            {
                return new ModelResult { ModelName = "SSA", Note = "Lỗi: " + ex.Message };
            }
        }

        // Đánh giá Regression (Giữ nguyên logic cũ nhưng bọc try-catch)
        // Đánh giá các mô hình Regression (FastTree, LightGBM, SDCA)
        private static ModelResult EvaluateRegression(string name, IEstimator<ITransformer> pipeline,
            IDataView trainSet, IDataView testSet, IDataView fullSet, List<DoanhThuInput> allData)
        {
            try
            {
                // 1. Train & Test
                var model = pipeline.Fit(trainSet);
                var predictions = model.Transform(testSet);

                // 2. Tính độ chính xác
                // SỬA LỖI TẠI ĐÂY: Thay "Label" bằng "Revenue" vì testSet chưa có cột Label
                var actuals = testSet.GetColumn<float>("Revenue").ToArray();
                var preds = predictions.GetColumn<float>("Score").ToArray();

                double totalError = 0;
                for (int i = 0; i < actuals.Length; i++)
                {
                    float actual = actuals[i] == 0 ? 1 : actuals[i]; // Tránh chia cho 0
                    totalError += Math.Abs((actual - preds[i]) / actual);
                }

                double accuracy = Math.Max(0, 100 * (1 - (totalError / actuals.Length)));

                // 3. Retrain Full Data (Huấn luyện lại trên toàn bộ dữ liệu)
                var finalModel = pipeline.Fit(fullSet);
                var predEngine = mlContext.Model.CreatePredictionEngine<DoanhThuInput, DoanhThuPrediction>(finalModel);

                // 4. Dự báo đệ quy cho 2 tháng tới
                // Đảm bảo list có đủ dữ liệu
                if (allData.Count < 12) return new ModelResult { ModelName = name, Note = "Dữ liệu không đủ để tính Lag" };

                var lastRow = allData.Last();
                var lastYearRow = allData[allData.Count - 12];
                var lastYearNextRow = allData[allData.Count - 11];

                // -- Dự báo Tháng 1/2026 --
                var inputT1 = new DoanhThuInput
                {
                    Revenue_Lag1 = lastRow.Revenue,
                    Revenue_Lag12 = lastYearRow.Revenue,
                    Month_Num = 1
                };
                float predT1 = predEngine.Predict(inputT1).ForecastedRevenue;

                // -- Dự báo Tháng 2/2026 --
                var inputT2 = new DoanhThuInput
                {
                    Revenue_Lag1 = predT1,   // Lấy kết quả dự báo T1 làm đầu vào (Lag 1)
                    Revenue_Lag12 = lastYearNextRow.Revenue, // Lag 12 của năm ngoái
                    Month_Num = 2
                };
                float predT2 = predEngine.Predict(inputT2).ForecastedRevenue;

                return new ModelResult
                {
                    ModelName = name,
                    Accuracy = accuracy,
                    ForecastT1 = predT1,
                    ForecastT2 = predT2
                };
            }
            catch (Exception ex)
            {
                return new ModelResult { ModelName = name, Note = $"Lỗi: {ex.Message}" };
            }
        }

        // Format kết quả
        private static string FormatOutput(List<ModelResult> results)
        {
            // Lọc ra các model chạy thành công
            var validResults = results.Where(r => r.ForecastT1 > 0).ToList();
            
            if (validResults.Count == 0) return "Không có mô hình nào chạy thành công.";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("📊 SO SÁNH HIỆU SUẤT 4 MÔ HÌNH AI:\n");

            var bestModel = validResults.OrderByDescending(r => r.Accuracy).First();

            foreach (var r in results)
            {
                if (!string.IsNullOrEmpty(r.Note)) // Nếu có lỗi
                {
                    sb.AppendLine($"❌ {r.ModelName}: {r.Note}");
                    continue;
                }

                string marker = (r == bestModel) ? "🏆 " : "   ";
                sb.AppendLine($"{marker}{r.ModelName.PadRight(15)} | Độ chính xác: {r.Accuracy:F2}%");
                sb.AppendLine($"      ➡ T1/2026: {r.ForecastT1:N0} đ");
                sb.AppendLine($"      ➡ T2/2026: {r.ForecastT2:N0} đ");
                sb.AppendLine("--------------------------------------------------");
            }

            sb.AppendLine($"\n✅ KHUYẾN NGHỊ: Sử dụng số liệu của {bestModel.ModelName}");
            sb.AppendLine($"   (Độ chính xác cao nhất trên dữ liệu kiểm thử năm 2025)");

            return sb.ToString();
        }
    }
}