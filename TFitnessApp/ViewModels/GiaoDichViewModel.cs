using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Collections.Generic;

using TFitnessApp.Entities;
using TFitnessApp.Interfaces;
using TFitnessApp.Repositories;

namespace TFitnessApp.ViewModels
{
    // Kế thừa từ BaseViewModel để có INotifyPropertyChanged
    public class GiaoDichViewModel : BaseViewModel
    {
        private readonly IGiaoDichRepository _giaoDichRepository;

        // --- Backing Fields và Properties ---
        private ObservableCollection<GiaoDich> _transactions;
        private GiaoDich _selectedTransaction;
        private string _searchText; // Bổ sung cho SearchTextBox

        public ObservableCollection<GiaoDich> Transactions
        {
            get => _transactions;
            set => SetProperty(ref _transactions, value);
        }

        public GiaoDich SelectedTransaction
        {
            get => _selectedTransaction;
            set
            {
                // Cập nhật thuộc tính SelectedTransaction
                if (SetProperty(ref _selectedTransaction, value))
                {
                    // Logic cập nhật Command CanExecute nếu cần
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
            // Có thể thêm logic Filter/Search ở đây sau khi SetProperty
        }

        // --- Commands ---
        public ICommand RefreshCommand { get; }
        public ICommand AddTransactionCommand { get; }
        public ICommand DeleteTransactionCommand { get; }
        public ICommand ExportTransactionCommand { get; }
        public ICommand ViewTransactionCommand { get; }
        public ICommand SearchCommand { get; } // Bổ sung cho nút Search

        public GiaoDichViewModel()
        {
            _giaoDichRepository = new GiaoDichRepository();
            Transactions = new ObservableCollection<GiaoDich>();

            // Khởi tạo Commands
            RefreshCommand = new RelayCommand(async () => await LoadGiaoDichAsync());
            AddTransactionCommand = new RelayCommand(() => MessageBox.Show("Chức năng Tạo Giao Dịch (Cần mở Window1)"));

            // Lệnh Search (ví dụ)
            SearchCommand = new RelayCommand(() => MessageBox.Show($"Tìm kiếm: {SearchText}"));

            // Commands cho Xóa/Xuất/Xem (giữ nguyên logic demo của bạn)
            DeleteTransactionCommand = new RelayCommand(
                () => MessageBox.Show($"Xóa Giao Dịch: {SelectedTransaction?.MaGD}", "Xóa", MessageBoxButton.OK, MessageBoxImage.Warning),
                () => SelectedTransaction != null);

            ExportTransactionCommand = new RelayCommand(
                () => MessageBox.Show("Xuất danh sách giao dịch ra Excel/CSV.", "Xuất", MessageBoxButton.OK, MessageBoxImage.Information));

            ViewTransactionCommand = new RelayCommand((param) => ViewTransactionDetails(param as GiaoDich));
        }

        /// <summary>
        /// Tải dữ liệu giao dịch từ Repository và gán cho ObservableCollection.
        /// </summary>
        public async Task LoadGiaoDichAsync()
        {
            try
            {
                // Gọi Repository (Model) để lấy dữ liệu trên Thread nền
                List<GiaoDich> giaoDichList = await Task.Run(() => _giaoDichRepository.GetAll());

                // Cập nhật ObservableCollection trên luồng UI chính
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Transactions.Clear();
                    foreach (var item in giaoDichList)
                    {
                        Transactions.Add(item);
                    }
                });
            }
            catch (Exception ex)
            {
                // Lỗi này chủ yếu bắt lỗi khi Task.Run thất bại hoặc Dispatcher có vấn đề
                MessageBox.Show($"Lỗi khi tải dữ liệu giao dịch (ViewModel): {ex.Message}", "Lỗi Tải Dữ Liệu", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewTransactionDetails(GiaoDich giaoDich)
        {
            if (giaoDich != null)
            {
                MessageBox.Show($"Xem chi tiết Giao dịch: {giaoDich.MaGD}", "Chi Tiết Giao Dịch", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}