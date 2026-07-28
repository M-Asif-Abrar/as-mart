using AsMart.Web.Models.ViewModels.Gallery;
using AsMart.Web.Services.Gallery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsMart.Web.Controllers
{
    [Route("gallery")]
    public sealed class GalleryController : Controller
    {
        private readonly IGalleryService _galleryService;
        private readonly ILogger<GalleryController> _logger;

        public GalleryController(
            IGalleryService galleryService,
            ILogger<GalleryController> logger)
        {
            _galleryService = galleryService;
            _logger = logger;
        }

        /// <summary>
        /// Displays all product main and additional images as
        /// individual Gallery table records.
        ///
        /// URL:
        /// /gallery
        /// </summary>
        [HttpGet("")]
        [HttpGet("index")]
        public async Task<IActionResult> Index(
            [FromQuery] GalleryQueryViewModel query,
            CancellationToken cancellationToken)
        {
            try
            {
                var viewModel =
                    await _galleryService.GetGalleryAsync(
                        query,
                        cancellationToken);

                return View(viewModel);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                /*
                 * Request was cancelled by the browser/client.
                 * Let ASP.NET Core handle it instead of logging it as
                 * an application failure.
                 */
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while loading the product Gallery.");

                TempData["ErrorMessage"] =
                    "The Gallery could not be loaded. Please try again.";

                return View(
                    new GalleryIndexViewModel
                    {
                        Query = query
                    });
            }
        }
    }
}