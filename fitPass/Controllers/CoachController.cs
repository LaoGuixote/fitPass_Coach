using fitPass.Models;
using fitPass.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitPass.Controllers
{
    public class CoachController : Controller
    {
        private readonly GymManagementContext _context;

        public CoachController(GymManagementContext context)
        {
            _context = context;
        }

        //教練登入後主頁
        public async Task<IActionResult> Index()
        {
            int? memberId = HttpContext.Session.GetInt32("MemberId");

            if (memberId == null)
                return RedirectToAction("Login", "Account");

            var coach = await _context.Coaches
                .Include(c => c.Account)
                .FirstOrDefaultAsync(c => c.AccountId == memberId.Value);

            if (coach == null)
                return RedirectToAction("Login", "Account");

            var today = DateOnly.FromDateTime(DateTime.Today);

            var upcomingClassCount = await _context.CourseSchedules
                .CountAsync(c => c.CoachId == coach.CoachId && c.ClassStartDate >= today);

            var scheduledSlotsCount = await _context.CoachTimes
                .CountAsync(c => c.CoachId == coach.CoachId);

            var vm = new CoachDashboardViewModel
            {
                Name = coach.Account.Name,
                Specialty = coach.Specialty,
                Description = coach.Description,
                CoachPhoto = coach.Photo != null
                    ? $"data:image/jpeg;base64,{Convert.ToBase64String(coach.Photo)}"
                    : null,
                UpcomingClassCount = upcomingClassCount,
                ScheduledSlotsCount = scheduledSlotsCount
            };

            return View(vm);  // 對應 Views/Coach/Index.cshtml
        }

        //教練總覽
        public async Task<IActionResult> CoachOverview()
        {
            var coachData = await _context.Coaches
                .Include(c => c.Account)
                .Select(c => new CoachOverviewViewModel
                {
                    CoachId = c.CoachId,
                    Name = c.Account.Name,
                    Specialty = c.Specialty,
                    CoachPhoto = c.Photo != null ? "data:image/jpeg;base64," + Convert.ToBase64String(c.Photo) : null
                })
                .ToListAsync();
            return View(coachData);
        }

        //教練個人詳細頁面
        public async Task<IActionResult> CoachDetail(int id)
        {
            var coach = await _context.Coaches
                .Include(c => c.Account)
                .FirstOrDefaultAsync(c => c.CoachId == id);

            if (coach == null)
                return NotFound();

            var coachDetail = new CoachDetailViewModel
            {
                CoachId = coach.CoachId,
                Name = coach.Account.Name,
                Specialty = coach.Specialty,
                Description = coach.Description,
                CoachPhoto = coach.Photo != null
                    ? $"data:image/jpeg;base64,{Convert.ToBase64String(coach.Photo)}"
                    : null
            };

            return View(coachDetail);
        }

        //教練班表
        public IActionResult CoachSchedule()
        {
            var coachVerify = HttpContext.Session.GetInt32("Admin");
            var memberId = HttpContext.Session.GetInt32("MemberId");

            if (coachVerify != 2 || coachVerify == null)
            {
                return RedirectToAction("Index", "Member");
            }
            var coach = _context.Coaches.FirstOrDefault(c=> c.AccountId == memberId);
            if (coach == null) return NotFound("教練資料不存在");
            ViewBag.CoachId = coach.CoachId; 
            return View();
        }

        [HttpPost]
        public IActionResult SubmitSchedule(DateOnly date, List<int> timeSlots)
        {
            var coachVerify = HttpContext.Session.GetInt32("Admin");
            var memberId = HttpContext.Session.GetInt32("MemberId");

            if (coachVerify != 2 || coachVerify == null)
            {
                return RedirectToAction("Index", "Member");
            }

            var coach = _context.Coaches.FirstOrDefault(c => c.AccountId == memberId);
            if (coach == null) return NotFound();
            foreach (var slot in timeSlots)
            {
                var exists = _context.CoachTimes.FirstOrDefault(ct => ct.CoachId == coach.CoachId && ct.Date == date && ct.TimeSlot == slot);
                if (exists == null)
                {
                    _context.CoachTimes.Add(new CoachTime
                    {
                        CoachId = coach.CoachId,
                        Date = date,
                        TimeSlot = slot,
                        Status = 0
                    });
                }
            }
            _context.SaveChanges();
            return Content("OK");
        }

        [HttpGet]
        public IActionResult GetScheduleCount()
        {
            var coachVerify = HttpContext.Session.GetInt32("Admin");
            var memberId = HttpContext.Session.GetInt32("MemberId");

            if (coachVerify != 2 || coachVerify == null)
            {
                return RedirectToAction("Index", "Member");
            }

            var coach = _context.Coaches.FirstOrDefault(c => c.AccountId == memberId);
            if (coach == null) return NotFound();

            var result = _context.CoachTimes
                .Where(ct => ct.CoachId == coach.CoachId)
                .GroupBy(ct => ct.Date)
                .Select(g => new {
                    title = $"已排 {g.Count()} 段",
                    start = g.Key.ToString("yyyy-MM-dd"), // FullCalendar 要用這個欄位當日期
                    color = "#0d6efd"
                })
                .ToList();

            return Json(result);
        }

        //查詢被選取時段
        [HttpGet]
        public IActionResult GetTimeSlots(DateOnly date)
        {
            var coachVerify = HttpContext.Session.GetInt32("Admin");
            var memberId = HttpContext.Session.GetInt32("MemberId");

            if (coachVerify != 2 || coachVerify == null)
            {
                return RedirectToAction("Index", "Member");
            }

            var coach = _context.Coaches.FirstOrDefault(c => c.AccountId == memberId);
            if (coach == null) return NotFound();

            var slots = _context.CoachTimes
                .Where(ct => ct.CoachId == coach.CoachId && ct.Date == date)
                .Select(ct => ct.TimeSlot)
                .ToList();

            return Json(slots); 
        }
    }
}
