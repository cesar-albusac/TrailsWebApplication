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
        private string apiUrl = "";

        public TrailsController(IConfiguration configuration)
        {
            _configuration = configuration;
            apiUrl = GetSecretFromKeyVault("trailsapiurl");

        }

        public string GetSecretFromKeyVault(string secretName)
        {
            string keyVaultUrl = _configuration["KeyVaultUrl"]; // Retrieve the Key Vault URL from appsettings.json or configuration
            keyVaultUrl = "https://hikingtrailskeyvault.vault.azure.net/";

            var secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

            try
            {
                KeyVaultSecret secret = secretClient.GetSecretAsync(secretName).Result;
                string secretValue = secret.Value;

                // Now you have the secret value, you can use it as needed
                // For example, you can pass it to a view or return it as JSON

                return secretValue;
            }
            catch (Exception ex)
            {
                // Handle exceptions
                return null;
            }
        }

        private int _nextId = 1;


        // GET: Student
        public ActionResult Index()
        {
            IEnumerable<Trail?> trails = new List<Trail?>();

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(apiUrl);
                //HTTP GET
                var responseTask = client.GetAsync("Trails");
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
                    //log response status here..

                    trails = Enumerable.Empty<Trail>();

                    ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");
                }
            }
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
                return NotFound();
            }

            Trail? trail = new Trail();
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(apiUrl);
                //HTTP GET
                var responseTask = client.GetAsync("Trails/" + id);
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
                    //log response status here..

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
                    var postTask = client.PostAsJsonAsync<Trail>("Trails", trail);
                    postTask.Wait();

                    var result = postTask.Result;
                    if (result.IsSuccessStatusCode)
                    {
                        return RedirectToAction("Index");
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
                var responseTask = client.GetAsync("Trails/" + id);
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
                var responseTask = client.GetAsync("Trails/" + id);
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
                    //log response status here..

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
                var responseTask = client.DeleteAsync("Trails?id=" + trail.Id);
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
            }   
            return Index();
        }

       
    }
}
