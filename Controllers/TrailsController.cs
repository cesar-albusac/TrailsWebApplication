using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Trails.Models;

namespace TrailsWebApplication.Controllers
{
    public class TrailsController : Controller
    {
        public TrailsController()
        {
        }

        // GET: Student
        public ActionResult Index()
        {
            IEnumerable<Trail> routes = null;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://localhost:7224/api/Routes");
                //HTTP GET
                var responseTask = client.GetAsync("Routes");
                responseTask.Wait();

                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    var readTask = result.Content.ReadFromJsonAsync<IList<Trail>>();
                    readTask.Wait();

                    routes = readTask.Result;
                }
                else //web api sent error response 
                {
                    //log response status here..

                    routes = Enumerable.Empty<Trail>();

                    ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");
                }
            }
            return View(routes);
        }

        //GET: Trails/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Trail Trail = null;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://localhost:7224/api/Routes");
                //HTTP GET
                var responseTask = client.GetAsync("Routes?id=" + id);
                responseTask.Wait();

                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    var readTask = result.Content.ReadFromJsonAsync<Trail>();
                    readTask.Wait();

                    Trail = readTask.Result;
                    if (Trail == null)
                    {
                        return NotFound();
                    }
                }
                else //web api sent error response 
                {
                    //log response status here..

                    Trail = new Trail();

                    ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");
                }
            }


            return View(Trail);
        }

        // GET: Trails/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Trails/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection/*[Bind("Id")] Trail Trail*/)
        {
            if (ModelState.IsValid)
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://localhost:7224/api/Routes");
                    string name = collection["Name"];
                    string description = collection["Description"];
                    string difficulty = collection["Difficulty"];
                    string length = collection["Length"];

                    Trail Trail = new Trail()
                    {
                        Name = name,
                        Description = description,
                        Difficulty = Trail.DifficultyLevel.Easy,
                    };
                    //HTTP POST
                    var postTask = client.PostAsJsonAsync<Trail>("Routes", Trail);
                    postTask.Wait();

                    var result = postTask.Result;
                    if (result.IsSuccessStatusCode)
                    {
                        return RedirectToAction("Index");
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            return View(null);
        }

        // GET: Trails/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://localhost:7224/api/Routes");
                //HTTP GET
                var responseTask = client.GetAsync("Routes?id=" + id);
                responseTask.Wait();

                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    var readTask = result.Content.ReadFromJsonAsync<Trail>();
                    readTask.Wait();

                    var Trail = readTask.Result;
                    if (Trail == null)
                    {
                        return NotFound();
                    }

                    return View(Trail);
                }
            }
            return NotFound();
        }

        // POST: Trails/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, [Bind("Id,Title,ReleaseDate,Genre,Price")] Trail Trail)
        //{
        //    if (id.ToString() != Trail.Id)
        //    {
        //        return NotFound();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            _context.Update(Trail);
        //            await _context.SaveChangesAsync();
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            if (!TrailExists(int.Parse(Trail.Id)))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(Trail);
        //}

        // GET: Trails/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Trail Trail = null;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://localhost:7224/api/Routes");
                //HTTP GET
                var responseTask = client.DeleteAsync("Routes?id=" + id);
                responseTask.Wait();

                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    var readTask = result.Content.ReadAsStream();
                    //readTask.Wait();

                    //Trail = readTask.Result;
                    if (Trail == null)
                    {
                        return NotFound();
                    }
                }
            }   
            return View(Trail);
        }

        // POST: Trails/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    if (_context.Trail == null)
        //    {
        //        return Problem("Entity set 'TrailsWebApplicationContext.Trail'  is null.");
        //    }
        //    var Trail = await _context.Trail.FindAsync(id);
        //    if (Trail != null)
        //    {
        //        _context.Trail.Remove(Trail);
        //    }

        //    await _context.SaveChangesAsync();
        //    return RedirectToAction(nameof(Index));
        //}

        //private bool TrailExists(int id)
        //{
        //  return (_context.Trail?.Any(e => e.Id == id.ToString())).GetValueOrDefault();
        //}
    }
}
