using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopProject.Models;
using ShopProject.Data;

namespace ShopProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PromoCodesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PromoCodesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /PromoCodes
        public async Task<IActionResult> Index()
        {
            var promoCodes = await _context.PromoCodes.OrderByDescending(p => p.Id).ToListAsync();
            return View(promoCodes);
        }

        // GET: /PromoCodes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /PromoCodes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PromoCode promoCode)
        {
            if (ModelState.IsValid)
            {
                promoCode.Code = promoCode.Code.ToUpperInvariant().Trim();
                promoCode.UsedCount = 0; // Убеждаемся, что счётчик обнулён

                _context.PromoCodes.Add(promoCode);
                await _context.SaveChangesAsync();

                TempData["Success"] = "✅ Промокод успешно создан!";
                return RedirectToAction(nameof(Index));
            }
            return View(promoCode);
        }

        // GET: /PromoCodes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var promoCode = await _context.PromoCodes.FindAsync(id);
            if (promoCode == null)
                return NotFound();

            return View(promoCode);
        }

        // POST: /PromoCodes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PromoCode promoCode)
        {
            if (id != promoCode.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    promoCode.Code = promoCode.Code.ToUpperInvariant().Trim();
                    _context.Update(promoCode);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "✅ Промокод обновлён!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.PromoCodes.Any(p => p.Id == id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(promoCode);
        }

        // POST: /PromoCodes/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var promoCode = await _context.PromoCodes.FindAsync(id);
            if (promoCode != null)
            {
                _context.PromoCodes.Remove(promoCode);
                await _context.SaveChangesAsync();
                TempData["Success"] = "✅ Промокод удалён!";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /PromoCodes/Toggle/5
        [HttpPost]
        public async Task<IActionResult> Toggle(int id)
        {
            var promoCode = await _context.PromoCodes.FindAsync(id);
            if (promoCode != null)
            {
                promoCode.IsActive = !promoCode.IsActive;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"✅ Промокод {(promoCode.IsActive ? "активирован" : "деактивирован")}!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}