using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Reservas_Temporales.Models;
using Reservas_Temporales.Repositorios;
using System.Linq;

namespace Reservas_Temporales.Controllers
{
    // Las acciones redirigen siempre a Inmueble/Details, ya que las imágenes
    // se administran desde la ficha del inmueble, no con vistas propias.
    public class ImagenInmuebleController : ControladorBase
    {
        private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long TamanioMaximoBytes = 5 * 1024 * 1024; // 5 MB

        private readonly IRepositorioImagenInmueble _repositorioImagenInmueble;
        private readonly IWebHostEnvironment _entorno;

        public ImagenInmuebleController(
            IRepositorioImagenInmueble repositorioImagenInmueble, IWebHostEnvironment entorno)
        {
            _repositorioImagenInmueble = repositorioImagenInmueble;
            _entorno = entorno;
        }

        // Sube el archivo físico a wwwroot/uploads/inmuebles/{idInmueble}/ y guarda la ruta relativa en la BD.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(TamanioMaximoBytes)]
        public async Task<IActionResult> Create(int idInmueble, IFormFile archivo, bool esPortada = false)
        {
            if (archivo == null || archivo.Length == 0)
            {
                TempData["Error"] = "Elegí un archivo de imagen para subir.";
                return RedirectToAction("Details", "Inmueble", new { id = idInmueble });
            }

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(extension))
            {
                TempData["Error"] = "Formato no permitido. Usá JPG, PNG o WEBP.";
                return RedirectToAction("Details", "Inmueble", new { id = idInmueble });
            }

            if (archivo.Length > TamanioMaximoBytes)
            {
                TempData["Error"] = "La imagen supera el tamaño máximo permitido (5 MB).";
                return RedirectToAction("Details", "Inmueble", new { id = idInmueble });
            }

            var nombreArchivo = $"{Guid.NewGuid()}{extension}";
            var carpeta = Path.Combine(_entorno.WebRootPath, "uploads", "inmuebles", idInmueble.ToString());
            var rutaFisica = Path.Combine(carpeta, nombreArchivo);

            try
            {
                Directory.CreateDirectory(carpeta);
                using (var stream = new FileStream(rutaFisica, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }
            }
            catch (IOException)
            {
                TempData["Error"] = "No se pudo guardar la imagen en el servidor. Probá nuevamente.";
                return RedirectToAction("Details", "Inmueble", new { id = idInmueble });
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "No se pudo guardar la imagen: sin permisos de escritura en el servidor.";
                return RedirectToAction("Details", "Inmueble", new { id = idInmueble });
            }

            // Ruta relativa (web), la que se guarda en la BD y se usa en los <img src="...">.
            var urlRelativa = $"/uploads/inmuebles/{idInmueble}/{nombreArchivo}";

            await _repositorioImagenInmueble.CrearAsync(new ImagenInmueble
            {
                IdInmueble = idInmueble,
                Url = urlRelativa,
                EsPortada = esPortada
            });

            TempData["Mensaje"] = "Imagen subida correctamente.";
            return RedirectToAction("Details", "Inmueble", new { id = idInmueble });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarPortada(int id, int idInmueble)
        {
            await _repositorioImagenInmueble.MarcarComoPortadaAsync(id, idInmueble);
            TempData["Mensaje"] = "Imagen de portada actualizada.";
            return RedirectToAction("Details", "Inmueble", new { id = idInmueble });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id, int idInmueble)
        {
            // Borra también el archivo físico del disco, no solo el registro de la BD.
            var imagen = await _repositorioImagenInmueble.ObtenerPorIdAsync(id);
            if (imagen != null && imagen.Url.StartsWith("/uploads/"))
            {
                var rutaFisica = Path.Combine(
                    _entorno.WebRootPath,
                    imagen.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(rutaFisica))
                    System.IO.File.Delete(rutaFisica);
            }

            await _repositorioImagenInmueble.EliminarAsync(id);
            TempData["Mensaje"] = "Imagen eliminada.";
            return RedirectToAction("Details", "Inmueble", new { id = idInmueble });
        }
    }
}