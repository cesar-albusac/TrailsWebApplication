using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Mvc;
using Trails.Data;
using Trails.Models;
using TrailsWebApplication.Helpers;

namespace TrailsWebApplication.Controllers
{
    public class TrailsController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ITrailRepository _TrailRepository;
        private const string Trails = "Trails";
        private string apiUrl = "";
        private string blobConnectionString = "";
        private readonly ILogger<TrailsController> _logger;

        public TrailsController(IConfiguration configuration, ITrailRepository TrailRepository, ILogger<TrailsController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _TrailRepository = TrailRepository;
            apiUrl = KeyVaultSecrets.Instance.ApiUrl;
            blobConnectionString = KeyVaultSecrets.Instance.BlobConnectionString;
        }

        public string GetSecretFromKeyVault(string secretName)
        {
            string keyVaultUrl = _configuration["KeyVaultUrl"]; 
            var secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

            try
            {
                KeyVaultSecret secret = secretClient.GetSecretAsync(secretName).Result;
                return secret.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return string.Empty;
            }
        }

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

            return View("Index",trails);
        }

        //GET: Trails/Details/sdfsdf-234234-1212kf
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
        [Route("Trails/Create")]
        public IActionResult Create()
        {
            return View("Create");
        }


        // POST: Trails/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Route("Trails/Create")]
        [ValidateAntiForgeryToken]
        public ActionResult Create([FromForm]Trail trail)
        {
            if (ModelState.IsValid)
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(apiUrl);
                    List<Task> tasks = new List<Task>();
                    if (trail.GPXFile != null)
                    {
                        Task gpxTask = AzureStorageHelper.UploadFileToStorage(trail.GPXFile, trail, blobConnectionString, "trails");
                        tasks.Add(gpxTask);
                    }

                    if (trail.ImageFile != null)
                    {
                        Task imageTask = AzureStorageHelper.UploadFileToStorage(trail.ImageFile, trail, blobConnectionString, "images");
                        tasks.Add(imageTask);
                    }

                    Task.WaitAll(tasks.ToArray());

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
        [Route("Trails/Edit")]
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

        [HttpPost]
        [Route("Trails/Edit")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([FromForm] Trail trail)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(apiUrl);
                List<Task> tasks = new List<Task>();
                if (trail.GPXFile != null)
                {
                    Task gpxTask = AzureStorageHelper.UploadFileToStorage(trail.GPXFile, trail, blobConnectionString, "trails");
                    tasks.Add(gpxTask);
                }

                if (trail.ImageFile != null)
                {
                    Task imageTask = AzureStorageHelper.UploadFileToStorage(trail.ImageFile, trail, blobConnectionString, "images");
                    tasks.Add(imageTask);
                }

                Task.WaitAll(tasks.ToArray());

                //HTTP PUT
                var putTask = client.PutAsJsonAsync<Trail>(Trails + "/" + trail.Id, trail);
                putTask.Wait();

                var result = putTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    return View("Details", trail);
                }
                else
                {
                    _logger.LogError(string.Format("Web Api Error : Status Code {0}. Response Content:  {1}", result.StatusCode, result.Content));
                }
            }

            return RedirectToAction(nameof(Index));
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
                var responseTask = client.DeleteAsync(string.Join('/', Trails, trail.Id));
                responseTask.Wait();

                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    if(deleteGpx && gpxUrl != null)
                    {
                        AzureStorageHelper.DeleteFileFromStorage(gpxUrl, blobConnectionString,"trails");
                    }

                    if (deleteImage && imageUrl != null)
                    {
                        AzureStorageHelper.DeleteFileFromStorage(imageUrl, blobConnectionString,"images");
                    }

                    var readTask = result.Content.ReadAsStream();

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
