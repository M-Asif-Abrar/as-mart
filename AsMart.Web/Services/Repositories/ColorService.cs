using System;
using System.Linq;


namespace AsMart.Web.Services
{
    public class ColorService : IColorService
    {
        private static readonly string[] Colors =
        {
            "bg-danger", "bg-secondary", "bg-success", "bg-info", "bg-primary", "bg-warning"
        };

        private static readonly string[] ButtonColors =
        {
            "btn-danger", "btn-secondary", "btn-success", "btn-info", "btn-primary", "btn-warning"
        };

        private readonly Random _random;
        private string? _lastColor;
        private string? _lastButtonColor;

        public ColorService()
        {
            _random = new Random();
        }

        public string GetRandomColor(params string[] excludedColors)
        {
            var availableColors = Colors
                .Except(excludedColors ?? Array.Empty<string>())
                .Where(color => color != _lastColor)
                .ToList();

            if (!availableColors.Any())
            {
                throw new InvalidOperationException("No colors available after exclusions.");
            }

            var newColor = availableColors[_random.Next(availableColors.Count)];
            _lastColor = newColor;
            return newColor;
        }

        public string GetRandomButtonColor(params string[] excludedColors)
        {
            var availableButtonColors = ButtonColors
                .Except(excludedColors ?? Array.Empty<string>())
                .Where(color => color != _lastButtonColor)
                .ToList();

            if (!availableButtonColors.Any())
            {
                throw new InvalidOperationException("No button colors available after exclusions.");
            }

            var newButtonColor = availableButtonColors[_random.Next(availableButtonColors.Count)];
            _lastButtonColor = newButtonColor;
            return newButtonColor;
        }

        public string GetProgressBarColor(int popularity)
        {
            if (popularity >= 80)
                return "bg-success"; // Green
            else if (popularity >= 50)
                return "bg-info";    // Blue
            else if (popularity >= 30)
                return "bg-warning"; // Yellow
            else
                return "bg-danger";  // Red
        }
    }
}
