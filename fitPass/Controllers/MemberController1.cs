using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using fitPass.Models;



public class MemberController : Controller
{
    private readonly GymManagementContext _context;

    public MemberController(GymManagementContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var memberId = HttpContext.Session.GetInt32("MemberId");
        if (memberId == null) return RedirectToAction("Login", "Account");

        var member = _context.Accounts.FirstOrDefault(a => a.MemberId == memberId);
        if (member == null) return NotFound();

        var reservations = _context.Reservations
            .Include(r => r.Course)
            .Where(r => r.MemberId == memberId && r.Status == 1)
            .ToList();

        var news = _context.News
            .Where(n => n.IsVisible == true)
            .OrderByDescending(n => n.PublishTime)
            .Take(3)
            .ToList();
        var isCheckedIn = _context.CheckInRecords
            .Any(r => r.MemberId == memberId && r.CheckInTime.Value.Date == DateTime.Today && r.CheckOutTime == null);


        var viewModel = new MemberDashboardViewModel
        {
            Member = member,
            Reservations = reservations,
            NewsList = news,
            PeopleNow = _context.CheckInRecords
            .Count(r => r.CheckInTime.Value.Date == DateTime.Today && r.CheckOutTime == null),
            IsCheckedIn = isCheckedIn
        };
        // ✅ 傳給 Layout 用的 ViewData
        ViewData["MemberName"] = member.Name;
        ViewData["PeopleNow"] = viewModel.PeopleNow;
        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Edit()
    {
        var memberId = HttpContext.Session.GetInt32("MemberId");
        var member = _context.Accounts.FirstOrDefault(a => a.MemberId == memberId);
        return View(member);
    }

    [HttpPost]
    public IActionResult Edit(Account updated)
    {
        var memberId = HttpContext.Session.GetInt32("MemberId");
        var member = _context.Accounts.FirstOrDefault(a => a.MemberId == memberId);
        if (member == null) return NotFound();

        member.Name = updated.Name;
        member.Phone = updated.Phone;
        member.Email = updated.Email;
        member.Gender = updated.Gender;
        member.Birthday = updated.Birthday;

        _context.SaveChanges();
        TempData["Msg"] = "會員資料已更新";
        return RedirectToAction("Index");
    }
    [HttpPost]
    public IActionResult CheckInOut()
    {
        var memberId = HttpContext.Session.GetInt32("MemberId");
        if (memberId == null) return Unauthorized();

        var today = DateTime.Today;

        var record = _context.CheckInRecords
            .FirstOrDefault(r => r.MemberId == memberId && r.CheckInTime.Value.Date == today && r.CheckOutTime == null);

        string currentStatus;

        if (record == null)
        {
            // 入場
            _context.CheckInRecords.Add(new CheckInRecord
            {
                MemberId = memberId.Value,
                CheckInTime = DateTime.Now,
                Status = "正常入場",
                Device = "WebQRCode",
                CheckType = 1
            });
            currentStatus = "已入場";
        }
        else
        {
            // 出場
            record.CheckOutTime = DateTime.Now;
            record.Status = "正常退場";
            currentStatus = "已退場";
        }

        _context.SaveChanges();

        int peopleNow = _context.CheckInRecords
            .Count(r => r.CheckInTime.Value.Date == today && r.CheckOutTime == null);

        return Json(new { success = true, peopleNow, currentStatus });
    }



}
