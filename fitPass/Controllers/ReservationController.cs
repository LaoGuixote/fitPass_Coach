using fitPass.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace fitPass.Controllers
{
    public class ReservationController : Controller
    {
        private readonly GymManagementContext _context;
        public ReservationController(GymManagementContext context)
        {
            _context = context;
        }

        // 顯示月曆可預約的日期
        public IActionResult ReserveCalendar(int coachId)
        {
            var coach = _context.Coaches
                .Include(c => c.Account)
                .FirstOrDefault(c => c.CoachId == coachId);
            ViewBag.CoachId = coachId;
            return View();
        }

        //取得教練有哪些日子可預約
        [HttpGet]
        public IActionResult GetAvailableDates(int coachId)
        {
            var days = _context.CoachTimes
                .Where(c => c.CoachId == coachId && c.Status == 0)
                .GroupBy(c => c.Date)
                .Select(g => new {
                    title = "可預約",
                    start = g.Key.ToString("yyyy-MM-dd")
                }).ToList();

            return Json(days);
        }

        //點日期顯示當日可預約時段
        public IActionResult SelectTime(int coachId, DateOnly date)
        {
            var timeSlots = _context.CoachTimes
                .Where(c => c.CoachId == coachId && c.Date == date && c.Status == 0)
                .OrderBy(c => c.TimeSlot)
                .ToList();

            ViewBag.CoachId = coachId;
            ViewBag.Date = date;
            return View(timeSlots);
        }

        //送出預約
        [HttpPost]
        public IActionResult SubmitReservation(int timeId)
        {
            int? memberId = HttpContext.Session.GetInt32("MemberId");
            if (memberId == null)
                return RedirectToAction("Login", "Account");

            var time = _context.CoachTimes.FirstOrDefault(c => c.TimeId == timeId && c.Status == 0);
            if (time == null)
                return BadRequest("此時段已無法預約");

            time.Status = 1;

            var session = new PrivateSession
            {
                TimeId = time.TimeId,
                MemberId = memberId.Value,
                CreateTime = DateTime.Now,
                //Status = 0
            };

            _context.PrivateSessions.Add(session);
            _context.SaveChanges();

            return Redirect("/CourseSchedule");
        }
    }
}
