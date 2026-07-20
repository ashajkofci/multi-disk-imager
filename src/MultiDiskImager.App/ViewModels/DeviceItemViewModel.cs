using MultiDiskImager.Core;
using MultiDiskImager.Localization;

namespace MultiDiskImager.ViewModels;

internal sealed class DeviceItemViewModel(DeviceDescriptor device) : ObservableObject
{
    private bool _isSelected;

    public DeviceDescriptor Device { get; } = device;
    public string Id => Device.Id;
    public string Model => Device.Model;
    public string Size => ByteSize.Format(Device.Size);
    public string Details => $"{Device.BusType} • {Device.LogicalSectorSize}-{Localizer.Get("SectorsLabel")}" +
                             (Device.MountPoints.Count > 0 ? $" • {string.Join(", ", Device.MountPoints)}" : string.Empty);
    public bool IsReadOnly => Device.IsReadOnly;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
