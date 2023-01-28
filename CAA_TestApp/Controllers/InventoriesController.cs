﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CAA_TestApp.Data;
using CAA_TestApp.Models;
using QRCoder;
using System.Drawing.Imaging;
using System.Drawing;
using CAA_TestApp.Utilities;

namespace CAA_TestApp.Controllers
{
    public class InventoriesController : Controller
    {
        private readonly CaaContext _context;

        public InventoriesController(CaaContext context)
        {
            _context = context;
        }

        // GET: Inventories
        public async Task<IActionResult> Index(string SearchSearchStringName, string SearchStringISBN, int? CategoryID, int? LocationID, int? page, string actionButton, int? pageSizeID)
        {
            PopulateDropDownListsCategories();
            PopulateDropDownListsLocations();

            ViewData["Filtering"] = "btn-outline-secondary";

            var caaContext = _context.Inventories
                .Include(i => i.Location)
                .Include(i => i.Product)
                .ThenInclude(c => c.Category)
                .AsNoTracking();

            if (CategoryID.HasValue)
            {
                caaContext = caaContext.Where(p => p.Product.CategoryID == CategoryID);
                ViewData["Filtering"] = " btn-danger";
            }
            if (LocationID.HasValue)
            {
                caaContext = caaContext.Where(p => p.LocationID == LocationID);
                ViewData["Filtering"] = " btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchSearchStringName))
            {
                caaContext = caaContext.Where(p => p.Product.Name.ToUpper().Contains(SearchSearchStringName.ToUpper()));
                ViewData["Filtering"] = " btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchStringISBN))
            {
                caaContext = caaContext.Where(p => p.ISBN.ToUpper().Contains(SearchStringISBN.ToUpper()));
                ViewData["Filtering"] = " btn-danger";
            }

            if (!String.IsNullOrEmpty(actionButton)) //Form Submitted!
            {
                page = 1;
            }

            //int pageSize = 10;//Change as required
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID);
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<Inventory>.CreateAsync(caaContext.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        // GET: Inventories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Inventories == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories
                .Include(i => i.Location)
                .Include(i => i.Product)
                .Include(i => i.ItemPhoto)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (inventory == null)
            {
                return NotFound();
            }

            return View(inventory);
        }

        // GET: Inventories/Create
        public IActionResult Create()
        {
            ViewData["LocationID"] = new SelectList(_context.Locations, "ID", "ID");
            ViewData["ProductID"] = new SelectList(_context.Products, "ID", "ID");
            return View();
        }

        // POST: Inventories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,ISBN,Quantity,Notes,ShelfOn,Cost,DateReceived," +
            "LocationID,ProductID")] Inventory inventory, IFormFile thePicture)
        {
            if (ModelState.IsValid)
            {
                await AddPicture(inventory, thePicture);
                _context.Add(inventory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["LocationID"] = new SelectList(_context.Locations, "ID", "ID", inventory.LocationID);
            ViewData["ProductID"] = new SelectList(_context.Products, "ID", "ID", inventory.ProductID);
            return View(inventory);
        }

        // GET: Inventories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Inventories == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories
                .Include(i => i.ItemPhoto)
                .FirstOrDefaultAsync(i => i.ID == id);
            if (inventory == null)
            {
                return NotFound();
            }

            

            ViewData["LocationID"] = new SelectList(_context.Locations, "ID", "ID", inventory.LocationID);
            ViewData["ProductID"] = new SelectList(_context.Products, "ID", "ID", inventory.ProductID);
            return View(inventory);
        }

        // POST: Inventories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,ISBN,Quantity,Notes,ShelfOn,Cost,DateReceived," +
            "LocationID,ProductID")] Inventory inventory, string chkRemoveImage, IFormFile thePicture)
        {
            if (id != inventory.ID)
            {
                return NotFound();
            }


            if (ModelState.IsValid)
            {
                try
                {
                    //For the image
                    if (chkRemoveImage != null)
                    {
                        //If we are just deleting the two versions of the photo, we need to make sure the Change Tracker knows
                        //about them both so go get the Thumbnail since we did not include it.
                        inventory.ItemThumbnail = _context.ItemsThumbnails.Where(p => p.invID == inventory.ID).FirstOrDefault();
                        //Then, setting them to null will cause them to be deleted from the database.
                        inventory.ItemPhoto = null;
                        inventory.ItemThumbnail = null;
                    }
                    else
                    {
                        await AddPicture(inventory, thePicture);
                    }

                    _context.Update(inventory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventoryExists(inventory.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["LocationID"] = new SelectList(_context.Locations, "ID", "ID", inventory.LocationID);
            ViewData["ProductID"] = new SelectList(_context.Products, "ID", "ID", inventory.ProductID);
            return View(inventory);
        }

        // GET: Inventories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Inventories == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories
                .Include(i => i.Location)
                .Include(i => i.Product)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (inventory == null)
            {
                return NotFound();
            }

            return View(inventory);
        }

        // POST: Inventories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Inventories == null)
            {
                return Problem("Entity set 'CaaContext.Inventories'  is null.");
            }
            var inventory = await _context.Inventories.FindAsync(id);
            if (inventory != null)
            {
                _context.Inventories.Remove(inventory);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> GenerateQr(int? id)
        {
            //Random r = new Random();
            //int isbn = r.Next(10, 50);

            var inventory = await _context.Inventories
                .Include(i => i.Location)
                .Include(i => i.Product)
                .FirstOrDefaultAsync(m => m.ID == id);
            //string code = ViewData["ISBN"].ToString();

            QRCodeGenerator qrCodeGen = new QRCodeGenerator();
            QRCodeData qrData = qrCodeGen.CreateQrCode(inventory.ISBN, QRCodeGenerator.ECCLevel.Q);
            QRCode qr = new QRCode(qrData);

            using (MemoryStream ms = new MemoryStream())
            {
                using (Bitmap bitmap = qr.GetGraphic(20))
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    ViewBag.QRCodeImage = "data:image/png;base64" + Convert.ToBase64String(ms.ToArray());
                }
            }

            return View(inventory);
        }

        private SelectList LocationSelectList(int? id)
        {
            var dQuery = from d in _context.Locations
                         orderby d.Name
                         select d;
            return new SelectList(dQuery, "ID", "Name", id);
        }

        private SelectList CategorySelectList(int? id)
        {
            var dQuery = from d in _context.Categories
                         orderby d.Name
                         select d;
            return new SelectList(dQuery, "ID", "Name", id);
        }

        private void PopulateDropDownListsLocations(Location location = null)
        {
            ViewData["LocationID"] = LocationSelectList(location?.ID);
        }
        private void PopulateDropDownListsCategories(Category category = null)
        {
            ViewData["CategoryID"] = CategorySelectList(category?.ID);
        }

        private bool InventoryExists(int id)
        {
          return _context.Inventories.Any(e => e.ID == id);
        }



        private async Task AddPicture(Inventory inventory, IFormFile thePicture)
        {
            //Get the picture and save it with the Patient (2 sizes)
            if (thePicture != null)
            {
                string mimeType = thePicture.ContentType;
                long fileLength = thePicture.Length;
                if (!(mimeType == "" || fileLength == 0))//Looks like we have a file!!!
                {
                    if (mimeType.Contains("image"))
                    {
                        using var memoryStream = new MemoryStream();
                        await thePicture.CopyToAsync(memoryStream);
                        var pictureArray = memoryStream.ToArray();//Gives us the Byte[]

                        //Check if we are replacing or creating new
                        if (inventory.ItemPhoto != null)
                        {
                            //We already have pictures so just replace the Byte[]
                            inventory.ItemPhoto.Content = ResizeImage.shrinkImageWebp(pictureArray, 500, 600);

                            //Get the Thumbnail so we can update it.  Remember we didn't include it
                            inventory.ItemThumbnail = _context.ItemsThumbnails.Where(p => p.invID == inventory.ID).FirstOrDefault();
                            inventory.ItemThumbnail.Content = ResizeImage.shrinkImageWebp(pictureArray, 100, 120);
                        }
                        else //No pictures saved so start new
                        {
                            inventory.ItemPhoto = new ItemPhoto
                            {
                                Content = ResizeImage.shrinkImageWebp(pictureArray, 500, 600),
                                MimeType = "image/webp"
                            };
                            inventory.ItemThumbnail = new ItemThumbnail
                            {
                                Content = ResizeImage.shrinkImageWebp(pictureArray, 100, 120),
                                MimeType = "image/webp"
                            };
                        }
                    }
                }
            }
        }
    }
}
