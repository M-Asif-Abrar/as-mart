using System;

namespace AsMart.Web.Services
{
    public interface IColorService
    {
        string GetRandomColor(params string[] excludedColors);
        string GetRandomButtonColor(params string[] excludedColors);
        string GetProgressBarColor(int popularity);
    }
}
