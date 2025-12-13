using System.Windows.Input;
using System.Windows.Media;
using BloxCord.Client.Models;

namespace BloxCord.Client.ViewModels;

public class BannerViewModel
{
    private readonly BannerDto _dto;

    public BannerViewModel(BannerDto dto)
    {
        _dto = dto;
        OpenCtaCommand = new RelayCommand(_ =>
        {
            if (!string.IsNullOrEmpty(_dto.Cta?.Url))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = _dto.Cta.Url,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        });
    }

    public string Title => _dto.Title;
    public string Message => _dto.Message;
    public bool IsDismissible => _dto.Dismissible;
    
    public bool HasCta => _dto.Cta?.Enabled == true;
    public string CtaText => _dto.Cta?.Text ?? string.Empty;

    public Brush Background => GetBrush(_dto.Colors?.Background);
    public Brush Border => GetBrush(_dto.Colors?.Border);
    public Brush TitleColor => GetBrush(_dto.Colors?.Title);
    public Brush MessageColor => GetBrush(_dto.Colors?.Message);
    public Brush ButtonBackground => GetBrush(_dto.Colors?.ButtonBackground);
    public Brush ButtonText => GetBrush(_dto.Colors?.ButtonText);

    public ICommand OpenCtaCommand { get; }

    private Brush GetBrush(string? hex)
    {
        if (string.IsNullOrEmpty(hex)) return Brushes.Transparent;
        try
        {
            return (Brush)new BrushConverter().ConvertFromString(hex)!;
        }
        catch
        {
            return Brushes.Transparent;
        }
    }
}
