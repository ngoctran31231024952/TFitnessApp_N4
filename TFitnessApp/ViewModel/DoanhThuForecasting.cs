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
    // --- CÁC CLASS DỮ LIỆU ---
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
        public float ForecastT1 { get; set; } // Tháng 1/2026
        public float ForecastT2 { get; set; } // Tháng 2/2026
        public string Note { get; set; }
    }

    public class ForecastReport
    {
        public string LogText { get; set; } // Dùng báo lỗi chung
        public string BestModelName { get; set; }
        public float ForecastT1 { get; set; }
        public float ForecastT2 { get; set; }
        public List<double> HistoryData { get; set; }
        public List<double> ForecastData { get; set; }
        public string[] Labels { get; set; }

        // QUAN TRỌNG: Trả về danh sách kết quả để giao diện tô màu
        public List<ModelResult> ModelResults { get; set; }
    }

    public class DoanhThuPredictor
    {
        private static MLContext mlContext = new MLContext(seed: 1);

        public static ForecastReport TrainAndPredict(string csvPath)
        {
            var report = new ForecastReport
            {
                HistoryData = new List<double>(),
                ForecastData = new List<double>(),
                ModelResults = new List<ModelResult>()
            };

            try
            {
                if (!File.Exists(csvPath)) { report.LogText = "Lỗi: Không tìm thấy file datahuanluyen.csv"; return report; }

                // 1. Load dữ liệu
                List<DoanhThuInput> dataList = File.ReadAllLines(csvPath)
                                                   .Skip(1)
                                                   .Select(LineToObj)
                                                   .Where(x => x != null)
                                                   .ToList();

                if (dataList.Count < 24) { report.LogText = "Dữ liệu quá ít để chạy mô hình."; return report; }

                // Lấy lịch sử vẽ biểu đồ (12 tháng cuối)
                var history2025 = dataList.Skip(dataList.Count - 12).Take(12).ToList();
                report.HistoryData = history2025.Select(x => (double)x.Revenue).ToList();

                var labels = new List<string>();
                foreach (var item in history2025) labels.Add(item.Month);
                labels.Add("T1/2026");
                labels.Add("T2/2026");
                report.Labels = labels.ToArray();

                // 2. Chia Train/Test
                int totalRows = dataList.Count;
                int testSize = 12;
                int trainSize = totalRows - testSize;

                var trainDataView = mlContext.Data.LoadFromEnumerable(dataList.Take(trainSize));
                var testDataView = mlContext.Data.LoadFromEnumerable(dataList.Skip(trainSize));
                var fullDataView = mlContext.Data.LoadFromEnumerable(dataList);

                // 3. Huấn luyện 4 mô hình
                var results = new List<ModelResult>();

                results.Add(EvaluateSSA(trainDataView, testDataView, fullDataView, totalRows, trainSize));

                var featurePipeline = mlContext.Transforms.CopyColumns("Label", "Revenue")
                    .Append(mlContext.Transforms.Concatenate("Features", "Revenue_Lag1", "Revenue_Lag12", "Month_Num"));

                results.Add(EvaluateRegression("FastTree", featurePipeline.Append(mlContext.Regression.Trainers.FastTree()), trainDataView, testDataView, fullDataView, dataList));
                results.Add(EvaluateRegression("LightGBM", featurePipeline.Append(mlContext.Regression.Trainers.LightGbm()), trainDataView, testDataView, fullDataView, dataList));
                //results.Add(EvaluateRegression("SDCA", featurePipeline.Append(mlContext.Regression.Trainers.Sdca()), trainDataView, testDataView, fullDataView, dataList));

                report.ModelResults = results;

                // 4. Chọn model tốt nhất
                var validResults = results.Where(r => r.ForecastT1 > 0).ToList();
                if (validResults.Count > 0)
                {
                    var bestModel = validResults.OrderByDescending(r => r.Accuracy).First();
                    report.BestModelName = bestModel.ModelName;
                    report.ForecastT1 = bestModel.ForecastT1;
                    report.ForecastT2 = bestModel.ForecastT2;

                    // Dữ liệu vẽ biểu đồ nối tiếp
                    double lastRealValue = report.HistoryData.Last();
                    report.ForecastData.Add(lastRealValue);
                    report.ForecastData.Add(bestModel.ForecastT1);
                    report.ForecastData.Add(bestModel.ForecastT2);
                }
                else
                {
                    report.LogText = "Không có mô hình nào chạy thành công.";
                }

                return report;
            }
            catch (Exception ex)
            {
                report.LogText = $"Lỗi hệ thống AI: {ex.Message}";
                return report;
            }
        }

        // --- CÁC HÀM PHỤ TRỢ ---
        private static DoanhThuInput LineToObj(string line) { if (string.IsNullOrWhiteSpace(line)) return null; var cols = line.Split(','); if (cols.Length < 8) return null; try { return new DoanhThuInput { Month = cols[0], Revenue = float.Parse(cols[1], CultureInfo.InvariantCulture), Revenue_Lag1 = float.Parse(cols[5], CultureInfo.InvariantCulture), Revenue_Lag12 = float.Parse(cols[6], CultureInfo.InvariantCulture), Month_Num = float.Parse(cols[7], CultureInfo.InvariantCulture) }; } catch { return null; } }

        private static ModelResult EvaluateSSA(IDataView trainSet, IDataView testSet, IDataView fullSet, int totalRows, int trainRows) { try { var pipeline = mlContext.Forecasting.ForecastBySsa(nameof(SsaForecast.ForecastedRevenue), nameof(DoanhThuInput.Revenue), windowSize: 12, seriesLength: trainRows, trainSize: trainRows, horizon: 12, confidenceLevel: 0.95f, confidenceLowerBoundColumn: nameof(SsaForecast.LowerBoundRevenue), confidenceUpperBoundColumn: nameof(SsaForecast.UpperBoundRevenue)); var model = pipeline.Fit(trainSet); var forecast = model.CreateTimeSeriesEngine<DoanhThuInput, SsaForecast>(mlContext).Predict(); var actuals = testSet.GetColumn<float>("Revenue").ToArray(); double totalError = 0; for (int i = 0; i < 12; i++) totalError += Math.Abs((actuals[i] - forecast.ForecastedRevenue[i]) / actuals[i]); var finalPipeline = mlContext.Forecasting.ForecastBySsa(nameof(SsaForecast.ForecastedRevenue), nameof(DoanhThuInput.Revenue), windowSize: 12, seriesLength: totalRows, trainSize: totalRows, horizon: 2, confidenceLevel: 0.95f, confidenceLowerBoundColumn: nameof(SsaForecast.LowerBoundRevenue), confidenceUpperBoundColumn: nameof(SsaForecast.UpperBoundRevenue)); var futureForecast = finalPipeline.Fit(fullSet).CreateTimeSeriesEngine<DoanhThuInput, SsaForecast>(mlContext).Predict(); return new ModelResult { ModelName = "SSA (Time Series)", Accuracy = Math.Max(0, 100 * (1 - (totalError / 12))), ForecastT1 = futureForecast.ForecastedRevenue[0], ForecastT2 = futureForecast.ForecastedRevenue[1] }; } catch (Exception ex) { return new ModelResult { ModelName = "SSA", Note = ex.Message }; } }

        private static ModelResult EvaluateRegression(string name, IEstimator<ITransformer> pipeline, IDataView trainSet, IDataView testSet, IDataView fullSet, List<DoanhThuInput> allData) { try { var model = pipeline.Fit(trainSet); var preds = model.Transform(testSet).GetColumn<float>("Score").ToArray(); var actuals = testSet.GetColumn<float>("Revenue").ToArray(); double totalError = 0; for (int i = 0; i < actuals.Length; i++) { float act = actuals[i] == 0 ? 1 : actuals[i]; totalError += Math.Abs((act - preds[i]) / act); } var predEngine = mlContext.Model.CreatePredictionEngine<DoanhThuInput, DoanhThuPrediction>(pipeline.Fit(fullSet)); var lastRow = allData.Last(); var lastYearRow = allData[allData.Count - 12]; var lastYearNextRow = allData[allData.Count - 11]; float t1 = predEngine.Predict(new DoanhThuInput { Revenue_Lag1 = lastRow.Revenue, Revenue_Lag12 = lastYearRow.Revenue, Month_Num = 1 }).ForecastedRevenue; float t2 = predEngine.Predict(new DoanhThuInput { Revenue_Lag1 = t1, Revenue_Lag12 = lastYearNextRow.Revenue, Month_Num = 2 }).ForecastedRevenue; return new ModelResult { ModelName = name, Accuracy = Math.Max(0, 100 * (1 - (totalError / actuals.Length))), ForecastT1 = t1, ForecastT2 = t2 }; } catch (Exception ex) { return new ModelResult { ModelName = name, Note = ex.Message }; } }

        // Hàm này không dùng nữa vì giao diện sẽ tự format
        private static string FormatOutput(List<ModelResult> results, ModelResult bestModel) { return ""; }
    }
}