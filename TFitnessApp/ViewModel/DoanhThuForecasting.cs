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
        [LoadColumn(0)] public string Month { get; set; } // Dùng để parse ngày nếu cần
        [LoadColumn(1)] public float Revenue { get; set; }      // Label (Mục tiêu)
        [LoadColumn(5)] public float Revenue_Lag1 { get; set; } // Feature
        [LoadColumn(6)] public float Revenue_Lag12 { get; set; } // Feature
        [LoadColumn(7)] public float Month_Num { get; set; }    // Feature
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
        public double Accuracy { get; set; } // Phần trăm chính xác (1 - MAPE)
        public float ForecastT1 { get; set; } // Dự báo tháng 1/2026
        public float ForecastT2 { get; set; } // Dự báo tháng 2/2026
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

                // --- GIAI ĐOẠN 1: CHUẨN BỊ DỮ LIỆU ---
                // Load dữ liệu vào List để dễ xử lý thủ công (lấy dữ liệu quá khứ cho dự báo đệ quy)
                List<DoanhThuInput> dataList = File.ReadAllLines(csvPath)
                                   .Skip(1) // Bỏ header
                                   .Select(LineToObj) // Chuyển đổi
                                   .Where(x => x != null) // Lọc bỏ dòng lỗi/rỗng
                                   .ToList();

                if (dataList.Count < 24) return "Dữ liệu quá ít để so sánh các mô hình.";

                // Chia Train/Test: Lấy 12 tháng cuối làm Test để tính độ chính xác
                int totalRows = dataList.Count;
                int testSize = 12;
                int trainSize = totalRows - testSize;

                var trainDataView = mlContext.Data.LoadFromEnumerable(dataList.Take(trainSize));
                var testDataView = mlContext.Data.LoadFromEnumerable(dataList.Skip(trainSize));
                var fullDataView = mlContext.Data.LoadFromEnumerable(dataList);

                List<ModelResult> results = new List<ModelResult>();

                // --- GIAI ĐOẠN 2: HUẤN LUYỆN & ĐÁNH GIÁ TỪNG MÔ HÌNH ---

                // 1. SSA (Time Series)
                results.Add(EvaluateSSA(trainDataView, testDataView, fullDataView, totalRows, trainSize));

                // Định nghĩa Pipeline cho Regression (FastTree, LightGBM, SDCA)
                var featurePipeline = mlContext.Transforms.CopyColumns("Label", "Revenue")
                    .Append(mlContext.Transforms.Concatenate("Features", "Revenue_Lag1", "Revenue_Lag12", "Month_Num"));

                // 2. FastTree
                var fastTreePipeline = featurePipeline.Append(mlContext.Regression.Trainers.FastTree());
                results.Add(EvaluateRegression("FastTree", fastTreePipeline, trainDataView, testDataView, fullDataView, dataList));

                // 3. LightGBM
                var lightGbmPipeline = featurePipeline.Append(mlContext.Regression.Trainers.LightGbm());
                results.Add(EvaluateRegression("LightGBM", lightGbmPipeline, trainDataView, testDataView, fullDataView, dataList));

                // 4. SDCA
                var sdcaPipeline = featurePipeline.Append(mlContext.Regression.Trainers.Sdca());
                results.Add(EvaluateRegression("SDCA", sdcaPipeline, trainDataView, testDataView, fullDataView, dataList));

                // --- GIAI ĐOẠN 3: TỔNG HỢP VÀ CHỌN TỐT NHẤT ---
                return FormatOutput(results);
            }
            catch (Exception ex)
            {
                return $"Lỗi xử lý AI: {ex.Message}";
            }
        }

        // --- CÁC HÀM HỖ TRỢ ---

        // Hàm parse dòng CSV sang Object
        private static DoanhThuInput LineToObj(string line)
        {
            // 1. Kiểm tra dòng rỗng
            if (string.IsNullOrWhiteSpace(line)) return null;

            var cols = line.Split(',');

            // 2. Kiểm tra đủ cột chưa (file bạn có 8 cột, nên index max là 7)
            if (cols.Length < 8) return null;

            // 3. Sử dụng CultureInfo.InvariantCulture để ép buộc dùng dấu chấm (.) cho số thập phân
            try
            {
                return new DoanhThuInput
                {
                    Month = cols[0],
                    // Dùng InvariantCulture để máy nào cũng hiểu dấu chấm là thập phân
                    Revenue = float.Parse(cols[1], CultureInfo.InvariantCulture),
                    Revenue_Lag1 = float.Parse(cols[5], CultureInfo.InvariantCulture),
                    Revenue_Lag12 = float.Parse(cols[6], CultureInfo.InvariantCulture),
                    Month_Num = float.Parse(cols[7], CultureInfo.InvariantCulture)
                };
            }
            catch
            {
                // Nếu dòng nào bị lỗi format thì bỏ qua luôn, không crash app
                return null;
            }
        }

        // Đánh giá mô hình SSA
        private static ModelResult EvaluateSSA(IDataView trainSet, IDataView testSet, IDataView fullSet, int totalRows, int trainRows)
        {
            // 1. Train trên tập Train & Test trên tập Test để tính độ chính xác
            var pipeline = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: nameof(SsaForecast.ForecastedRevenue),
                inputColumnName: nameof(DoanhThuInput.Revenue),
                windowSize: 12, seriesLength: trainRows, trainSize: trainRows, horizon: 12);

            var model = pipeline.Fit(trainSet);
            var engine = model.CreateTimeSeriesEngine<DoanhThuInput, SsaForecast>(mlContext);
            var forecast = engine.Predict(); // Dự báo 12 tháng của năm 2025

            // Tính MAPE (Mean Absolute Percentage Error)
            var actuals = testSet.GetColumn<float>("Revenue").ToArray();
            double totalError = 0;
            for (int i = 0; i < 12; i++)
            {
                totalError += Math.Abs((actuals[i] - forecast.ForecastedRevenue[i]) / actuals[i]);
            }
            double accuracy = Math.Max(0, 100 * (1 - (totalError / 12)));

            // 2. Retrain trên TOÀN BỘ dữ liệu để dự báo tương lai (2026)
            var finalPipeline = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: nameof(SsaForecast.ForecastedRevenue),
                inputColumnName: nameof(DoanhThuInput.Revenue),
                windowSize: 12, seriesLength: totalRows, trainSize: totalRows, horizon: 2);

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

        // Đánh giá các mô hình Regression (FastTree, LightGBM, SDCA)
        private static ModelResult EvaluateRegression(string name, IEstimator<ITransformer> pipeline,
            IDataView trainSet, IDataView testSet, IDataView fullSet, List<DoanhThuInput> allData)
        {
            // 1. Train & Test
            var model = pipeline.Fit(trainSet);
            var predictions = model.Transform(testSet);
            var metrics = mlContext.Regression.Evaluate(predictions, "Label", "Score");

            // Tính độ chính xác từ MAE (Mean Absolute Error) hoặc MAPE tương tự
            // Ở đây dùng công thức xấp xỉ từ MeanSquaredError để đơn giản hoặc dùng metric chuẩn của Regression
            // Cách tính chuẩn MAPE cho Regression:
            var actuals = testSet.GetColumn<float>("Label").ToArray();
            var preds = predictions.GetColumn<float>("Score").ToArray();
            double totalError = 0;
            for (int i = 0; i < actuals.Length; i++)
                totalError += Math.Abs((actuals[i] - preds[i]) / actuals[i]);

            double accuracy = Math.Max(0, 100 * (1 - (totalError / actuals.Length)));

            // 2. Retrain Full Data
            var finalModel = pipeline.Fit(fullSet);
            var predEngine = mlContext.Model.CreatePredictionEngine<DoanhThuInput, DoanhThuPrediction>(finalModel);

            // 3. Dự báo đệ quy (Recursive Forecasting) cho 2 tháng tới
            // Lấy dữ liệu tháng cuối cùng thực tế (T12/2025)
            var lastMonth = allData.Last();
            var t12_2025_Revenue = lastMonth.Revenue;

            // Lấy dữ liệu cùng kỳ năm ngoái (T1/2025 và T2/2025)
            var t1_2025 = allData[allData.Count - 12].Revenue;
            var t2_2025 = allData[allData.Count - 11].Revenue;

            // -- Dự báo Tháng 1/2026 --
            var inputT1 = new DoanhThuInput
            {
                Revenue_Lag1 = t12_2025_Revenue, // Của tháng trước
                Revenue_Lag12 = t1_2025,         // Của năm ngoái
                Month_Num = 1
            };
            float predT1 = predEngine.Predict(inputT1).ForecastedRevenue;

            // -- Dự báo Tháng 2/2026 --
            var inputT2 = new DoanhThuInput
            {
                Revenue_Lag1 = predT1,   // Lấy kết quả dự báo T1 làm đầu vào
                Revenue_Lag12 = t2_2025, // Của năm ngoái
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

        // Định dạng kết quả hiển thị ra màn hình
        private static string FormatOutput(List<ModelResult> results)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("📊 SO SÁNH HIỆU SUẤT 4 MÔ HÌNH AI:\n");

            // Tìm mô hình tốt nhất cho từng tháng (thực tế thường chọn model có Accuracy cao nhất chung)
            var bestModel = results.OrderByDescending(r => r.Accuracy).First();

            foreach (var r in results)
            {
                string marker = (r == bestModel) ? "🏆 " : "   ";
                sb.AppendLine($"{marker}{r.ModelName.PadRight(15)} | Độ chính xác: {r.Accuracy:F2}%");
                sb.AppendLine($"      ➡ T1/2026: {r.ForecastT1:N0} đ");
                sb.AppendLine($"      ➡ T2/2026: {r.ForecastT2:N0} đ");
                sb.AppendLine("--------------------------------------------------");
            }

            sb.AppendLine($"\n✅ KHUYẾN NGHỊ: Sử dụng số liệu của {bestModel.ModelName}");
            sb.AppendLine($"   (Vì có độ chính xác cao nhất trên dữ liệu kiểm thử năm 2025)");

            return sb.ToString();
        }
    }
}