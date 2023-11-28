using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Group_6_Final_Project.DAL;
using Group_6_Final_Project.Models;
using Group_6_Final_Project.ViewModels;

namespace Group6FinalProject.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly AppDbContext _context;

        public ScheduleController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Schedule/Index
        public async Task<IActionResult> Index()
        {
            // Get all schedules
            var schedulesQuery = GetFilteredSchedules(null, null, null);

            // Pass through ScheduleViewModel
            var viewModel = new ScheduleViewModel
            {
                Schedules = await schedulesQuery.ToListAsync(),
                TheatreOptions = _context.Schedules.Select(s => s.Theatre.ToString()).Distinct(),
            };

            // Initiate ViewBags
            ViewBag.AllMovieSchedule = viewModel.Schedules.Count();
            ViewBag.FilteredMovieSchedule = viewModel.Schedules.Count();

            return View(viewModel);
        }

        // POST: Schedule/Index
        [HttpPost]
        public IActionResult Index(Theatre? selectedTheatre, DateTime? weekStartDate, string? searchString)
        {
            // Filter the schedules
            var schedulesQuery = GetFilteredSchedules(selectedTheatre, weekStartDate, searchString);

            // Pass through ScheduleViewModel
            var viewModel = new ScheduleViewModel
            {
                Schedules = schedulesQuery.ToList(),
                TheatreOptions = _context.Schedules.Select(s => s.Theatre.ToString()).Distinct(),
                SelectedWeekStartDate = weekStartDate
            };

            // Initiate ViewBags
            ViewBag.AllMovieSchedule = viewModel.Schedules.Count();
            ViewBag.FilteredMovieSchedule = viewModel.Schedules.Count();

            return View(viewModel);
        }

        // GET: Schedule/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Schedules == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedules
                .Include(s => s.Movie)
                .Include(s => s.Price)
                .FirstOrDefaultAsync(m => m.ScheduleID == id);
            if (schedule == null)
            {
                return NotFound();
            }

            return View(schedule);
        }

        // GET: Schedule/Create
        public IActionResult Create()
        {
            ViewData["MovieID"] = new SelectList(_context.Movies, "MovieID", "MovieID");
            ViewData["PriceID"] = new SelectList(_context.Prices, "PriceID", "PriceID");
            return View();
        }

        // POST: Schedule/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ScheduleID,StartTime,EndTime,Theatre,PriceID,MovieID")] Schedule schedule)
        {
            if (ModelState.IsValid)
            {
                _context.Add(schedule);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MovieID"] = new SelectList(_context.Movies, "MovieID", "MovieID", schedule.MovieID);
            ViewData["PriceID"] = new SelectList(_context.Prices, "PriceID", "PriceID", schedule.PriceID);

            return View(schedule);
        }

        // GET: Schedule/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Schedules == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                return NotFound();
            }
            ViewData["MovieID"] = new SelectList(_context.Movies, "MovieID", "MovieID", schedule.MovieID);
            ViewData["PriceID"] = new SelectList(_context.Prices, "PriceID", "PriceID", schedule.PriceID);
            return View(schedule);
        }

        // POST: Schedule/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ScheduleID,StartTime,EndTime,Theatre,PriceID,MovieID")] Schedule schedule)
        {
            if (id != schedule.ScheduleID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(schedule);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ScheduleExists(schedule.ScheduleID))
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
            ViewData["MovieID"] = new SelectList(_context.Movies, "MovieID", "MovieID", schedule.MovieID);
            ViewData["PriceID"] = new SelectList(_context.Prices, "PriceID", "PriceID", schedule.PriceID);
            return View(schedule);
        }

        // GET: Schedule/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Schedules == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedules
                .Include(s => s.Movie)
                .Include(s => s.Price)
                .FirstOrDefaultAsync(m => m.ScheduleID == id);
            if (schedule == null)
            {
                return NotFound();
            }

            return View(schedule);
        }

        // POST: Schedule/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Schedules == null)
            {
                return Problem("Entity set 'AppDbContext.Schedules'  is null.");
            }
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule != null)
            {
                _context.Schedules.Remove(schedule);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Helper function to filter the schedule
        private IQueryable<Schedule> GetFilteredSchedules(Theatre? selectedTheatre, DateTime? weekStartDate, string? searchString)
        {
            var schedulesQuery = _context.Schedules.Include(s => s.Movie).Include(s => s.Price).AsQueryable();

            if (selectedTheatre.HasValue)
            {
                schedulesQuery = schedulesQuery.Where(s => s.Theatre == selectedTheatre.Value);
            }

            if (weekStartDate.HasValue)
            {
                var weekEndDate = weekStartDate.Value.AddDays(6);
                schedulesQuery = schedulesQuery.Where(s => s.StartTime.Date >= weekStartDate.Value.Date && s.StartTime.Date <= weekEndDate.Date);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                // Convert both the search string and movie titles to lowercase for case-insensitive comparison
                var searchLower = searchString.ToLower();
                schedulesQuery = schedulesQuery.Where(m => m.Movie.Title.ToLower().Contains(searchLower) || m.Movie.Description.ToLower().Contains(searchLower));

                // Check if a valid date string is provided
                if (DateTime.TryParse(searchString, out DateTime searchDate))
                {
                    // Convert the DateTime to an int (Unix timestamp, for example)
                    int searchTimestamp = Convert.ToInt32(searchDate.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);

                    // Example: Search for schedules with a specific start time
                    schedulesQuery = schedulesQuery.Where(m => m.StartTime == searchDate);
                }
            }

            return schedulesQuery;
        }

        private bool ScheduleExists(int id)
        {
            return (_context.Schedules?.Any(e => e.ScheduleID == id)).GetValueOrDefault();
        }
    }
}
