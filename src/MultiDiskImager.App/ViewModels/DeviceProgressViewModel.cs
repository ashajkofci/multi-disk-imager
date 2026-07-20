namespace MultiDiskImager.ViewModels;

internal sealed class DeviceProgressViewModel(string deviceId) : ObservableObject
{
    private double _percentage;
    private string _details = string.Empty;

    public string DeviceId { get; } = deviceId;
    public double Percentage { get => _percentage; set => SetProperty(ref _percentage, value); }
    public string Details { get => _details; set => SetProperty(ref _details, value); }
}
