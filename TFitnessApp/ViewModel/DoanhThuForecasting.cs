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
    // --- CÁC CLASS DATA ---
    public class DoanhThuInput
    {
        [LoadColumn(0)] public string Month { get; set; }
        [LoadColumn(1)] public float Revenue { get; set; }
        [LoadColumn(5)] public float Revenue_Lag1 { get; set; }
        [LoadColumn(6)] public float Revenue_Lag12 { get; set; }
        [LoadColumn(7)] public float Month_Num { get; set; }
    }

    public class DoanhThuPrediction { [ColumnName("Score")] public float ForecastedRevenue { get; set; } }

    public class SsaForecast
    {
        public float[] ForecastedRevenue { get; set; }
        public float[] LowerBoundRevenue { get; set; }
        public float[] UpperBoundRevenue { get; set; }
    }

    public class ModelResult
    {
        public string ModelName { get; set; }
        public double Accuracy { get; set; }
        public float ForecastT1 { get; set; }
        public float ForecastT2 { get; set; }
        public string Note { get; set; }
    }

    // --- CLASS KẾT QUẢ TRẢ VỀ (MỚI) ---
    public class ForecastReport
    {
        public string LogText { get; set; }                 // Nội dung chữ hiển thị
        public List<double> HistoryData { get; set; }       // Dữ liệu 12 tháng năm 2025
        public List<double> ForecastData { get; set; }      // Dữ liệu nối tiếp (T12/25 -> T1/26 -> T2/26)
        public string[] Labels { get; set; }                // Nhãn trục hoành
    }

    public class DoanhThuPredictor
    {
        private static MLContext mlContext = new MLContext(seed: 1);

        public static ForecastReport TrainAndPredict(string csvPath)
        {
            var report = new ForecastReport { HistoryData = new List<double>(), ForecastData = new List<double>() };

            try
            {
                if (!File.Exists(csvPath))
                {
                    report.LogText = "Lỗi: Không tìm thấy file dữ liệu.";
                    return report;
                }

                // Load dữ liệu
                List<DoanhThuInput> dataList = File.ReadAllLines(csvPath)
                                                   .Skip(1)
                                                   .Select(LineToObj)
                                                   .Where(x => x != null)
                                                   .ToList();

                if (dataList.Count < 24)
                {
                    report.LogText = "Dữ liệu quá ít để chạy mô hình.";
                    return report;
                }

                // 1. Lấy dữ liệu lịch sử (12 tháng cuối cùng trong file - Năm 2025)
                var history2025 = dataList.Skip(dataList.Count - 12).Take(12).ToList();
                report.HistoryData = history2025.Select(x => (double)x.Revenue).ToList();

                // Tạo nhãn thời gian (12 tháng cũ + 2 tháng mới)
                var labels = new List<string>();
                foreach (var item in history2025) labels.Add(item.Month); // Ví dụ: 2025-01...
                labels.Add("Dự báo T1");
                labels.Add("Dự báo T2");
                report.Labels = labels.ToArray();

                // 2. Chạy huấn luyện các mô hình
                int totalRows = dataList.Count;
                int testSize = 12;
                int trainSize = totalRows - testSize;

                var trainDataView = mlContext.Data.LoadFromEnumerable(dataList.Take(trainSize));
                var testDataView = mlContext.Data.LoadFromEnumerable(dataList.Skip(trainSize));
                var fullDataView = mlContext.Data.LoadFromEnumerable(dataList);

                List<ModelResult> results = new List<ModelResult>();

                results.Add(EvaluateSSA(trainDataView, testDataView, fullDataView, totalRows, trainSize));

                var featurePipeline = mlContext.Transforms.CopyColumns("Label", "Revenue")
                    .Append(mlContext.Transforms.Concatenate("Features", "Revenue_Lag1", "Revenue_Lag12", "Month_Num"));

                results.Add(EvaluateRegression("FastTree", featurePipeline.Append(mlContext.Regression.Trainers.FastTree()), trainDataView, testDataView, fullDataView, dataList));
                results.Add(EvaluateRegression("LightGBM", featurePipeline.Append(mlContext.Regression.Trainers.LightGbm()), trainDataView, testDataView, fullDataView, dataList));
                results.Add(EvaluateRegression("SDCA", featurePipeline.Append(mlContext.Regression.Trainers.Sdca()), trainDataView, testDataView, fullDataView, dataList));

                // 3. Chọn mô hình tốt nhất
                var validResults = results.Where(r => r.ForecastT1 > 0).ToList();
                if (validResults.Count > 0)
                {
                    var bestModel = validResults.OrderByDescending(r => r.Accuracy).First();
                    report.LogText = FormatOutput(results, bestModel);

                    // Chuẩn bị dữ liệu vẽ biểu đồ dự báo (Nối từ điểm cuối cùng của lịch sử)
                    // Logic: [null, ..., null, GiáTrịT12_2025, DuBaoT1, DuBaoT2]
                    // Để vẽ nối liền mạch, ta cần điểm bắt đầu là tháng 12/2025
                    double lastRealValue = report.HistoryData.Last();

                    report.ForecastData.Add(lastRealValue);      // Điểm neo (T12/2025)
                    report.ForecastData.Add(bestModel.ForecastT1); // T1/2026
                    report.ForecastData.Add(bestModel.ForecastT2); // T2/2026
                }
                else
                {
                    report.LogText = FormatOutput(results, null);
                }

                return report;
            }
            catch (Exception ex)
            {
                report.LogText = $"Lỗi xử lý AI: {ex.Message}";
                return report;
            }
        }

        // --- CÁC HÀM PHỤ TRỢ (GIỮ NGUYÊN LOGIC CŨ, CHỈ RÚT GỌN CHO GỌN) ---
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

        private static ModelResult EvaluateSSA(IDataView trainSet, IDataView testSet, IDataView fullSet, int totalRows, int trainRows)
        {
            try
            {
                var pipeline = mlContext.Forecasting.ForecastBySsa(nameof(SsaForecast.ForecastedRevenue), nameof(DoanhThuInput.Revenue),
                    windowSize: 12, seriesLength: trainRows, trainSize: trainRows, horizon: 12,
                    confidenceLevel: 0.95f, confidenceLowerBoundColumn: nameof(SsaForecast.LowerBoundRevenue), confidenceUpperBoundColumn: nameof(SsaForecast.UpperBoundRevenue));

                var model = pipeline.Fit(trainSet);
                var forecast = model.CreateTimeSeriesEngine<DoanhThuInput, SsaForecast>(mlContext).Predict();

                var actuals = testSet.GetColumn<float>("Revenue").ToArray();
                double totalError = 0;
                for (int i = 0; i < 12; i++) totalError += Math.Abs((actuals[i] - forecast.ForecastedRevenue[i]) / actuals[i]);

                // Retrain
                var finalPipeline = mlContext.Forecasting.ForecastBySsa(nameof(SsaForecast.ForecastedRevenue), nameof(DoanhThuInput.Revenue),
                    windowSize: 12, seriesLength: totalRows, trainSize: totalRows, horizon: 2,
                    confidenceLevel: 0.95f, confidenceLowerBoundColumn: nameof(SsaForecast.LowerBoundRevenue), confidenceUpperBoundColumn: nameof(SsaForecast.UpperBoundRevenue));
                var futureForecast = finalPipeline.Fit(fullSet).CreateTimeSeriesEngine<DoanhThuInput, SsaForecast>(mlContext).Predict();

                return new ModelResult { ModelName = "SSA (Time Series)", Accuracy = Math.Max(0, 100 * (1 - (totalError / 12))), ForecastT1 = futureForecast.ForecastedRevenue[0], ForecastT2 = futureForecast.ForecastedRevenue[1] };
            }
            catch (Exception ex) { return new ModelResult { ModelName = "SSA", Note = ex.Message }; }
        }

        private static ModelResult EvaluateRegression(string name, IEstimator<ITransformer> pipeline, IDataView trainSet, IDataView testSet, IDataView fullSet, List<DoanhThuInput> allData)
        {
            try
            {
                var model = pipeline.Fit(trainSet);
                var preds = model.Transform(testSet).GetColumn<float>("Score").ToArray();
                var actuals = testSet.GetColumn<float>("Revenue").ToArray();

                double totalError = 0;
                for (int i = 0; i < actuals.Length; i++) totalError += Math.Abs(((actuals[i] == 0 ? 1 : actuals[i]) - preds[i]) / (actuals[i] == 0 ? 1 : actuals[i]));

                var predEngine = mlContext.Model.CreatePredictionEngine<DoanhThuInput, DoanhThuPrediction>(pipeline.Fit(fullSet));
                var lastRow = allData.Last();
                var lastYearRow = allData[allData.Count - 12];
                var lastYearNextRow = allData[allData.Count - 11];

                float predT1 = predEngine.Predict(new DoanhThuInput { Revenue_Lag1 = lastRow.Revenue, Revenue_Lag12 = lastYearRow.Revenue, Month_Num = 1 }).ForecastedRevenue;
                float predT2 = predEngine.Predict(new DoanhThuInput { Revenue_Lag1 = predT1, Revenue_Lag12 = lastYearNextRow.Revenue, Month_Num = 2 }).ForecastedRevenue;

                return new ModelResult { ModelName = name, Accuracy = Math.Max(0, 100 * (1 - (totalError / actuals.Length))), ForecastT1 = predT1, ForecastT2 = predT2 };
            }
            catch (Exception ex) { return new ModelResult { ModelName = name, Note = ex.Message }; }
        }

        private static string FormatOutput(List<ModelResult> results, ModelResult bestModel)
        {
            if (bestModel == null) return "Không có mô hình nào chạy thành công.";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("📊 KẾT QUẢ SO SÁNH:");
            foreach (var r in results)
            {
                if (!string.IsNullOrEmpty(r.Note)) { sb.AppendLine($"❌ {r.ModelName}: {r.Note}"); continue; }
                string marker = (r == bestModel) ? "🏆" : "  ";
                sb.AppendLine($"{marker} {r.ModelName}: {r.Accuracy:F2}% (T1: {r.ForecastT1 / 1000000:F1}M, T2: {r.ForecastT2 / 1000000:F1}M)");
            }
            return sb.ToString();
        }
    }
}