using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PlantTracker.Messages;
using PlantTracker.Services;
using PlantTracker.Shared.DTOs.Garden;

namespace PlantTracker.ViewModels;

[QueryProperty(nameof(UserPlantId), "UserPlantId")]
[QueryProperty(nameof(PlantName), "PlantName")]
public partial class PlantGalleryViewModel : BaseViewModel
{
    private readonly GardenService _garden;

    [ObservableProperty] private int _userPlantId;
    [ObservableProperty] private string _plantName = string.Empty;
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private bool _isUploading;
    [ObservableProperty] private string? _coverPhotoData; // currently active cover

    public ObservableCollection<PlantPhotoDto> Photos { get; } = [];

    public PlantGalleryViewModel(GardenService garden)
    {
        _garden = garden;
        Title = "My Photos";
    }

    partial void OnUserPlantIdChanged(int value)
    {
        if (value > 0)
            LoadPhotosCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task LoadPhotosAsync()
    {
        if (IsBusy || UserPlantId == 0) return;
        IsBusy = true;
        Photos.Clear();
        IsEmpty = false;

        try
        {
            var results = await _garden.GetPhotosAsync(UserPlantId);
            foreach (var p in results)
                Photos.Add(p);

            IsEmpty = Photos.Count == 0;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        try
        {
            var photos = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
            {
                Title = "Select a photo"
            });

            var photo = photos?.FirstOrDefault();
            if (photo is null) return;

            IsUploading = true;

            await using var stream = await photo.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            if (bytes.Length > 5 * 1024 * 1024)
            {
                await Shell.Current.DisplayAlertAsync("Photo Too Large",
                    "Please choose a photo under 5 MB.", "OK");
                return;
            }

            var mimeType = string.IsNullOrEmpty(photo.ContentType) ? "image/jpeg" : photo.ContentType;
            var dataUri = $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";

            // Ask if they want to use this as the cover photo
            var setAsCover = await Shell.Current.DisplayAlertAsync(
                "Set as Cover?",
                "Would you like to use this photo as the cover for this plant in My Garden?",
                "Yes", "No");

            var dto = new PlantPhotoDto
            {
                UserPlantId = UserPlantId,
                ImageData = dataUri,
                TakenAt = DateTime.UtcNow
            };

            var (success, error) = await _garden.AddPhotoAsync(UserPlantId, dto);

            if (success)
            {
                if (setAsCover)
                    await ApplyCoverPhotoAsync(dataUri);

                await LoadPhotosAsync();
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Upload Failed", error ?? "Could not upload photo.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            IsUploading = false;
        }
    }

    [RelayCommand]
    private async Task SetAsCoverAsync(PlantPhotoDto photo)
    {
        var confirm = await Shell.Current.DisplayAlertAsync(
            "Set as Cover",
            "Use this photo as the cover for this plant in My Garden?",
            "Set", "Cancel");

        if (!confirm) return;

        await ApplyCoverPhotoAsync(photo.ImageData);
    }

    private async Task ApplyCoverPhotoAsync(string imageData)
    {
        var success = await _garden.SetCoverPhotoAsync(UserPlantId, imageData);
        if (success)
        {
            CoverPhotoData = imageData;
            WeakReferenceMessenger.Default.Send(new PlantCoverPhotoChangedMessage(UserPlantId, imageData));
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Error", "Could not update cover photo.", "OK");
        }
    }

    [RelayCommand]
    private async Task DeletePhotoAsync(PlantPhotoDto photo)
    {
        var confirm = await Shell.Current.DisplayAlertAsync(
            "Delete Photo", "Remove this photo?", "Delete", "Cancel");

        if (!confirm) return;

        var success = await _garden.DeletePhotoAsync(UserPlantId, photo.Id);
        if (success)
            Photos.Remove(photo);
    }
}
