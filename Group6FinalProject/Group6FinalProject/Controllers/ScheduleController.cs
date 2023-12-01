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
using Microsoft.IdentityModel.Tokens;

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
        public async Task<IActionResult> Index(string movieId)
        {
            IQueryable<Schedule> schedulesQuery;

            if (!string.IsNullOrEmpty(movieId))
            {
                // Filter schedules based on the provided movieId
                schedulesQuery = _context.Schedules
                                         .Include(s => s.Movie)
                                         .Include(s => s.Price)
                                         .Where(s => s.Movie.MovieID == movieId);
            }
            else
            {
                // Get all schedules if no specific movieId is provided
                schedulesQuery = GetFilteredSchedules(null, null, null, null);
            }

            var viewModel = new ScheduleViewModel
            {
                Schedules = await schedulesQuery.ToListAsync(),
                TheatreOptions = _context.Schedules.Select(s => s.Theatre.ToString()).Distinct(),
            };

            // Update ViewBag to reflect filtered count if movieId is provided
            ViewBag.AllMovieSchedule = _context.Schedules.Count();
            ViewBag.FilteredMovieSchedule = viewModel.Schedules.Count();

            return View(viewModel);
        }


        // POST: Schedule/Index
        // POST: Schedule/Index
        [HttpPost]
        public IActionResult Index(Theatre? selectedTheatre, DateTime? startDate, DateTime? endDate, string? searchString, MPAARating? selectedMPAARating)
        {
            // Filter the schedules
            var schedulesQuery = GetFilteredSchedules(selectedTheatre, startDate, endDate, searchString, selectedMPAARating);

            // Pass through ScheduleViewModel
            var viewModel = new ScheduleViewModel
            {
                Schedules = schedulesQuery.ToList(),
                TheatreOptions = _context.Schedules.Select(s => s.Theatre.ToString()).Distinct(),
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
            var prices = _context.Prices.ToList();
            var movies = _context.Movies.ToList();

            // Populate ViewBag.PriceID with the list of Price entities
            ViewBag.PriceID = new SelectList(prices, "PriceID", "PriceID");

            // Populate ViewBag.MovieID with the list of Movie entities
            ViewBag.MovieID = new SelectList(movies, "MovieID", "Title");

            ViewBag.TicketTypes = new SelectList(Enum.GetValues(typeof(TicketType)));
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ScheduleID,StartTime,Theatre,TicketType,MovieID")] Schedule schedule)
        {
            if (ModelState.IsValid)
            {
                // Set TicketType based on the selected value in the form
                Enum.TryParse(Request.Form["TicketType"], out TicketType selectedTicketType);
                schedule.TicketType = selectedTicketType;

                // Set PriceID based on TicketType
                SetPriceBasedOnTicketType(schedule);

                // Check for time gaps between movies
                var previousMovieEndTime = _context.Schedules
                    .Where(s => s.Theatre == schedule.Theatre && s.StartTime < schedule.StartTime)
                    .OrderByDescending(s => s.StartTime)
                    .Select(s => s.StartTime.AddMinutes(s.Movie.Runtime.TotalMinutes + 25)) // Use Runtime property
                    .FirstOrDefault();

                if (previousMovieEndTime != default && (schedule.StartTime - previousMovieEndTime).TotalMinutes < 25)
                {
                    // Less than 25 minutes between movies, display an error
                    ModelState.AddModelError("StartTime", "There must be at least 25 minutes between movies.");
                }

                // Check for more than 45 minutes gap between movies
                var nextMovieStartTime = _context.Schedules
                    .Where(s => s.Theatre == schedule.Theatre && s.StartTime > schedule.StartTime)
                    .OrderBy(s => s.StartTime)
                    .Select(s => s.StartTime)
                    .FirstOrDefault();

                if (nextMovieStartTime != default && (nextMovieStartTime - schedule.StartTime).TotalMinutes > 45)
                {
                    // More than 45 minutes between movies, display an error
                    ModelState.AddModelError("StartTime", "There should not be more than 45 minutes between movies.");
                }

                if (ModelState.IsValid)
                {
                    // If all checks passed, save the schedule
                    _context.Add(schedule);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            // Repopulate dropdowns in case of validation errors
            ViewData["MovieID"] = new SelectList(_context.Movies, "MovieID", "Title", schedule.MovieID);
            ViewBag.TicketTypes = new SelectList(Enum.GetValues(typeof(TicketType)), schedule.TicketType);

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

            var prices = _context.Prices.ToList();

            // Populate ViewBag.PriceID with the list of Price entities
            ViewBag.PriceID = new SelectList(prices, "PriceID", "PriceID", schedule.PriceID);

            ViewData["MovieID"] = new SelectList(_context.Movies, "MovieID", "Title", schedule.MovieID);
            ViewBag.TicketTypes = new SelectList(Enum.GetValues(typeof(TicketType)), schedule.TicketType);
            return View(schedule);
        }

        // POST: Schedule/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ScheduleID,StartTime,Theatre,TicketType,MovieID")] Schedule schedule)
        {
            if (id != schedule.ScheduleID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    SetPriceBasedOnTicketType(schedule);
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

            ViewData["MovieID"] = new SelectList(_context.Movies, "MovieID", "Title", schedule.MovieID);
            ViewBag.TicketTypes = new SelectList(Enum.GetValues(typeof(TicketType)), schedule.TicketType);
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
        private IQueryable<Schedule> GetFilteredSchedules(Theatre? selectedTheatre, DateTime? startDate, DateTime? endDate, string? searchString, MPAARating? selectedMPAARating)
        {
            var schedulesQuery = _context.Schedules.Include(s => s.Movie).Include(s => s.Price).AsQueryable();

            if (selectedTheatre.HasValue)
            {
                schedulesQuery = schedulesQuery.Where(s => s.Theatre == selectedTheatre.Value);
            }

            if (startDate.HasValue)
            {
                // Use greater than or equal to for the start date
                schedulesQuery = schedulesQuery.Where(s => s.StartTime.Date >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                // Use less than or equal to for the end date
                schedulesQuery = schedulesQuery.Where(s => s.StartTime.Date <= endDate.Value.Date);
            }

            if (selectedMPAARating.HasValue)
            {
                schedulesQuery = schedulesQuery.Where(s => s.Movie.MPAARating == selectedMPAARating.Value);
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

        private void SetPriceBasedOnTicketType(Schedule schedule)
        {
            switch (schedule.TicketType)
            {
                case TicketType.WeekdayBase:
                    schedule.PriceID = 1;
                    break;
                case TicketType.Matinee:
                    schedule.PriceID = 2;
                    break;
                case TicketType.DiscountTuesday:
                    schedule.PriceID = 3;
                    break;
                case TicketType.Weekends:
                    schedule.PriceID = 4;
                    break;
                case TicketType.SpecialEvent:
                    schedule.PriceID = 5;
                    break;
                default:
                    // Handle any other cases or provide a default price
                    break;
            }
        }
        private bool ScheduleExists(int id)
        {
            return _context.Schedules.Any(e => e.ScheduleID == id);
        }
    }
}
