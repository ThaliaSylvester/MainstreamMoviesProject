using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Group_6_Final_Project.DAL;
using Group_6_Final_Project.Models;
using Group_6_Final_Project.Utilities;
using System.Security.Claims;

namespace Group_6_Final_Project.Controllers
{
    [Authorize]
    public class TransactionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public TransactionController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Transactions
        public IActionResult Index()
        {
            List<Transaction> Transactions;

            if (User.IsInRole("Admin"))
            {
                Transactions = _context.Transactions
                                .Include(r => r.TransactionDetail)
                                .ToList();
            }
            else
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get the user's ID claim

                Transactions = _context.Transactions
                                .Include(r => r.TransactionDetail)
                                .Where(r => r.AppUserId == userId)
                                .ToList();
            }

            return View(Transactions);
        }

        // GET: Transactions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            string currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (id == null)
            {
                return View("Error", new String[] { "Please specify a transaction to view!" });
            }

            Transaction transaction = await _context.Transactions
                .Include(t => t.TransactionDetail)
                //.Include(td => td.Schedule)
                .FirstOrDefaultAsync(m => m.TransactionID == id);

            if (transaction == null)
            {
                return View("Error", new String[] { "This transaction was not found!" });
            }

            // Check if the user is in the 'Customer' role and the transaction's UserID matches the current user
            if (User.IsInRole("Customer") && transaction.AppUserId != currentUserId)
            {
                return View("Error", new String[] { "This is not your transaction! Don't be such a snoop!" });
            }

            return View(transaction);
        }

        //public IActionResult Details(int id)
        //{
        //    var transaction = _context.Transactions.Find(id);

        //    if (transaction == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(transaction);
        //}

        public async Task<IActionResult> AddToCart(int? ScheduleID)
        {
            if (ScheduleID == null)
            {
                return View("Error", new string[] { "Please specify a Schedule to add to the Transaction" });
            }

            string currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            Schedule dbSchedule = _context.Schedules.Find(ScheduleID);

            if (dbSchedule == null)
            {
                return View("Error", new string[] { "This Schedule was not in the database!" });
            }

            // Assuming UserID is of type AppUser
            AppUser user = await _userManager.FindByNameAsync(currentUserId);

            Transaction tran = _context.Transactions.FirstOrDefault(r => r.AppUserId == currentUserId);

            if (tran == null)
            {
                tran = new Transaction();

                tran.TransactionDate = DateTime.Now;
                tran.TransactionNumber = GenerateNextOrderNumber.GetNextOrderNumber(_context);
                tran.AppUserId = currentUserId;

                _context.Transactions.Add(tran);
                await _context.SaveChangesAsync();
            }

            TransactionDetail od = new TransactionDetail();

            od.Transaction = tran;

            _context.TransactionDetails.Add(od);
            await _context.SaveChangesAsync(); // Use await here to make it asynchronous

            return RedirectToAction("Details", new { id = tran.TransactionID });
        }

        // GET: Transactions/Edit/5
        [Authorize(Roles = "Customer")]
        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                return View("Error", new String[] { "Please specify an Transaction to edit." });
            }

            Transaction Transaction = _context.Transactions
                                       .Include(o => o.TransactionDetail)
                                       //.Include(r => r.Schedule)
                                       .FirstOrDefault(r => r.TransactionID == id);

            if (Transaction == null)
            {
                return View("Error", new String[] { "This Transaction was not found in the database!" });
            }

            string currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (User.IsInRole("Customer") && Transaction.AppUserId != currentUserId)
            {
                return View("Error", new String[] { "You are not authorized to edit this Transaction!" });
            }

            return View(Transaction);
        }

        // POST: Transactions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Edit(int id, Transaction Transaction)
        {
            if (id != Transaction.TransactionID)
            {
                return View("Error", new String[] { "There was a problem editing this Transaction. Try again!" });
            }

            if (ModelState.IsValid == false)
            {
                return View(Transaction);
            }

            try
            {
                Transaction dbTransaction = _context.Transactions.Find(Transaction.TransactionID);

                dbTransaction.TransactionNote = Transaction.TransactionNote;

                _context.Update(dbTransaction);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return View("Error", new String[] { "There was an error updating this Transaction!", ex.Message });
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create(int? scheduleID)
        {
            string currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (User.IsInRole("Customer"))
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    Console.WriteLine("User ID is null or empty.");
                    return View("Error", new string[] { "User ID is null or empty." });
                }

                AppUser user = await _userManager.FindByIdAsync(currentUserId);

                if (user != null)
                {
                    Transaction ord = new Transaction();
                    ord.AppUserId = user.Id; // Assuming 'Id' is the property representing the user's ID
                    return View(ord);
                }
                else
                {
                    // Return an error view or handle the situation accordingly
                    return View("Error", new string[] { "User not found." });
                }
            }
            else
            {
                ViewBag.ScheduleID = scheduleID;
                ViewBag.UserNames = await GetAllCustomerUserNamesSelectList();
                return View("SelectCustomerForTransaction");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([Bind("UserID, TransactionNotes")] Transaction transaction)
        {
            string currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            transaction.TransactionNumber = Utilities.GenerateNextOrderNumber.GetNextOrderNumber(_context);
            transaction.TransactionDate = DateTime.Now;
            transaction.TransactionNote = transaction.TransactionNote;

            if (User.IsInRole("Customer"))
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return View("Error", new string[] { "User ID is null or empty." });
                }

                AppUser user = await _userManager.FindByIdAsync(currentUserId);

                if (user != null)
                {
                    transaction.AppUserId = user.Id; // Assuming 'Id' is the property representing the user's ID
                    string TransactionNote = transaction.TransactionNote;
                }
                else
                {
                    return View("Error", new string[] { "User not found." });
                }
            }
            else
            {
                ViewBag.UserNames = await GetAllCustomerUserNamesSelectList();
                return View("SelectCustomerForTransaction");
            }

            _context.Add(transaction);
            await _context.SaveChangesAsync();

            return RedirectToAction("Create", "TransactionDetails", new { TransactionID = transaction.TransactionID });
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SelectCustomerForTransaction(String SelectedCustomer)
        {
            if (String.IsNullOrEmpty(SelectedCustomer))
            {
                ViewBag.UserNames = await GetAllCustomerUserNamesSelectList();
                return View("SelectCustomerForTransaction");
            }

            string currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            Transaction ord = new Transaction();
            ord.AppUserId = currentUserId;
            return View("Create", ord);
        }


        [Authorize]
        public async Task<IActionResult> CheckoutTransaction(int? id)
        {
            string currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (id == null)
            {
                return View("Error", new String[] { "Please specify an Transaction to view!" });
            }

            Transaction Transaction = await _context.Transactions
                                              .Include(o => o.TransactionDetail)
                                              //.Include(o => o.Schedule)
                                              .FirstOrDefaultAsync(m => m.TransactionID == id);

            if (Transaction == null)
            {
                return View("Error", new String[] { "This Transaction was not found!" });
            }

            if (User.IsInRole("Customer") && Transaction.AppUserId != currentUserId)
            {
                return View("Error", new String[] { "This is not your Transaction!  Don't be such a snoop!" });
            }

            return View("Confirm", Transaction);
        }

        private async Task<IActionResult> Confirm(int? id)
        {
            Transaction dbOrd = await _context.Transactions.FindAsync(id);
            _context.Update(dbOrd);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }


        private async Task<SelectList> GetAllCustomerUserNamesSelectList()
        {
            List<AppUser> allCustomers = new List<AppUser>();

            foreach (AppUser dbUser in _context.Users)
            {
                if (await _userManager.IsInRoleAsync(dbUser, "Customer") == true)//user is a customer
                {
                    allCustomers.Add(dbUser);
                }
            }

            SelectList sl = new SelectList(allCustomers.OrderBy(c => c.UserName), nameof(AppUser.Id), nameof(AppUser.UserName));

            return sl;

        }
    }
}