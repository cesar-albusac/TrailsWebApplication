using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using Trails.Models;
using TrailsWebApplication.Helpers;

namespace TrailsWebApplication.Controllers
{
    public class TrailsController : Controller
    {
        private readonly IConfiguration _configuration;
        private const string Trails = "Trails";
        private string apiUrl = "";
        private readonly ILogger<TrailsController> _logger;

        public TrailsController(IConfiguration configuration,ILogger<TrailsController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            apiUrl = GetSecretFromKeyVault("trailsapiurl");
        }

        public string GetSecretFromKeyVault(string secretName)
        {
            // Retrieve the Key Vault URL from appsettings.json 
            string keyVaultUrl = _configuration["KeyVaultUrl"]; 
            var secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

            try
            {
                KeyVaultSecret secret = secretClient.GetSecretAsync(secretName).Result;
                string secretValue = secret.Value;

                System.Diagnostics.Trace.TraceInformation("secretValue is" + secretValue);
                // Now you have the secret value, you can use it as needed
                // For example, you can pass it to a view or return it as JSON

                return secretValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                // Handle exceptions
                return string.Empty;
            }
        }

        private int _nextId = 1;

        public ActionResult Index()
        {
            IEnumerable<Trail?> trails = new List<Trail?>();
            _logger.LogInformation("Index() - Getting all Trails");
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(apiUrl);
                //HTTP GET
                var responseTask = client.GetAsync(Trails);
                responseTask.Wait();

                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    var readTask = result.Content.ReadFromJsonAsync<IList<Trail>>();
                    readTask.Wait();

                    trails = readTask.Result != null? readTask.Result : new List<Trail>();
                }
                else //web api sent error response 
                {
                    _logger.LogError(string.Format("Web Api Error : Status Code {0}. Response Content:  {1}", result.StatusCode, result.Content));
                    trails = Enumerable.Empty<Trail>();
                    ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");
                }
            }
            // TODO  : REplace this code and auto generate ids 
            if (trails.Any())
            {
                _nextId = trails.Count() + 1;
            }

            return View("Index",trails);
        }

        //GET: Trails/Details/5
        public ActionResult Details(string? id)
        {
            if (id == null)
            {
                _logger.LogInformation("id is null in Details()");
                return NotFound();
            }

            Trail? trail = new Trail();
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(apiUrl);
                //HTTP GET
                var responseTask = client.GetAsync(string.Join('/', Trails, id));
                responseTask.Wait();

                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {

                    var readTask = result.Content.ReadFromJsonAsync<Trail>();
                    readTask.Wait();

                    trail = readTask.Result;
                    if (trail == null)
                    {
                        return NotFound();
                    }
                }
                else //web api sent error response 
                {
                    _logger.LogError(string.Format("Web Api Error : Status Code {0}. Response Content:  {1}", result.StatusCode, result.Content));

                    return Index();
                }
            }


            return View("Details", trail);
        }

        // GET: Trails/Create
        public IActionResult Create()
        {
            return View("Create");
        }


        // POST: Trails/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Trail trail)
        {
            if (ModelState.IsValid)
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(apiUrl);
                    List<Task> tasks = new List<Task>();
                    if (trail.GPXFile != null)
                    {
                        Task gpxTask = AzureStorageHelper.UploadFileToStorage(trail.GPXFile, trail);
                        tasks.Add(gpxTask);
                    }

                    if (trail.ImageFile != null)
                    {
                        Task imageTask = AzureStorageHelper.UploadFileToStorage(trail.ImageFile, trail);
                        tasks.Add(imageTask);
                    }

                    Task.WaitAll(tasks.ToArray());

                    // Get Next id
                    trail.Id = Guid.NewGuid().ToString();
                    //HTTP POST
                    var postTask = client.PostAsJsonAsync<Trail>(Trails, trail);
                    postTask.Wait();

                    var result = postTask.Result;
                    if (result.IsSuccessStatusCode)
                    {
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        _logger.LogError(string.Format("Web Api Error : Status Code {0}. Response Content:  {1}", result.StatusCode, result.Content));
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            return View("Create",null);
        }

        // GET: Trails/Edit/5
        public ActionResult Edit(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(apiUrl);
                //HTTP GET
                var responseTask = client.GetAsync(string.Join('/', Trails, id));
                responseTask.Wait();
                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    var readTask = result.Content.ReadFromJsonAsync<Trail>();
                    readTask.Wait();

                    var trail = readTask.Result;
                    if (trail == null)
                    {
                        return NotFound();
                    }

                    return View("Edit", trail);
                }
                else
                {
                    _logger.LogError(string.Format("Web Api Error : Status Code {0}. Response Content:  {1}", result.StatusCode, result.Content));

                }
            }
            return NotFound();
        }

        public ActionResult Delete(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Trail? trail = new Trail();
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(apiUrl);
                //HTTP GET
                var responseTask = client.GetAsync(string.Join('/', Trails, id));
                responseTask.Wait();

                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {

                    var readTask = result.Content.ReadFromJsonAsync<Trail>();
                    readTask.Wait();

                    trail = readTask.Result;
                    if (trail == null)
                    {
                        return NotFound();
                    }
                }
                else //web api sent error response 
                {
                    _logger.LogError(string.Format("Web Api Error : Status Code {0}. Response Content:  {1}", result.StatusCode, result.Content));

                    trail = new Trail();

                    ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");
                }
            }


            return View("Delete", trail);
        }

        // GET: Trails/Delete/5
        [HttpPost]
        public ActionResult Delete(Trail trail)
        {
            if (trail == null)
            {
                return NotFound();
            }
            bool deleteImage = false;
            bool deleteGpx = false;
            string? gpxUrl = null;
            string? imageUrl = null;

            using (var client = new HttpClient())
            {
                if(trail.ImageUrl != null)
                {
                    imageUrl = trail.ImageUrl;
                    deleteImage = true;
                }

                if(trail.GPXUrl != null)
                {
                    gpxUrl = trail.GPXUrl;
                    deleteGpx = true;
                }

                client.BaseAddress = new Uri(apiUrl);
                //HTTP GET
                var responseTask = client.DeleteAsync(string.Format("{0}?id={1}", Trails, trail.Id));
                responseTask.Wait();

                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    if(deleteGpx && gpxUrl != null)
                    {
                        AzureStorageHelper.DeleteFileFromStorage(gpxUrl);
                    }

                    if (deleteImage && imageUrl != null)
                    {
                        AzureStorageHelper.DeleteFileFromStorage(imageUrl);
                    }

                    var readTask = result.Content.ReadAsStream();
                    //readTask.Wait();

                    //Trail = readTask.Result;
                    if (trail == null)
                    {
                        return NotFound();
                    }
                }
                else
                {
                    _logger.LogError(string.Format("Web Api Error : Status Code {0}. Response Content:  {1}", result.StatusCode, result.Content));
                }
            }   
            return Index();
        }

       
    }
}
